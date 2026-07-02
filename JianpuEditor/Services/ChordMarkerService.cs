using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class ChordMarkerService
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

            if (measure.ChordMarkers == null)
            {
                measure.ChordMarkers = new List<ChordMarker>();
            }

            TrimAndSort(measure);
        }

        public static void ImportLegacyChordText(JianpuMeasure measure, string legacyText)
        {
            if (measure == null || string.IsNullOrWhiteSpace(legacyText))
            {
                return;
            }

            if (measure.ChordMarkers == null)
            {
                measure.ChordMarkers = new List<ChordMarker>();
            }

            if (measure.ChordMarkers.Count > 0)
            {
                return;
            }

            var tokens = Regex.Split(legacyText.Trim(), @"\s+")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Take(JianpuMeasure.MaxChordMarkers)
                .ToList();
            if (tokens.Count == 0)
            {
                return;
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            var slotDuration = duration / tokens.Count;
            measure.ChordMarkers.Clear();
            for (var i = 0; i < tokens.Count; i++)
            {
                measure.ChordMarkers.Add(new ChordMarker
                {
                    Text = tokens[i],
                    BeatPosition = SnapBeatPosition(i * slotDuration, duration)
                });
            }
        }

        public static bool TryAddMarker(JianpuMeasure measure, double beatPosition, string text = "")
        {
            NormalizeMeasure(measure);
            if (measure.ChordMarkers.Count >= JianpuMeasure.MaxChordMarkers)
            {
                return false;
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            var snapped = SnapBeatPosition(beatPosition, duration);
            if (IsBeatOccupied(measure, snapped, -1))
            {
                snapped = FindNextFreeBeat(measure, snapped, duration);
                if (snapped < 0)
                {
                    return false;
                }
            }

            measure.ChordMarkers.Add(new ChordMarker
            {
                Text = text ?? string.Empty,
                BeatPosition = snapped
            });
            TrimAndSort(measure);
            return true;
        }

        public static bool TryRemoveMarker(JianpuMeasure measure, int markerIndex)
        {
            if (measure?.ChordMarkers == null || markerIndex < 0 || markerIndex >= measure.ChordMarkers.Count)
            {
                return false;
            }

            measure.ChordMarkers.RemoveAt(markerIndex);
            return true;
        }

        public static void SetMarkerBeat(JianpuMeasure measure, int markerIndex, double beatPosition)
        {
            if (measure?.ChordMarkers == null || markerIndex < 0 || markerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            var snapped = SnapBeatPosition(beatPosition, duration);
            var marker = measure.ChordMarkers[markerIndex];
            var otherIndex = FindMarkerAtBeat(measure, snapped, markerIndex);
            if (otherIndex >= 0)
            {
                var other = measure.ChordMarkers[otherIndex];
                var temp = other.BeatPosition;
                other.BeatPosition = marker.BeatPosition;
                marker.BeatPosition = temp;
            }
            else
            {
                marker.BeatPosition = snapped;
            }

            TrimAndSort(measure);
        }

        public static double SnapBeatPosition(double beatPosition, double measureDuration)
        {
            if (measureDuration <= 0)
            {
                measureDuration = ScoreMidiSchedule.DefaultMeasureBeats;
            }

            var maxBeat = Math.Max(0, measureDuration - 1);
            var snapped = Math.Round(beatPosition);
            return Math.Max(0, Math.Min(maxBeat, snapped));
        }

        public static double MapXToBeat(int measureX, int measureWidth, int x, double measureDuration)
        {
            if (measureWidth <= 0)
            {
                return 0;
            }

            if (measureDuration <= 0)
            {
                measureDuration = ScoreMidiSchedule.DefaultMeasureBeats;
            }

            var relative = (x - measureX) / (double)measureWidth * measureDuration;
            return SnapBeatPosition(relative, measureDuration);
        }

        private static void TrimAndSort(JianpuMeasure measure)
        {
            if (measure.ChordMarkers == null)
            {
                measure.ChordMarkers = new List<ChordMarker>();
                return;
            }

            if (measure.ChordMarkers.Count > JianpuMeasure.MaxChordMarkers)
            {
                measure.ChordMarkers = measure.ChordMarkers
                    .OrderBy(marker => marker.BeatPosition)
                    .Take(JianpuMeasure.MaxChordMarkers)
                    .ToList();
            }

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            foreach (var marker in measure.ChordMarkers)
            {
                marker.BeatPosition = SnapBeatPosition(marker.BeatPosition, duration);
                marker.Text = marker.Text ?? string.Empty;
            }

            measure.ChordMarkers = measure.ChordMarkers
                .OrderBy(marker => marker.BeatPosition)
                .ThenBy(marker => marker.Text)
                .ToList();
        }

        private static bool IsBeatOccupied(JianpuMeasure measure, double beatPosition, int excludeIndex)
        {
            return FindMarkerAtBeat(measure, beatPosition, excludeIndex) >= 0;
        }

        private static int FindMarkerAtBeat(JianpuMeasure measure, double beatPosition, int excludeIndex)
        {
            for (var i = 0; i < measure.ChordMarkers.Count; i++)
            {
                if (i == excludeIndex)
                {
                    continue;
                }

                if (Math.Abs(measure.ChordMarkers[i].BeatPosition - beatPosition) < 0.001)
                {
                    return i;
                }
            }

            return -1;
        }

        private static double FindNextFreeBeat(JianpuMeasure measure, double preferredBeat, double measureDuration)
        {
            var maxBeat = (int)Math.Max(0, Math.Round(measureDuration) - 1);
            var start = (int)Math.Round(preferredBeat);
            for (var offset = 0; offset <= maxBeat; offset++)
            {
                var right = start + offset;
                if (right <= maxBeat && !IsBeatOccupied(measure, right, -1))
                {
                    return right;
                }

                var left = start - offset;
                if (offset > 0 && left >= 0 && !IsBeatOccupied(measure, left, -1))
                {
                    return left;
                }
            }

            return -1;
        }
    }
}