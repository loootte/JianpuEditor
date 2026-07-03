using System;
using System.Collections.Generic;

namespace JianpuEditor.ViewModels
{
    public sealed class ScoreEditResult
    {
        public static ScoreEditResult Unchanged
        {
            get { return new ScoreEditResult(); }
        }

        public bool Changed { get; set; }

        public string Message { get; set; }

        public bool RequiresScoreRefresh { get; set; } = true;

        public int? SelectMeasureIndex { get; set; }

        public int? SelectNoteMeasureIndex { get; set; }

        public int? SelectNoteIndex { get; set; }

        public IReadOnlyList<int> SetSelectedMeasureIndices { get; set; }

        public int? SetPrimaryMeasureIndex { get; set; }

        public bool ClearMelodySelection { get; set; }

        public bool ClearTieSelection { get; set; }

        public bool ClearChordSelection { get; set; }

        public int? SelectChordMeasureIndex { get; set; }

        public int? SelectChordMarkerIndex { get; set; }

        public int? UpdatedMeasureIndex { get; set; }

        public static ScoreEditResult WithMessage(string message)
        {
            return new ScoreEditResult
            {
                Changed = true,
                Message = message
            };
        }
    }
}
