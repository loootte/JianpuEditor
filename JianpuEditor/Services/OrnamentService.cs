using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class OrnamentService
    {
        public const string ParamPitch = "pitch";
        public const string ParamDirection = "direction";
        public const string ParamCount = "count";
        public const string ParamLabel = "label";

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

            if (measure.Ornaments == null)
            {
                measure.Ornaments = new List<JianpuOrnament>();
            }

            TrimAndSort(measure);
        }

        public static double ResolveBeatPosition(JianpuMeasure measure, JianpuOrnament ornament)
        {
            if (measure == null || ornament == null)
            {
                return 0;
            }

            if (ornament.NoteIndex >= 0 && ornament.NoteIndex < (measure.MelodyNotes?.Count ?? 0))
            {
                return LyricSyllableService.GetNoteBeatPosition(measure, ornament.NoteIndex);
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            return LyricSyllableService.SnapBeatPosition(ornament.BeatPosition, duration);
        }

        public static int ResolveNoteIndex(JianpuMeasure measure, JianpuOrnament ornament)
        {
            if (measure == null || ornament == null)
            {
                return -1;
            }

            if (ornament.NoteIndex >= 0 && ornament.NoteIndex < (measure.MelodyNotes?.Count ?? 0))
            {
                return ornament.NoteIndex;
            }

            return FindNoteIndexAtBeat(measure, ResolveBeatPosition(measure, ornament));
        }

        public static string GetParameter(JianpuOrnament ornament, string key)
        {
            if (ornament?.Parameters == null || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return ornament.Parameters.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
        }

        public static List<JianpuOrnament> CloneOrnaments(IReadOnlyList<JianpuOrnament> ornaments)
        {
            var clone = new List<JianpuOrnament>();
            if (ornaments == null)
            {
                return clone;
            }

            foreach (var ornament in ornaments)
            {
                if (ornament == null)
                {
                    continue;
                }

                clone.Add(new JianpuOrnament
                {
                    Type = ornament.Type,
                    NoteIndex = ornament.NoteIndex,
                    BeatPosition = ornament.BeatPosition,
                    Parameters = CloneParameters(ornament.Parameters)
                });
            }

            return clone;
        }

        public static bool HasOrnament(JianpuMeasure measure, int noteIndex, OrnamentType type)
        {
            NormalizeMeasure(measure);
            if (noteIndex < 0 || noteIndex >= (measure?.MelodyNotes?.Count ?? 0))
            {
                return false;
            }

            return measure.Ornaments.Any(item => item.NoteIndex == noteIndex && item.Type == type);
        }

        public static bool HasAnyOrnament(JianpuMeasure measure, int noteIndex)
        {
            NormalizeMeasure(measure);
            if (noteIndex < 0 || noteIndex >= (measure?.MelodyNotes?.Count ?? 0))
            {
                return false;
            }

            return measure.Ornaments.Any(item => item.NoteIndex == noteIndex);
        }

        public static bool TryAddOrnament(JianpuMeasure measure, int noteIndex, OrnamentType type)
        {
            if (measure == null || type == OrnamentType.Unknown)
            {
                return false;
            }

            NormalizeMeasure(measure);
            if (noteIndex < 0 || noteIndex >= (measure.MelodyNotes?.Count ?? 0))
            {
                return false;
            }

            var existingIndex = measure.Ornaments.FindIndex(item => item.NoteIndex == noteIndex && item.Type == type);
            if (existingIndex >= 0)
            {
                measure.Ornaments[existingIndex] = new JianpuOrnament
                {
                    Type = type,
                    NoteIndex = noteIndex,
                    BeatPosition = LyricSyllableService.GetNoteBeatPosition(measure, noteIndex)
                };
            }
            else
            {
                measure.Ornaments.Add(new JianpuOrnament
                {
                    Type = type,
                    NoteIndex = noteIndex,
                    BeatPosition = LyricSyllableService.GetNoteBeatPosition(measure, noteIndex)
                });
            }

            TrimAndSort(measure);
            return true;
        }

        public static bool TryRemoveForNote(JianpuMeasure measure, int noteIndex, OrnamentType? type = null)
        {
            if (measure == null)
            {
                return false;
            }

            NormalizeMeasure(measure);
            if (noteIndex < 0 || noteIndex >= (measure.MelodyNotes?.Count ?? 0))
            {
                return false;
            }

            var removed = measure.Ornaments.RemoveAll(item =>
                item.NoteIndex == noteIndex && (!type.HasValue || item.Type == type.Value));
            if (removed > 0)
            {
                TrimAndSort(measure);
                return true;
            }

            return false;
        }

        public static void OnNoteRemoved(JianpuMeasure measure, int removedNoteIndex)
        {
            if (measure?.Ornaments == null || measure.Ornaments.Count == 0)
            {
                return;
            }

            measure.Ornaments.RemoveAll(item => item.NoteIndex == removedNoteIndex);
            foreach (var ornament in measure.Ornaments)
            {
                if (ornament.NoteIndex > removedNoteIndex)
                {
                    ornament.NoteIndex--;
                }
            }

            TrimAndSort(measure);
        }

        public static string GetPlaceholderGlyph(OrnamentType type)
        {
            switch (type)
            {
                case OrnamentType.GraceNote:
                    return "倚";
                case OrnamentType.Trill:
                    return "tr";
                case OrnamentType.Turn:
                    return "回";
                case OrnamentType.Fermata:
                    return "延";
                case OrnamentType.Mordent:
                    return "波";
                default:
                    return type.ToString();
            }
        }

        private static void TrimAndSort(JianpuMeasure measure)
        {
            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            var noteCount = measure.MelodyNotes?.Count ?? 0;
            var normalized = new List<JianpuOrnament>();
            foreach (var ornament in measure.Ornaments)
            {
                if (ornament == null || ornament.Type == OrnamentType.Unknown)
                {
                    continue;
                }

                var clone = new JianpuOrnament
                {
                    Type = ornament.Type,
                    NoteIndex = ornament.NoteIndex,
                    BeatPosition = ornament.BeatPosition,
                    Parameters = CloneParameters(ornament.Parameters)
                };

                if (clone.NoteIndex >= 0 && clone.NoteIndex < noteCount)
                {
                    clone.BeatPosition = LyricSyllableService.GetNoteBeatPosition(measure, clone.NoteIndex);
                }
                else
                {
                    clone.NoteIndex = -1;
                    clone.BeatPosition = LyricSyllableService.SnapBeatPosition(clone.BeatPosition, duration);
                }

                normalized.Add(clone);
            }

            measure.Ornaments.Clear();
            foreach (var ornament in normalized
                         .OrderBy(item => item.BeatPosition)
                         .ThenBy(item => item.NoteIndex)
                         .ThenBy(item => item.Type))
            {
                measure.Ornaments.Add(ornament);
            }
        }

        private static Dictionary<string, string> CloneParameters(Dictionary<string, string> parameters)
        {
            var clone = new Dictionary<string, string>(StringComparer.Ordinal);
            if (parameters == null)
            {
                return clone;
            }

            foreach (var pair in parameters)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                clone[pair.Key] = pair.Value ?? string.Empty;
            }

            return clone;
        }

        private static int FindNoteIndexAtBeat(JianpuMeasure measure, double beatPosition)
        {
            var notes = measure?.MelodyNotes;
            if (notes == null || notes.Count == 0)
            {
                return -1;
            }

            var beat = 0.0;
            for (var i = 0; i < notes.Count; i++)
            {
                if (Math.Abs(beat - beatPosition) < 0.001)
                {
                    return i;
                }

                beat += JianpuRenderer.GetDurationUnits(notes[i]);
            }

            return -1;
        }
    }
}
