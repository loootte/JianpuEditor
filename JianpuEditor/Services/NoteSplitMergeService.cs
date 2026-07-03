using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services
{
    public static class NoteSplitMergeService
    {
        private const double Epsilon = 0.0001;
        private const int MaxQuarterMergeCount = 4;

        public static bool TrySplitNote(JianpuNote source, out IReadOnlyList<JianpuNote> parts)
        {
            parts = Array.Empty<JianpuNote>();
            if (source == null || source.Type == NoteType.Rest)
            {
                return false;
            }

            var tier = NoteEditorViewModel.GetDurationTier(source);
            if (tier <= 0)
            {
                return false;
            }

            if (tier > 2)
            {
                var count = GetQuarterSplitCount(source);
                if (count < 2)
                {
                    return false;
                }

                var quarters = new List<JianpuNote>(count);
                for (var i = 0; i < count; i++)
                {
                    quarters.Add(CreateQuarterFromTemplate(source));
                }

                parts = quarters;
                return true;
            }

            var child = CloneNote(source);
            NoteEditorViewModel.ApplyDurationTier(child, tier - 1);
            child.Dotted = false;
            parts = new[] { CloneNote(child), CloneNote(child) };
            return true;
        }

        public static bool CanMerge(JianpuNote left, JianpuNote right)
        {
            if (left == null || right == null
                || left.Type == NoteType.Rest || right.Type == NoteType.Rest)
            {
                return false;
            }

            var leftDuration = JianpuRenderer.GetDurationUnits(left);
            var rightDuration = JianpuRenderer.GetDurationUnits(right);
            if (leftDuration <= 0 || rightDuration <= 0)
            {
                return false;
            }

            var ratio = Math.Max(leftDuration, rightDuration) / Math.Min(leftDuration, rightDuration);
            return ratio <= 2.0 + Epsilon;
        }

        public static bool TryMergeNotes(IReadOnlyList<JianpuNote> notes, out JianpuNote merged)
        {
            merged = null;
            if (notes == null || notes.Count < 2)
            {
                return false;
            }

            if (notes.Any(note => note == null || note.Type == NoteType.Rest))
            {
                return false;
            }

            if (notes.All(IsPureQuarter))
            {
                var count = Math.Min(notes.Count, MaxQuarterMergeCount);
                merged = CreateQuarterFromTemplate(notes[0]);
                if (count <= 1)
                {
                    return false;
                }

                NoteEditorViewModel.ApplyDurationTier(merged, 2 + (count - 1));
                return true;
            }

            if (notes.Count != 2 || !CanMerge(notes[0], notes[1]))
            {
                return false;
            }

            var totalDuration = JianpuRenderer.GetDurationUnits(notes[0]) + JianpuRenderer.GetDurationUnits(notes[1]);
            merged = CloneNote(notes[0]);
            if (!TryApplyDurationUnits(merged, totalDuration))
            {
                merged = null;
                return false;
            }

            return true;
        }

        public static IReadOnlyList<IReadOnlyList<ScoreNoteRef>> GetAdjacentRuns(IReadOnlyList<ScoreNoteRef> refs)
        {
            if (refs == null || refs.Count == 0)
            {
                return Array.Empty<IReadOnlyList<ScoreNoteRef>>();
            }

            var runs = new List<IReadOnlyList<ScoreNoteRef>>();
            foreach (var group in refs.GroupBy(item => item.MeasureIndex).OrderBy(item => item.Key))
            {
                var ordered = group.OrderBy(item => item.NoteIndex).ToList();
                var current = new List<ScoreNoteRef> { ordered[0] };
                for (var i = 1; i < ordered.Count; i++)
                {
                    if (ordered[i].NoteIndex == ordered[i - 1].NoteIndex + 1)
                    {
                        current.Add(ordered[i]);
                        continue;
                    }

                    if (current.Count >= 2)
                    {
                        runs.Add(current.ToArray());
                    }

                    current = new List<ScoreNoteRef> { ordered[i] };
                }

                if (current.Count >= 2)
                {
                    runs.Add(current.ToArray());
                }
            }

            return runs;
        }

        public static void ReplaceNoteWithMany(
            JianpuScore score,
            int measureIndex,
            int noteIndex,
            IReadOnlyList<JianpuNote> replacements)
        {
            if (score?.Measures == null
                || measureIndex < 0
                || measureIndex >= score.Measures.Count
                || replacements == null
                || replacements.Count == 0)
            {
                return;
            }

            var notes = score.Measures[measureIndex].MelodyNotes;
            if (noteIndex < 0 || noteIndex >= notes.Count)
            {
                return;
            }

            notes.RemoveAt(noteIndex);
            TieMaintenanceService.OnNoteRemoved(score, measureIndex, noteIndex);
            for (var i = 0; i < replacements.Count; i++)
            {
                notes.Insert(noteIndex + i, CloneNote(replacements[i]));
            }

            if (replacements.Count > 1)
            {
                TieMaintenanceService.OnMelodyNoteCountChanged(
                    score,
                    measureIndex,
                    noteIndex + 1,
                    replacements.Count - 1);
            }
        }

        public static void RemoveNotes(
            JianpuScore score,
            int measureIndex,
            IReadOnlyList<int> noteIndices)
        {
            if (score?.Measures == null
                || measureIndex < 0
                || measureIndex >= score.Measures.Count
                || noteIndices == null
                || noteIndices.Count == 0)
            {
                return;
            }

            foreach (var index in noteIndices.Distinct().OrderByDescending(item => item))
            {
                var notes = score.Measures[measureIndex].MelodyNotes;
                if (index < 0 || index >= notes.Count)
                {
                    continue;
                }

                notes.RemoveAt(index);
                TieMaintenanceService.OnNoteRemoved(score, measureIndex, index);
            }
        }

        private static int GetQuarterSplitCount(JianpuNote note)
        {
            var units = JianpuRenderer.GetDurationUnits(note);
            var count = (int)Math.Round(units, MidpointRounding.AwayFromZero);
            return Math.Max(2, Math.Min(MaxQuarterMergeCount, count));
        }

        private static bool IsPureQuarter(JianpuNote note)
        {
            return note != null
                && note.Type != NoteType.Rest
                && !note.Dotted
                && NoteEditorViewModel.GetDurationTier(note) == 2;
        }

        private static JianpuNote CreateQuarterFromTemplate(JianpuNote template)
        {
            var note = CloneNote(template);
            NoteEditorViewModel.ApplyDurationTier(note, 2);
            note.Dotted = false;
            return note;
        }

        private static bool TryApplyDurationUnits(JianpuNote note, double units)
        {
            if (note == null || units <= 0)
            {
                return false;
            }

            note.Dotted = false;

            if (units >= 1.0 - Epsilon)
            {
                var rounded = (int)Math.Round(units, MidpointRounding.AwayFromZero);
                if (Math.Abs(units - rounded) < Epsilon && rounded >= 1 && rounded <= MaxQuarterMergeCount)
                {
                    NoteEditorViewModel.ApplyDurationTier(note, 2 + (rounded - 1));
                    return true;
                }

                if (Math.Abs(units - 1.5) < Epsilon)
                {
                    NoteEditorViewModel.ApplyDurationTier(note, 2);
                    note.Dotted = true;
                    return true;
                }
            }

            if (Math.Abs(units - 0.25) < Epsilon)
            {
                NoteEditorViewModel.ApplyDurationTier(note, 0);
                return true;
            }

            if (Math.Abs(units - 0.5) < Epsilon)
            {
                NoteEditorViewModel.ApplyDurationTier(note, 1);
                return true;
            }

            if (Math.Abs(units - 0.75) < Epsilon)
            {
                NoteEditorViewModel.ApplyDurationTier(note, 1);
                note.Dotted = true;
                return true;
            }

            return false;
        }

        private static JianpuNote CloneNote(JianpuNote source)
        {
            if (source == null)
            {
                return new JianpuNote();
            }

            return new JianpuNote
            {
                Type = source.Type,
                Pitch = source.Pitch,
                Octave = source.Octave,
                Underlines = source.Underlines,
                Dashes = source.Dashes,
                Dotted = source.Dotted
            };
        }
    }
}