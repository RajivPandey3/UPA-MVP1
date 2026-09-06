[CmdletBinding()]
param([string]$ProjectRoot)

$ErrorActionPreference = 'Stop'
$ProjectRoot = if ($ProjectRoot) { $ProjectRoot } else { Join-Path (Split-Path $PSScriptRoot -Parent) 'fixtures\real-project-dotnet' }
$program = Join-Path $ProjectRoot 'Program.cs'
$original = Get-Content $program -Raw
dotnet build (Join-Path $ProjectRoot 'RealProjectProof.csproj') --configuration Release --nologo | Out-Host
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
[pscustomobject]@{
    Project = (Resolve-Path $ProjectRoot).Path
    BaselineSha256 = $baseline
    ChangedSha256 = $changed
    RestoredSha256 = $restored
    Proof = 'PASS'
} | ConvertTo-Json | Write-Output
