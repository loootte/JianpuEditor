using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class MeasureNormalizationService
    {
        private const double DurationEpsilon = 0.02;

        public static List<JianpuMeasure> NormalizeMeasures(
            IReadOnlyList<JianpuMeasure> measures,
            int measureBeats = ScoreMidiSchedule.DefaultMeasureBeats)
        {
            if (measures == null || measures.Count == 0)
            {
                return new List<JianpuMeasure> { CreateMeasure() };
            }

            var slots = FlattenChordSlots(measures);
            if (slots.Count == 0)
            {
                return new List<JianpuMeasure> { CreateMeasure() };
            }

            var normalized = RebuildMeasures(slots, measureBeats);
            MergeShortTrailingMeasure(normalized, measureBeats);
            return normalized;
        }

        private static List<ChordSlot> FlattenChordSlots(IReadOnlyList<JianpuMeasure> measures)
        {
            var slots = new List<ChordSlot>();
            foreach (var measure in measures)
            {
                if (measure == null)
                {
                    continue;
                }

                MelodyChordService.NormalizeMeasure(measure);
                for (var i = 0; i < measure.Chords.Count; i++)
                {
                    var chord = measure.Chords[i];
                    if (chord?.Notes == null || chord.Notes.Count == 0)
                    {
                        continue;
                    }

                    slots.Add(new ChordSlot
                    {
                        Notes = chord.Notes.Select(CloneNote).ToList(),
                        BeatPosition = chord.BeatPosition,
                        Text = chord.Text ?? string.Empty
                    });
                }
            }

            return slots;
        }

        private static List<JianpuMeasure> RebuildMeasures(IReadOnlyList<ChordSlot> slots, int measureBeats)
        {
            var measures = new List<JianpuMeasure>();
            var current = CreateMeasure();
            var used = 0.0;

            foreach (var slot in slots)
            {
                var primary = slot.Notes.FirstOrDefault(note => note.Type == NoteType.Note) ?? slot.Notes[0];
                var remaining = JianpuRenderer.GetDurationUnits(primary);
                while (remaining > DurationEpsilon)
                {
                    var room = measureBeats - used;
                    if (room <= DurationEpsilon)
                    {
                        PadMeasureToBeats(current, measureBeats, used);
                        measures.Add(current);
                        current = CreateMeasure();
                        used = 0;
                        room = measureBeats;
                    }

                    var take = Math.Min(remaining, room);
                    var sliceNotes = new List<JianpuNote>();
                    foreach (var source in slot.Notes)
                    {
                        var piece = CreateDurationSlice(source, take);
                        if (piece != null)
                        {
                            sliceNotes.Add(piece);
                        }
                    }

                    if (sliceNotes.Count == 0)
                    {
                        break;
                    }

                    MelodyChordService.AppendChord(
                        current,
                        MelodyChordService.CreateChord(used, sliceNotes, slot.Text));
                    used += take;
                    remaining -= take;
                }
            }

            if (current.MelodyNotes.Count > 0 || used > DurationEpsilon)
            {
                PadMeasureToBeats(current, measureBeats, used);
                measures.Add(current);
            }

            if (measures.Count == 0)
            {
                measures.Add(CreateMeasure());
            }

            return measures;
        }

        private static void MergeShortTrailingMeasure(List<JianpuMeasure> measures, int measureBeats)
        {
            if (measures.Count < 2)
            {
                return;
            }

            var last = measures[measures.Count - 1];
            var lastUsed = GetMeasureDuration(last);
            if (lastUsed >= measureBeats - DurationEpsilon || lastUsed >= 2.0)
            {
                return;
            }

            var previous = measures[measures.Count - 2];
            var previousUsed = GetMeasureDuration(previous);
            if (previousUsed + lastUsed > measureBeats + DurationEpsilon)
            {
                return;
            }

            foreach (var chord in last.Chords)
            {
                if (chord == null)
                {
                    continue;
                }

                MelodyChordService.AppendChord(
                    previous,
                    MelodyChordService.CreateChord(
                        MelodyChordService.GetMeasureDurationUnits(previous),
                        chord.Notes,
                        chord.Text));
            }

            measures.RemoveAt(measures.Count - 1);
            PadMeasureToBeats(previous, measureBeats, GetMeasureDuration(previous));
        }

        private static void PadMeasureToBeats(JianpuMeasure measure, int measureBeats, double used)
        {
            var gap = measureBeats - used;
            while (gap > DurationEpsilon)
            {
                var rest = CreateRestSlice(gap);
                if (rest == null)
                {
                    break;
                }

                MelodyChordService.AppendChord(
                    measure,
                    MelodyChordService.CreateChord(
                        MelodyChordService.GetMeasureDurationUnits(measure),
                        new[] { rest }));
                gap -= JianpuRenderer.GetDurationUnits(rest);
            }
        }

        private static double GetMeasureDuration(JianpuMeasure measure)
        {
            if (measure?.MelodyNotes == null)
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

        private static JianpuNote CreateDurationSlice(JianpuNote source, double units)
        {
            if (source == null || units <= DurationEpsilon)
            {
                return null;
            }

            if (source.Type == NoteType.Rest)
            {
                return CreateRestSlice(units);
            }

            var note = CloneNote(source);
            return MidiImportService.ApplyDurationUnits(note, units) ? note : null;
        }

        private static JianpuNote CreateRestSlice(double units)
        {
            var rest = new JianpuNote { Type = NoteType.Rest, Pitch = 0, Octave = 0 };
            return MidiImportService.ApplyDurationUnits(rest, units) ? rest : null;
        }

        private static JianpuNote CloneNote(JianpuNote source)
        {
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

        private static JianpuMeasure CreateMeasure()
        {
            return new JianpuMeasure
            {
                MelodyNotes = new List<JianpuNote>(),
                Chords = new List<JianpuChord>(),
                LyricText = " "
            };
        }

        private sealed class ChordSlot
        {
            public List<JianpuNote> Notes { get; set; } = new List<JianpuNote>();

            public double BeatPosition { get; set; }

            public string Text { get; set; } = string.Empty;
        }
    }
}
