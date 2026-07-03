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

            _snapshots.Add(ScoreCloneService.Clone(score));
            if (_snapshots.Count > MaxDepth)
            {
                _snapshots.RemoveAt(0);
            }

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
            if (_snapshots.Count == 0)
            {
                return null;
            }

            var index = _snapshots.Count - 1;
            var snapshot = _snapshots[index];
            _snapshots.RemoveAt(index);
            OnStackChanged();
            return snapshot;
        }

        public void Clear()
        {
            if (_snapshots.Count == 0)
            {
                return;
            }

            _snapshots.Clear();
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
