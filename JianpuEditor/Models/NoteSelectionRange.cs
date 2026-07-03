using System;
using System.Collections.Generic;
using System.Linq;

namespace JianpuEditor.Models
{
    internal static class NoteSelectionRange
    {
        public static IReadOnlyList<ScoreNoteRef> Enumerate(
            JianpuScore score,
            ScoreNoteRef start,
            ScoreNoteRef end)
        {
            if (score?.Measures == null || score.Measures.Count == 0)
            {
                return Array.Empty<ScoreNoteRef>();
            }

            var from = Compare(start, end) <= 0 ? start : end;
            var to = Compare(start, end) <= 0 ? end : start;
            var refs = new List<ScoreNoteRef>();

            for (var measureIndex = from.MeasureIndex; measureIndex <= to.MeasureIndex; measureIndex++)
            {
                if (measureIndex < 0 || measureIndex >= score.Measures.Count)
                {
                    continue;
                }

                var notes = score.Measures[measureIndex].MelodyNotes;
                if (notes == null || notes.Count == 0)
                {
                    continue;
                }

                var noteStart = measureIndex == from.MeasureIndex ? from.NoteIndex : 0;
                var noteEnd = measureIndex == to.MeasureIndex ? to.NoteIndex : notes.Count - 1;
                noteStart = Math.Max(0, Math.Min(noteStart, notes.Count - 1));
                noteEnd = Math.Max(0, Math.Min(noteEnd, notes.Count - 1));

                for (var noteIndex = noteStart; noteIndex <= noteEnd; noteIndex++)
                {
                    refs.Add(new ScoreNoteRef(measureIndex, noteIndex));
                }
            }

            return refs;
        }

        public static bool ContainsAll(IReadOnlyList<ScoreNoteRef> selected, IReadOnlyList<ScoreNoteRef> range)
        {
            if (range == null || range.Count == 0)
            {
                return false;
            }

            if (selected == null || selected.Count == 0)
            {
                return false;
            }

            return range.All(item => selected.Any(existing => existing.Equals(item)));
        }

        private static int Compare(ScoreNoteRef left, ScoreNoteRef right)
        {
            return ScoreNoteRef.Compare(left, right);
        }
    }
}