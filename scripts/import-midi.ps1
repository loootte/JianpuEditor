param(
    [Parameter(Mandatory = $true)]
    [string]$InputMidi,
    [Parameter(Mandatory = $true)]
    [string]$OutputJianpu
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$dotnet = "D:\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

if (-not (Test-Path $InputMidi)) {
    throw "MIDI 文件不存在: $InputMidi"
}

Push-Location $repoRoot
try {
    & $dotnet build JianpuEditor.sln -c Debug -v q | Out-Null
    $runner = @"
using System;
using JianpuEditor.Services;
class Program {
  static void Main(string[] args) {
    MidiImportService.ImportToJianpuFile(args[0], args[1]);
    Console.WriteLine("Wrote " + args[1]);
  }
}
"@
    $runnerDir = Join-Path $env:TEMP "jianpu-midi-import-cli"
    New-Item -ItemType Directory -Force -Path $runnerDir | Out-Null
    $proj = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""$repoRoot\JianpuEditor\JianpuEditor.csproj"" />
  </ItemGroup>
</Project>
"@
    Set-Content -Path (Join-Path $runnerDir "Runner.csproj") -Value $proj -Encoding UTF8
    Set-Content -Path (Join-Path $runnerDir "Program.cs") -Value $runner -Encoding UTF8
    & $dotnet run --project (Join-Path $runnerDir "Runner.csproj") -c Release -- $InputMidi $OutputJianpu
}
finally {
    Pop-Location
}