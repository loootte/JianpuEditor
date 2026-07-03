using System;
using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IScoreUndoService
    {
        event EventHandler StackChanged;

        bool CanUndo { get; }

        bool CanRedo { get; }

        bool IsRestoring { get; }

        void RecordSnapshot(JianpuScore score);

        void DiscardLastSnapshot();

        JianpuScore PopSnapshot();

        JianpuScore PopSnapshotForUndo(JianpuScore currentScore);

        JianpuScore PopSnapshotForRedo(JianpuScore currentScore);

        void Clear();

        void EnterRestore();

        void LeaveRestore();
    }
}
