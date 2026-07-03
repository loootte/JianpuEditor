using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class LyricSyllableService
    {
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

            if (measure.LyricSyllables == null)
            {
                measure.LyricSyllables = new List<LyricSyllable>();
            }

            measure.LyricText = measure.LyricText ?? string.Empty;
            TrimAndSort(measure);
        }

        public static bool HasStructuredLyrics(JianpuMeasure measure)
        {
            return measure?.LyricSyllables != null
                && measure.LyricSyllables.Any(syllable => !string.IsNullOrEmpty(syllable?.Text));
        }

        public static string GetDisplayText(JianpuMeasure measure)
        {
            if (measure == null)
            {
                return string.Empty;
            }

            if (HasStructuredLyrics(measure))
            {
                return string.Join(string.Empty, measure.LyricSyllables
                    .Where(syllable => !string.IsNullOrEmpty(syllable?.Text))
                    .Select(syllable => syllable.Text));
            }

            return measure.LyricText ?? string.Empty;
        }

        public static string GetFallbackText(JianpuMeasure measure)
        {
            return measure?.LyricText ?? string.Empty;
        }

        public static void ImportLegacyLyricText(JianpuMeasure measure)
        {
            if (measure == null || string.IsNullOrWhiteSpace(measure.LyricText))
            {
                return;
            }

            NormalizeMeasure(measure);
            if (HasStructuredLyrics(measure))
            {
                return;
            }

            var tokens = TokenizeLegacyLyricText(measure.LyricText);
            if (tokens.Count == 0)
            {
                return;
            }

            var noteCount = measure.MelodyNotes?.Count ?? 0;
            if (noteCount == 0)
            {
                return;
            }

            measure.LyricSyllables.Clear();
            var count = Math.Min(tokens.Count, noteCount);
            for (var i = 0; i < count; i++)
            {
                measure.LyricSyllables.Add(new LyricSyllable
                {
                    Text = tokens[i],
                    NoteIndex = i,
                    BeatPosition = GetNoteBeatPosition(measure, i)
                });
            }

            TrimAndSort(measure);
        }

        public static double GetNoteBeatPosition(JianpuMeasure measure, int noteIndex)
        {
            var notes = measure?.MelodyNotes;
            if (notes == null || noteIndex < 0 || noteIndex >= notes.Count)
            {
                return 0;
            }

            var beat = 0.0;
            for (var i = 0; i < noteIndex; i++)
            {
                beat += JianpuRenderer.GetDurationUnits(notes[i]);
            }

            return beat;
        }

        public static double ResolveBeatPosition(JianpuMeasure measure, LyricSyllable syllable)
        {
            if (measure == null || syllable == null)
            {
                return 0;
            }

            if (syllable.NoteIndex >= 0 && syllable.NoteIndex < (measure.MelodyNotes?.Count ?? 0))
            {
                return GetNoteBeatPosition(measure, syllable.NoteIndex);
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            return SnapBeatPosition(syllable.BeatPosition, duration);
        }

        public static int ResolveNoteIndex(JianpuMeasure measure, LyricSyllable syllable)
        {
            if (measure == null || syllable == null)
            {
                return -1;
            }

            if (syllable.NoteIndex >= 0 && syllable.NoteIndex < (measure.MelodyNotes?.Count ?? 0))
            {
                return syllable.NoteIndex;
            }

            return FindNoteIndexAtBeat(measure, ResolveBeatPosition(measure, syllable));
        }

        public static double SnapBeatPosition(double beatPosition, double measureDuration)
        {
            if (measureDuration <= 0)
            {
                measureDuration = ScoreMidiSchedule.DefaultMeasureBeats;
            }

            var maxBeat = Math.Max(0, measureDuration - 0.001);
            var snapped = Math.Round(beatPosition * 1000) / 1000;
            return Math.Max(0, Math.Min(maxBeat, snapped));
        }

        private static List<string> TokenizeLegacyLyricText(string lyricText)
        {
            var trimmed = lyricText?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return new List<string>();
            }

            if (Regex.IsMatch(trimmed, @"\s"))
            {
                return Regex.Split(trimmed, @"\s+")
                    .Where(token => !string.IsNullOrWhiteSpace(token))
                    .ToList();
            }

            return trimmed.ToCharArray().Select(character => character.ToString()).ToList();
        }

        private static void TrimAndSort(JianpuMeasure measure)
        {
            if (measure.LyricSyllables == null)
            {
                measure.LyricSyllables = new List<LyricSyllable>();
                return;
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            var noteCount = measure.MelodyNotes?.Count ?? 0;
            var normalized = new List<LyricSyllable>();
            foreach (var syllable in measure.LyricSyllables)
            {
                if (syllable == null || string.IsNullOrWhiteSpace(syllable.Text))
                {
                    continue;
                }

                var clone = new LyricSyllable
                {
                    Text = syllable.Text.Trim(),
                    NoteIndex = syllable.NoteIndex,
                    BeatPosition = syllable.BeatPosition
                };

                if (clone.NoteIndex >= noteCount)
                {
                    clone.NoteIndex = -1;
                }

                if (clone.NoteIndex >= 0)
                {
                    clone.BeatPosition = GetNoteBeatPosition(measure, clone.NoteIndex);
                }
                else
                {
                    clone.BeatPosition = SnapBeatPosition(clone.BeatPosition, duration);
                }

                normalized.Add(clone);
            }

            measure.LyricSyllables = normalized
                .OrderBy(syllable => syllable.BeatPosition)
                .ThenBy(syllable => syllable.NoteIndex)
                .ThenBy(syllable => syllable.Text)
                .ToList();
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
