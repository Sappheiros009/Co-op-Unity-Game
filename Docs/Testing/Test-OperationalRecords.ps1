#requires -Version 7.2
[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSCommandPath)))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check([string]$Id, [bool]$Passed, [string]$Detail) {
    $checks.Add([pscustomobject]@{ id = $Id; passed = $Passed; detail = $Detail })
}

function Get-RequiredText([string]$RelativePath) {
    $path = Join-Path $ProjectRoot ($RelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Check "PATH:$RelativePath" $false 'Required record file is missing.'
        return ''
    }
    Add-Check "PATH:$RelativePath" $true 'Required record file exists.'
    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string]$Id, [string]$Text, [string[]]$Needles) {
    foreach ($needle in $Needles) {
        Add-Check "${Id}:$needle" ($Text.Contains($needle)) "Required text: $needle"
    }
}

try {
    if (-not (Test-Path -LiteralPath $ProjectRoot -PathType Container)) {
        throw "Project root does not exist: $ProjectRoot"
    }

    $testReadme = Get-RequiredText 'Docs/Testing/README.md'
    $testTemplate = Get-RequiredText 'Docs/Testing/TEST_REPORT_TEMPLATE.md'
    $testBaseline = Get-RequiredText 'Docs/Testing/TEST-0012-TestBugOperationsBaseline.md'
    $bugReadme = Get-RequiredText 'Docs/Bugs/README.md'
    $bugTemplate = Get-RequiredText 'Docs/Bugs/BUG_TEMPLATE.md'
    $maintenanceReadme = Get-RequiredText 'Docs/Maintenance/README.md'
    $maintenanceTemplate = Get-RequiredText 'Docs/Maintenance/TASK_TEMPLATE.md'
    $operationsReadme = Get-RequiredText 'Docs/Operations/README.md'
    $incidentTemplate = Get-RequiredText 'Docs/Operations/INCIDENT_TEMPLATE.md'
    $releaseTemplate = Get-RequiredText 'Docs/Operations/RELEASE_TEMPLATE.md'
    $runbook = Get-RequiredText 'Docs/Operations/RUNBOOK-0001-ReleaseIncidentRecovery.md'

    $statusValues = @('PASS', 'FAIL', 'BLOCKED', 'NOT_RUN', 'NOT_APPLICABLE')
    Assert-Contains 'STATUS:baseline' $testBaseline $statusValues
    Assert-Contains 'STATUS:template' $testTemplate $statusValues
    Assert-Contains 'TEST:readme-links' $testReadme @('TEST-0012-TestBugOperationsBaseline.md', 'Test-OperationalRecords.ps1')
    Assert-Contains 'TEST:template-fields' $testTemplate @('브랜치·커밋:', '실제 결과·증거', '아직 확인하지 않은 범위')
    Assert-Contains 'BUG:template-fields' $bugTemplate @('우선순위:', '분류:', '재현 판정:', '확인된 원인:', '관련 테스트')
    Assert-Contains 'BUG:readme-policy' $bugReadme @('검증 대기', '기획 미정', '실제 증거')
    Assert-Contains 'TASK:template-fields' $maintenanceTemplate @('실제 파일·설정', '아직 확인하지 않은 범위')
    Assert-Contains 'TASK:readme-link' $maintenanceReadme @('TASK-0017-TestBugOperationsBaseline.md')
    Assert-Contains 'OPS:readme-links' $operationsReadme @('INCIDENT_TEMPLATE.md', 'RELEASE_TEMPLATE.md', 'RUNBOOK-0001-ReleaseIncidentRecovery.md')
    Assert-Contains 'OPS:incident-fields' $incidentTemplate @('확인된 사실:', '아직 추정인 내용:', '되돌릴 조건과 방법:')
    Assert-Contains 'OPS:release-fields' $releaseTemplate @('실제 빌드 결과·파일 식별 정보:', '실제 배포 시각·대상·결과:', '아직 확인하지 않은 범위:')
    Assert-Contains 'OPS:runbook' $runbook @('롤백 조건', '원본 보존', 'Steam Game Ban', '미검증 범위')

    $markdownFiles = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Docs') -Filter '*.md' -Recurse -File
    foreach ($file in $markdownFiles) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        Add-Check "SECRET:$($file.Name)" ($text -notmatch '(?i)(api[_-]?key|secret|password|bearer\s+[a-z0-9._-]{12,})\s*[:=]\s*[^`\s]+') 'No obvious credential assignment in documentation.'
    }
} catch {
    Add-Check 'SCRIPT:exception' $false $_.Exception.Message
}

$failed = @($checks | Where-Object { -not $_.passed })
$report = [ordered]@{
    schemaVersion = 1
    scope = 'Test, bug, maintenance, and operations record structure; not game or release verification'
    status = if ($failed.Count -eq 0) { 'PASSED' } else { 'FAILED' }
    checkCount = $checks.Count
    failedCount = $failed.Count
    checks = @($checks.ToArray())
}
$report | ConvertTo-Json -Depth 8
exit $(if ($failed.Count -eq 0) { 0 } else { 1 })
