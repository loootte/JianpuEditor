#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot "JianpuEditor.sln"
$issFile = Join-Path $repoRoot "installer\JianpuEditor.iss"
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
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

Write-Host "Building Release..."
& $dotnet build $solution -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6 first."
}

Write-Host "Compiling installer with $iscc ..."
Push-Location (Join-Path $repoRoot "installer")
try {
    & $iscc $issFile
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

$setup = Get-ChildItem (Join-Path $repoRoot "installer\output\JianpuEditor-Setup-*.exe") |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setup) {
    throw "Installer output not found."
}

Write-Host "Installer created: $($setup.FullName)"
Write-Host "Size: $([math]::Round($setup.Length / 1MB, 2)) MB"