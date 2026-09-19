#requires -Version 7.2
[CmdletBinding()]
param(
    [ValidateSet('Validate', 'Readiness')][string]$Mode = 'Validate',
    [string]$ProjectRoot = $PSScriptRoot,
    [switch]$WriteReport
)

# This validates planning artifacts, not Unity gameplay or anti-cheat effectiveness.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$checks = [System.Collections.Generic.List[object]]::new()
$metrics = [ordered]@{ markdownFiles = 0; localLinks = 0; publishedSpecifications = 0; powershellFiles = 0 }
$contract = $null
$rootPath = [IO.Path]::GetFullPath($ProjectRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$rootPrefix = $rootPath + [IO.Path]::DirectorySeparatorChar

function Add-Check([string]$Id, [bool]$Passed, [string]$Detail) {
    $checks.Add([pscustomobject]@{ id = $Id; passed = $Passed; detail = $Detail })
}

function Get-ProjectPath([string]$RelativePath) {
    if ([IO.Path]::IsPathRooted($RelativePath)) { throw "Expected relative project path: $RelativePath" }
    $candidatePath = [IO.Path]::GetFullPath([IO.Path]::Combine($rootPath, $RelativePath))
    if (-not $candidatePath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path leaves project: $RelativePath"
    }
    # Reject reparse points to keep reads and generated reports within this project.
    $currentPath = $rootPath
    foreach ($part in [IO.Path]::GetRelativePath($rootPath, $candidatePath).Split([IO.Path]::DirectorySeparatorChar)) {
        $currentPath = Join-Path $currentPath $part
        if (Test-Path -LiteralPath $currentPath) {
            if ((Get-Item -LiteralPath $currentPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Reparse point is not supported: $RelativePath"
            }
        }
    }
    return $candidatePath
}

function Test-ExactPath([string]$RelativePath) {
    $null = Get-ProjectPath $RelativePath
    $cursorPath = $rootPath
    foreach ($part in $RelativePath.Split('/')) {
        $match = @(Get-ChildItem -LiteralPath $cursorPath -Force | Where-Object { $_.Name -ceq $part })
        if ($match.Count -ne 1) { return $false }
        $cursorPath = $match[0].FullName
    }
    return $true
}

try {
    if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) { throw 'Project root does not exist.' }
    if ((Get-Item -LiteralPath $rootPath).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Project root cannot be a reparse point.' }

    $requiredPaths = @(
        'Plan.md', 'AGENTS.md', 'README.md', 'ProjectPipeline.ps1', 'Docs/PROJECT_CONTRACT.json',
        'Docs/DESIGN-0001-StageExit.md', 'Docs/ARCHITECTURE.md', 'Docs/PIPELINE.md',
        'Docs/Testing/Test-ProjectPipeline.ps1', '.github/workflows/project-validation.yml',
        'Assets/Game/Features/Monster/README.md', 'Assets/Game/Features/MapGeneration/README.md',
        'Assets/Game/Features/StartandExit/README.md', 'Assets/Game/Features/Story/README.md',
        'Assets/Game/Levels/Episode01/Chapter02_LAVA/README.md',
        'Assets/Game/Levels/Episode01/Chapter05_Square/README.md',
        'Assets/Game/Levels/Episode01/Chapter06_FrozenMountain/README.md',
        'Assets/Game/Levels/Episode01/Chapter07_Hometown/README.md'
    )
    foreach ($relativePath in $requiredPaths) {
        Add-Check "PATH:$relativePath" (Test-ExactPath $relativePath) 'Required path and exact casing.'
    }

    $contractPath = Get-ProjectPath 'Docs/PROJECT_CONTRACT.json'
    try {
        $contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json -AsHashtable
        Add-Check 'CONTRACT:parse' $true 'Valid JSON.'
        Add-Check 'CONTRACT:schema' ($contract.schemaVersion -is [long] -and $contract.schemaVersion -eq 2 -and $contract.scope -ceq 'planning-validation-only') 'Schema 2, confirmed specification validation only.'
        $expectedRules = [ordered]@{
            clearOnFirstValidArrival = $true
            exitWindowSeconds = 5
            earlyCloseBasis = 'stage-start-roster'
            shrinkRosterAfterDeathOrLeave = $false
            arrivalBonusBasis = 'actual-arrival-count'
            soloArrivalBonus = 0
            separateAllArrivedBonus = $false
            normalizeSmallTeamToFour = $false
            scoreOwner = 'team-only'
            personalScoreOrLeaderboard = $false
            nonArrivalInvalidatesTeamClear = $false
            speedEvaluationEndsAt = 'settlement-close'
            rankingStartPlayerCount = 4
            ordinaryDepartureInvalidatesRanking = $false
            rankingOrder = 'team-score-desc-then-time-asc'
            shrinkRequiredRolesAfterDeath = $false
            confirmedCheatResponse = 'immediate-session-kick'
            accountSanctionDecision = 'human-evidence-review'
            appealDecision = 'human-review'
            runtimeAuthority = 'operator-dedicated-server'
            serverProvider = 'PlayFab-MPS'
            unityEditorTarget = '6000.6.1f1'
            antiCheatScope = 'server-validation-and-steam-game-ban'
            externalAntiCheatProduct = $false
            automaticIdentityBan = $false
            hostDeparture = 'transfer-room-owner-keep-server-run'
            wipeResponse = 'lobby-then-current-chapter-start'
            monsterKillScore = $false
            demoChapterCount = 1
            releaseChapterCount = 3
            toolRecommendationsDelegated = $true
            lobbyPresentation = '2d'
            waitingRoomPresentation = 'walkable-first-person-3d'
            chapterStartInteraction = 'chapter-station-then-ready-station'
            storyContinueConsent = 'all-currently-connected'
            storyDepartureChangesExitRoster = $false
        }
        Add-Check 'CONTRACT:rule-count' ($contract.approvedRules.Count -eq $expectedRules.Count) 'Only reviewed rules are in the approved section.'
        foreach ($entry in $expectedRules.GetEnumerator()) {
            $actualValue = $contract.approvedRules[$entry.Key]
            $sameType = if ($entry.Value -is [bool]) { $actualValue -is [bool] }
                elseif ($entry.Value -is [string]) { $actualValue -is [string] }
                else { $actualValue -is [long] -or $actualValue -is [int] }
            Add-Check "RULE:$($entry.Key)" ($sameType -and $actualValue -ceq $entry.Value) 'Matches the reviewed decision; changes require a reviewed contract and test update.'
        }
        $pendingKeys = @('networkPackage', 'scoreFormulaAndValues', 'clockStartUnitAndExactDeadline', 'disconnectReconnectAndServerRecovery', 'serverRegionBudgetAndCapacity', 'saveRecoveryRetentionAndSeasons', 'sanctionDurationsAndRetention', 'releasePlatformsPerformanceAndLanguages', 'contentAndInteractionDetails')
        Add-Check 'CONTRACT:pending-count' ($contract.pendingDecisions.Count -eq $pendingKeys.Count) 'Pending decisions cannot silently disappear.'
        foreach ($key in $pendingKeys) {
            Add-Check "PENDING:$key" ($contract.pendingDecisions.ContainsKey($key) -and $null -eq $contract.pendingDecisions[$key]) 'No selection has yet been reviewed for this baseline.'
        }

        Add-Check 'SOURCE:path' ($contract.decisionSource -ceq 'Plan.md') 'Confirmed game specification.'
        foreach ($specPath in @('Plan.md', 'Docs/DESIGN-0001-StageExit.md', 'Docs/ARCHITECTURE.md')) {
            $specText = Get-Content -LiteralPath (Get-ProjectPath $specPath) -Raw
            Add-Check "SPEC:no-dialogue:$specPath" ($specText -notmatch '(?m)^\s*(?:-\s*)?\[[OoXx]\]|사용자 작성:|AI 검토|^#{1,4}\s+(?:EXIT|SEC|ARCH|BUILD)-\d+|^#{1,4}\s+[A-Z]\d{2}\s') 'No completed questionnaire or conversation in published specifications.'
            $metrics.publishedSpecifications++
        }
        $planText = Get-Content -LiteralPath (Get-ProjectPath 'Plan.md') -Raw
        foreach ($token in @('PlayFab Multiplayer Servers', '6000.6.1f1', 'Steam Game Ban', '최대 5초', '기록 시작 4인', 'Chapter02_LAVA', 'Chapter05_Square', 'Chapter06_FrozenMountain', 'Chapter07_Hometown')) {
            Add-Check "SPEC:required:$token" ($planText.Contains($token)) 'Core reviewed decision appears in the canonical plan.'
        }
    } catch {
        Add-Check 'CONTRACT:read' $false $_.Exception.Message
    }

    $projectFiles = [System.Collections.Generic.List[object]]::new()
    foreach ($rootFile in Get-ChildItem -LiteralPath $rootPath -File -Force) { $projectFiles.Add($rootFile) }
    foreach ($directory in @('Docs', 'Assets', '.github')) {
        $directoryPath = Get-ProjectPath $directory
        if (Test-Path -LiteralPath $directoryPath) {
            foreach ($file in Get-ChildItem -LiteralPath $directoryPath -File -Recurse -Force) { $projectFiles.Add($file) }
        }
    }
    foreach ($file in $projectFiles) {
        if ($file.Extension -notin @('.md', '.ps1', '.yml', '.yaml')) { continue }
        $relativeFile = [IO.Path]::GetRelativePath($rootPath, $file.FullName).Replace('\', '/')
        $fileText = Get-Content -LiteralPath (Get-ProjectPath $relativeFile) -Raw
        if ($file.Extension -eq '.md') {
            $metrics.markdownFiles++
            # Inline links only. Fenced examples, URL availability and heading anchors are not verified.
            $withoutFences = [regex]::Replace($fileText, '(?ms)^\s*(`{3,}|~{3,})[^\r\n]*\r?\n.*?^\s*\1\s*$', '')
            $links = [regex]::Matches($withoutFences, '\[[^\]\r\n]*\]\((?:<(?<target>[^>]+)>|(?<target>[^\s\)]+))(?:\s+"[^"\r\n]*")?\)')
            foreach ($link in $links) {
                $target = $link.Groups['target'].Value
                if ($target -match '^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|#|//)') { continue }
                $pathPart = [Uri]::UnescapeDataString(($target -split '[#?]', 2)[0])
                if ([string]::IsNullOrWhiteSpace($pathPart)) { continue }
                $metrics.localLinks++
                try {
                    $absoluteTarget = [IO.Path]::GetFullPath([IO.Path]::Combine($file.DirectoryName, $pathPart))
                    $linkRelative = [IO.Path]::GetRelativePath($rootPath, $absoluteTarget).Replace('\', '/')
                    $safeTarget = Get-ProjectPath $linkRelative
                    Add-Check "LINK:$relativeFile->$target" (Test-Path -LiteralPath $safeTarget) 'Local inline Markdown target exists within the project.'
                } catch {
                    Add-Check "LINK:$relativeFile->$target" $false $_.Exception.Message
                }
            }
        }
        if ($file.Extension -eq '.ps1') {
            $metrics.powershellFiles++
            $parseTokens = $null; $parseErrors = $null
            $null = [Management.Automation.Language.Parser]::ParseInput($fileText, [ref]$parseTokens, [ref]$parseErrors)
            Add-Check "PS:$relativeFile" (@($parseErrors).Count -eq 0) 'PowerShell syntax parses; behavior is tested separately.'
        }
        if ($relativeFile.StartsWith('.github/workflows/')) {
            # Deliberately small policy checks, not a full YAML parser or security audit.
            $uses = [regex]::Matches($fileText, '(?m)^\s*(?:-\s*)?uses:\s*(\S+)')
            Add-Check "CI:actions:$relativeFile" ($uses.Count -ge 1) 'Workflow declares at least one action.'
            foreach ($use in $uses) {
                Add-Check "CI:pin:$($use.Groups[1].Value)" ($use.Groups[1].Value -cmatch '^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_./-]+@[a-f0-9]{40}$') 'External actions pinned to full commit SHA.'
            }
            Add-Check "CI:permissions:$relativeFile" ($fileText -match '(?m)^permissions:\s*\r?\n\s+contents: read\s*$' -and $fileText -notmatch '(?m)\b(?:write|write-all)\s*$') 'Explicit read-only contents, no write grant.'
            Add-Check "CI:credentials:$relativeFile" ($fileText -match 'persist-credentials: false') 'Checkout does not persist credentials.'
            Add-Check "CI:untrusted:$relativeFile" ($fileText -notmatch 'pull_request_target|secrets\.|runs-on:\s*self-hosted|github\.event\.') 'No privileged PR trigger, secrets, self-hosted runner, or raw event interpolation in this baseline.'
        }
    }
} catch {
    Add-Check 'PIPELINE:exception' $false $_.Exception.Message
}

$blockers = [System.Collections.Generic.List[string]]::new()
if ($Mode -eq 'Readiness') {
    if ($null -ne $contract -and $contract.ContainsKey('pendingDecisions')) {
        foreach ($entry in $contract.pendingDecisions.GetEnumerator()) {
            if ($null -eq $entry.Value) { $blockers.Add("DECISION:$($entry.Key)") }
        }
    }
    foreach ($unityPath in @('ProjectSettings/ProjectVersion.txt', 'Packages/manifest.json', 'Packages/packages-lock.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $rootPath $unityPath) -PathType Leaf)) { $blockers.Add("MISSING:$unityPath") }
    }
    # These are implementation gates, not user-editable boolean approvals.
    $blockers.Add('NOT_IMPLEMENTED:Unity EditMode/PlayMode and multi-client test runners')
    $blockers.Add('NOT_IMPLEMENTED:reproducible Unity build, package verification and staged deployment')
    $blockers.Add('NOT_VERIFIED:server validation, Steam Game Ban integration, false-positive and appeal recovery tests')
    $blockers.Add('NOT_VERIFIED:release approval, server operations, data migration and rollback rehearsal')
}
$failed = @($checks | Where-Object { -not $_.passed })
$status = if ($failed.Count -gt 0) { 'FAILED' } elseif ($blockers.Count -gt 0) { 'BLOCKED' } else { 'PASSED' }
$exitCode = if ($status -eq 'FAILED') { 1 } elseif ($status -eq 'BLOCKED') { 2 } else { 0 }
$report = [ordered]@{
    schemaVersion = 1; generatedAtUtc = [DateTime]::UtcNow.ToString('o'); mode = $Mode
    status = $status; exitCode = $exitCode; scope = 'Planning artifact validation, NOT Unity/game/security/release verification'
    metrics = $metrics; checkCount = $checks.Count; failedCount = $failed.Count
    failures = @($failed); blockers = @($blockers); checks = @($checks.ToArray())
}
if ($WriteReport) {
    try {
        $reportPath = Get-ProjectPath 'Docs/Testing/LatestPipelineReport.json'
        [IO.File]::WriteAllText($reportPath, ($report | ConvertTo-Json -Depth 12), [Text.UTF8Encoding]::new($false))
    } catch {
        $report.status = 'FAILED'; $report.exitCode = 1; $exitCode = 1
        $report.failures += [pscustomobject]@{ id = 'REPORT:write'; passed = $false; detail = $_.Exception.Message }
        $report.failedCount++
    }
}
$report | ConvertTo-Json -Depth 12
exit $exitCode
