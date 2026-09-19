[CmdletBinding()]
param(
    [ValidateSet('Test', 'Build', 'Capture', 'Network', 'Lifecycle', 'Lobby', 'Wipe', 'Full')]
    [string]$Mode = 'Test'
)

# Local Unity verification only. Never publishes, installs packages, changes credentials, or commits.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$prototypeRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$prototypeEditor = '6000.6.1f1'
$prototypeLogRoot = Join-Path $prototypeRoot 'Logs'
$prototypeBuildRoot = Join-Path $prototypeRoot 'Build/FullPrototype'
$prototypeCaptureRoot = Join-Path $prototypeBuildRoot 'Captures'
$prototypeExe = Join-Path $prototypeBuildRoot 'SlimeCoopPrototype.exe'
$prototypeStages = [Collections.Generic.List[string]]::new()
$prototypeStarted = [DateTime]::UtcNow
$prototypeStatus = 'FAILED'

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) { throw 'Unity CLI가 필요합니다. 설치 상태를 먼저 확인하세요.' }
if ($Mode -notin @('Capture','Network','Lifecycle','Lobby','Wipe') -and (Get-Process -Name Unity -ErrorAction SilentlyContinue)) {
    throw 'Unity 편집기가 열려 있습니다. 작업을 저장하고 닫은 뒤 다시 실행하세요. 파이프라인이 사용자 편집기를 종료하지 않습니다.'
}
[IO.Directory]::CreateDirectory($prototypeLogRoot) | Out-Null

function Invoke-PrototypeUnity {
    param([string[]]$Arguments)
    & unity @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Unity 작업 실패: $($Arguments[0]); exit=$LASTEXITCODE" }
}

