using System.Collections.Generic;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal sealed class ModifyMeasureOrnamentsCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly int _measureIndex;
        private readonly List<JianpuOrnament> _oldOrnaments;
        private readonly List<JianpuOrnament> _newOrnaments;
        private readonly string _message;

        public ModifyMeasureOrnamentsCommand(
            JianpuScore score,
            IAppMessenger messenger,
            int measureIndex,
            IReadOnlyList<JianpuOrnament> oldOrnaments,
            IReadOnlyList<JianpuOrnament> newOrnaments,
            string description,
            string message)
        {
            _score = score;
            _messenger = messenger;
            _measureIndex = measureIndex;
            _oldOrnaments = OrnamentService.CloneOrnaments(oldOrnaments);
            _newOrnaments = OrnamentService.CloneOrnaments(newOrnaments);
            Description = description;
            _message = message;
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            ApplyOrnaments(_newOrnaments, _message);
        }

        public void Undo()
        {
            ApplyOrnaments(_oldOrnaments, "已撤回: " + Description);
        }

        private void ApplyOrnaments(IReadOnlyList<JianpuOrnament> ornaments, string message)
        {
            if (_measureIndex < 0 || _measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var measure = _score.Measures[_measureIndex];
            measure.Ornaments = OrnamentService.CloneOrnaments(ornaments);
            OrnamentService.NormalizeMeasure(measure);
            _messenger.Send(new ScoreEditedMessage(message, markDirty: true));
            Result = ScoreEditResult.WithMessage(message);
        }
    }
}
