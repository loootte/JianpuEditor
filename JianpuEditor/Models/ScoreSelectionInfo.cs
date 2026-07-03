using System;
using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public sealed class ScoreSelectionInfo
    {
        public int MeasureIndex { get; set; } = -1;

        public int NoteIndex { get; set; } = -1;

        public int InsertIndex { get; set; } = -1;

        public int TieIndex { get; set; } = -1;

        public int ChordMeasureIndex { get; set; } = -1;

        public int ChordMarkerIndex { get; set; } = -1;

        public IReadOnlyList<int> SelectedMeasureIndices { get; set; } = Array.Empty<int>();

        public bool HasNoteSelected
        {
            get { return NoteIndex >= 0; }
        }

        public bool HasGapSelected
        {
            get { return InsertIndex >= 0; }
        }

        public bool HasTieSelected
        {
            get { return TieIndex >= 0; }
        }

        public bool HasChordSelected
        {
            get { return ChordMarkerIndex >= 0; }
        }
    }
}
