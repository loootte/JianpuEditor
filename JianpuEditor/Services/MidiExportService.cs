using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class MidiExportService
    {
        private const int TicksPerQuarter = 480;
        private const int DefaultBpm = 120;
        private const int MinBpm = 30;
        private const int MaxBpm = 300;

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

            var schedule = ScoreMidiSchedule.Build(score);
            var noteEvents = ToTickEvents(schedule.Notes);
            var tempoBpm = ClampBpm(score.Bpm);
            var track = BuildTrack(noteEvents, tempoBpm);
            WriteMidiFile(path, track);
        }

        private static List<MidiTickNoteEvent> ToTickEvents(IList<ScheduledMidiNote> notes)
        {
            var events = new List<MidiTickNoteEvent>(notes.Count);
            foreach (var note in notes)
            {
                events.Add(new MidiTickNoteEvent
                {
                    StartTicks = ToTicks(note.StartQuarter),
                    DurationTicks = Math.Max(1, ToTicks(note.DurationQuarter)),
                    MidiNote = note.MidiNote,
                    Channel = note.Channel,
                    Velocity = note.Velocity
                });
            }

            return events;
        }

        private static int ToTicks(double quarterLength)
        {
            return (int)Math.Round(quarterLength * TicksPerQuarter);
        }

        private static int ClampBpm(int bpm)
        {
            if (bpm <= 0)
            {
                return DefaultBpm;
            }

            return Math.Max(MinBpm, Math.Min(MaxBpm, bpm));
        }

        private static byte[] BuildTrack(List<MidiTickNoteEvent> noteEvents, int tempoBpm)
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

        private sealed class MidiTickNoteEvent
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