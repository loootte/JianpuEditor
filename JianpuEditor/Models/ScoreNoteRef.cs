using System;

namespace JianpuEditor.Models
{
    public readonly struct ScoreNoteRef : IEquatable<ScoreNoteRef>
    {
        public ScoreNoteRef(int measureIndex, int noteIndex)
        {
            MeasureIndex = measureIndex;
            NoteIndex = noteIndex;
        }

        public int MeasureIndex { get; }

        public int NoteIndex { get; }

        public bool Equals(ScoreNoteRef other)
        {
            return MeasureIndex == other.MeasureIndex && NoteIndex == other.NoteIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ScoreNoteRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (MeasureIndex * 397) ^ NoteIndex;
        }

        public static int Compare(ScoreNoteRef left, ScoreNoteRef right)
        {
            var measureCompare = left.MeasureIndex.CompareTo(right.MeasureIndex);
            return measureCompare != 0 ? measureCompare : left.NoteIndex.CompareTo(right.NoteIndex);
        }
    }
}
