param(
    [string]$DotnetPath = "",
    [string]$UnityPath = "",
    [string]$ResultsPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    if (!$DotnetPath) {
        $DotnetPath = if (Test-Path "D:\dotnet\dotnet.exe") { "D:\dotnet\dotnet.exe" } else { "dotnet" }
    }
    if (!$ResultsPath) {
        $ResultsPath = Join-Path $root ("verification\fix-evidence\auto-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    }
    $ResultsPath = [IO.Path]::GetFullPath($ResultsPath)
    New-Item -ItemType Directory -Path $ResultsPath -Force | Out-Null
    if (Get-ChildItem $ResultsPath -Filter "*.trx") { throw "Use a fresh results directory; stale test results cannot count as proof." }
    function Invoke-Dotnet {
        param([string[]]$Arguments, [string]$LogName)
        & $DotnetPath @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $ResultsPath $LogName)
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE. See $LogName." }
    }
    Invoke-Dotnet -Arguments @("restore", ".\UPA-MVP1.sln") -LogName "restore.log"
    Invoke-Dotnet -Arguments @("build", ".\UPA-MVP1.sln", "--configuration", "Release", "--no-restore") -LogName "build.log"
    $projects = Get-ChildItem tests -Recurse -Filter "*.Tests.csproj" | Sort-Object FullName
    if (!$projects) { throw "No test projects discovered." }
    foreach ($project in $projects) {
        Write-Host "Testing $($project.BaseName)"
        Invoke-Dotnet -Arguments @("test", $project.FullName, "--configuration", "Release", "--no-build", "--no-restore",
            "--logger", "trx;LogFileName=$($project.BaseName).trx", "--results-directory", $ResultsPath) -LogName ($project.BaseName + ".log")
    }
    $total = 0
    $passed = 0
    foreach ($trx in Get-ChildItem $ResultsPath -Filter "*.trx") {
        [xml]$report = Get-Content -LiteralPath $trx.FullName
        $counters = $report.TestRun.ResultSummary.Counters
        $total += [int]$counters.total
        $passed += [int]$counters.passed
        if ([int]$counters.total -eq 0 -or [int]$counters.total -ne [int]$counters.passed) {
            throw "Failed, skipped or missing tests in $($trx.Name)."
        }
    }
    if ($total -eq 0 -or @(Get-ChildItem $ResultsPath -Filter "*.trx").Count -ne $projects.Count) {
        throw "Missing test-result files; verification is incomplete."
    }
    Invoke-Dotnet -Arguments @("build", "src/UPA.Cli", "--configuration", "Release") -LogName "cli-build.log"
    Invoke-Dotnet -Arguments @("run", "--project", "verification/RecoveryProbe", "--configuration", "Release") -LogName "crash-recovery.log"
    if ($UnityPath) {
        $unityProject = Join-Path $root "verification\UnityOutsider"
        Copy-Item -Path "UnityPackage\UPA.UnityExecutor\Editor\*.cs" -Destination "$unityProject\Assets\Editor" -Force
        $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList "-batchmode", "-nographics", "-projectPath", "`"$unityProject`"",
            "-executeMethod", "OutsiderUnityProbe.Run", "-logFile", "`"$(Join-Path $ResultsPath 'unity-components.log')`"" -WindowStyle Hidden -PassThru
        if (!$unityProcess.WaitForExit(180000)) {
            $unityProcess.Kill()
            throw "Unity component tests timed out."
        }
        if ($unityProcess.ExitCode -ne 0) { throw "Unity component tests failed. See unity-components.log." }
        Copy-Item -LiteralPath "$unityProject\outsider-unity-results.txt" -Destination $ResultsPath
        Invoke-Dotnet -Arguments @("run", "--project", "verification/PipelineProbe", "--configuration", "Release", "--", $UnityPath, $unityProject) -LogName "unity-pipeline.log"
        Copy-Item -LiteralPath "$unityProject\pipeline-proof.json" -Destination $ResultsPath
    }
    @{ total = $total; passed = $passed; failed = 0; skipped = 0; unity = $(if ($UnityPath) { "PASS" } else { "NOT RUN" }) } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $ResultsPath "summary.json")
    Write-Host "Verified: $passed/$total .NET tests passed. Results: $ResultsPath"
}
finally {
    Pop-Location
}
