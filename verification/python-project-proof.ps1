[CmdletBinding()]
param([string]$ProjectRoot, [string]$EvidencePath)
$ErrorActionPreference = 'Stop'
$ProjectRoot = if ($ProjectRoot) { $ProjectRoot } else { Join-Path (Split-Path $PSScriptRoot -Parent) 'fixtures\real-project-python' }
$script = Join-Path $ProjectRoot 'main.py'
$original = Get-Content $script -Raw
$timer = [Diagnostics.Stopwatch]::StartNew()
python $script | Out-Host
$timer.Stop()
$baseline = (Get-FileHash $script -Algorithm SHA256).Hash
try { Add-Content $script "`n# mutation"; $changed = (Get-FileHash $script -Algorithm SHA256).Hash }
finally { Set-Content $script $original -NoNewline }
$restored = (Get-FileHash $script -Algorithm SHA256).Hash
if ($changed -eq $baseline -or $restored -ne $baseline) { throw 'Python mutation/restore proof failed.' }
$evidence = [pscustomobject]@{ Project = (Resolve-Path $ProjectRoot).Path; BaselineSha256 = $baseline; ChangedSha256 = $changed; RestoredSha256 = $restored; ExecutionMilliseconds = $timer.ElapsedMilliseconds; FixtureBytes = (Get-ChildItem $ProjectRoot -File -Recurse | Measure-Object Length -Sum).Sum; Proof = 'PASS' } | ConvertTo-Json
if ($EvidencePath) { $evidence | Set-Content $EvidencePath -Encoding utf8 }
$evidence
