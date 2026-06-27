$ErrorActionPreference = "Stop"
Add-Type -Path "D:\workspace\JianpuEditor\JianpuEditor\bin\Release\net472\JianpuEditor.exe" 2>$null

$assembly = [Reflection.Assembly]::LoadFrom("D:\workspace\JianpuEditor\JianpuEditor\bin\Release\net472\JianpuEditor.exe")
$midiType = $assembly.GetType("JianpuEditor.Services.MidiExportService")
$scoreType = $assembly.GetType("JianpuEditor.Models.JianpuScore")
$measureType = $assembly.GetType("JianpuEditor.Models.JianpuMeasure")
$noteType = $assembly.GetType("JianpuEditor.Models.JianpuNote")
$noteEnum = $assembly.GetType("JianpuEditor.Models.NoteType")

$score = [Activator]::CreateInstance($scoreType)
$score.Title = "Test"
$score.KeySignature = "1=C"
$score.Tempo = "中速"
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
$measure.SecondaryText = "D    Bm7"
$score.Measures = New-Object "System.Collections.Generic.List[$measureType]"
$score.Measures.Add($measure)

$out = "$env:TEMP\jianpu-test.mid"
$midiType.GetMethod("Export").Invoke($null, @($score, $out))
Format-Hex $out | Select-Object -First 6