using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public class JianpuMeasure
    {
        public const int MaxChordMarkers = 4;

        public List<JianpuNote> MelodyNotes { get; set; } = new List<JianpuNote>();

        public List<ChordMarker> ChordMarkers { get; set; } = new List<ChordMarker>();

        /// <summary>旧版副旋律单行文本，读入时自动迁移为 ChordMarkers。</summary>
        public string SecondaryText { get; set; } = string.Empty;

        public string LyricText { get; set; } = string.Empty;
    }
}