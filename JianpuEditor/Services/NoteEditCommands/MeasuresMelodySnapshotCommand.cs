using System;
using System.Collections.Generic;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.NoteEditCommands
{
    internal sealed class MeasuresMelodySnapshotCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly Dictionary<int, List<JianpuNote>> _backup;
        private readonly Func<ScoreEditResult> _apply;
        private readonly string _description;

        public MeasuresMelodySnapshotCommand(
            JianpuScore score,
            IAppMessenger messenger,
            Func<ScoreEditResult> apply,
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

            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description is required.", nameof(description));
            }

            _score = score;
            _messenger = messenger;
            _apply = apply;
            _description = description;
            _backup = NoteEditState.CaptureAllMeasuresMelody(score);
            Description = description;
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            Result = _apply() ?? ScoreEditResult.Unchanged;
            if (Result.Changed)
            {
                _messenger.Send(new ScoreEditedMessage(Result.Message ?? Description));
            }
        }

        public void Undo()
        {
            NoteEditState.RestoreMeasuresMelody(_score, _backup);
            var message = "已撤回: " + Description;
            _messenger.Send(new ScoreEditedMessage(message));
            Result = new ScoreEditResult
            {
                Changed = true,
                Message = message,
                ClearMelodySelection = true
            };
        }
    }
}
