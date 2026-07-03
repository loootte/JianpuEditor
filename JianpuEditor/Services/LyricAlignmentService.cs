using System;
using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class LyricAlignmentService
    {
        public static bool TryBuildAlignment(
            JianpuMeasure measure,
            string lyricText,
            IReadOnlyList<JianpuTie> ties,
            int measureIndex,
            out List<LyricSyllable> syllables,
            out string message)
        {
            syllables = new List<LyricSyllable>();
            message = string.Empty;

            if (measure == null)
            {
                message = "小节不存在";
                return false;
            }

            if (string.IsNullOrWhiteSpace(lyricText))
            {
                message = "请先输入歌词";
                return false;
            }

            var tokens = LyricSyllableService.TokenizeLyricText(lyricText);
            if (tokens.Count == 0)
            {
                message = "歌词内容为空";
                return false;
            }

            var alignableNoteIndices = GetAlignableNoteIndices(measure, ties, measureIndex);
            if (alignableNoteIndices.Count == 0)
            {
                message = "当前小节没有可对齐的音符";
                return false;
            }

            var alignedCount = Math.Min(tokens.Count, alignableNoteIndices.Count);
            for (var i = 0; i < alignedCount; i++)
            {
                var noteIndex = alignableNoteIndices[i];
                syllables.Add(new LyricSyllable
                {
                    Text = tokens[i],
                    NoteIndex = noteIndex,
                    BeatPosition = LyricSyllableService.GetNoteBeatPosition(measure, noteIndex)
                });
            }

            message = BuildResultMessage(alignedCount, tokens.Count, alignableNoteIndices.Count);
            return alignedCount > 0;
        }

        public static List<int> GetAlignableNoteIndices(
            JianpuMeasure measure,
            IReadOnlyList<JianpuTie> ties,
            int measureIndex)
        {
            var result = new List<int>();
            var notes = measure?.MelodyNotes;
            if (notes == null || notes.Count == 0)
            {
                return result;
            }

            var tieEndIndices = GetTieEndNoteIndices(ties, measureIndex);
            for (var i = 0; i < notes.Count; i++)
            {
                if (notes[i].Type == NoteType.Rest)
                {
                    continue;
                }

                if (tieEndIndices.Contains(i))
                {
                    continue;
                }

                result.Add(i);
            }

            return result;
        }

        private static HashSet<int> GetTieEndNoteIndices(IReadOnlyList<JianpuTie> ties, int measureIndex)
        {
            var result = new HashSet<int>();
            if (ties == null)
            {
                return result;
            }

            for (var i = 0; i < ties.Count; i++)
            {
                var tie = ties[i];
                if (tie.EndMeasureIndex == measureIndex)
                {
                    result.Add(tie.EndNoteIndex);
                }
            }

            return result;
        }

        private static string BuildResultMessage(int alignedCount, int tokenCount, int alignableNoteCount)
        {
            if (tokenCount > alignableNoteCount)
            {
                return "已对齐 " + alignedCount + " 个音节（歌词超出 "
                    + (tokenCount - alignableNoteCount) + " 个字）";
            }

            if (alignableNoteCount > tokenCount)
            {
                return "已对齐 " + alignedCount + " 个音节（剩余 "
                    + (alignableNoteCount - tokenCount) + " 个音符未填词）";
            }

            return "已对齐 " + alignedCount + " 个音节";
        }
    }
}
