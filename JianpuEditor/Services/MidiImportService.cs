using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class MidiImportService
    {
        private const double QuantizeGrid = 0.25;
        private const double DurationEpsilon = 0.02;
        private const int DrumChannel = 9;

        private static readonly int[] MajorScaleOffsets = { 0, 2, 4, 5, 7, 9, 11 };
        private static readonly string[] TonicNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static void ImportToJianpuFile(string midiPath, string jianpuPath)
        {
            var score = Import(midiPath);
            ScoreFileService.Save(score, jianpuPath);
        }

        public static JianpuScore Import(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            var file = MidiFileReader.Read(path);
            var track = SelectMelodyTrack(file);
            var notes = ExtractNotes(track, file.TicksPerQuarter);
            if (notes.Count == 0)
            {
                throw new InvalidOperationException("MIDI 文件未包含可导入的旋律音符。");
            }

            var bpm = track.TempoChanges.Count > 0
                ? track.TempoChanges[0].Bpm
                : 120;
            var tonicMidi = DetectTonicMidi(notes);
            var keySignature = KeySignatureService.FormatKeySignature(tonicMidi % 12);
            var measures = BuildMeasures(notes, tonicMidi, ScoreMidiSchedule.DefaultMeasureBeats);
            measures = MeasureNormalizationService.NormalizeMeasures(measures, ScoreMidiSchedule.DefaultMeasureBeats);

            return new JianpuScore
            {
                Title = Path.GetFileNameWithoutExtension(path) ?? "MIDI 导入",
                KeySignature = keySignature,
                Tempo = "中速",
                Bpm = bpm,
                Composer = string.Empty,
                Measures = measures,
                Ties = new List<JianpuTie>()
            };
        }

        private static ParsedTrack SelectMelodyTrack(MidiFileData file)
        {
            if (file.Tracks.Count == 1)
            {
                return file.Tracks[0];
            }

            ParsedTrack best = null;
            var bestCount = -1;
            foreach (var track in file.Tracks)
            {
                var count = track.NoteOnEvents.Count(item => item.Channel != DrumChannel);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = track;
                }
            }

            return best ?? file.Tracks[0];
        }

        private static List<ImportedNote> ExtractNotes(ParsedTrack track, int ticksPerQuarter)
        {
            var active = new Dictionary<NoteKey, NoteOnEvent>();
            var notes = new List<ImportedNote>();

            foreach (var evt in track.Events.OrderBy(item => item.Ticks))
            {
                if (evt.Type == MidiTrackEventType.NoteOn && evt.Velocity > 0 && evt.Channel != DrumChannel)
                {
                    active[new NoteKey(evt.Channel, evt.NoteNumber)] = new NoteOnEvent
                    {
                        Ticks = evt.Ticks,
                        Channel = evt.Channel,
                        NoteNumber = evt.NoteNumber,
                        Velocity = evt.Velocity
                    };
                    continue;
                }

                if (evt.Type == MidiTrackEventType.NoteOff
                    || (evt.Type == MidiTrackEventType.NoteOn && evt.Velocity == 0))
                {
                    var key = new NoteKey(evt.Channel, evt.NoteNumber);
                    if (!active.TryGetValue(key, out var start))
                    {
                        continue;
                    }

                    active.Remove(key);
                    var durationTicks = Math.Max(1, evt.Ticks - start.Ticks);
                    notes.Add(new ImportedNote
                    {
                        Channel = start.Channel,
                        MidiNote = start.NoteNumber,
                        StartQuarter = start.Ticks / (double)ticksPerQuarter,
                        DurationQuarter = QuantizeDuration(durationTicks / (double)ticksPerQuarter)
                    });
                }
            }

            return notes
                .Where(item => item.Channel == ScoreMidiSchedule.MelodyChannel)
                .OrderBy(item => item.StartQuarter)
                .ThenBy(item => item.MidiNote)
                .ToList();
        }

        private static double QuantizeDuration(double quarterLength)
        {
            if (quarterLength <= DurationEpsilon)
            {
                return QuantizeGrid;
            }

            var quantized = Math.Round(quarterLength / QuantizeGrid) * QuantizeGrid;
            return Math.Max(QuantizeGrid, quantized);
        }

        private static int DetectTonicMidi(IReadOnlyList<ImportedNote> notes)
        {
            var bestTonic = ScoreMidiSchedule.DefaultTonicMidi;
            var bestScore = int.MinValue;
            for (var tonic = 0; tonic < 12; tonic++)
            {
                var score = 0;
                foreach (var note in notes)
                {
                    if (TryMidiToJianpu(note.MidiNote, 60 + tonic, out _, out _, out _, out var error))
                    {
                        score += Math.Max(0, 3 - error);
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTonic = 60 + tonic;
                }
            }

            return bestTonic;
        }

        private static List<JianpuMeasure> BuildMeasures(
            IReadOnlyList<ImportedNote> notes,
            int tonicMidi,
            int measureBeats)
        {
            var measures = new List<JianpuMeasure>();
            var current = CreateMeasure();
            var measureStart = 0.0;
            var cursor = 0.0;

            foreach (var group in GroupNotesByStart(notes))
            {
                if (group.Count == 0)
                {
                    continue;
                }

                var groupStart = group[0].StartQuarter;
                if (groupStart > cursor + DurationEpsilon)
                {
                    AppendRests(current, groupStart - cursor, ref measureStart, measureBeats, measures, ref current, ref cursor);
                }

                var jianpuNotes = new List<JianpuNote>();
                var durationUnits = 0.0;
                foreach (var note in group)
                {
                    if (!TryMidiToJianpu(
                            note.MidiNote,
                            tonicMidi,
                            out var pitch,
                            out var octave,
                            out var accidental,
                            out _))
                    {
                        continue;
                    }

                    var jianpuNote = CreateNote(pitch, accidental, octave, note.DurationQuarter);
                    if (jianpuNote == null)
                    {
                        continue;
                    }

                    jianpuNotes.Add(jianpuNote);
                    durationUnits = Math.Max(durationUnits, JianpuRenderer.GetDurationUnits(jianpuNote));
                }

                if (jianpuNotes.Count == 0)
                {
                    continue;
                }

                foreach (var jianpuNote in jianpuNotes)
                {
                    ApplyDurationUnits(jianpuNote, durationUnits);
                }

                var measureUsed = cursor - measureStart;
                if (measureUsed + durationUnits > measureBeats + DurationEpsilon)
                {
                    AppendRests(current, measureBeats - measureUsed, ref measureStart, measureBeats, measures, ref current, ref cursor);
                }

                var beatPosition = cursor - measureStart;
                var chord = MelodyChordService.CreateChord(beatPosition, jianpuNotes);
                MelodyChordService.AppendChord(current, chord);
                cursor += durationUnits;
            }

            if (current.MelodyNotes.Count > 0)
            {
                measures.Add(current);
            }

            if (measures.Count == 0)
            {
                measures.Add(CreateMeasure());
            }

            return measures;
        }

        private static List<List<ImportedNote>> GroupNotesByStart(IReadOnlyList<ImportedNote> notes)
        {
            var groups = new List<List<ImportedNote>>();
            if (notes == null || notes.Count == 0)
            {
                return groups;
            }

            List<ImportedNote> current = null;
            double? currentStart = null;
            foreach (var note in notes)
            {
                if (current == null || Math.Abs(note.StartQuarter - currentStart.Value) > DurationEpsilon)
                {
                    current = new List<ImportedNote> { note };
                    groups.Add(current);
                    currentStart = note.StartQuarter;
                }
                else
                {
                    current.Add(note);
                }
            }

            return groups;
        }

        private static void AppendRests(
            JianpuMeasure measure,
            double gap,
            ref double measureStart,
            int measureBeats,
            List<JianpuMeasure> measures,
            ref JianpuMeasure current,
            ref double cursor)
        {
            while (gap > DurationEpsilon)
            {
                var measureUsed = cursor - measureStart;
                var room = measureBeats - measureUsed;
                if (room <= DurationEpsilon)
                {
                    measures.Add(current);
                    current = CreateMeasure();
                    measureStart = cursor;
                    room = measureBeats;
                }

                var restDuration = Math.Min(gap, room);
                var rest = CreateRest(restDuration);
                if (rest == null)
                {
                    break;
                }

                MelodyChordService.AppendChord(
                    current,
                    MelodyChordService.CreateChord(
                        cursor - measureStart,
                        new[] { rest }));
                var units = JianpuRenderer.GetDurationUnits(rest);
                cursor += units;
                gap -= units;
            }
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

        private static JianpuNote CreateNote(
            double pitch,
            AccidentalKind accidental,
            int octave,
            double durationUnits)
        {
            var note = new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = pitch,
                Accidental = accidental,
                Octave = octave
            };
            return ApplyDurationUnits(note, durationUnits) ? note : null;
        }

        private static JianpuNote CreateRest(double durationUnits)
        {
            var note = new JianpuNote { Type = NoteType.Rest, Pitch = 0, Octave = 0 };
            return ApplyDurationUnits(note, durationUnits) ? note : null;
        }

        internal static bool TryMidiToJianpu(
            int midiNote,
            int tonicMidi,
            out double pitch,
            out int octave,
            out AccidentalKind accidental,
            out int semitoneError)
        {
            pitch = 1;
            octave = 0;
            accidental = AccidentalKind.None;
            semitoneError = 127;
            var bestError = 127;
            var bestDegree = 1;
            var bestCandidate = tonicMidi;
            int? sharpDegree = null;
            var sharpOctave = 0;
            int? flatDegree = null;
            var flatOctave = 0;

            for (var octaveDot = -1; octaveDot <= 1; octaveDot++)
            {
                for (var degree = 1; degree <= 7; degree++)
                {
                    var candidate = tonicMidi + MajorScaleOffsets[degree - 1] + octaveDot * 12;
                    var error = Math.Abs(candidate - midiNote);
                    if (error < bestError)
                    {
                        bestError = error;
                        bestDegree = degree;
                        bestCandidate = candidate;
                        octave = octaveDot;
                        semitoneError = error;
                        sharpDegree = null;
                        flatDegree = null;
                    }

                    if (error != 1)
                    {
                        continue;
                    }

                    if (midiNote > candidate)
                    {
                        sharpDegree = degree;
                        sharpOctave = octaveDot;
                    }
                    else if (midiNote < candidate)
                    {
                        flatDegree = degree;
                        flatOctave = octaveDot;
                    }
                }
            }

            if (bestError == 0)
            {
                pitch = bestDegree;
                accidental = AccidentalKind.None;
                return true;
            }

            if (bestError == 1)
            {
                if (sharpDegree.HasValue && flatDegree.HasValue)
                {
                    var lowerDegree = Math.Min(sharpDegree.Value, flatDegree.Value - 1);
                    if (PreferFlatAccidental(lowerDegree))
                    {
                        accidental = AccidentalKind.Flat;
                        pitch = (flatDegree.Value - 1) + JianpuPitchCodec.AccidentalFraction;
                        octave = flatOctave;
                    }
                    else
                    {
                        accidental = AccidentalKind.Sharp;
                        pitch = sharpDegree.Value + JianpuPitchCodec.AccidentalFraction;
                        octave = sharpOctave;
                    }
                }
                else if (sharpDegree.HasValue)
                {
                    accidental = AccidentalKind.Sharp;
                    pitch = sharpDegree.Value + JianpuPitchCodec.AccidentalFraction;
                    octave = sharpOctave;
                }
                else if (flatDegree.HasValue)
                {
                    accidental = AccidentalKind.Flat;
                    pitch = (flatDegree.Value - 1) + JianpuPitchCodec.AccidentalFraction;
                    octave = flatOctave;
                }
                else if (midiNote > bestCandidate)
                {
                    accidental = AccidentalKind.Sharp;
                    pitch = bestDegree + JianpuPitchCodec.AccidentalFraction;
                }
                else
                {
                    accidental = AccidentalKind.Flat;
                    pitch = (bestDegree - 1) + JianpuPitchCodec.AccidentalFraction;
                }

                return true;
            }

            return false;
        }

        private static bool PreferFlatAccidental(int lowerDegree)
        {
            return lowerDegree == 2 || lowerDegree == 4 || lowerDegree == 6;
        }

        internal static bool ApplyDurationUnits(JianpuNote note, double units)
        {
            if (note == null || units <= 0)
            {
                return false;
            }

            note.Underlines = 0;
            note.Dashes = 0;
            note.Dotted = false;

            if (Math.Abs(units - 0.25) < DurationEpsilon)
            {
                note.Underlines = 2;
                return true;
            }

            if (Math.Abs(units - 0.5) < DurationEpsilon)
            {
                note.Underlines = 1;
                return true;
            }

            if (Math.Abs(units - 0.75) < DurationEpsilon)
            {
                note.Underlines = 1;
                note.Dotted = true;
                return true;
            }

            if (Math.Abs(units - 1.0) < DurationEpsilon)
            {
                return true;
            }

            if (Math.Abs(units - 1.5) < DurationEpsilon)
            {
                note.Dotted = true;
                return true;
            }

            if (units >= 1.0 - DurationEpsilon)
            {
                var rounded = (int)Math.Round(units, MidpointRounding.AwayFromZero);
                if (Math.Abs(units - rounded) < DurationEpsilon && rounded >= 1 && rounded <= 4)
                {
                    note.Dashes = rounded - 1;
                    return true;
                }
            }

            note.Underlines = 1;
            return true;
        }

        private sealed class ImportedNote
        {
            public int Channel { get; set; }

            public int MidiNote { get; set; }

            public double StartQuarter { get; set; }

            public double DurationQuarter { get; set; }
        }

        private readonly struct NoteKey : IEquatable<NoteKey>
        {
            public NoteKey(int channel, int noteNumber)
            {
                Channel = channel;
                NoteNumber = noteNumber;
            }

            public int Channel { get; }

            public int NoteNumber { get; }

            public bool Equals(NoteKey other)
            {
                return Channel == other.Channel && NoteNumber == other.NoteNumber;
            }

            public override bool Equals(object obj)
            {
                return obj is NoteKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Channel * 397) ^ NoteNumber;
                }
            }
        }

        private sealed class NoteOnEvent
        {
            public long Ticks { get; set; }

            public int Channel { get; set; }

            public int NoteNumber { get; set; }

            public int Velocity { get; set; }
        }

        private enum MidiTrackEventType
        {
            NoteOn,
            NoteOff,
            Meta
        }

        private sealed class MidiTrackEvent
        {
            public long Ticks { get; set; }

            public MidiTrackEventType Type { get; set; }

            public int Channel { get; set; }

            public int NoteNumber { get; set; }

            public int Velocity { get; set; }
        }

        private sealed class TempoChange
        {
            public long Ticks { get; set; }

            public int Bpm { get; set; }
        }

        private sealed class ParsedTrack
        {
            public List<MidiTrackEvent> Events { get; } = new List<MidiTrackEvent>();

            public List<NoteOnEvent> NoteOnEvents { get; } = new List<NoteOnEvent>();

            public List<TempoChange> TempoChanges { get; } = new List<TempoChange>();
        }

        private sealed class MidiFileData
        {
            public int Format { get; set; }

            public int TicksPerQuarter { get; set; }

            public List<ParsedTrack> Tracks { get; } = new List<ParsedTrack>();
        }

        private static class MidiFileReader
        {
            public static MidiFileData Read(string path)
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    var header = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (header != "MThd")
                    {
                        throw new InvalidOperationException("不是有效的 MIDI 文件（缺少 MThd）。");
                    }

                    var headerLength = ReadInt32Be(reader);
                    if (headerLength < 6)
                    {
                        throw new InvalidOperationException("MIDI 文件头损坏。");
                    }

                    var format = ReadInt16Be(reader);
                    var trackCount = ReadInt16Be(reader);
                    var division = ReadInt16Be(reader);
                    if (headerLength > 6)
                    {
                        reader.ReadBytes(headerLength - 6);
                    }

                    if ((division & 0x8000) != 0)
                    {
                        throw new NotSupportedException("Spike 暂不支持 SMPTE 时间码格式的 MIDI。");
                    }

                    var data = new MidiFileData
                    {
                        Format = format,
                        TicksPerQuarter = division
                    };

                    for (var i = 0; i < trackCount; i++)
                    {
                        data.Tracks.Add(ReadTrack(reader));
                    }

                    return data;
                }
            }

            private static ParsedTrack ReadTrack(BinaryReader reader)
            {
                var marker = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (marker != "MTrk")
                {
                    throw new InvalidOperationException("MIDI 轨道块损坏（缺少 MTrk）。");
                }

                var trackLength = ReadInt32Be(reader);
                var end = reader.BaseStream.Position + trackLength;
                var track = new ParsedTrack();
                long absoluteTicks = 0;
                byte? runningStatus = null;

                while (reader.BaseStream.Position < end)
                {
                    var delta = ReadVarLength(reader);
                    absoluteTicks += delta;
                    if (reader.BaseStream.Position >= end)
                    {
                        break;
                    }

                    var status = reader.ReadByte();
                    if (status < 0x80)
                    {
                        if (!runningStatus.HasValue)
                        {
                            throw new InvalidOperationException("MIDI 事件流损坏（缺少状态字节）。");
                        }

                        reader.BaseStream.Position--;
                        status = runningStatus.Value;
                    }
                    else
                    {
                        if (status < 0xF0)
                        {
                            runningStatus = status;
                        }
                    }

                    if (status == 0xFF)
                    {
                        var metaType = reader.ReadByte();
                        var length = ReadVarLength(reader);
                        if (metaType == 0x51 && length == 3)
                        {
                            var b1 = reader.ReadByte();
                            var b2 = reader.ReadByte();
                            var b3 = reader.ReadByte();
                            var usPerQuarter = (b1 << 16) | (b2 << 8) | b3;
                            if (usPerQuarter > 0)
                            {
                                track.TempoChanges.Add(new TempoChange
                                {
                                    Ticks = absoluteTicks,
                                    Bpm = Math.Max(30, Math.Min(300, 60_000_000 / usPerQuarter))
                                });
                            }
                        }
                        else
                        {
                            reader.ReadBytes(length);
                        }

                        continue;
                    }

                    if (status == 0xF0 || status == 0xF7)
                    {
                        var length = ReadVarLength(reader);
                        reader.ReadBytes(length);
                        continue;
                    }

                    var eventType = status & 0xF0;
                    var channel = status & 0x0F;
                    if (eventType == 0x90)
                    {
                        var note = reader.ReadByte();
                        var velocity = reader.ReadByte();
                        var evt = new MidiTrackEvent
                        {
                            Ticks = absoluteTicks,
                            Type = MidiTrackEventType.NoteOn,
                            Channel = channel,
                            NoteNumber = note,
                            Velocity = velocity
                        };
                        track.Events.Add(evt);
                        if (velocity > 0)
                        {
                            track.NoteOnEvents.Add(new NoteOnEvent
                            {
                                Ticks = absoluteTicks,
                                Channel = channel,
                                NoteNumber = note,
                                Velocity = velocity
                            });
                        }
                    }
                    else if (eventType == 0x80)
                    {
                        var note = reader.ReadByte();
                        reader.ReadByte();
                        track.Events.Add(new MidiTrackEvent
                        {
                            Ticks = absoluteTicks,
                            Type = MidiTrackEventType.NoteOff,
                            Channel = channel,
                            NoteNumber = note,
                            Velocity = 0
                        });
                    }
                    else if (eventType == 0xA0 || eventType == 0xB0 || eventType == 0xE0)
                    {
                        reader.ReadBytes(2);
                    }
                    else if (eventType == 0xC0 || eventType == 0xD0)
                    {
                        reader.ReadByte();
                    }
                }

                return track;
            }

            private static int ReadInt16Be(BinaryReader reader)
            {
                var b1 = reader.ReadByte();
                var b2 = reader.ReadByte();
                return (b1 << 8) | b2;
            }

            private static int ReadInt32Be(BinaryReader reader)
            {
                var b1 = reader.ReadByte();
                var b2 = reader.ReadByte();
                var b3 = reader.ReadByte();
                var b4 = reader.ReadByte();
                return (b1 << 24) | (b2 << 16) | (b3 << 8) | b4;
            }

            private static int ReadVarLength(BinaryReader reader)
            {
                var value = 0;
                while (true)
                {
                    var b = reader.ReadByte();
                    value = (value << 7) | (b & 0x7F);
                    if ((b & 0x80) == 0)
                    {
                        return value;
                    }
                }
            }
        }
    }
}
