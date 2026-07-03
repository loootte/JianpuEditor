using System;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class ScoreSnapshotEditCommand : INoteEditCommand
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly MeasureNavigationViewModel _navigation;
        private readonly IAppMessenger _messenger;
        private readonly JianpuScore _backup;
        private readonly int _backupMeasureIndex;
        private readonly Func<ScoreEditResult> _apply;

        public ScoreSnapshotEditCommand(
            ScoreDocumentViewModel document,
            MeasureNavigationViewModel navigation,
            IAppMessenger messenger,
            Func<ScoreEditResult> apply,
            string description)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
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

            _document = document;
            _navigation = navigation;
            _messenger = messenger;
            _apply = apply;
            Description = description;
            _backup = ScoreCloneService.Clone(document.Score);
            _backupMeasureIndex = navigation.CurrentMeasureIndex;
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            Result = _apply() ?? ScoreEditResult.Unchanged;
            if (Result.Changed && !string.IsNullOrEmpty(Result.Message))
            {
                _messenger.Send(new ScoreEditedMessage(Result.Message));
            }
        }

        public void Undo()
        {
            RestoreSnapshot(_backup, _backupMeasureIndex, "已撤回: " + Description);
        }

        private void RestoreSnapshot(JianpuScore snapshot, int measureIndex, string message)
        {
            _document.Score = ScoreCloneService.Clone(snapshot);
            _navigation.SyncCurrentMeasureIndex(measureIndex);
            _messenger.Send(new ScoreEditedMessage(message));
            Result = new ScoreEditResult
            {
                Changed = true,
                Message = message,
                RequiresScoreRefresh = true,
                SelectMeasureIndex = measureIndex,
                ClearMelodySelection = true,
                ClearTieSelection = true,
                ClearChordSelection = true
            };
        }
    }
}
