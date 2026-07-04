using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    internal static class HarmonyProgressionService
    {
        private static readonly string[][] TwoMeasureTemplates =
        {
            new[] { "I", "V" },
            new[] { "I", "IV" },
            new[] { "IV", "V" },
            new[] { "vi", "V" }
        };

        private static readonly string[][] ThreeMeasureTemplates =
        {
            new[] { "I", "IV", "V" },
            new[] { "I", "vi", "IV" },
            new[] { "IV", "V", "I" },
            new[] { "vi", "IV", "V" }
        };

        private static readonly string[][] FourMeasureTemplates =
        {
            new[] { "I", "IV", "V", "I" },
            new[] { "I", "vi", "IV", "V" },
            new[] { "I", "V", "vi", "IV" },
            new[] { "IV", "I", "V", "I" }
        };

        public static IReadOnlyList<HarmonyProgressionSuggestion> SuggestForMeasureRange(
            IReadOnlyList<JianpuMeasure> measures,
            string keySignature)
        {
            if (measures == null || measures.Count < 2)
            {
                return Array.Empty<HarmonyProgressionSuggestion>();
            }

            if (!KeySignatureService.TryParseTonicPitchClass(keySignature, out var tonicPitchClass))
            {
                return Array.Empty<HarmonyProgressionSuggestion>();
            }

            var tonicMidi = ScoreMidiSchedule.DefaultTonicMidi;
            var profiles = MeasureMelodyProfileService.AnalyzeMeasures(measures, tonicMidi);
            var templates = BuildTemplates(measures.Count);
            var scored = templates
                .Select(romans => new
                {
                    Romans = romans,
                    Score = ScoreTemplate(romans, profiles)
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Romans[0], StringComparer.Ordinal)
                .Take(3)
                .ToList();

            return scored
                .Select(item => BuildProgressionSuggestion(item.Romans, profiles, tonicPitchClass, item.Score))
                .ToList();
        }

        private static List<string[]> BuildTemplates(int measureCount)
        {
            var templates = new List<string[]>();
            if (measureCount == 2)
            {
                templates.AddRange(TwoMeasureTemplates);
            }
            else if (measureCount == 3)
            {
                templates.AddRange(ThreeMeasureTemplates);
            }
            else if (measureCount == 4)
            {
                templates.AddRange(FourMeasureTemplates);
            }
            else
            {
                foreach (var template in FourMeasureTemplates)
                {
                    templates.Add(ExpandTemplate(template, measureCount));
                }
            }

            return templates;
        }

        private static string[] ExpandTemplate(string[] template, int measureCount)
        {
            if (template.Length >= measureCount)
            {
                return template.Take(measureCount).ToArray();
            }

            var expanded = new List<string>(template);
            while (expanded.Count < measureCount)
            {
                if (expanded.Count == measureCount - 1)
                {
                    expanded.Add("I");
                }
                else if (expanded.Count == measureCount - 2)
                {
                    expanded.Add("V");
                    expanded.Add("I");
                }
                else
                {
                    expanded.Add(expanded.Count % 2 == 0 ? "IV" : "V");
                }
            }

            return expanded.ToArray();
        }

        private static double ScoreTemplate(string[] romans, IReadOnlyList<MeasureMelodyProfile> profiles)
        {
            var score = 0.0;
            for (var i = 0; i < romans.Length && i < profiles.Count; i++)
            {
                var profile = profiles[i];
                var rootDegree = HarmonySuggestionService.GetRomanRootDegree(romans[i]);
                var triadDegrees = HarmonySuggestionService.GetTriadScaleDegrees(romans[i]);
                if (triadDegrees.Contains(profile.PrimaryDegree))
                {
                    score += 2.0;
                }

                if (triadDegrees.Contains(profile.EndDegree))
                {
                    score += 1.0;
                }

                if (rootDegree == profile.BassDegree)
                {
                    score += 3.0;
                }
                else if (triadDegrees.Contains(profile.BassDegree))
                {
                    score += 1.5;
                }

                if (i > 0)
                {
                    var previousRoot = HarmonySuggestionService.GetRomanRootDegree(romans[i - 1]);
                    score += ScoreBassMotion(previousRoot, rootDegree, profiles[i - 1].BassDegree, profile.BassDegree);
                }
            }

            if (romans.Length >= 2)
            {
                var penultimate = romans[romans.Length - 2];
                var last = romans[romans.Length - 1];
                if (string.Equals(penultimate, "V", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(last, "I", StringComparison.OrdinalIgnoreCase))
                {
                    score += 2.5;
                }
            }

            return score;
        }

        private static double ScoreBassMotion(
            int previousRoot,
            int currentRoot,
            int previousMelodyBass,
            int currentMelodyBass)
        {
            var expectedStep = NormalizeDegreeDelta(currentRoot - previousRoot);
            var actualStep = NormalizeDegreeDelta(currentMelodyBass - previousMelodyBass);
            if (expectedStep == actualStep)
            {
                return 2.0;
            }

            if (Math.Abs(expectedStep) <= 2 || Math.Abs(actualStep) <= 2)
            {
                return 0.75;
            }

            return 0.0;
        }

        private static int NormalizeDegreeDelta(int delta)
        {
            var wrapped = ((delta % 7) + 7) % 7;
            return wrapped > 3 ? wrapped - 7 : wrapped;
        }

        private static HarmonyProgressionSuggestion BuildProgressionSuggestion(
            string[] romans,
            IReadOnlyList<MeasureMelodyProfile> profiles,
            int tonicPitchClass,
            double score)
        {
            var bassDegrees = romans
                .Select(HarmonySuggestionService.GetRomanRootDegree)
                .ToList();
            var steps = new List<HarmonyMeasureChordSuggestion>();
            for (var i = 0; i < romans.Length; i++)
            {
                steps.Add(new HarmonyMeasureChordSuggestion
                {
                    MeasureOffset = i,
                    BeatPosition = 0,
                    RomanNumeral = romans[i],
                    ChordSymbol = HarmonySuggestionService.RomanNumeralToChordSymbol(romans[i], tonicPitchClass)
                });
            }

            return new HarmonyProgressionSuggestion
            {
                Label = string.Join(" - ", romans),
                BassLineSummary = "低音走向 " + string.Join("→", bassDegrees),
                HarmonicSummary = "和声走向 " + string.Join("→", romans.Select(GetHarmonicFunctionName)),
                Reason = BuildReason(profiles, bassDegrees, score),
                Steps = steps
            };
        }

        private static string BuildReason(
            IReadOnlyList<MeasureMelodyProfile> profiles,
            IReadOnlyList<int> bassDegrees,
            double score)
        {
            var melodyBass = string.Join("→", profiles.Select(profile => profile.BassDegree.ToString()));
            return "旋律低音 " + melodyBass + "，匹配度 " + score.ToString("0.0");
        }

        private static string GetHarmonicFunctionName(string roman)
        {
            if (string.IsNullOrWhiteSpace(roman))
            {
                return string.Empty;
            }

            var text = roman.Trim();
            if (text.StartsWith("vii", StringComparison.OrdinalIgnoreCase))
            {
                return "导";
            }

            if (text.StartsWith("iii", StringComparison.OrdinalIgnoreCase))
            {
                return "中";
            }

            if (text.StartsWith("ii", StringComparison.OrdinalIgnoreCase))
            {
                return "ii";
            }

            if (text.StartsWith("vi", StringComparison.OrdinalIgnoreCase))
            {
                return "vi";
            }

            if (text.StartsWith("iv", StringComparison.OrdinalIgnoreCase))
            {
                return "下属";
            }

            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return "属";
            }

            if (text.StartsWith("i", StringComparison.OrdinalIgnoreCase))
            {
                return "主";
            }

            return text;
        }
    }
}
