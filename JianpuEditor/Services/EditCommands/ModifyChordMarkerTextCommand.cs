using System;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class ModifyChordMarkerTextCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly int _measureIndex;
        private readonly int _markerIndex;
        private readonly string _oldText;
        private readonly string _newText;

        public ModifyChordMarkerTextCommand(
            JianpuScore score,
            IAppMessenger messenger,
            int measureIndex,
            int markerIndex,
            string oldText,
            string newText)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            _score = score;
            _messenger = messenger;
            _measureIndex = measureIndex;
            _markerIndex = markerIndex;
            _oldText = oldText ?? string.Empty;
            _newText = newText ?? string.Empty;
            Description = "更新和弦标识";
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            ApplyText(_newText, "已更新和弦标识");
        }

        public void Undo()
        {
            ApplyText(_oldText, "已撤回: " + Description);
        }

        private void ApplyText(string text, string message)
        {
            if (_measureIndex < 0 || _measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var measure = _score.Measures[_measureIndex];
            if (_markerIndex < 0 || _markerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            measure.ChordMarkers[_markerIndex].Text = text;
            _messenger.Send(new ScoreEditedMessage(message, stopPlayback: false));
            Result = ScoreEditResult.WithMessage(message);
        }
    }
}
