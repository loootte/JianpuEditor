using System.Drawing;

namespace JianpuEditor.Models
{
    public enum ScoreHitType
    {
        None,
        Note,
        Gap,
        Tie,
        ChordMarker,
        ChordDragHandle,
        ChordDelete,
        ChordAddSlot,
        SecondaryText,
        LyricText,
        Measure
    }

    public sealed class ScoreHitResult
    {
        public ScoreHitType HitType { get; set; } = ScoreHitType.None;

        public int MeasureIndex { get; set; } = -1;

        public int NoteIndex { get; set; } = -1;

        /// <summary>Insertion index in MelodyNotes when HitType is Gap.</summary>
        public int InsertIndex { get; set; } = -1;

        /// <summary>Index in Score.Ties when HitType is Tie.</summary>
        public int TieIndex { get; set; } = -1;

        /// <summary>Index in Measure.ChordMarkers when HitType is chord-related.</summary>
        public int ChordMarkerIndex { get; set; } = -1;

        public Rectangle Bounds { get; set; } = Rectangle.Empty;
    }
}