using System;
using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;

namespace JianpuEditor.Services
{
    public sealed class EditCommandHistory : IEditCommandHistory
    {
        public const int DefaultMaxDepth = 50;

        private readonly List<IEditCommand> _undoStack = new List<IEditCommand>();
        private readonly List<IEditCommand> _redoStack = new List<IEditCommand>();
        private readonly int _maxDepth;

        public EditCommandHistory(IAppMessenger messenger, int maxDepth = DefaultMaxDepth)
        {
            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            if (maxDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be at least 1.");
            }

            _maxDepth = maxDepth;
            messenger.Register<EditCommandHistory, ScoreLoadedMessage>(this, OnScoreLoaded);
        }

        public event EventHandler HistoryChanged;

        public bool CanUndo
        {
            get { return _undoStack.Count > 0; }
        }

        public bool CanRedo
        {
            get { return _redoStack.Count > 0; }
        }

        public int UndoCount
        {
            get { return _undoStack.Count; }
        }

        public int RedoCount
        {
            get { return _redoStack.Count; }
        }

        public void Execute(IEditCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            PushUndo(command);
            _redoStack.Clear();
            OnHistoryChanged();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            var command = PopUndo();
            command.Undo();
            _redoStack.Add(command);
            OnHistoryChanged();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            var command = PopRedo();
            command.Execute();
            PushUndo(command);
            OnHistoryChanged();
        }

        public void Clear()
        {
            if (_undoStack.Count == 0 && _redoStack.Count == 0)
            {
                return;
            }

            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged();
        }

        private void PushUndo(IEditCommand command)
        {
            _undoStack.Add(command);
            if (_undoStack.Count > _maxDepth)
            {
                _undoStack.RemoveAt(0);
            }
        }

        private IEditCommand PopUndo()
        {
            var index = _undoStack.Count - 1;
            var command = _undoStack[index];
            _undoStack.RemoveAt(index);
            return command;
        }

        private IEditCommand PopRedo()
        {
            var index = _redoStack.Count - 1;
            var command = _redoStack[index];
            _redoStack.RemoveAt(index);
            return command;
        }

        private void OnScoreLoaded(EditCommandHistory recipient, ScoreLoadedMessage message)
        {
            recipient.Clear();
        }

        private void OnHistoryChanged()
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}