using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Rendering
{
    public readonly struct UnderlineSpan
    {
        public UnderlineSpan(int startNoteIndex, int endNoteIndex)
        {
            StartNoteIndex = startNoteIndex;
            EndNoteIndex = endNoteIndex;
        }

        public int StartNoteIndex { get; }

        public int EndNoteIndex { get; }
    }

    public static class BeatGroupUnderlinePlanner
    {
        public static List<List<int>> GroupNotesByQuarterBeat(IList<JianpuNote> notes)
        {
            var groups = new List<List<int>>();
            if (notes == null || notes.Count == 0)
            {
                return groups;
            }

            var current = new List<int>();
            var sum = 0.0;

            for (var i = 0; i < notes.Count; i++)
            {
                var duration = JianpuRenderer.GetDurationUnits(notes[i]);
                if (duration <= 0)
                {
                    duration = 1;
                }

                if (current.Count > 0 && sum + duration > 1.0001)
                {
                    groups.Add(current);
                    current = new List<int>();
                    sum = 0;
                }

                current.Add(i);
                sum += duration;

                if (sum >= 0.9999)
                {
                    groups.Add(current);
                    current = new List<int>();
                    sum = 0;
                }
            }

            if (current.Count > 0)
            {
                groups.Add(current);
            }

            return groups;
        }

        public static List<UnderlineSpan> CollectSpans(
            IList<int> group,
            IList<JianpuNote> notes,
            int underlineIndex)
        {
            var spans = new List<UnderlineSpan>();
            if (group == null || notes == null)
            {
                return spans;
            }

            var spanStart = -1;
            var spanEnd = -1;
            foreach (var noteIndex in group)
            {
                if (noteIndex < 0
                    || noteIndex >= notes.Count
                    || notes[noteIndex] == null
                    || notes[noteIndex].Underlines <= underlineIndex)
                {
                    FlushSpan(spans, ref spanStart, ref spanEnd);
                    continue;
                }

                if (spanStart < 0)
                {
                    spanStart = noteIndex;
                }

                spanEnd = noteIndex;
            }

            FlushSpan(spans, ref spanStart, ref spanEnd);
            return spans;
        }

        private static void FlushSpan(List<UnderlineSpan> spans, ref int spanStart, ref int spanEnd)
        {
            if (spanStart < 0)
            {
                return;
            }

            spans.Add(new UnderlineSpan(spanStart, spanEnd));
            spanStart = -1;
            spanEnd = -1;
        }
    }
}
