using System;
using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IScoreUndoService
    {
        event EventHandler StackChanged;

        bool CanUndo { get; }

        bool IsRestoring { get; }

        void RecordSnapshot(JianpuScore score);

        void DiscardLastSnapshot();

        JianpuScore PopSnapshot();

        void Clear();

        void EnterRestore();

        void LeaveRestore();
    }
}
