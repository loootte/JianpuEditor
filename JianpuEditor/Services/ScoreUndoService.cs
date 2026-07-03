using System;
using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class ScoreUndoService : IScoreUndoService
    {
        public const int MaxDepth = 50;

        private readonly List<JianpuScore> _snapshots = new List<JianpuScore>();
        private readonly List<JianpuScore> _redoSnapshots = new List<JianpuScore>();
        private bool _isRestoring;

        public ScoreUndoService(IAppMessenger messenger)
        {
            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            messenger.Register<ScoreUndoService, ScoreLoadedMessage>(this, OnScoreLoaded);
        }

        public event EventHandler StackChanged;

        public bool CanUndo
        {
            get { return _snapshots.Count > 0 && !_isRestoring; }
        }

        public bool CanRedo
        {
            get { return _redoSnapshots.Count > 0 && !_isRestoring; }
        }

        public bool IsRestoring
        {
            get { return _isRestoring; }
        }

        public void RecordSnapshot(JianpuScore score)
        {
            if (_isRestoring || score == null)
            {
                return;
            }

            _redoSnapshots.Clear();
            PushUndo(ScoreCloneService.Clone(score));
            OnStackChanged();
        }

        public void DiscardLastSnapshot()
        {
            if (_isRestoring || _snapshots.Count == 0)
            {
                return;
            }

            _snapshots.RemoveAt(_snapshots.Count - 1);
            OnStackChanged();
        }

        public JianpuScore PopSnapshot()
        {
            return PopStack(_snapshots);
        }

        public JianpuScore PopSnapshotForUndo(JianpuScore currentScore)
        {
            if (_isRestoring || currentScore == null || _snapshots.Count == 0)
            {
                return null;
            }

            PushRedo(ScoreCloneService.Clone(currentScore));
            return PopStack(_snapshots);
        }

        public JianpuScore PopSnapshotForRedo(JianpuScore currentScore)
        {
            if (_isRestoring || currentScore == null || _redoSnapshots.Count == 0)
            {
                return null;
            }

            PushUndo(ScoreCloneService.Clone(currentScore));
            return PopStack(_redoSnapshots);
        }

        public void Clear()
        {
            if (_snapshots.Count == 0 && _redoSnapshots.Count == 0)
            {
                return;
            }

            _snapshots.Clear();
            _redoSnapshots.Clear();
            OnStackChanged();
        }

        public void EnterRestore()
        {
            _isRestoring = true;
        }

        public void LeaveRestore()
        {
            _isRestoring = false;
            OnStackChanged();
        }

        private void PushUndo(JianpuScore snapshot)
        {
            _snapshots.Add(snapshot);
            if (_snapshots.Count > MaxDepth)
            {
                _snapshots.RemoveAt(0);
            }
        }

        private void PushRedo(JianpuScore snapshot)
        {
            _redoSnapshots.Add(snapshot);
            if (_redoSnapshots.Count > MaxDepth)
            {
                _redoSnapshots.RemoveAt(0);
            }
        }

        private JianpuScore PopStack(List<JianpuScore> stack)
        {
            if (stack.Count == 0)
            {
                return null;
            }

            var index = stack.Count - 1;
            var snapshot = stack[index];
            stack.RemoveAt(index);
            OnStackChanged();
            return snapshot;
        }

        private void OnScoreLoaded(ScoreUndoService recipient, ScoreLoadedMessage message)
        {
            recipient.Clear();
        }

        private void OnStackChanged()
        {
            StackChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
