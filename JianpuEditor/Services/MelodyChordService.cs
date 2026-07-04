using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class MelodyChordService
    {
        private const double BeatEpsilon = 0.001;

        public static void NormalizeScore(JianpuScore score)
        {
            if (score?.Measures == null)
            {
                return;
            }

            foreach (var measure in score.Measures)
            {
                NormalizeMeasure(measure);
            }
        }

        public static void NormalizeMeasure(JianpuMeasure measure)
        {
            if (measure == null)
            {
                return;
            }

            measure.Chords ??= new List<JianpuChord>();
            measure.MelodyNotes ??= new List<JianpuNote>();

            if (measure.Chords.Count == 0 && measure.MelodyNotes.Count > 0)
            {
                BuildChordsFromMelodyNotes(measure);
                return;
            }

            if (measure.Chords.Count > 0)
            {
                SyncMelodyNotesFromChords(measure);
            }
        }

        public static void SyncChordsFromMelodyNotes(JianpuMeasure measure)
        {
            if (measure == null)
            {
                return;
            }

            measure.Chords ??= new List<JianpuChord>();
            measure.MelodyNotes ??= new List<JianpuNote>();
            BuildChordsFromMelodyNotes(measure);
        }

        public static void InsertSlot(JianpuMeasure measure, int insertIndex, JianpuNote note)
        {
            if (measure == null || note == null)
            {
                return;
            }

            NormalizeMeasure(measure);
            measure.MelodyNotes.Insert(insertIndex, CloneNote(note));
            BuildChordsFromMelodyNotes(measure);
        }

        public static void RemoveSlot(JianpuMeasure measure, int removeIndex)
        {
            if (measure == null)
            {
                return;
            }

            NormalizeMeasure(measure);
            if (removeIndex >= 0 && removeIndex < measure.MelodyNotes.Count)
            {
                measure.MelodyNotes.RemoveAt(removeIndex);
            }

            BuildChordsFromMelodyNotes(measure);
        }

        public static int GetSlotCount(JianpuMeasure measure)
        {
            if (measure == null)
            {
                return 0;
            }

            NormalizeMeasure(measure);
            return measure.MelodyNotes.Count;
        }

        public static IReadOnlyList<JianpuNote> GetNotesAtSlot(JianpuMeasure measure, int slotIndex)
        {
            if (measure == null)
            {
                return Array.Empty<JianpuNote>();
            }

            NormalizeMeasure(measure);
            if (slotIndex < 0 || slotIndex >= measure.Chords.Count)
            {
                return Array.Empty<JianpuNote>();
            }

            var chord = measure.Chords[slotIndex];
            if (chord?.Notes == null || chord.Notes.Count == 0)
            {
                if (slotIndex < measure.MelodyNotes.Count)
                {
                    return new[] { measure.MelodyNotes[slotIndex] };
                }

                return Array.Empty<JianpuNote>();
            }

            return chord.Notes;
        }

        public static JianpuNote GetPrimaryNote(JianpuMeasure measure, int slotIndex)
        {
            var notes = GetNotesAtSlot(measure, slotIndex);
            if (notes.Count == 0)
            {
                return new JianpuNote();
            }

            return notes.FirstOrDefault(note => note.Type == NoteType.Note) ?? notes[0];
        }

        public static double GetSlotDuration(JianpuMeasure measure, int slotIndex)
        {
            if (measure?.MelodyNotes == null || slotIndex < 0 || slotIndex >= measure.MelodyNotes.Count)
            {
                return 0;
            }

            return JianpuRenderer.GetDurationUnits(measure.MelodyNotes[slotIndex]);
        }

        public static bool HasSimultaneousNotes(JianpuMeasure measure, int slotIndex)
        {
            var notes = GetNotesAtSlot(measure, slotIndex);
            return notes.Count(note => note.Type == NoteType.Note) > 1;
        }

        public static void SyncMelodyNotesFromChords(JianpuMeasure measure)
        {
            if (measure?.Chords == null)
            {
                return;
            }

            measure.MelodyNotes ??= new List<JianpuNote>();
            measure.MelodyNotes.Clear();

            foreach (var chord in measure.Chords)
            {
                if (chord?.Notes == null || chord.Notes.Count == 0)
                {
                    measure.MelodyNotes.Add(new JianpuNote { Type = NoteType.Rest, Pitch = 0 });
                    continue;
                }

                var primary = chord.Notes.FirstOrDefault(note => note.Type == NoteType.Note) ?? chord.Notes[0];
                measure.MelodyNotes.Add(CloneNote(primary));
            }
        }

        private static void BuildChordsFromMelodyNotes(JianpuMeasure measure)
        {
            measure.Chords.Clear();
            var beat = 0.0;
            foreach (var note in measure.MelodyNotes)
            {
                if (note == null)
                {
                    continue;
                }

                measure.Chords.Add(new JianpuChord
                {
                    BeatPosition = beat,
                    Notes = new List<JianpuNote> { CloneNote(note) }
                });
                beat += JianpuRenderer.GetDurationUnits(note);
            }
        }

        public static JianpuChord CreateChord(double beatPosition, IEnumerable<JianpuNote> notes, string text = "")
        {
            var chord = new JianpuChord
            {
                BeatPosition = beatPosition,
                Text = text ?? string.Empty,
                Notes = new List<JianpuNote>()
            };

            if (notes != null)
            {
                foreach (var note in notes)
                {
                    if (note != null)
                    {
                        chord.Notes.Add(CloneNote(note));
                    }
                }
            }

            return chord;
        }

        public static void AppendChord(JianpuMeasure measure, JianpuChord chord)
        {
            if (measure == null || chord == null)
            {
                return;
            }

            measure.Chords ??= new List<JianpuChord>();
            measure.MelodyNotes ??= new List<JianpuNote>();

            var primary = chord.Notes.FirstOrDefault(note => note.Type == NoteType.Note) ?? chord.Notes.FirstOrDefault();
            if (primary != null)
            {
                measure.MelodyNotes.Add(CloneNote(primary));
            }
            else
            {
                measure.MelodyNotes.Add(new JianpuNote { Type = NoteType.Rest, Pitch = 0 });
            }

            measure.Chords.Add(chord);
        }

        public static double GetMeasureDurationUnits(JianpuMeasure measure)
        {
            if (measure?.MelodyNotes == null || measure.MelodyNotes.Count == 0)
            {
                return 0;
            }

            var total = 0.0;
            foreach (var note in measure.MelodyNotes)
            {
                total += JianpuRenderer.GetDurationUnits(note);
            }

            return total;
        }

        public static bool BeatPositionsMatch(double left, double right)
        {
            return Math.Abs(left - right) <= BeatEpsilon;
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
                Accidental = source.Accidental,
                Octave = source.Octave,
                Underlines = source.Underlines,
                Dashes = source.Dashes,
                Dotted = source.Dotted
            };
        }
    }
}