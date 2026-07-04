using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public sealed class ScheduledMidiNote
    {
        public double StartQuarter { get; set; }

        public double DurationQuarter { get; set; }

        public int MidiNote { get; set; }

        public int Channel { get; set; }

        public int Velocity { get; set; }
    }

    public sealed class ScoreMidiSchedule
    {
        public const int MelodyChannel = 0;
        public const int ChordChannel = 1;
        public const int MelodyVelocity = 90;
        public const int ChordVelocity = 72;
        public const int DefaultMeasureBeats = 4;
        public const int DefaultTonicMidi = 60;

        private static readonly int[] MajorScaleOffsets = { 0, 2, 4, 5, 7, 9, 11 };

        public IList<ScheduledMidiNote> Notes { get; private set; } = new List<ScheduledMidiNote>();

        public double TotalQuarterLength { get; private set; }

        public static ScoreMidiSchedule Build(JianpuScore score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            var schedule = new ScoreMidiSchedule();
            var melody = BuildMelodyNotes(score);
            var chords = BuildChordNotes(score);
            var notes = new List<ScheduledMidiNote>(melody.Count + chords.Count);
            notes.AddRange(melody);
            notes.AddRange(chords);
            schedule.Notes = notes;
            schedule.TotalQuarterLength = ComputeTotalQuarterLength(score);
            return schedule;
        }

        public static double ComputeTotalQuarterLength(JianpuScore score)
        {
            var total = 0.0;
            var measures = score?.Measures;
            if (measures == null)
            {
                return 0;
            }

            foreach (var measure in measures)
            {
                MelodyChordService.NormalizeMeasure(measure);
                total += GetMeasureDurationUnits(measure);
            }

            return total;
        }

        public static double GetMeasureDurationUnits(JianpuMeasure measure)
        {
            var duration = 0.0;
            var notes = measure?.MelodyNotes;
            if (notes != null)
            {
                foreach (var note in notes)
                {
                    duration += JianpuRenderer.GetDurationUnits(note);
                }
            }

            return duration > 0 ? duration : DefaultMeasureBeats;
        }

        private static List<ScheduledMidiNote> BuildMelodyNotes(JianpuScore score)
        {
            var tonicMidi = ParseTonicMidi(score.KeySignature);
            var suppressed = BuildTieEndSet(score.Ties);
            var tieExtensionCache = new Dictionary<NotePosition, double>();
            var events = new List<ScheduledMidiNote>();
            var quarterTime = 0.0;

            var measures = score.Measures ?? new List<JianpuMeasure>();
            for (var measureIndex = 0; measureIndex < measures.Count; measureIndex++)
            {
                var measure = measures[measureIndex];
                MelodyChordService.NormalizeMeasure(measure);
                var notes = measure.MelodyNotes;
                if (notes == null)
                {
                    continue;
                }

                for (var noteIndex = 0; noteIndex < notes.Count; noteIndex++)
                {
                    var slotNote = notes[noteIndex];
                    var duration = JianpuRenderer.GetDurationUnits(slotNote);
                    var position = new NotePosition(measureIndex, noteIndex);

                    if (suppressed.Contains(position))
                    {
                        quarterTime += duration;
                        continue;
                    }

                    var chordNotes = MelodyChordService.GetNotesAtSlot(measure, noteIndex);
                    var playableNotes = chordNotes
                        .Where(note => note.Type == NoteType.Note && JianpuPitchCodec.IsValidMelodyPitch(note))
                        .ToList();
                    if (playableNotes.Count == 0)
                    {
                        quarterTime += duration;
                        continue;
                    }

                    var totalDuration = duration + GetTieExtension(score, position, tieExtensionCache);
                    if (playableNotes.Count == 1)
                    {
                        events.AddRange(OrnamentPlaybackService.ScheduleMelodyNote(
                            measure,
                            playableNotes[0],
                            noteIndex,
                            quarterTime,
                            totalDuration,
                            tonicMidi,
                            MelodyChannel,
                            MelodyVelocity));
                    }
                    else
                    {
                        foreach (var note in playableNotes)
                        {
                            events.Add(new ScheduledMidiNote
                            {
                                StartQuarter = quarterTime,
                                DurationQuarter = totalDuration,
                                MidiNote = ToMelodyMidiNote(note, tonicMidi),
                                Channel = MelodyChannel,
                                Velocity = MelodyVelocity
                            });
                        }
                    }

                    quarterTime += duration;
                }
            }

            return events;
        }

        private static List<ScheduledMidiNote> BuildChordNotes(JianpuScore score)
        {
            var events = new List<ScheduledMidiNote>();
            var measures = score.Measures ?? new List<JianpuMeasure>();
            var measureStart = 0.0;

            for (var measureIndex = 0; measureIndex < measures.Count; measureIndex++)
            {
                var measure = measures[measureIndex];
                var measureDuration = GetMeasureDurationUnits(measure);
                var chordSymbols = ChordParser.ExtractScheduledChords(measure);
                if (chordSymbols.Count > 0)
                {
                    for (var chordIndex = 0; chordIndex < chordSymbols.Count; chordIndex++)
                    {
                        var chord = chordSymbols[chordIndex];
                        var chordStart = measureStart + chord.BeatPosition;
                        var chordEnd = chordIndex + 1 < chordSymbols.Count
                            ? measureStart + chordSymbols[chordIndex + 1].BeatPosition
                            : measureStart + measureDuration;
                        var chordDuration = Math.Max(0.01, chordEnd - chordStart);
                        var midiNotes = ChordParser.ToBlockChordMidiNotes(chord.Symbol);
                        foreach (var midiNote in midiNotes)
                        {
                            events.Add(new ScheduledMidiNote
                            {
                                StartQuarter = chordStart,
                                DurationQuarter = chordDuration,
                                MidiNote = midiNote,
                                Channel = ChordChannel,
                                Velocity = ChordVelocity
                            });
                        }
                    }
                }

                measureStart += measureDuration;
            }

            return events;
        }

        private static HashSet<NotePosition> BuildTieEndSet(IList<JianpuTie> ties)
        {
            var set = new HashSet<NotePosition>();
            if (ties == null)
            {
                return set;
            }

            foreach (var tie in ties)
            {
                set.Add(new NotePosition(tie.EndMeasureIndex, tie.EndNoteIndex));
            }

            return set;
        }

        private static double GetTieExtension(
            JianpuScore score,
            NotePosition start,
            IDictionary<NotePosition, double> cache)
        {
            if (cache.TryGetValue(start, out var cached))
            {
                return cached;
            }

            var extension = 0.0;
            var ties = score.Ties ?? new List<JianpuTie>();
            foreach (var tie in ties)
            {
                if (tie.StartMeasureIndex != start.MeasureIndex || tie.StartNoteIndex != start.NoteIndex)
                {
                    continue;
                }

                var endNote = GetNote(score, tie.EndMeasureIndex, tie.EndNoteIndex);
                if (endNote == null)
                {
                    continue;
                }

                var endPosition = new NotePosition(tie.EndMeasureIndex, tie.EndNoteIndex);
                var endDuration = JianpuRenderer.GetDurationUnits(endNote);
                extension += endDuration + GetTieExtension(score, endPosition, cache);
            }

            cache[start] = extension;
            return extension;
        }

        private static JianpuNote GetNote(JianpuScore score, int measureIndex, int noteIndex)
        {
            if (score.Measures == null || measureIndex < 0 || measureIndex >= score.Measures.Count)
            {
                return null;
            }

            var notes = score.Measures[measureIndex].MelodyNotes;
            if (notes == null || noteIndex < 0 || noteIndex >= notes.Count)
            {
                return null;
            }

            return notes[noteIndex];
        }

        public static int ToMelodyMidiNote(JianpuNote note, int tonicMidi)
        {
            return JianpuPitchCodec.ToMelodyMidiNote(note, tonicMidi);
        }

        private static int ParseTonicMidi(string keySignature)
        {
            if (!KeySignatureService.TryParseTonicPitchClass(keySignature, out var pitchClass))
            {
                return DefaultTonicMidi;
            }

            var octaveBase = DefaultTonicMidi - (DefaultTonicMidi % 12);
            return Math.Max(0, Math.Min(127, octaveBase + pitchClass));
        }

        private readonly struct NotePosition : IEquatable<NotePosition>
        {
            public NotePosition(int measureIndex, int noteIndex)
            {
                MeasureIndex = measureIndex;
                NoteIndex = noteIndex;
            }

            public int MeasureIndex { get; }

            public int NoteIndex { get; }

            public bool Equals(NotePosition other)
            {
                return MeasureIndex == other.MeasureIndex && NoteIndex == other.NoteIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is NotePosition other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (MeasureIndex * 397) ^ NoteIndex;
                }
            }
        }
    }
}
