[CmdletBinding()]
param(
    [ValidateSet('Play','Test')][string]$Mode='Play',
    [ValidateRange(2,4)][int]$Players=2,
    [ValidateRange(1024,65535)][int]$Port=7797,
    [ValidateSet('Room','Guard','World','LifecycleFixture','WipeFixture')][string]$Scenario='Room',
    [switch]$Capture,
    [switch]$CrashOwner
)

# Same-PC UDP only. No cloud accounts, firewall rules, downloads, publishing, or existing-process shutdown.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$networkRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$networkExe=Join-Path $networkRoot 'Build/FullPrototype/SlimeCoopPrototype.exe'
if(-not(Test-Path -LiteralPath $networkExe)){throw '먼저 PrototypePipeline.ps1 -Mode Build로 실행 파일을 만드세요.'}
$networkRunTag=[Guid]::NewGuid().ToString('N')
$networkReportRoot=Join-Path $networkRoot "Logs/Network/$Players-player"
[IO.Directory]::CreateDirectory($networkReportRoot)|Out-Null
$networkProcesses=[Collections.Generic.List[object]]::new()
$networkStarted=[DateTime]::UtcNow
$networkStatus='FAILED'
if($CrashOwner){
    if($Mode -ne 'Test' -or $Scenario -ne 'Room'){throw 'CrashOwner는 Room의 Test 모드에서만 사용하세요.'}
    $networkReportRoot=Join-Path $networkRoot "Logs/Network/$Players-player-crash"
    [IO.Directory]::CreateDirectory($networkReportRoot)|Out-Null
}
if($Scenario -eq 'Guard'){
    if($Mode -ne 'Test' -or $Players -ne 2 -or $Capture){throw 'Guard 검사는 Test 모드, 2인, Capture 없이 실행하세요.'}
    $networkReportRoot=Join-Path $networkRoot 'Logs/Network/guards'
    [IO.Directory]::CreateDirectory($networkReportRoot)|Out-Null
}
if($Scenario -eq 'World'){
    if($Mode -ne 'Test'){throw 'World 검사는 Test 모드에서 사용하세요. 일반 플레이에는 Mode Play를 사용합니다.'}
    $networkReportRoot=Join-Path $networkRoot "Logs/Network/$Players-player-world"
    [IO.Directory]::CreateDirectory($networkReportRoot)|Out-Null
}
if($Scenario -eq 'LifecycleFixture'){
    if($Mode -ne 'Test'){throw 'LifecycleFixture는 시험 배치로 정산·저장·복귀만 검사하는 Test 모드입니다.'}
    $networkReportRoot=Join-Path $networkRoot "Logs/Network/$Players-player-lifecycle"
    [IO.Directory]::CreateDirectory($networkReportRoot)|Out-Null
}
if($Scenario -eq 'WipeFixture'){
    if($Mode -ne 'Test'){throw 'WipeFixture는 격리된 저장과 서버 시험 배치로 전멸·재시작을 검사하는 Test 모드입니다.'}
    $networkReportRoot=Join-Path $networkRoot "Logs/Network/$Players-player-wipe"
    [IO.Directory]::CreateDirectory($networkReportRoot)|Out-Null
}

