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

            measure.Ornaments = normalized
                .OrderBy(item => item.BeatPosition)
                .ThenBy(item => item.NoteIndex)
                .ThenBy(item => item.Type)
                .ToList();
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
