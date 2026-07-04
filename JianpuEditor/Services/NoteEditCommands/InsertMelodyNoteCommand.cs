using System;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.NoteEditCommands
{
    internal sealed class InsertMelodyNoteCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly int _measureIndex;
        private readonly int _insertIndex;
        private readonly JianpuNote _note;
        private readonly string _message;
        private readonly Action _resetPendingModifiers;

        public InsertMelodyNoteCommand(
            JianpuScore score,
            IAppMessenger messenger,
            int measureIndex,
            int insertIndex,
            JianpuNote note,
            string message,
            Action resetPendingModifiers)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required.", nameof(message));
            }

            if (resetPendingModifiers == null)
            {
                throw new ArgumentNullException(nameof(resetPendingModifiers));
            }

            _score = score;
            _messenger = messenger;
            _measureIndex = measureIndex;
            _insertIndex = insertIndex;
            _note = NoteEditState.CloneNote(note);
            _message = message;
            _resetPendingModifiers = resetPendingModifiers;
            Description = message;
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            var measure = _score.Measures[_measureIndex];
            MelodyChordService.InsertSlot(measure, _insertIndex, NoteEditState.CloneNote(_note));
            _resetPendingModifiers();
            PublishEdit(_message);
        }

        public void Undo()
        {
            var measure = _score.Measures[_measureIndex];
            MelodyChordService.RemoveSlot(measure, _insertIndex);
            PublishEdit("已撤回: " + Description);
        }

        private void PublishEdit(string message)
        {
            _messenger.Send(new ScoreEditedMessage(message));
            Result = new ScoreEditResult
            {
                Changed = true,
                Message = message,
                SelectNoteMeasureIndex = _measureIndex,
                SelectNoteIndex = _insertIndex
            };
        }
    }
}
