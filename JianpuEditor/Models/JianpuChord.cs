using System.Collections.Generic;

namespace JianpuEditor.Models
{
    /// <summary>
    /// Simultaneous melody notes at a single beat position within a measure.
    /// </summary>
    public class JianpuChord
    {
        public double BeatPosition { get; set; }

        public List<JianpuNote> Notes { get; set; } = new List<JianpuNote>();

        public string Text { get; set; } = string.Empty;
    }
}