using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class TieMaintenanceService
    {
        public static void OnNoteRemoved(JianpuScore score, int measureIndex, int removedNoteIndex)
        {
            if (score?.Ties == null || score.Ties.Count == 0)
            {
                return;
            }

            for (var i = score.Ties.Count - 1; i >= 0; i--)
            {
                var tie = score.Ties[i];
                if (ReferencesNote(tie, measureIndex, removedNoteIndex))
                {
                    score.Ties.RemoveAt(i);
                    continue;
                }

                if (tie.StartMeasureIndex == measureIndex && tie.StartNoteIndex > removedNoteIndex)
                {
                    tie.StartNoteIndex--;
                }

                if (tie.EndMeasureIndex == measureIndex && tie.EndNoteIndex > removedNoteIndex)
                {
                    tie.EndNoteIndex--;
                }
            }
        }

        public static void OnMeasureRemoved(JianpuScore score, int removedMeasureIndex)
        {
            if (score?.Ties == null || score.Ties.Count == 0)
            {
                return;
            }

            for (var i = score.Ties.Count - 1; i >= 0; i--)
            {
                var tie = score.Ties[i];
                if (tie.StartMeasureIndex == removedMeasureIndex || tie.EndMeasureIndex == removedMeasureIndex)
                {
                    score.Ties.RemoveAt(i);
                    continue;
                }

                if (tie.StartMeasureIndex > removedMeasureIndex)
                {
                    tie.StartMeasureIndex--;
                }

                if (tie.EndMeasureIndex > removedMeasureIndex)
                {
                    tie.EndMeasureIndex--;
                }
            }
        }

        private static bool ReferencesNote(JianpuTie tie, int measureIndex, int noteIndex)
        {
            return (tie.StartMeasureIndex == measureIndex && tie.StartNoteIndex == noteIndex)
                || (tie.EndMeasureIndex == measureIndex && tie.EndNoteIndex == noteIndex);
        }
    }
}