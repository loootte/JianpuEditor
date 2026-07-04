using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class OrnamentPlaybackService
    {
        public const double GraceDurationQuarter = 0.125;
        public const double TrillSegmentQuarter = 0.125;
        public const double FermataDurationMultiplier = 1.5;
        public const double MinNoteDurationQuarter = 0.0625;

        public static List<ScheduledMidiNote> ScheduleMelodyNote(
            JianpuMeasure measure,
            JianpuNote note,
            int noteIndex,
            double startQuarter,
            double durationQuarter,
            int tonicMidi,
            int channel,
            int velocity)
        {
            if (note == null)
            {
                return new List<ScheduledMidiNote>();
            }

            OrnamentService.NormalizeMeasure(measure);
            var ornaments = GetOrnamentsForNote(measure, noteIndex);
            if (ornaments.Count == 0)
            {
                return new List<ScheduledMidiNote>
                {
                    MakeEvent(startQuarter, durationQuarter, note, tonicMidi, channel, velocity)
                };
            }

            var duration = durationQuarter;
            if (ornaments.Any(item => item.Type == OrnamentType.Fermata))
            {
                duration *= FermataDurationMultiplier;
            }

            var cursor = startQuarter;
            var events = new List<ScheduledMidiNote>();
            var grace = ornaments.FirstOrDefault(item => item.Type == OrnamentType.GraceNote);
            if (grace != null)
            {
                var graceDuration = Math.Min(GraceDurationQuarter, duration * 0.25);
                graceDuration = Math.Max(MinNoteDurationQuarter, graceDuration);
                events.Add(MakeEvent(
                    cursor,
                    graceDuration,
                    BuildGraceNote(note, grace),
                    tonicMidi,
                    channel,
                    velocity));
                cursor += graceDuration;
                duration = Math.Max(MinNoteDurationQuarter, duration - graceDuration);
            }

            if (ornaments.Any(item => item.Type == OrnamentType.Trill))
            {
                var trill = ornaments.First(item => item.Type == OrnamentType.Trill);
                events.AddRange(BuildTrill(note, cursor, duration, trill, tonicMidi, channel, velocity));
            }
            else if (ornaments.Any(item => item.Type == OrnamentType.Turn))
            {
                events.AddRange(BuildTurn(note, cursor, duration, tonicMidi, channel, velocity));
            }
            else if (ornaments.Any(item => item.Type == OrnamentType.Mordent))
            {
                events.AddRange(BuildMordent(note, cursor, duration, tonicMidi, channel, velocity));
            }
            else
            {
                events.Add(MakeEvent(cursor, duration, note, tonicMidi, channel, velocity));
            }

            return events;
        }

        private static List<JianpuOrnament> GetOrnamentsForNote(JianpuMeasure measure, int noteIndex)
        {
            if (measure?.Ornaments == null || noteIndex < 0)
            {
                return new List<JianpuOrnament>();
            }

            return measure.Ornaments
                .Where(item => OrnamentService.ResolveNoteIndex(measure, item) == noteIndex)
                .ToList();
        }

        private static List<ScheduledMidiNote> BuildTrill(
            JianpuNote note,
            double startQuarter,
            double durationQuarter,
            JianpuOrnament ornament,
            int tonicMidi,
            int channel,
            int velocity)
        {
            var segmentCount = ResolveTrillSegmentCount(durationQuarter, ornament);
            if (segmentCount < 2)
            {
                return new List<ScheduledMidiNote>
                {
                    MakeEvent(startQuarter, durationQuarter, note, tonicMidi, channel, velocity)
                };
            }

            var upper = WithPitch(note, UpperDiatonicPitch(note.Pitch));
            var segmentDuration = durationQuarter / segmentCount;
            var events = new List<ScheduledMidiNote>(segmentCount);
            for (var i = 0; i < segmentCount; i++)
            {
                var pitchNote = i % 2 == 0 ? note : upper;
                events.Add(MakeEvent(
                    startQuarter + i * segmentDuration,
                    segmentDuration,
                    pitchNote,
                    tonicMidi,
                    channel,
                    velocity));
            }

            return events;
        }

        private static List<ScheduledMidiNote> BuildTurn(
            JianpuNote note,
            double startQuarter,
            double durationQuarter,
            int tonicMidi,
            int channel,
            int velocity)
        {
            var pattern = new[]
            {
                WithPitch(note, UpperDiatonicPitch(note.Pitch)),
                note,
                WithPitch(note, LowerDiatonicPitch(note.Pitch)),
                note
            };
            return BuildPattern(pattern, startQuarter, durationQuarter, tonicMidi, channel, velocity);
        }

        private static List<ScheduledMidiNote> BuildMordent(
            JianpuNote note,
            double startQuarter,
            double durationQuarter,
            int tonicMidi,
            int channel,
            int velocity)
        {
            var pattern = new[]
            {
                note,
                WithPitch(note, LowerDiatonicPitch(note.Pitch)),
                note
            };
            return BuildPattern(pattern, startQuarter, durationQuarter, tonicMidi, channel, velocity);
        }

        private static List<ScheduledMidiNote> BuildPattern(
            JianpuNote[] pattern,
            double startQuarter,
            double durationQuarter,
            int tonicMidi,
            int channel,
            int velocity)
        {
            if (pattern == null || pattern.Length == 0)
            {
                return new List<ScheduledMidiNote>();
            }

            var segmentDuration = durationQuarter / pattern.Length;
            var events = new List<ScheduledMidiNote>(pattern.Length);
            for (var i = 0; i < pattern.Length; i++)
            {
                events.Add(MakeEvent(
                    startQuarter + i * segmentDuration,
                    segmentDuration,
                    pattern[i],
                    tonicMidi,
                    channel,
                    velocity));
            }

            return events;
        }

        private static int ResolveTrillSegmentCount(double durationQuarter, JianpuOrnament ornament)
        {
            var countText = OrnamentService.GetParameter(ornament, OrnamentService.ParamCount);
            if (int.TryParse(countText, out var count) && count >= 2)
            {
                return count;
            }

            return Math.Max(4, (int)Math.Floor(durationQuarter / TrillSegmentQuarter));
        }

        private static JianpuNote BuildGraceNote(JianpuNote main, JianpuOrnament ornament)
        {
            return WithPitch(main, ResolveGracePitch(main, ornament));
        }

        private static int ResolveGracePitch(JianpuNote main, JianpuOrnament ornament)
        {
            var pitchText = OrnamentService.GetParameter(ornament, OrnamentService.ParamPitch);
            if (int.TryParse(pitchText, out var pitch) && pitch >= 1 && pitch <= 7)
            {
                return pitch;
            }

            var direction = OrnamentService.GetParameter(ornament, OrnamentService.ParamDirection);
            if (string.Equals(direction, "up", StringComparison.OrdinalIgnoreCase))
            {
                return UpperDiatonicPitch(main.Pitch);
            }

            return LowerDiatonicPitch(main.Pitch);
        }

        private static JianpuNote WithPitch(JianpuNote source, int pitch)
        {
            return new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = pitch,
                Octave = source.Octave,
                Dashes = 0,
                Underlines = 0,
                Dotted = false
            };
        }

        private static int UpperDiatonicPitch(int pitch)
        {
            return Math.Min(7, pitch + 1);
        }

        private static int LowerDiatonicPitch(int pitch)
        {
            return Math.Max(1, pitch - 1);
        }

        private static ScheduledMidiNote MakeEvent(
            double startQuarter,
            double durationQuarter,
            JianpuNote note,
            int tonicMidi,
            int channel,
            int velocity)
        {
            return new ScheduledMidiNote
            {
                StartQuarter = startQuarter,
                DurationQuarter = Math.Max(MinNoteDurationQuarter, durationQuarter),
                MidiNote = ScoreMidiSchedule.ToMelodyMidiNote(note, tonicMidi),
                Channel = channel,
                Velocity = velocity
            };
        }
    }
}
