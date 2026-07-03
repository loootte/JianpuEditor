using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class ChordTransposeService
    {
        public static bool TryTransposeChords(JianpuScore score, string targetKeySignature, out string errorMessage, out int transposedCount)
        {
            transposedCount = 0;
            errorMessage = null;

            if (score == null)
            {
                errorMessage = "当前没有可转调的曲谱。";
                return false;
            }

            if (!KeySignatureService.TryParseTonicPitchClass(score.KeySignature, out var sourcePitchClass))
            {
                errorMessage = "无法识别当前调号「" + (score.KeySignature ?? string.Empty) + "」。请使用如 C、1=G、F# 的格式。";
                return false;
            }

            if (!KeySignatureService.TryParseTonicPitchClass(targetKeySignature, out var targetPitchClass))
            {
                errorMessage = "无法识别目标调号「" + (targetKeySignature ?? string.Empty) + "」。请使用如 C、1=G、F# 的格式。";
                return false;
            }

            var semitones = KeySignatureService.GetTransposeSemitones(sourcePitchClass, targetPitchClass);
            if (semitones == 0)
            {
                errorMessage = "目标调与当前调相同，无需转调。";
                return false;
            }

            if (score.Measures != null)
            {
                foreach (var measure in score.Measures)
                {
                    ChordMarkerService.NormalizeMeasure(measure);
                    if (measure.ChordMarkers == null)
                    {
                        continue;
                    }

                    foreach (var marker in measure.ChordMarkers)
                    {
                        var text = marker.Text?.Trim();
                        if (string.IsNullOrEmpty(text) || !ChordParser.IsChordSymbol(text))
                        {
                            continue;
                        }

                        marker.Text = ChordParser.TransposeSymbol(text, semitones);
                        transposedCount++;
                    }
                }
            }

            score.KeySignature = KeySignatureService.FormatKeySignature(targetPitchClass);
            return true;
        }
    }
}
