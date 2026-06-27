using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class MidiExportService
    {
        private const int TicksPerQuarter = 480;
        private const int DefaultBpm = 120;
        private const int MinBpm = 30;
        private const int MaxBpm = 300;
        private const int DefaultTonicMidi = 60;
        private const int DefaultMeasureBeats = 4;
        private const int MelodyChannel = 0;
        private const int ChordChannel = 1;
        private const int MelodyVelocity = 90;
        private const int ChordVelocity = 72;
        private static readonly int[] MajorScaleOffsets = { 0, 2, 4, 5, 7, 9, 11 };

        public static void Export(JianpuScore score, string path)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            var noteEvents = BuildNoteEvents(score);
            noteEvents.AddRange(BuildChordEvents(score));
            var tempoBpm = ClampBpm(score.Bpm);
            var track = BuildTrack(noteEvents, tempoBpm);
            WriteMidiFile(path, track);
        }

        private static List<MidiNoteEvent> BuildNoteEvents(JianpuScore score)
        {
            var tonicMidi = ParseTonicMidi(score.KeySignature);
            var suppressed = BuildTieEndSet(score.Ties);
            var tieExtensionCache = new Dictionary<NotePosition, double>();
            var events = new List<MidiNoteEvent>();
            var quarterTime = 0.0;

            var measures = score.Measures ?? new List<JianpuMeasure>();
            for (var measureIndex = 0; measureIndex < measures.Count; measureIndex++)
            {
                var notes = measures[measureIndex].MelodyNotes;
                if (notes == null)
                {
                    continue;
                }

                for (var noteIndex = 0; noteIndex < notes.Count; noteIndex++)
                {
                    var note = notes[noteIndex];
                    var duration = JianpuRenderer.GetDurationUnits(note);
                    var position = new NotePosition(measureIndex, noteIndex);

                    if (suppressed.Contains(position))
                    {
                        quarterTime += duration;
                        continue;
                    }

                    if (note.Type == NoteType.Rest || note.Pitch < 1 || note.Pitch > 7)
                    {
                        quarterTime += duration;
                        continue;
                    }

                    var totalDuration = duration + GetTieExtension(score, position, tieExtensionCache);
                    var midiNote = ToMidiNoteNumber(note, tonicMidi);
                    events.Add(new MidiNoteEvent
                    {
                        StartTicks = ToTicks(quarterTime),
                        DurationTicks = Math.Max(1, ToTicks(totalDuration)),
                        MidiNote = midiNote,
                        Channel = MelodyChannel,
                        Velocity = MelodyVelocity
                    });

                    quarterTime += duration;
                }
            }

            return events;
        }

        private static List<MidiNoteEvent> BuildChordEvents(JianpuScore score)
        {
            var events = new List<MidiNoteEvent>();
            var measures = score.Measures ?? new List<JianpuMeasure>();
            var measureStart = 0.0;

            for (var measureIndex = 0; measureIndex < measures.Count; measureIndex++)
            {
                var measure = measures[measureIndex];
                var measureDuration = GetMeasureDurationUnits(measure);
                var chordSymbols = ChordParser.ExtractChordSymbols(measure.SecondaryText);
                if (chordSymbols.Count > 0)
                {
                    var chordDuration = measureDuration / chordSymbols.Count;
                    for (var chordIndex = 0; chordIndex < chordSymbols.Count; chordIndex++)
                    {
                        var chordStart = measureStart + chordDuration * chordIndex;
                        var midiNotes = ChordParser.ToBlockChordMidiNotes(chordSymbols[chordIndex]);
                        foreach (var midiNote in midiNotes)
                        {
                            events.Add(new MidiNoteEvent
                            {
                                StartTicks = ToTicks(chordStart),
                                DurationTicks = Math.Max(1, ToTicks(chordDuration)),
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

        private static double GetMeasureDurationUnits(JianpuMeasure measure)
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

        private static int ToMidiNoteNumber(JianpuNote note, int tonicMidi)
        {
            var midi = tonicMidi + MajorScaleOffsets[note.Pitch - 1] + note.Octave * 12;
            return Math.Max(0, Math.Min(127, midi));
        }

        private static int ToTicks(double quarterLength)
        {
            return (int)Math.Round(quarterLength * TicksPerQuarter);
        }

        private static int ParseTonicMidi(string keySignature)
        {
            if (string.IsNullOrWhiteSpace(keySignature))
            {
                return DefaultTonicMidi;
            }

            var text = keySignature.Trim();
            var equalIndex = text.IndexOf('=');
            if (equalIndex >= 0)
            {
                text = text.Substring(equalIndex + 1).Trim();
            }

            text = text.Replace("大调", string.Empty)
                .Replace("小调", string.Empty)
                .Replace("major", string.Empty)
                .Replace("Major", string.Empty)
                .Replace("minor", string.Empty)
                .Replace("Minor", string.Empty)
                .Trim();

            if (text.Length == 0)
            {
                return DefaultTonicMidi;
            }

            var accidental = 0;
            if (text.StartsWith("#", StringComparison.Ordinal) || text.StartsWith("＃", StringComparison.Ordinal))
            {
                accidental = 1;
                text = text.Substring(1);
            }
            else if (text.StartsWith("b", StringComparison.OrdinalIgnoreCase) || text.StartsWith("♭", StringComparison.Ordinal))
            {
                accidental = -1;
                text = text.Substring(1);
            }

            var letter = char.ToUpperInvariant(text[0]);
            int baseMidi;
            switch (letter)
            {
                case 'C': baseMidi = 60; break;
                case 'D': baseMidi = 62; break;
                case 'E': baseMidi = 64; break;
                case 'F': baseMidi = 65; break;
                case 'G': baseMidi = 67; break;
                case 'A': baseMidi = 69; break;
                case 'B': baseMidi = 71; break;
                default: return DefaultTonicMidi;
            }

            return Math.Max(0, Math.Min(127, baseMidi + accidental));
        }

        private static int ClampBpm(int bpm)
        {
            if (bpm <= 0)
            {
                return DefaultBpm;
            }

            return Math.Max(MinBpm, Math.Min(MaxBpm, bpm));
        }

        private static byte[] BuildTrack(List<MidiNoteEvent> noteEvents, int tempoBpm)
        {
            var ordered = new List<RawMidiEvent>();
            var microsecondsPerQuarter = 60_000_000 / tempoBpm;
            ordered.Add(new RawMidiEvent(0, EventType.Tempo, microsecondsPerQuarter));

            foreach (var note in noteEvents)
            {
                ordered.Add(new RawMidiEvent(note.StartTicks, EventType.NoteOn, note.MidiNote, note.Velocity, note.Channel));
                ordered.Add(new RawMidiEvent(
                    note.StartTicks + note.DurationTicks,
                    EventType.NoteOff,
                    note.MidiNote,
                    0,
                    note.Channel));
            }

            ordered.Sort((a, b) =>
            {
                var cmp = a.AbsoluteTicks.CompareTo(b.AbsoluteTicks);
                if (cmp != 0)
                {
                    return cmp;
                }

                return a.Type.CompareTo(b.Type);
            });

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                var dataStart = stream.Position;
                WriteInt32Be(writer, 0);

                var lastTick = 0L;
                foreach (var evt in ordered)
                {
                    WriteVarLength(writer, (int)Math.Max(0, evt.AbsoluteTicks - lastTick));
                    lastTick = evt.AbsoluteTicks;
                    WriteEvent(writer, evt);
                }

                WriteVarLength(writer, 0);
                writer.Write(new byte[] { 0xFF, 0x2F, 0x00 });

                var dataEnd = stream.Position;
                var trackLength = (int)(dataEnd - dataStart - 4);
                stream.Position = dataStart;
                WriteInt32Be(writer, trackLength);
                return stream.ToArray();
            }
        }

        private static void WriteEvent(BinaryWriter writer, RawMidiEvent evt)
        {
            switch (evt.Type)
            {
                case EventType.Tempo:
                    writer.Write((byte)0xFF);
                    writer.Write((byte)0x51);
                    writer.Write((byte)0x03);
                    var us = evt.Data1;
                    writer.Write((byte)((us >> 16) & 0xFF));
                    writer.Write((byte)((us >> 8) & 0xFF));
                    writer.Write((byte)(us & 0xFF));
                    break;
                case EventType.NoteOn:
                    writer.Write((byte)(0x90 | (evt.Channel & 0x0F)));
                    writer.Write((byte)evt.Data1);
                    writer.Write((byte)evt.Data2);
                    break;
                case EventType.NoteOff:
                    writer.Write((byte)(0x80 | (evt.Channel & 0x0F)));
                    writer.Write((byte)evt.Data1);
                    writer.Write((byte)evt.Data2);
                    break;
            }
        }

        private static void WriteMidiFile(string path, byte[] trackData)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                WriteInt32Be(writer, 6);
                WriteInt16Be(writer, 0);
                WriteInt16Be(writer, 1);
                WriteInt16Be(writer, TicksPerQuarter);
                writer.Write(trackData);
            }
        }

        private static void WriteInt16Be(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteInt32Be(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private static void WriteVarLength(BinaryWriter writer, int value)
        {
            var buffer = new List<byte>();
            buffer.Add((byte)(value & 0x7F));
            value >>= 7;
            while (value > 0)
            {
                buffer.Insert(0, (byte)(0x80 | (value & 0x7F)));
                value >>= 7;
            }

            foreach (var b in buffer)
            {
                writer.Write(b);
            }
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

        private sealed class MidiNoteEvent
        {
            public long StartTicks { get; set; }

            public int DurationTicks { get; set; }

            public int MidiNote { get; set; }

            public int Channel { get; set; }

            public int Velocity { get; set; }
        }

        private enum EventType
        {
            Tempo = 0,
            NoteOff = 1,
            NoteOn = 2
        }

        private sealed class RawMidiEvent : IComparable<RawMidiEvent>
        {
            public RawMidiEvent(long absoluteTicks, EventType type, int data1, int data2 = 0, int channel = 0)
            {
                AbsoluteTicks = absoluteTicks;
                Type = type;
                Data1 = data1;
                Data2 = data2;
                Channel = channel;
            }

            public long AbsoluteTicks { get; }

            public EventType Type { get; }

            public int Data1 { get; }

            public int Data2 { get; }

            public int Channel { get; }

            public int CompareTo(RawMidiEvent other)
            {
                if (other == null)
                {
                    return 1;
                }

                return AbsoluteTicks.CompareTo(other.AbsoluteTicks);
            }
        }
    }
}