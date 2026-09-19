[CmdletBinding()]
param(
    [ValidateRange(2,4)][int]$Players=2,
    [ValidateRange(1024,65535)][int]$Port=7807
)

# Ordinary menu integration check. No public network, account changes, or shutdown of existing user processes.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$lobbyRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$lobbyExe=Join-Path $lobbyRoot 'Build/FullPrototype/SlimeCoopPrototype.exe'
if(-not(Test-Path -LiteralPath $lobbyExe)){throw 'Windows 프로토타입을 먼저 빌드하세요.'}
if(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue){throw "시험 포트 $Port 가 사용 중입니다. 다른 Port를 지정하세요."}
$lobbyRun=[Guid]::NewGuid().ToString('N')
$lobbyFolder=Join-Path $lobbyRoot "Logs/Network/$Players-player-lobby"
[IO.Directory]::CreateDirectory($lobbyFolder)|Out-Null
$lobbyProcesses=[Collections.Generic.List[object]]::new()
$lobbyServer=$null
$lobbyStarted=[DateTime]::UtcNow
$lobbyStatus='FAILED'
$lobbyResults=[Collections.Generic.List[object]]::new()

function Start-LobbyClient {
    param([string]$Name,[string]$Role)
    $folder=Join-Path $lobbyFolder $Name
    [IO.Directory]::CreateDirectory($folder)|Out-Null
    $args='-batchmode -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --local-lobby-qa {0} --lobby-qa-folder "{1}" --lobby-qa-run {2} --lobby-qa-port {3} --lobby-qa-capacity {4} -logFile "{5}"' -f $Role,$folder,$lobbyRun,$Port,$Players,(Join-Path $folder 'player.log')
    $process=Start-Process -FilePath $lobbyExe -ArgumentList $args -WindowStyle Hidden -PassThru
    $client=[pscustomobject]@{Name=$Name;Process=$process;Folder=$folder;Report=(Join-Path $folder 'result.json')}
    $lobbyProcesses.Add($client)
    return $client
}
function Read-LobbyReport {
    param($Client)
    if(-not(Test-Path -LiteralPath $Client.Report)){return $null}
    $reader=$null
    try {
        $stream=[IO.File]::Open($Client.Report,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $reader=[IO.StreamReader]::new($stream)
        $report=$reader.ReadToEnd()|ConvertFrom-Json
    }catch{return $null}finally{if($null -ne $reader){$reader.Dispose()}}
    if($report.runTag -ne $lobbyRun){return $null}
    return $report
}
function Wait-LobbyReport {
    param($Client,[scriptblock]$Condition,[int]$Seconds=110)
    $deadline=[DateTime]::UtcNow.AddSeconds($Seconds)
    while([DateTime]::UtcNow -lt $deadline){
        $report=Read-LobbyReport $Client
        if($null -ne $report){
            if($report.status -eq 'FAILED'){throw "$($Client.Name) failed: $($report.errors -join ' | ')"}
            if(& $Condition $report){return $report}
        }
        if($Client.Process.HasExited){throw "$($Client.Name) exited before required report. See $($Client.Folder)"}
        Start-Sleep -Milliseconds 150
    }
    throw "$($Client.Name) timeout"
}
function Assert-LobbyExit {
    param($Client,$Report)
    if(-not $Client.Process.WaitForExit(5000)){throw "$($Client.Name) did not exit"}
    if($Client.Process.ExitCode -ne 0){throw "$($Client.Name) exit code $($Client.Process.ExitCode)"}
    if($Report.errors.Count -ne 0){throw "$($Client.Name) reported errors"}
    $required=@('ordinary_2d_lobby_no_network_shortcut')
    if($Client.Name -eq 'failure'){$required+=@('pending_join_cancelled_to_2d','connection_failure_has_retry_and_cancel','retry_menu_reopened')}
    else {
        $required+=@('real_menu_connection_approved','all_real_clients_visible_in_3d')
        if(-not $Report.saveRoot.StartsWith($Client.Folder,[StringComparison]::OrdinalIgnoreCase)){throw 'Test save escaped its isolated client folder'}
        if($Client.Name -eq 'creator'){$required+=@('separate_dedicated_server_process','creator_returned_to_2d_lobby','rejoined_same_server_without_stealing_owner','server_survived_other_clients_leaving','final_client_left_to_2d_lobby')}
        else{$required+=@('owner_departure_kept_server_and_clock','creator_rejoined_same_room','guest_left_to_2d_lobby')}
    }
    foreach($check in $required){if($check -notin $Report.checks){throw "Missing required check: $check"}}
    foreach($capture in $Report.captures){
        $file=Join-Path $Client.Folder ($capture+'.png')
        if(-not(Test-Path -LiteralPath $file) -or (Get-Item -LiteralPath $file).LastWriteTimeUtc -lt $lobbyStarted){throw "Missing fresh capture $capture"}
    }
    $lobbyResults.Add([pscustomobject]@{name=$Client.Name;processId=$Report.processId;serverId=$Report.serverId;saveRoot=$Report.saveRoot;checks=$Report.checks;captures=$Report.captures;exitCode=$Client.Process.ExitCode})
}

try {
    $failure=Start-LobbyClient 'failure' 'failure'
    $failureReport=Wait-LobbyReport $failure {param($r) $r.status -eq 'PASSED'}
    Assert-LobbyExit $failure $failureReport
    $creator=Start-LobbyClient 'creator' 'creator'
    $created=Wait-LobbyReport $creator {param($r) $r.phase -eq 'WaitingRoom'}
    if($created.serverProcessId -le 0 -or -not $created.serverLog.StartsWith($creator.Folder,[StringComparison]::OrdinalIgnoreCase) -or -not $created.serverLog.Contains($lobbyRun)){
        throw 'Created server ownership evidence is invalid'
    }
    $lobbyServer=Get-Process -Id $created.serverProcessId
    if($lobbyServer.Path -ne $lobbyExe -or $lobbyServer.StartTime.ToUniversalTime() -lt $lobbyStarted){$lobbyServer=$null;throw 'Server process identity differs'}
    # This server was spawned by the game, not this PowerShell process. Pin its OS handle while alive so ExitCode remains readable after exit.
    $lobbyServerHandle=$lobbyServer.Handle
    if($lobbyServerHandle -eq [IntPtr]::Zero){throw 'Cannot retain the owned server process handle'}
    for($index=1;$index -lt $Players;$index++){Start-LobbyClient "guest-$index" 'guest'|Out-Null}
    foreach($client in $lobbyProcesses | Where-Object Name -ne 'failure'){
        $report=Wait-LobbyReport $client {param($r) $r.status -eq 'PASSED'}
        if($report.serverId -ne $created.serverId){throw 'Clients joined different servers'}
        Assert-LobbyExit $client $report
    }
    if(-not $lobbyServer.WaitForExit(15000)){throw 'Empty server did not exit after all clients left'}
    if($lobbyServer.ExitCode -ne 0){throw "Server exit code $($lobbyServer.ExitCode)"}
    $lobbyStatus='PASSED'
}
finally {
    foreach($client in $lobbyProcesses){if(-not $client.Process.HasExited){Stop-Process -InputObject $client.Process}}
    if($null -ne $lobbyServer -and -not $lobbyServer.HasExited){Stop-Process -InputObject $lobbyServer}
    $summary=[ordered]@{
        status=$lobbyStatus;runTag=$lobbyRun;players=$Players;port=$Port;startedUtc=$lobbyStarted.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o')
        runtimeDllSHA256=(Get-FileHash -LiteralPath (Join-Path $lobbyRoot 'Build/FullPrototype/SlimeCoopPrototype_Data/Managed/SlimeCoop.Prototype.Runtime.dll')).Hash
        results=$lobbyResults.ToArray();serverExitedNormally=($lobbyStatus -eq 'PASSED');serverExitCode=$(if($lobbyStatus -eq 'PASSED'){$lobbyServer.ExitCode}else{$null})
        evidenceBoundary='Real ordinary-lobby button handlers, separate loopback server and real client processes, retry/cancel, first-person waiting view, leave/rejoin, owner handoff and empty-server exit. Not native mouse clicking, WAN/Steam/PlayFab, chapter completion, or performance proof.'
    }
    $summary|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $lobbyFolder 'result.json') -Encoding utf8
}
$summary|ConvertTo-Json -Depth 8
