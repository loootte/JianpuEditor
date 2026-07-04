using System;
using System.Collections.Generic;
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

            var notes = FlattenNotes(measures);
            if (notes.Count == 0)
            {
                return new List<JianpuMeasure> { CreateMeasure() };
            }

            var normalized = RebuildMeasures(notes, measureBeats);
            MergeShortTrailingMeasure(normalized, measureBeats);
            return normalized;
        }

        private static List<JianpuNote> FlattenNotes(IReadOnlyList<JianpuMeasure> measures)
        {
            var notes = new List<JianpuNote>();
            foreach (var measure in measures)
            {
                if (measure?.MelodyNotes == null)
                {
                    continue;
                }

                foreach (var note in measure.MelodyNotes)
                {
                    if (note == null)
                    {
                        continue;
                    }

                    notes.Add(CloneNote(note));
                }
            }

            return notes;
        }

        private static List<JianpuMeasure> RebuildMeasures(IReadOnlyList<JianpuNote> notes, int measureBeats)
        {
            var measures = new List<JianpuMeasure>();
            var current = CreateMeasure();
            var used = 0.0;

            foreach (var note in notes)
            {
                var remaining = JianpuRenderer.GetDurationUnits(note);
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
                    var piece = CreateDurationSlice(note, take);
                    if (piece == null)
                    {
                        break;
                    }

                    current.MelodyNotes.Add(piece);
                    used += JianpuRenderer.GetDurationUnits(piece);
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

            foreach (var note in last.MelodyNotes)
            {
                previous.MelodyNotes.Add(note);
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

                measure.MelodyNotes.Add(rest);
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
                LyricText = " "
            };
        }
    }
}
