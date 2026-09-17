#requires -Version 7.2
[CmdletBinding()]
param([string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$shellPath = (Get-Process -Id $PID).Path
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
$fixtureRoot = Join-Path $tempBase ('coop-pipeline-tests-' + [Guid]::NewGuid().ToString('N'))
$results = [System.Collections.Generic.List[object]]::new()
$encoding = [Text.UTF8Encoding]::new($false)

function Write-Fixture([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, $encoding)
}
function Invoke-Case([string]$Name, [scriptblock]$Mutation, [int]$ExpectedExit, [string]$ExpectedFailure = '', [string]$Mode = 'Validate') {
    $casePath = Join-Path $fixtureRoot $Name
    $null = New-Item -ItemType Directory -Path $casePath
    foreach ($sourceFile in $fixtureFiles) {
        $relative = [IO.Path]::GetRelativePath($sourceRoot, $sourceFile.FullName)
        $destination = Join-Path $casePath $relative
        $null = New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force
        Copy-Item -LiteralPath $sourceFile.FullName -Destination $destination
    }
    & $Mutation $casePath
    # Execute the unchanged validator; it reads the isolated, possibly corrupted fixture.
    $jsonOutput = & $shellPath -NoProfile -File (Join-Path $sourceRoot 'ProjectPipeline.ps1') -ProjectRoot $casePath -Mode $Mode
    $actualExit = $LASTEXITCODE
    try {
        $data = ($jsonOutput -join "`n") | ConvertFrom-Json
        $failureFound = [string]::IsNullOrEmpty($ExpectedFailure) -or @($data.failures | Where-Object { $_.id -like $ExpectedFailure }).Count -gt 0
        $passed = $actualExit -eq $ExpectedExit -and $failureFound -and $data.exitCode -eq $actualExit
        if ($Mode -eq 'Readiness') { $passed = $passed -and $data.status -eq 'BLOCKED' -and $data.blockers.Count -gt 0 }
        $failureIds = @($data.failures | ForEach-Object { $_.id })
        $results.Add([pscustomobject]@{ name = $Name; passed = $passed; expectedExit = $ExpectedExit; actualExit = $actualExit; failures = $failureIds })
    } catch {
        $results.Add([pscustomobject]@{ name = $Name; passed = $false; expectedExit = $ExpectedExit; actualExit = $actualExit; failures = @($_.Exception.Message) })
    }
}

try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot
    $fixtureFiles = [System.Collections.Generic.List[object]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -File -Force) {
        if ($file.Extension -in @('.md', '.ps1')) { $fixtureFiles.Add($file) }
    }
    foreach ($folder in @('Docs', 'Assets', '.github')) {
        foreach ($file in Get-ChildItem -LiteralPath (Join-Path $sourceRoot $folder) -File -Recurse -Force) {
            if ($file.Extension -in @('.md', '.json', '.ps1', '.yml', '.yaml') -and $file.Name -ne 'LatestPipelineReport.json') {
                if ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Fixture source cannot be a reparse point.' }
                $fixtureFiles.Add($file)
            }
        }
    }
    Invoke-Case 'valid-baseline' {} 0
    Invoke-Case 'fenced-example-not-a-link' { param($p)
        $f = Join-Path $p 'README.md'
        $example = @'

```markdown
[example only](Docs/INTENTIONALLY-NOT-A-FILE.md)
```
'@
        Write-Fixture $f ((Get-Content -LiteralPath $f -Raw) + $example)
    } 0
    Invoke-Case 'broken-link' { param($p)
        $f = Join-Path $p 'README.md'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw) + "`n[missing](Docs/DOES-NOT-EXIST.md)`n")
    } 1 'LINK:*'
    Invoke-Case 'link-outside-project' { param($p)
        $f = Join-Path $p 'README.md'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw) + "`n[escape](../outside.md)`n")
    } 1 'LINK:*'
    Invoke-Case 'renamed-feature' { param($p)
        Rename-Item -LiteralPath (Join-Path $p 'Assets/Game/Features/Monster') -NewName 'Enemies'
    } 1 'PATH:Assets/Game/Features/Monster/README.md'
    Invoke-Case 'wrong-folder-casing' { param($p)
        Rename-Item -LiteralPath (Join-Path $p 'Assets/Game/Features/Monster') -NewName 'MonsterCaseFixture'
        Rename-Item -LiteralPath (Join-Path $p 'Assets/Game/Features/MonsterCaseFixture') -NewName 'monster'
    } 1 'PATH:Assets/Game/Features/Monster/README.md'
    Invoke-Case 'invalid-json' { param($p)
        Write-Fixture (Join-Path $p 'Docs/PROJECT_CONTRACT.json') '{'
    } 1 'CONTRACT:read'
    Invoke-Case 'changed-score-rule' { param($p)
        $f = Join-Path $p 'Docs/PROJECT_CONTRACT.json'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('"team-only"', '"individual"'))
    } 1 'RULE:scoreOwner'
    Invoke-Case 'changed-server-decision' { param($p)
        $f = Join-Path $p 'Docs/PROJECT_CONTRACT.json'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('"operator-dedicated-server"', '"player-host"'))
    } 1 'RULE:runtimeAuthority'
    Invoke-Case 'changed-identity-ban-policy' { param($p)
        $f = Join-Path $p 'Docs/PROJECT_CONTRACT.json'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('"automaticIdentityBan": false', '"automaticIdentityBan": true'))
    } 1 'RULE:automaticIdentityBan'
    Invoke-Case 'changed-unity-target' { param($p)
        $f = Join-Path $p 'Docs/PROJECT_CONTRACT.json'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('"6000.6.1f1"', '"6000.3.15f1"'))
    } 1 'RULE:unityEditorTarget'
    Invoke-Case 'wrong-value-type' { param($p)
        $f = Join-Path $p 'Docs/PROJECT_CONTRACT.json'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('"exitWindowSeconds": 5', '"exitWindowSeconds": "5"'))
    } 1 'RULE:exitWindowSeconds'
    Invoke-Case 'questionnaire-returned' { param($p)
        $f = Join-Path $p 'Plan.md'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw) + "`n사용자 작성:`n선택:A`n")
    } 1 'SPEC:no-dialogue:Plan.md'
    Invoke-Case 'missing-server-specification' { param($p)
        $f = Join-Path $p 'Plan.md'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('PlayFab Multiplayer Servers', 'Unselected Provider'))
    } 1 'SPEC:required:PlayFab Multiplayer Servers'
    Invoke-Case 'silently-selected-network' { param($p)
        $f = Join-Path $p 'Docs/PROJECT_CONTRACT.json'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('"networkPackage": null', '"networkPackage": "unreviewed-sdk"'))
    } 1 'PENDING:networkPackage'
    Invoke-Case 'powershell-syntax-error' { param($p)
        Write-Fixture (Join-Path $p 'Docs/Testing/BrokenFixture.ps1') 'function Broken {'
    } 1 'PS:Docs/Testing/BrokenFixture.ps1'
    Invoke-Case 'mutable-action-tag' { param($p)
        $f = Join-Path $p '.github/workflows/project-validation.yml'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('actions/checkout@d23441a48e516b6c34aea4fa41551a30e30af803', 'actions/checkout@v6'))
    } 1 'CI:pin:*'
    Invoke-Case 'write-permission' { param($p)
        $f = Join-Path $p '.github/workflows/project-validation.yml'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('contents: read', 'contents: write'))
    } 1 'CI:permissions:*'
    Invoke-Case 'untrusted-privileged-trigger' { param($p)
        $f = Join-Path $p '.github/workflows/project-validation.yml'; Write-Fixture $f ((Get-Content -LiteralPath $f -Raw).Replace('pull_request:', 'pull_request_target:'))
    } 1 'CI:untrusted:*'
    Invoke-Case 'release-not-ready' {} 2 '' 'Readiness'
} catch {
    $results.Add([pscustomobject]@{ name = 'harness'; passed = $false; failures = @($_.Exception.Message) })
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        $resolvedFixture = (Resolve-Path -LiteralPath $fixtureRoot).Path
        if ([IO.Path]::GetDirectoryName($resolvedFixture) -cne $tempBase -or [IO.Path]::GetFileName($resolvedFixture) -cnotmatch '^coop-pipeline-tests-[a-f0-9]{32}$') {
            throw 'Refusing cleanup outside the exact generated temporary test directory.'
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
$failedCount = @($results | Where-Object { -not $_.passed }).Count
[ordered]@{ scope = 'Validator self-tests, NOT game tests'; total = $results.Count; passed = $results.Count - $failedCount; failed = $failedCount; cases = @($results.ToArray()) } | ConvertTo-Json -Depth 8
if ($failedCount -gt 0) { exit 1 }
exit 0
