using System;
using System.Collections.Generic;
using System.Text;
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
            LogCommandStack("Execute", command.Description);
            OnHistoryChanged();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                LogCommandStack("Undo(skipped: empty stack)", null);
                return;
            }

            var command = PopUndo();
            command.Undo();
            _redoStack.Add(command);
            LogCommandStack("Undo", command.Description);
            OnHistoryChanged();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                LogCommandStack("Redo(skipped: empty stack)", null);
                return;
            }

            var command = PopRedo();
            command.Execute();
            PushUndo(command);
            LogCommandStack("Redo", command.Description);
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
            LogCommandStack("Clear", null);
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

        private void LogCommandStack(string action, string commandDescription)
        {
            var line = EditCommandHistoryDebugFormatter.Format(action, commandDescription, _undoStack, _redoStack);
            Console.WriteLine(line);
            AppLog.Info(line);
        }
    }

    internal static class EditCommandHistoryDebugFormatter
    {
        public static string Format(string action, string commandDescription, IReadOnlyList<IEditCommand> undoStack, IReadOnlyList<IEditCommand> redoStack)
        {
            var builder = new StringBuilder();
            builder.Append("[CommandHistory] ");
            builder.Append(action);
            if (!string.IsNullOrEmpty(commandDescription))
            {
                builder.Append(" | cmd=");
                builder.Append(commandDescription);
            }

            builder.Append(" | undo(");
            builder.Append(undoStack.Count);
            builder.Append("): ");
            builder.Append(FormatStack(undoStack, "next"));
            builder.Append(" | redo(");
            builder.Append(redoStack.Count);
            builder.Append("): ");
            builder.Append(FormatStack(redoStack, "next redo"));
            return builder.ToString();
        }

        private static string FormatStack(IReadOnlyList<IEditCommand> stack, string topMarker)
        {
            if (stack == null || stack.Count == 0)
            {
                return "(empty)";
            }

            var parts = new string[stack.Count];
            for (var i = 0; i < stack.Count; i++)
            {
                var description = stack[i]?.Description ?? "(null)";
                parts[i] = i == stack.Count - 1 ? description + " <- " + topMarker : description;
            }

            return string.Join(" | ", parts);
        }
    }
}
