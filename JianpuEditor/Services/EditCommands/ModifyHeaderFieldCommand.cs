using System;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class ModifyHeaderFieldCommand : INoteEditCommand
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly IAppMessenger _messenger;
        private readonly ScoreHeaderField _field;
        private readonly string _oldStringValue;
        private readonly string _newStringValue;
        private readonly int _oldBpm;
        private readonly int _newBpm;

        public ModifyHeaderFieldCommand(
            ScoreDocumentViewModel document,
            IAppMessenger messenger,
            ScoreHeaderField field,
            string oldStringValue,
            string newStringValue,
            int oldBpm,
            int newBpm)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            _document = document;
            _messenger = messenger;
            _field = field;
            _oldStringValue = oldStringValue ?? string.Empty;
            _newStringValue = newStringValue ?? string.Empty;
            _oldBpm = oldBpm;
            _newBpm = newBpm;
            Description = "更新谱头";
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            Apply(_newStringValue, _newBpm, "已更新谱头");
        }

        public void Undo()
        {
            Apply(_oldStringValue, _oldBpm, "已撤回: " + Description);
        }

        private void Apply(string stringValue, int bpm, string message)
        {
            switch (_field)
            {
                case ScoreHeaderField.Title:
                    _document.Title = stringValue;
                    break;
                case ScoreHeaderField.KeySignature:
                    _document.KeySignature = stringValue;
                    break;
                case ScoreHeaderField.Tempo:
                    _document.Tempo = stringValue;
                    break;
                case ScoreHeaderField.Bpm:
                    _document.Bpm = bpm;
                    break;
                case ScoreHeaderField.Composer:
                    _document.Composer = stringValue;
                    break;
            }

            _messenger.Send(new ScoreEditedMessage(message, stopPlayback: false));
            Result = new ScoreEditResult
            {
                Changed = true,
                Message = message,
                RequiresScoreRefresh = true
            };
        }
    }
}
