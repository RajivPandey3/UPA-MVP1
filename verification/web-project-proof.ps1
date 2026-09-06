[CmdletBinding()]
param([string]$ProjectRoot, [string]$EvidencePath)
$ErrorActionPreference = 'Stop'
$ProjectRoot = if ($ProjectRoot) { $ProjectRoot } else { Join-Path (Split-Path $PSScriptRoot -Parent) 'fixtures\real-project-web' }
$index = Join-Path $ProjectRoot 'index.html'; $app = Join-Path $ProjectRoot 'app.js'
if (-not (Test-Path $index) -or -not (Test-Path $app)) { throw 'Required web assets are missing.' }
$original = Get-Content $index -Raw; $timer = [Diagnostics.Stopwatch]::StartNew()
if ((Get-Content $index -Raw) -notmatch 'app\.js') { throw 'Entry does not reference app.js.' }
$timer.Stop(); $baseline = (Get-FileHash $index -Algorithm SHA256).Hash
try { Add-Content $index "`n<!-- mutation -->"; $changed = (Get-FileHash $index -Algorithm SHA256).Hash }
finally { Set-Content $index $original -NoNewline }
$restored = (Get-FileHash $index -Algorithm SHA256).Hash
if ($changed -eq $baseline -or $restored -ne $baseline) { throw 'Web mutation/restore proof failed.' }
$evidence = [pscustomobject]@{ Project = (Resolve-Path $ProjectRoot).Path; BaselineSha256 = $baseline; ChangedSha256 = $changed; RestoredSha256 = $restored; ValidationMilliseconds = $timer.ElapsedMilliseconds; FixtureBytes = (Get-ChildItem $ProjectRoot -File -Recurse | Measure-Object Length -Sum).Sum; Proof = 'PASS' } | ConvertTo-Json
if ($EvidencePath) { $evidence | Set-Content $EvidencePath -Encoding utf8 }
$evidence