function Start-LocalPeer {
    param([string]$Name,[string]$Role,[string]$Qa='', [string]$Protocol='slime-local-room-v4')
    $report=Join-Path $networkReportRoot ($Name+'.json')
    $log=Join-Path $networkReportRoot ($Name+'.log')
    $save=Join-Path $networkReportRoot ('Save-'+$Name)
    if($Scenario -in @('LifecycleFixture','WipeFixture')){$save=Join-Path $save $networkRunTag}
    $arguments='--slime-network {0} --network-port {1} --network-capacity {2} --network-name {3} --network-protocol {4} --network-save-root "{5}" -logFile "{6}"' -f $Role,$Port,$Players,$Name,$Protocol,$save,$log
    if($Mode -eq 'Test'){
        $arguments+=' -batchmode --network-qa {0} --network-output "{1}" --network-run-tag {2}' -f $Qa,$report,$networkRunTag
        if($Capture -and $Name -eq 'client-0'){
            $captureName=if($Scenario -eq 'World'){'network-chapter.png'}elseif($Scenario -eq 'LifecycleFixture'){'network-story.png'}elseif($Scenario -eq 'WipeFixture'){'wipe-lobby.png'}else{'waiting-room.png'}
            $arguments+=' -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --network-capture "{0}"' -f (Join-Path $networkReportRoot $captureName)
        }else{$arguments+=' -nographics'}
    }elseif($Role -eq 'server'){$arguments+=' -batchmode -nographics'}
    else{$arguments+=' -screen-fullscreen 0 -screen-width 960 -screen-height 540'}
    $window=if($Mode -eq 'Play' -and $Role -eq 'client'){'Normal'}else{'Hidden'}
    $process=Start-Process -FilePath $networkExe -ArgumentList $arguments -WindowStyle $window -PassThru
    $peer=[pscustomobject]@{Name=$Name;Process=$process;Report=$report;Role=$Role;Qa=$Qa}
    $networkProcesses.Add($peer)
    return $peer
}
function Read-LocalReport {
    param($Peer)
    if(-not(Test-Path -LiteralPath $Peer.Report)){return $null}
    # The writer atomically replaces this file. Allow replacement while this reader holds a handle.
    $reader=$null
    try{
        $stream=[IO.File]::Open($Peer.Report,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $reader=[IO.StreamReader]::new($stream)
        $report=$reader.ReadToEnd()|ConvertFrom-Json
    }catch{return $null}finally{if($null -ne $reader){$reader.Dispose()}}
    if($report.runTag -ne $networkRunTag){return $null}
    return $report
}
function Wait-LocalReport {
    param($Peer,[scriptblock]$Condition,[int]$Timeout=30)
    $deadline=[DateTime]::UtcNow.AddSeconds($Timeout)
    while([DateTime]::UtcNow -lt $deadline){
        $report=Read-LocalReport $Peer
        if($null -ne $report){
            if($report.status -eq 'FAILED'){throw "$($Peer.Name): $($report.errors -join '; ')"}
            if(& $Condition $report){return $report}
        }
        if($Peer.Process.HasExited){throw "$($Peer.Name) exited before expected report; inspect $($Peer.Report) and log."}
        Start-Sleep -Milliseconds 150
    }
    throw "$($Peer.Name) report timeout"
}

try{
    $serverQa=if($Scenario -eq 'Guard'){'guard-server'}elseif($Scenario -eq 'World'){'world-server'}elseif($Scenario -eq 'LifecycleFixture'){'lifecycle-server'}elseif($Scenario -eq 'WipeFixture'){'wipe-server'}else{'server'}
    $server=Start-LocalPeer -Name 'server' -Role 'server' -Qa $serverQa
    if($Mode -eq 'Play'){
        Start-Sleep -Milliseconds 1200
        if($server.Process.HasExited){throw '로컬 서버가 종료되었습니다. server.log와 포트를 확인하세요.'}
        for($i=1;$i -le $Players;$i++){Start-LocalPeer -Name "Slime$i" -Role 'client'|Out-Null}
        Write-Output "클라이언트 $Players개와 별도 서버를 실행했습니다. 모두 종료하면 서버도 종료됩니다. 127.0.0.1:$Port"
        Write-Output '전원 준비 후 방장이 선택한 챕터로 출발합니다. 입력·월드 동기화 개발 시험이며 Steam/PlayFab은 연결하지 않습니다.'
        $networkStatus='STARTED'
        return
    }
    Wait-LocalReport $server {param($r)-not [string]::IsNullOrWhiteSpace($r.snapshot.serverId)}|Out-Null
    if($Scenario -eq 'WipeFixture'){
        for($i=0;$i -lt $Players;$i++){
            $client=Start-LocalPeer -Name "client-$i" -Role 'client' -Qa 'wipe-party'
            Wait-LocalReport $client {param($r)$null -ne $r.snapshot -and $r.snapshot.yourSlot -eq $i}|Out-Null
        }
        $serverResult=Wait-LocalReport $server {param($r)$r.status -eq 'PASSED'} -Timeout 180
        $wipeReports=@($networkProcesses|ForEach-Object{Read-LocalReport $_})
        foreach($field in @('wipedRunId','retryRunId')){
            $ids=@($wipeReports|ForEach-Object{$_.$field}|Select-Object -Unique)
            if($ids.Count -ne 1 -or [string]::IsNullOrWhiteSpace($ids[0])){throw "Wipe peer identity mismatch: $field"}
        }
        if($serverResult.wipedRunId -eq $serverResult.retryRunId -or
            @($wipeReports|ForEach-Object{$_.snapshot.serverId}|Select-Object -Unique).Count -ne 1){throw 'Wipe did not retain the server and create a fresh run.'}
        foreach($peer in $networkProcesses){
            $wipe=Read-LocalReport $peer
            if($wipe.status -ne 'PASSED' -or $wipe.errors.Count -ne 0 -or -not $wipe.serverPlacementFixture -or $wipe.scoreBeforeWipe -le 0){throw "Wipe peer failed: $($peer.Name)"}
            if(-not $peer.Process.WaitForExit(5000) -or $peer.Process.ExitCode -ne 0){throw "Wipe peer exit failure: $($peer.Name)"}
            foreach($check in @('actual_hazard_wipe_reset_server_and_client','new_run_same_chapter_from_stage_one')){
                if($wipe.checks -notcontains $check){throw "Missing $check on $($peer.Name)"}
            }
            if($peer.Role -eq 'server'){
                foreach($check in @('old_run_and_waiting_epoch_inputs_rejected','dedicated_server_has_no_personal_save')){
                    if($wipe.checks -notcontains $check){throw "Missing server check: $check"}
                }
                continue
            }
            foreach($check in @('role_shortage_did_not_auto_wipe','two_dimensional_wipe_lobby','three_dimensional_waiting_room_returned',
                'retry_accepts_fresh_input_without_stale_sideways_move','permanent_progress_records_memories_settings_preserved')){
                if($wipe.checks -notcontains $check){throw "Missing $check on $($peer.Name)"}
            }
            $disk=Get-Content -Raw -LiteralPath (Join-Path $wipe.saveRoot 'progress.json')|ConvertFrom-Json
            $settings=Get-Content -Raw -LiteralPath (Join-Path $wipe.saveRoot 'settings.json')|ConvertFrom-Json
            # Unity persists a System.Single; compare its numeric value, not an exact double literal.
            $savedSensitivity=[double]$settings.sensitivity
            if($disk.unlockedChapter -ne 4 -or $disk.completedMask -ne 4 -or $disk.memories.Count -ne 1 -or
                $disk.records.Count -ne 1 -or $disk.records[0].score -ne 321 -or
                -not [double]::IsFinite($savedSensitivity) -or [math]::Abs($savedSensitivity - 0.13) -gt 0.000001){throw 'Wipe disk readback differs from the isolated permanent fixture.'}
        }
        if($Capture){
            $captureReport=Read-LocalReport ($networkProcesses|Where-Object{$_.Name -eq 'client-0'})
            foreach($name in @('wipe-lobby.png','wipe-returned-waiting.png','wipe-retry-chapter.png')){
                $file=Join-Path $networkReportRoot $name
                if(-not(Test-Path -LiteralPath $file) -or (Get-Item -LiteralPath $file).LastWriteTimeUtc -lt $networkStarted){throw "Missing fresh wipe capture: $name"}
            }
            foreach($check in @('rendered_wipe_lobby','rendered_wipe_returned_waiting','rendered_wipe_retry_chapter')){
                if($captureReport.checks -notcontains $check){throw "Missing wipe render callback: $check"}
            }
        }
        $networkStatus='PASSED'
        return
    }
    if($Scenario -eq 'LifecycleFixture'){
        for($i=0;$i -lt $Players;$i++){
            $client=Start-LocalPeer -Name "client-$i" -Role 'client' -Qa 'lifecycle-party'
            Wait-LocalReport $client {param($r)$null -ne $r.snapshot -and $r.snapshot.yourSlot -eq $i}|Out-Null
        }
        $serverResult=Wait-LocalReport $server {param($r)$r.status -eq 'PASSED'} -Timeout 180
        foreach($stage in 1..6){if($serverResult.checks -notcontains "fixture_stage_${stage}_settled"){throw "Missing fixture stage settlement: $stage"}}
        if($serverResult.checks -notcontains 'dedicated_server_has_no_personal_save' -or $serverResult.rejectedCommands -ne 0){throw 'Server lifecycle result failed.'}
        $lifeReports=@($networkProcesses|ForEach-Object{Read-LocalReport $_})
        if(@($lifeReports|ForEach-Object{$_.snapshot.serverId}|Select-Object -Unique).Count -ne 1 -or
            @($lifeReports|ForEach-Object{$_.world.runId}|Select-Object -Unique).Count -ne 1){throw 'Lifecycle server or run identity mismatch.'}
        foreach($peer in $networkProcesses){
            $life=Read-LocalReport $peer
            if($life.status -ne 'PASSED' -or $life.errors.Count -ne 0 -or -not $life.serverPlacementFixture){throw "Lifecycle peer failed: $($peer.Name)"}
            if(-not $peer.Process.WaitForExit(5000) -or $peer.Process.ExitCode -ne 0){throw "Lifecycle peer exit failure: $($peer.Name)"}
            if($peer.Role -eq 'server'){continue}
            foreach($check in @('walked_to_ready_station','all_six_fixture_stages_received','one_arrival_waited_for_five_second_window','server_result_saved_once')){
                if($life.checks -notcontains $check){throw "Missing $check on $($peer.Name)"}
            }
            $departing=$Players -eq 4 -and $peer.Name -eq 'client-3'
            if($departing){if($life.checks -notcontains 'voter_left_after_persisted_result'){throw 'Missing saved disconnect result.'}}
            elseif($life.checks -notcontains 'local_progress_reloaded_after_return' -or $life.checks -notcontains 'withdrawal_delayed_unanimous_return'){throw 'Missing vote / return / durable progress evidence.'}
            $disk=Get-Content -Raw -LiteralPath (Join-Path $life.saveRoot 'progress.json')|ConvertFrom-Json
            if($disk.completedMask -ne 1 -or $disk.unlockedChapter -ne 2 -or $disk.memories.Count -ne 1){throw 'Disk progress disagrees with lifecycle result.'}
            if($Players -eq 4){
                if($disk.records.Count -ne 1 -or $disk.records[0].runId -ne $serverResult.world.completion.runId -or
                    $disk.records[0].score -ne $serverResult.world.completion.score -or $disk.records[0].seconds -ne $serverResult.world.completion.seconds -or
                    $disk.records[0].participants -ne 4){throw 'Disk team result differs from authoritative completion.'}
            }elseif($disk.records.Count -ne 0){throw 'Non-four-player run entered four-player records.'}
        }
        if($Capture){
            foreach($captureName in @('network-story.png','returned-waiting-room.png')){
                $captureFile=Join-Path $networkReportRoot $captureName
                if(-not(Test-Path -LiteralPath $captureFile) -or (Get-Item -LiteralPath $captureFile).LastWriteTimeUtc -lt $networkStarted){throw "Missing fresh lifecycle capture: $captureName"}
            }
            $captureReport=Read-LocalReport ($networkProcesses|Where-Object{$_.Name -eq 'client-0'})
            if($captureReport.checks -notcontains 'rendered_network_story' -or $captureReport.checks -notcontains 'rendered_returned_waiting_room'){throw 'Lifecycle capture callback missing.'}
        }
        $networkStatus='PASSED'
        return
    }
    if($Scenario -eq 'World'){
        for($i=0;$i -lt $Players;$i++){
            $client=Start-LocalPeer -Name "client-$i" -Role 'client' -Qa 'world-party'
            Wait-LocalReport $client {param($r)$null -ne $r.snapshot -and $r.snapshot.yourSlot -ge 0}|Out-Null
        }
        foreach($peer in $networkProcesses){
            $worldReport=Wait-LocalReport $peer {param($r)$r.status -eq 'PASSED'}
            if($worldReport.checks -notcontains 'only_bound_actor_moved'){throw "Missing actor isolation evidence: $($peer.Name)"}
            if($peer.Role -eq 'client' -and $worldReport.checks -notcontains 'walked_to_ready_station'){throw "Missing real waiting-room walk: $($peer.Name)"}
            if(-not $peer.Process.WaitForExit(5000) -or $peer.Process.ExitCode -ne 0){throw "World peer exit failure: $($peer.Name)"}
        }
        $worldReports=@($networkProcesses|ForEach-Object{Read-LocalReport $_})
        $serverIds=@($worldReports|ForEach-Object{$_.snapshot.serverId}|Select-Object -Unique)
        $runIds=@($worldReports|ForEach-Object{$_.world.runId}|Select-Object -Unique)
        if($serverIds.Count -ne 1 -or [string]::IsNullOrWhiteSpace($serverIds[0]) -or
            $runIds.Count -ne 1 -or [string]::IsNullOrWhiteSpace($runIds[0])){throw 'World peers did not share the same server and run identity.'}
        if($Capture){
            $captureReport=$worldReports|Where-Object{$_.capture -ne ''}
            $captureFile=Join-Path $networkReportRoot 'network-chapter.png'
            if('rendered_network_chapter' -notin $captureReport.checks -or -not(Test-Path -LiteralPath $captureFile) -or
                (Get-Item -LiteralPath $captureFile).LastWriteTimeUtc -lt $networkStarted){throw 'Fresh network chapter render missing'}
        }
        $networkStatus='PASSED'
        return
    }
    if($Scenario -eq 'Guard'){
        $survivor=Start-LocalPeer -Name 'normal-client' -Role 'client' -Qa 'survivor'
        Wait-LocalReport $survivor {param($r)$r.snapshot.yourSlot -eq 0}|Out-Null
        $flood=Start-LocalPeer -Name 'burst-client' -Role 'client' -Qa 'flood'
        Wait-LocalReport $flood {param($r)$r.status -eq 'PASSED'}|Out-Null
        Wait-LocalReport $survivor {param($r)$r.status -eq 'PASSED'}|Out-Null
        $guardReport=Wait-LocalReport $server {param($r)$r.status -eq 'PASSED'}
        if($guardReport.rejectedCommands -ne 1){throw 'Burst denial was not recorded exactly once.'}
        foreach($peer in $networkProcesses){if(-not $peer.Process.WaitForExit(5000) -or $peer.Process.ExitCode -ne 0){throw 'Guard peer exit failure'}}
        $networkStatus='PASSED'
        return
    }
    $badVersion=Start-LocalPeer -Name 'bad-version' -Role 'client' -Qa 'version_mismatch' -Protocol 'unsupported-version'
    Wait-LocalReport $badVersion {param($r)$r.status -eq 'PASSED'}|Out-Null
    $clients=[Collections.Generic.List[object]]::new()
    for($i=0;$i -lt $Players;$i++){
        $client=Start-LocalPeer -Name "client-$i" -Role 'client' -Qa 'party'
        $clients.Add($client)
        $joined=Wait-LocalReport $client {param($r)$r.snapshot.yourSlot -ge 0 -and -not [string]::IsNullOrWhiteSpace($r.snapshot.serverId)}
        if($joined.snapshot.yourSlot -ne $i){throw "Unexpected assigned slot for client-$i"}
    }
    Wait-LocalReport $server {param($r)$r.snapshot.peers.Count -eq $Players}|Out-Null
    $overflow=Start-LocalPeer -Name 'over-capacity' -Role 'client' -Qa 'room_full'
    Wait-LocalReport $overflow {param($r)$r.status -eq 'PASSED'}|Out-Null
    if($CrashOwner){
        Wait-LocalReport $server {param($r)$r.snapshot.phase -eq 'Reserved'}|Out-Null
        $ownerBeforeCrash=Read-LocalReport $clients[0]
        if($clients[0].Process.HasExited -or $ownerBeforeCrash.status -ne 'RUNNING'){throw 'Original owner exited before crash injection.'}
        # Only terminate the process created above, with an isolated QA save. No process-name based shutdown.
        Stop-Process -Id $clients[0].Process.Id
        if(-not $clients[0].Process.WaitForExit(5000)){throw 'Original owner process did not terminate.'}
    }
    foreach($client in $clients){
        if($CrashOwner -and $client.Name -eq 'client-0'){continue}
        Wait-LocalReport $client {param($r)$r.status -eq 'PASSED'}|Out-Null
    }
    $serverReport=Wait-LocalReport $server {param($r)$r.status -eq 'PASSED'}
    if($serverReport.rejectedJoins -ne 2 -or $serverReport.rejectedCommands -ne 2*($Players-1)+1){
        throw 'Server did not record the expected capacity/version/authority/replay denials.'
    }
    foreach($client in $clients){
        if($CrashOwner -and $client.Name -eq 'client-0'){continue}
        $walkReport=Read-LocalReport $client
        if($walkReport.checks -notcontains 'walked_to_ready_station' -or $walkReport.checks -notcontains 'waiting_replica_bound_without_client_physics'){throw "Missing 3D waiting-room evidence: $($client.Name)"}
    }
    $reports=@($networkProcesses|ForEach-Object{Read-LocalReport $_})
    $serverIds=@($reports|Where-Object{-not [string]::IsNullOrWhiteSpace($_.snapshot.serverId)}|ForEach-Object{$_.snapshot.serverId}|Select-Object -Unique)
    if($serverIds.Count -ne 1){throw 'Processes did not share a single authoritative server identity.'}
    foreach($peer in $networkProcesses){
        if($CrashOwner -and $peer.Name -eq 'client-0'){continue}
        if(-not $peer.Process.WaitForExit(5000) -or $peer.Process.ExitCode -ne 0){throw "Peer failed to exit cleanly: $($peer.Name)"}
    }
    if($Capture){
        $captureReport=Read-LocalReport $clients[0]
        $captureFile=Join-Path $networkReportRoot 'waiting-room.png'
        if('rendered_waiting_room' -notin $captureReport.checks -or -not(Test-Path -LiteralPath $captureFile) -or (Get-Item -LiteralPath $captureFile).LastWriteTimeUtc -lt $networkStarted){throw 'Fresh network-room render missing'}
        $readyCaptureFile=Join-Path $networkReportRoot 'waiting-ready.png'
        if('rendered_nearby_ready_device' -notin $captureReport.checks -or -not(Test-Path -LiteralPath $readyCaptureFile) -or (Get-Item -LiteralPath $readyCaptureFile).LastWriteTimeUtc -lt $networkStarted){throw 'Fresh near-device ready panel render missing'}
    }
    $networkStatus='PASSED'
}
finally{
    if($Mode -eq 'Test' -or $networkStatus -eq 'FAILED'){
        foreach($peer in $networkProcesses){if(-not $peer.Process.HasExited){Stop-Process -Id $peer.Process.Id -ErrorAction SilentlyContinue}}
    }
    if($Mode -eq 'Test'){
        $report=[ordered]@{status=$networkStatus;runTag=$networkRunTag;players=$Players;port=$Port;scenario=$Scenario;captureRequested=[bool]$Capture;ownerProcessKilled=[bool]$CrashOwner;
            startedUtc=$networkStarted.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');
            processes=@($networkProcesses|ForEach-Object{@{name=$_.Name;pid=$_.Process.Id;report=$_.Report}});
            evidenceBoundary=$(if($Scenario -eq 'WipeFixture'){'Real separate-process loopback UDP; server placement completes Chapter01 stage 1 and places actors beside the stage 2 death route. Client intents enter the real hazard, last survivor waits, wipe resets temporary state, 2D result returns to 3D waiting room, fresh same-chapter stage-1 run preserves isolated permanent data. NOT complete input-driven course traversal, cloud/WAN, performance or final art proof.'}elseif($Scenario -eq 'LifecycleFixture'){'Real separate-process loopback UDP; controlled SERVER actor/prop placement through six Chapter01 stages, real objectives/settlement, partial exit window, result persistence, story votes/withdrawal/disconnect, return. NOT client-input course traversal, all seven chapters online, cloud authentication, WAN, latency or performance proof.'}elseif($Scenario -eq 'World'){'Real separate-process loopback UDP waiting-room walking, station-controlled chapter startup, bound-input movement and client replica. Not full-course multiplayer, cloud authentication, WAN, latency or graphics performance proof.'}else{'Real separate-process loopback UDP room protocol, waiting-room walking and station checks. No cloud authentication, full-course multiplayer, WAN, latency or graphics performance proof.'})}
        $report|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $networkReportRoot 'result.json') -Encoding utf8
        $report|ConvertTo-Json -Depth 6
    }
}
