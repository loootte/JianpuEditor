#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projects = @(
    (Join-Path $repoRoot "JianpuEditor\JianpuEditor.csproj"),
    (Join-Path $repoRoot "JianpuEditor.Tests\JianpuEditor.Tests.csproj")
)

$dotnetCandidates = @(
    $env:DOTNET_EXE,
    "D:\dotnet\dotnet.exe",
    "dotnet"
) | Where-Object { $_ -and (($_ -eq "dotnet") -or (Test-Path $_)) }

$dotnet = $dotnetCandidates | Select-Object -First 1
if (-not $dotnet) {
    throw "dotnet SDK not found. Set DOTNET_EXE or install .NET SDK."
}

foreach ($project in $projects) {
    Write-Host "Checking format: $project"
    & $dotnet format $project --verify-no-changes --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet format check failed for $project"
    }
}

Write-Host "dotnet format check passed."