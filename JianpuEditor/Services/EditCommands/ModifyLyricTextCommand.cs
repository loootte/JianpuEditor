using System;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class ModifyLyricTextCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly int _measureIndex;
        private readonly string _oldText;
        private readonly string _newText;

        public ModifyLyricTextCommand(
            JianpuScore score,
            IAppMessenger messenger,
            int measureIndex,
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
            _oldText = oldText ?? string.Empty;
            _newText = newText ?? string.Empty;
            Description = "更新歌词";
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            ApplyText(_newText, "已更新歌词");
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

            _score.Measures[_measureIndex].LyricText = text;
            _messenger.Send(new ScoreEditedMessage(message, markDirty: true));
            Result = ScoreEditResult.WithMessage(message);
        }
    }
}
