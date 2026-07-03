using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public class JianpuMeasure
    {
        public const int MaxChordMarkers = 4;

        public List<JianpuNote> MelodyNotes { get; set; } = new List<JianpuNote>();

        public List<ChordMarker> ChordMarkers { get; set; } = new List<ChordMarker>();

        public List<LyricSyllable> LyricSyllables { get; set; } = new List<LyricSyllable>();

        public string LyricText { get; set; } = string.Empty;
    }
}
