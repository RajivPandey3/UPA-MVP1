[CmdletBinding()]
param([string]$ProjectRoot)

$ErrorActionPreference = 'Stop'
$ProjectRoot = if ($ProjectRoot) { $ProjectRoot } else { Join-Path (Split-Path $PSScriptRoot -Parent) 'fixtures\real-project-dotnet' }
$program = Join-Path $ProjectRoot 'Program.cs'
$original = Get-Content $program -Raw
$timer = [Diagnostics.Stopwatch]::StartNew()
dotnet build (Join-Path $ProjectRoot 'RealProjectProof.csproj') --configuration Release --nologo | Out-Host
$timer.Stop()
$baseline = (Get-FileHash $program -Algorithm SHA256).Hash
try {
    Add-Content $program "`n// proof mutation"
    $changed = (Get-FileHash $program -Algorithm SHA256).Hash
    if ($changed -eq $baseline) { throw 'Mutation was not detected.' }
}
finally {
    Set-Content $program $original -NoNewline
}
$restored = (Get-FileHash $program -Algorithm SHA256).Hash
if ($restored -ne $baseline) { throw 'Restore did not return to the baseline hash.' }
$fixtureBytes = (Get-ChildItem $ProjectRoot -File -Recurse | Measure-Object Length -Sum).Sum
$profile = if ($fixtureBytes -le 1MB) { 'Small' } elseif ($fixtureBytes -le 100MB) { 'Medium' } elseif ($fixtureBytes -le 1GB) { 'Large' } else { 'Stress' }
$timeBudget = @{ Small = 10000; Medium = 60000; Large = 600000; Stress = $null }[$profile]
if ($null -ne $timeBudget -and $timer.ElapsedMilliseconds -gt $timeBudget) { throw "$profile profile time budget exceeded." }
[pscustomobject]@{
    Project = (Resolve-Path $ProjectRoot).Path
    BaselineSha256 = $baseline
    ChangedSha256 = $changed
    RestoredSha256 = $restored
    BuildMilliseconds = $timer.ElapsedMilliseconds
    FixtureBytes = $fixtureBytes
    Profile = $profile
    ThresholdMilliseconds = $timeBudget
    Proof = 'PASS'
} | ConvertTo-Json | Write-Output