Push-Location -LiteralPath $prototypeRoot
try {
    if ($Mode -eq 'Full') {
        & pwsh -NoProfile -File (Join-Path $prototypeRoot 'ProjectPipeline.ps1') -Mode Validate -WriteReport | Out-Null
        if ($LASTEXITCODE -ne 0) { throw '기획·문서 계약 검사 실패' }
        $prototypeStages.Add('DocumentValidate')
    }
    if ($Mode -in @('Build', 'Full')) {
        Invoke-PrototypeUnity -Arguments @('run', '.', '--editor-version', $prototypeEditor, '--format', 'json', '--timeout', '600', '--',
            '-executeMethod', 'SlimeCoop.Prototype.Editor.PrototypeSceneBuilder.BuildScenes', '-logFile', 'Logs/full-prototype-buildscenes.log')
        $prototypeStages.Add('Generate16Scenes')
    }
    if ($Mode -in @('Test', 'Full')) {
        $prototypeTestStart = [DateTime]::UtcNow
        Invoke-PrototypeUnity -Arguments @('test', '.', '--mode', 'PlayMode', '--editor-version', $prototypeEditor, '--format', 'json',
            '--output', 'Logs/full-prototype-playmode.xml', '--timeout', '600', '--', '-logFile', 'Logs/full-prototype-tests.log')
        $prototypeResultPath = Join-Path $prototypeLogRoot 'full-prototype-playmode.xml'
        if ((Get-Item -LiteralPath $prototypeResultPath).LastWriteTimeUtc -lt $prototypeTestStart) { throw '이번 실행의 테스트 보고서가 아닙니다.' }
        [xml]$prototypeTests = Get-Content -Raw -LiteralPath $prototypeResultPath
        if ($prototypeTests.'test-run'.result -ne 'Passed' -or [int]$prototypeTests.'test-run'.total -lt 1 -or [int]$prototypeTests.'test-run'.failed -ne 0) {
            throw 'PlayMode 검사 실패 또는 빈 검사 결과'
        }
        $prototypeStages.Add("PlayMode:$($prototypeTests.'test-run'.passed)/$($prototypeTests.'test-run'.total)")
    }
    if ($Mode -in @('Build', 'Full')) {
        Invoke-PrototypeUnity -Arguments @('build', '.', '--target', 'StandaloneWindows64', '--execute-method', 'SlimeCoop.Prototype.Editor.PrototypeBuild.BuildWindows',
            '--output-path', 'Build/FullPrototype/SlimeCoopPrototype.exe', '--log-file', 'Logs/full-prototype-windows-build.log',
            '--editor-version', $prototypeEditor, '--allow-dirty-build', '--no-tail', '--timeout', '900', '--format', 'json')
        if (-not (Test-Path -LiteralPath $prototypeExe)) { throw '빌드 실행 파일이 없습니다.' }
        $prototypeStages.Add('WindowsBuild')
    }
    if ($Mode -in @('Capture', 'Full')) {
        if (-not (Test-Path -LiteralPath $prototypeExe)) { throw 'Build 모드로 먼저 실행 파일을 만드세요.' }
        $prototypeCaptureStart = [DateTime]::UtcNow
        $prototypePlayerLog = Join-Path $prototypeLogRoot 'full-prototype-player.log'
        $prototypeArguments = '-batchmode -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --prototype-capture "{0}" -logFile "{1}"' -f $prototypeCaptureRoot, $prototypePlayerLog
        $prototypePlayer = Start-Process -FilePath $prototypeExe -ArgumentList $prototypeArguments -WindowStyle Hidden -PassThru
        $prototypePlayer.WaitForExit()
        if ($prototypePlayer.ExitCode -ne 0) { throw "QA 플레이어 실패: exit=$($prototypePlayer.ExitCode)" }
        $prototypeResultPath = Join-Path $prototypeCaptureRoot 'result.json'
        if ((Get-Item -LiteralPath $prototypeResultPath).LastWriteTimeUtc -lt $prototypeCaptureStart) { throw '이번 실행의 캡처 보고서가 아닙니다.' }
        $prototypeCapture = Get-Content -Raw -LiteralPath $prototypeResultPath | ConvertFrom-Json
        $prototypeExpectedCaptures = @('01-Lobby', '02-WaitingRoom', '03-Chapter01', '04-Settings', '05-LAVA', '06-Story',
            '07-BossWarning', '08-CooperativeRaft', '09-MemoryJournal', '10-TeamRecords', '11-SaveFailure')
        if ($prototypeCapture.errors.Count -ne 0) { throw '캡처 오류. 보고서를 확인하세요.' }
        foreach ($prototypeCaptureName in $prototypeExpectedCaptures) {
            $prototypePng = Join-Path $prototypeCaptureRoot ($prototypeCaptureName + '.png')
            if ($prototypeCaptureName -notin $prototypeCapture.captures -or -not (Test-Path -LiteralPath $prototypePng) -or
                (Get-Item -LiteralPath $prototypePng).LastWriteTimeUtc -lt $prototypeCaptureStart) {
                throw "이번 실행의 필수 캡처가 없습니다: $prototypeCaptureName"
            }
        }
        $prototypeStages.Add("RenderedCaptures:$($prototypeCapture.captures.Count); human visual review still required")
    }
    if ($Mode -in @('Network','Full')) {
        foreach ($count in @(2,3,4)) {
            $networkArguments=@('-NoProfile','-File',(Join-Path $prototypeRoot 'LocalNetwork.ps1'),'-Mode','Test','-Players',"$count")
            if ($count -in @(2,4)) { $networkArguments+='-Capture' }
            & pwsh @networkArguments
            if ($LASTEXITCODE -ne 0) { throw "$count 인 로컬 전용 서버 검사 실패" }
            $prototypeStages.Add("SeparateProcesses:Server+$count clients")
        }
        & pwsh -NoProfile -File (Join-Path $prototypeRoot 'LocalNetwork.ps1') -Mode Test -Players 2 -Scenario Guard
        if ($LASTEXITCODE -ne 0) { throw '과도 요청 클라이언트 격리 검사 실패' }
        $prototypeStages.Add('NetworkBurstIsolation')
        foreach ($count in @(2,3,4)) {
            $worldArguments=@('-NoProfile','-File',(Join-Path $prototypeRoot 'LocalNetwork.ps1'),'-Mode','Test','-Players',"$count",'-Scenario','World')
            if ($count -in @(2,4)) { $worldArguments+='-Capture' }
            & pwsh @worldArguments
            if ($LASTEXITCODE -ne 0) { throw "$count 인 서버 챕터 시작·입력 격리·복제 검사 실패" }
            $prototypeStages.Add("ServerWorldInputAndReplica:$count clients")
        }
    }
    if ($Mode -in @('Lifecycle','Full')) {
        foreach ($count in @(2,3,4)) {
            $lifecycleArguments=@('-NoProfile','-File',(Join-Path $prototypeRoot 'LocalNetwork.ps1'),'-Mode','Test','-Players',"$count",'-Scenario','LifecycleFixture')
            if ($count -in @(2,4)) { $lifecycleArguments+='-Capture' }
            & pwsh @lifecycleArguments
            if ($LASTEXITCODE -ne 0) { throw "$count 인 서버 시험 배치·정산·결과 저장·이야기·복귀 검사 실패" }
            $prototypeStages.Add("ServerPlacementLifecycleFixture:$count clients")
        }
    }
    if ($Mode -in @('Lobby','Full')) {
        foreach ($count in @(2,4)) {
            & pwsh -NoProfile -File (Join-Path $prototypeRoot 'TestLocalLobby.ps1') -Players $count
            if ($LASTEXITCODE -ne 0) { throw "$count 인 일반 로비 생성·참가·재접속 검사 실패" }
            $prototypeStages.Add("OrdinaryLobbyCreateJoinLeaveRejoin:$count clients")
        }
    }
    if ($Mode -in @('Wipe','Full')) {
        foreach ($count in @(2,3,4)) {
            $wipeArguments=@('-NoProfile','-File',(Join-Path $prototypeRoot 'LocalNetwork.ps1'),'-Mode','Test','-Players',"$count",'-Scenario','WipeFixture')
            if ($count -in @(2,4)) { $wipeArguments+='-Capture' }
            & pwsh @wipeArguments
            if ($LASTEXITCODE -ne 0) { throw "$count 인 전멸·로비·대기방·새 런 재시작 검사 실패" }
            $prototypeStages.Add("ServerPlacementWipeRetryFixture:$count clients")
        }
    }
    $prototypeStatus = 'PASSED'
}
finally {
    $prototypeReport = [ordered]@{
        mode = $Mode; status = $prototypeStatus; editor = $prototypeEditor
        startedUtc = $prototypeStarted.ToString('o'); finishedUtc = [DateTime]::UtcNow.ToString('o')
        completedStages = $prototypeStages.ToArray()
        evidenceBoundary = 'Evidence is limited to completedStages and their scenario reports. Network covers loopback walking, station authority, bound-input chapter startup/replica. Lifecycle uses server placement through Chapter01. Wipe uses stage-1 placement and starts stage-2 clients beside a hazard, then actual client movement, wipe, 2D/3D return and clean retry. Lobby invokes actual ordinary-menu button handlers, not native mouse events. Not full-course input-driven multiplayer, cloud services, manual full play, minimum-spec performance, or publication proof.'
    }
    $prototypeReport | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $prototypeLogRoot 'PrototypePipelineReport.json') -Encoding utf8
    Pop-Location
}
$prototypeReport | ConvertTo-Json -Depth 5
