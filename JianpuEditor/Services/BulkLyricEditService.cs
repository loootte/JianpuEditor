using System;
using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Models;
using JianpuEditor.Services.EditCommands;

namespace JianpuEditor.Services
{
    public static class BulkLyricEditService
    {
        public static IReadOnlyList<string> GetLyricLines(JianpuScore score, int fromIndex, int toIndex)
        {
            if (score?.Measures == null || score.Measures.Count == 0)
            {
                return Array.Empty<string>();
            }

            var range = NormalizeRange(fromIndex, toIndex, score.Measures.Count);
            var lines = new List<string>();
            for (var i = range.fromIndex; i <= range.toIndex; i++)
            {
                lines.Add(score.Measures[i].LyricText ?? string.Empty);
            }

            return lines;
        }

        public static (int fromIndex, int toIndex) NormalizeRange(int fromIndex, int toIndex, int measureCount)
        {
            measureCount = Math.Max(1, measureCount);
            fromIndex = Math.Max(0, Math.Min(fromIndex, measureCount - 1));
            toIndex = Math.Max(0, Math.Min(toIndex, measureCount - 1));
            if (fromIndex > toIndex)
            {
                var swap = fromIndex;
                fromIndex = toIndex;
                toIndex = swap;
            }

            return (fromIndex, toIndex);
        }

        public static IReadOnlyList<IEditCommand> BuildCommands(
            JianpuScore score,
            IAppMessenger messenger,
            int fromIndex,
            int toIndex,
            IReadOnlyList<string> lyricLines,
            bool realign)
        {
            if (score?.Measures == null || messenger == null || lyricLines == null)
            {
                return Array.Empty<IEditCommand>();
            }

            var range = NormalizeRange(fromIndex, toIndex, score.Measures.Count);
            fromIndex = range.fromIndex;
            toIndex = range.toIndex;
            var count = toIndex - fromIndex + 1;
            if (lyricLines.Count != count)
            {
                throw new ArgumentException("Lyric line count must match the selected measure range.", nameof(lyricLines));
            }

            var commands = new List<IEditCommand>();
            for (var i = 0; i < count; i++)
            {
                var measureIndex = fromIndex + i;
                var measure = score.Measures[measureIndex];
                var newText = lyricLines[i] ?? string.Empty;
                var oldText = measure.LyricText ?? string.Empty;
                if (!string.Equals(oldText, newText, StringComparison.Ordinal))
                {
                    commands.Add(new ModifyLyricTextCommand(
                        score,
                        messenger,
                        measureIndex,
                        oldText,
                        newText));
                }

                if (!realign)
                {
                    continue;
                }

                var alignText = newText;
                if (string.IsNullOrWhiteSpace(alignText))
                {
                    continue;
                }

                if (!LyricAlignmentService.TryBuildAlignment(
                        measure,
                        alignText,
                        score.Ties,
                        measureIndex,
                        out var syllables,
                        out _))
                {
                    continue;
                }

                commands.Add(new AlignLyricSyllablesCommand(
                    score,
                    messenger,
                    measureIndex,
                    measure.LyricSyllables,
                    measure.LyricText,
                    syllables,
                    "对齐第 " + (measureIndex + 1) + " 小节歌词"));
            }

            return commands;
        }
    }
}
