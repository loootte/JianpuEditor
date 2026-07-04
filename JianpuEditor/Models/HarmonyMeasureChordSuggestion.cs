namespace JianpuEditor.Models
{
    public sealed class HarmonyMeasureChordSuggestion
    {
        public int MeasureOffset { get; set; }

        public double BeatPosition { get; set; }

        public string RomanNumeral { get; set; } = string.Empty;

        public string ChordSymbol { get; set; } = string.Empty;
    }
}
