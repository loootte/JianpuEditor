using System;

namespace JianpuEditor.Core.Abstractions
{
    public interface IEditCommandHistory
    {
        event EventHandler HistoryChanged;

        bool CanUndo { get; }

        bool CanRedo { get; }

        int UndoCount { get; }

        int RedoCount { get; }

        void Execute(IEditCommand command);

        void Undo();

        void Redo();

        void Clear();
    }
}
