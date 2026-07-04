namespace JianpuEditor.Models
{
    public enum NoteType
    {
        Note,
        Rest
    }

    public class JianpuNote
    {
        public NoteType Type { get; set; } = NoteType.Note;

        /// <summary>0 = rest, 1-7 = natural pitch, 1.5 / 2.5 = chromatic (.5 + Accidental).</summary>
        public double Pitch { get; set; } = 1;

        /// <summary>Sharp / flat spelling for .5 pitches (#1, b3).</summary>
        public AccidentalKind Accidental { get; set; }

        /// <summary>-1 = low octave dot, 0 = normal, 1 = high octave dot.</summary>
        public int Octave { get; set; }

        /// <summary>Number of duration underlines (0=quarter, 1=eighth, 2=sixteenth).</summary>
        public int Underlines { get; set; }

        /// <summary>增时线数量：0=四分，1=二分，3=全音（每条增时线 +1 拍）。</summary>
        public int Dashes { get; set; }

        public bool Dotted { get; set; }
    }
}
