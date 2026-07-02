#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$assemblyPath = Join-Path $repoRoot "JianpuEditor\bin\Release\net472\JianpuEditor.exe"

if (-not (Test-Path $assemblyPath)) {
    throw "Release build not found: $assemblyPath. Run 'dotnet build JianpuEditor.sln -c Release' first."
}

$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
$midiType = $assembly.GetType("JianpuEditor.Services.MidiExportService")
$scoreType = $assembly.GetType("JianpuEditor.Models.JianpuScore")
$measureType = $assembly.GetType("JianpuEditor.Models.JianpuMeasure")
$noteType = $assembly.GetType("JianpuEditor.Models.JianpuNote")
$chordMarkerType = $assembly.GetType("JianpuEditor.Models.ChordMarker")

$score = [Activator]::CreateInstance($scoreType)
$score.Title = "Test"
$score.KeySignature = "1=C"
$score.Tempo = "Moderate"
$score.Bpm = 120

$measure = [Activator]::CreateInstance($measureType)
$notes = New-Object "System.Collections.Generic.List[$noteType]"
$n1 = [Activator]::CreateInstance($noteType)
$n1.Pitch = 1
$n2 = [Activator]::CreateInstance($noteType)
$n2.Pitch = 2
$notes.Add($n1)
$notes.Add($n2)
$measure.MelodyNotes = $notes

$chordMarkers = New-Object "System.Collections.Generic.List[$chordMarkerType]"
$c1 = [Activator]::CreateInstance($chordMarkerType)
$c1.Text = "D"
$c1.BeatPosition = 0
$c2 = [Activator]::CreateInstance($chordMarkerType)
$c2.Text = "Bm7"
$c2.BeatPosition = 2
$chordMarkers.Add($c1)
$chordMarkers.Add($c2)
$measure.ChordMarkers = $chordMarkers
$score.Measures = New-Object "System.Collections.Generic.List[$measureType]"
$score.Measures.Add($measure)

$out = Join-Path $env:TEMP "jianpu-test.mid"
if (Test-Path $out) {
    Remove-Item $out -Force
}

$exportArgs = New-Object Object[] 2
$exportArgs[0] = $score
$exportArgs[1] = [string]$out
$midiType.GetMethod("Export").Invoke($null, $exportArgs)

if (-not (Test-Path $out)) {
    throw "MIDI export did not create output file."
}

$bytes = [IO.File]::ReadAllBytes($out)
if ($bytes.Length -lt 14) {
    throw ("MIDI output too small: {0} byte(s)." -f $bytes.Length)
}

if ([Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne "MThd") {
    throw "MIDI output does not start with MThd header."
}

Write-Host ("MIDI export smoke test passed: {0} byte(s)." -f $bytes.Length)