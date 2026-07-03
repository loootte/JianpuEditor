using System;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class ScoreStateCommand : INoteEditCommand
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly MeasureNavigationViewModel _navigation;
        private readonly IAppMessenger _messenger;
        private readonly JianpuScore _undoState;
        private readonly JianpuScore _redoState;
        private readonly int _undoMeasureIndex;
        private readonly int _redoMeasureIndex;
        private readonly string _description;
        private bool _skipInitialExecute = true;

        public ScoreStateCommand(
            ScoreDocumentViewModel document,
            MeasureNavigationViewModel navigation,
            IAppMessenger messenger,
            JianpuScore undoState,
            JianpuScore redoState,
            int undoMeasureIndex,
            int redoMeasureIndex,
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

            if (undoState == null)
            {
                throw new ArgumentNullException(nameof(undoState));
            }

            if (redoState == null)
            {
                throw new ArgumentNullException(nameof(redoState));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description is required.", nameof(description));
            }

            _document = document;
            _navigation = navigation;
            _messenger = messenger;
            _undoState = ScoreCloneService.Clone(undoState);
            _redoState = ScoreCloneService.Clone(redoState);
            _undoMeasureIndex = undoMeasureIndex;
            _redoMeasureIndex = redoMeasureIndex;
            _description = description;
            Description = description;
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            if (_skipInitialExecute)
            {
                _skipInitialExecute = false;
                Result = ScoreEditResult.WithMessage(_description);
                return;
            }

            RestoreState(_redoState, _redoMeasureIndex, _description);
        }

        public void Undo()
        {
            RestoreState(_undoState, _undoMeasureIndex, "已撤回: " + _description);
        }

        private void RestoreState(JianpuScore state, int measureIndex, string message)
        {
            _document.Score = ScoreCloneService.Clone(state);
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
