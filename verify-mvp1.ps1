$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$dotnet = if (Test-Path "D:\dotnet\dotnet.exe") { "D:\dotnet\dotnet.exe" } else { "dotnet" }
Write-Host "Using .NET: $dotnet"
& $dotnet restore .\UPA-MVP1.sln
& $dotnet build .\UPA-MVP1.sln --configuration Release
& $dotnet test .\UPA-MVP1.sln --configuration Release --no-build
if (Get-Command python -ErrorAction SilentlyContinue) {
  & python .\verification\verify.py
}
Write-Host "MVP-1 .NET verification completed."
