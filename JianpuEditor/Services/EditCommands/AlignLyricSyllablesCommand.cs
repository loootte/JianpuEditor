using System;
using System.Collections.Generic;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class AlignLyricSyllablesCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly int _measureIndex;
        private readonly List<LyricSyllable> _oldSyllables;
        private readonly string _oldLyricText;
        private readonly List<LyricSyllable> _newSyllables;

        public AlignLyricSyllablesCommand(
            JianpuScore score,
            IAppMessenger messenger,
            int measureIndex,
            List<LyricSyllable> oldSyllables,
            string oldLyricText,
            List<LyricSyllable> newSyllables,
            string description)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            if (newSyllables == null)
            {
                throw new ArgumentNullException(nameof(newSyllables));
            }

            _score = score;
            _messenger = messenger;
            _measureIndex = measureIndex;
            _oldSyllables = CloneSyllables(oldSyllables);
            _oldLyricText = oldLyricText ?? string.Empty;
            _newSyllables = CloneSyllables(newSyllables);
            Description = description ?? "歌词对齐";
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            Apply(_newSyllables, Description);
        }

        public void Undo()
        {
            Apply(_oldSyllables, "已撤回: " + Description);
        }

        private void Apply(List<LyricSyllable> syllables, string message)
        {
            if (_measureIndex < 0 || _measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var measure = _score.Measures[_measureIndex];
            measure.LyricSyllables = CloneSyllables(syllables);
            LyricSyllableService.NormalizeMeasure(measure);
            _messenger.Send(new ScoreEditedMessage(message, markDirty: true));
            Result = new ScoreEditResult
            {
                Changed = true,
                Message = message,
                RequiresScoreRefresh = true,
                UpdatedMeasureIndex = _measureIndex
            };
        }

        private static List<LyricSyllable> CloneSyllables(List<LyricSyllable> syllables)
        {
            var clone = new List<LyricSyllable>();
            if (syllables == null)
            {
                return clone;
            }

            for (var i = 0; i < syllables.Count; i++)
            {
                var syllable = syllables[i];
                if (syllable == null)
                {
                    continue;
                }

                clone.Add(new LyricSyllable
                {
                    Text = syllable.Text ?? string.Empty,
                    NoteIndex = syllable.NoteIndex,
                    BeatPosition = syllable.BeatPosition
                });
            }

            return clone;
        }
    }
}
