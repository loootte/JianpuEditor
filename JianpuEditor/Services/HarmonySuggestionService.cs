using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class HarmonySuggestionService
    {
        private static readonly int[] MajorScaleOffsets = { 0, 2, 4, 5, 7, 9, 11 };

        private static readonly IReadOnlyDictionary<int, string[]> DegreeToRomanCandidates =
            new Dictionary<int, string[]>
            {
                { 1, new[] { "I", "vi", "IV" } },
                { 2, new[] { "V", "ii", "IV" } },
                { 3, new[] { "I", "vi", "iii" } },
                { 4, new[] { "IV", "ii", "I" } },
                { 5, new[] { "V", "I", "iii" } },
                { 6, new[] { "vi", "IV", "ii" } },
                { 7, new[] { "V", "vi", "iii" } }
            };
        private static readonly string[] collection = new[] { "I", "IV", "V" };

        public static IReadOnlyList<HarmonySuggestion> SuggestForMeasure(
            JianpuMeasure measure,
            string keySignature,
            double beatPosition)
        {
            if (measure == null)
            {
                return Array.Empty<HarmonySuggestion>();
            }

            if (!KeySignatureService.TryParseTonicPitchClass(keySignature, out var tonicPitchClass))
            {
                return Array.Empty<HarmonySuggestion>();
            }

            var melodyDegrees = CollectMelodyDegrees(measure, beatPosition);
            if (melodyDegrees.Count == 0)
            {
                return new[]
                {
                    BuildSuggestion("I", tonicPitchClass, "默认主和弦"),
                    BuildSuggestion("IV", tonicPitchClass, "常见下属和弦"),
                    BuildSuggestion("V", tonicPitchClass, "常见属和弦")
                };
            }

            var rankedRomans = new List<string>();
            foreach (var degree in melodyDegrees)
            {
                if (!DegreeToRomanCandidates.TryGetValue(degree, out var candidates))
                {
                    continue;
                }

                foreach (var roman in candidates)
                {
                    if (!rankedRomans.Contains(roman, StringComparer.Ordinal))
                    {
                        rankedRomans.Add(roman);
                    }
                }
            }

            if (rankedRomans.Count == 0)
            {
                rankedRomans.AddRange(collection);
            }

            return rankedRomans
                .Take(3)
                .Select(roman => BuildSuggestion(roman, tonicPitchClass, BuildReason(melodyDegrees, roman)))
                .ToList();
        }

        public static string RomanNumeralToChordSymbol(string romanNumeral, int tonicPitchClass)
        {
            if (string.IsNullOrWhiteSpace(romanNumeral))
            {
                return string.Empty;
            }

            var text = romanNumeral.Trim();
            var quality = GetTriadQualitySuffix(text);
            var scaleDegree = ParseRomanScaleDegree(text);
            if (scaleDegree < 1 || scaleDegree > 7)
            {
                return string.Empty;
            }

            var rootPitchClass = Mod12(tonicPitchClass + MajorScaleOffsets[scaleDegree - 1]);
            return KeySignatureService.PitchClassToNoteName(rootPitchClass) + quality;
        }

        private static HarmonySuggestion BuildSuggestion(string roman, int tonicPitchClass, string reason)
        {
            return new HarmonySuggestion
            {
                RomanNumeral = roman,
                ChordSymbol = RomanNumeralToChordSymbol(roman, tonicPitchClass),
                Reason = reason
            };
        }

        private static string BuildReason(IReadOnlyList<int> melodyDegrees, string roman)
        {
            var degreeText = string.Join("/", melodyDegrees.Select(degree => degree.ToString()));
            return "旋律音 " + degreeText + " → " + roman;
        }

        private static List<int> CollectMelodyDegrees(JianpuMeasure measure, double beatPosition)
        {
            var degrees = new List<int>();
            var noteIndex = MelodyBeatService.FindNoteIndexAtBeat(measure, beatPosition);
            if (noteIndex >= 0)
            {
                AppendDegree(measure.MelodyNotes[noteIndex], degrees);
            }

            if (degrees.Count == 0)
            {
                foreach (var note in measure.MelodyNotes)
                {
                    AppendDegree(note, degrees);
                    if (degrees.Count >= 2)
                    {
                        break;
                    }
                }
            }

            return degrees;
        }

        private static void AppendDegree(JianpuNote note, ICollection<int> degrees)
        {
            if (note == null || note.Type == NoteType.Rest || !JianpuPitchCodec.IsValidMelodyPitch(note))
            {
                return;
            }

            var degree = GetDiatonicDegree(note);
            if (degree >= 1 && degree <= 7 && !degrees.Contains(degree))
            {
                degrees.Add(degree);
            }
        }

        private static int GetDiatonicDegree(JianpuNote note)
        {
            if (note.Accidental == AccidentalKind.Sharp)
            {
                return (int)Math.Floor(note.Pitch + 0.001);
            }

            if (note.Accidental == AccidentalKind.Flat)
            {
                return (int)Math.Ceiling(note.Pitch - 0.001);
            }

            return (int)Math.Round(note.Pitch);
        }

        private static int ParseRomanScaleDegree(string roman)
        {
            if (string.IsNullOrWhiteSpace(roman))
            {
                return -1;
            }

            var text = roman.Trim();
            if (text.StartsWith("vii", StringComparison.OrdinalIgnoreCase))
            {
                return 7;
            }

            if (text.StartsWith("iii", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (text.StartsWith("ii", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (text.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }

            if (text.StartsWith("iv", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }

            if (text.StartsWith("i", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return -1;
        }

        private static string GetTriadQualitySuffix(string roman)
        {
            if (string.IsNullOrWhiteSpace(roman))
            {
                return string.Empty;
            }

            if (roman.EndsWith("dim", StringComparison.OrdinalIgnoreCase))
            {
                return "dim";
            }

            if (roman.Length > 1 && char.IsLower(roman[roman.Length - 1]))
            {
                return "m";
            }

            return string.Empty;
        }

        private static int Mod12(int value)
        {
            var result = value % 12;
            return result < 0 ? result + 12 : result;
        }
    }
}
