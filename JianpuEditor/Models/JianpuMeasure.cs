using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public class JianpuMeasure
    {
        public List<JianpuNote> MelodyNotes { get; set; } = new List<JianpuNote>();

        public string SecondaryText { get; set; } = string.Empty;

        public string LyricText { get; set; } = string.Empty;
    }
}