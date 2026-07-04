using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public sealed class HarmonyProgressionSuggestion
    {
        public string Label { get; set; } = string.Empty;

        public string BassLineSummary { get; set; } = string.Empty;

        public string HarmonicSummary { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public IReadOnlyList<HarmonyMeasureChordSuggestion> Steps { get; set; } =
            new List<HarmonyMeasureChordSuggestion>();
    }
}
