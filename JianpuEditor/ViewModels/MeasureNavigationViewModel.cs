using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Services.EditCommands;

namespace JianpuEditor.ViewModels
{
    public sealed class MeasureNavigationViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly ScoreSelectionViewModel _selection;
        private readonly IAppMessenger _messenger;
        private readonly IEditCommandHistory _history;
        private int _currentMeasureIndex;

        public MeasureNavigationViewModel(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            IAppMessenger messenger,
            IEditCommandHistory history)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            AddMeasureCommand = new RelayCommand(() => AddMeasure());
            DuplicateMeasuresCommand = new RelayCommand(() => DuplicateMeasures());
        }

        public int CurrentMeasureIndex
        {
            get { return _currentMeasureIndex; }
            private set { SetProperty(ref _currentMeasureIndex, value); }
        }

        public int MeasureCount
        {
            get
            {
                _document.EnsureMeasures();
                return _document.Score.Measures.Count;
            }
        }

        public RelayCommand AddMeasureCommand { get; }

        public RelayCommand DuplicateMeasuresCommand { get; }

        public ScoreEditResult SelectMeasure(int index)
        {
            _document.EnsureMeasures();
            index = Math.Max(0, Math.Min(index, _document.Score.Measures.Count - 1));
            CurrentMeasureIndex = index;

            var result = new ScoreEditResult
            {
                Changed = true,
                Message = "当前编辑第 " + (index + 1) + " 小节",
                SelectMeasureIndex = index,
                RequiresScoreRefresh = false
            };
            _messenger.Send(new StatusChangedMessage(result.Message));
            return result;
        }

        public ScoreEditResult AddMeasure()
        {
            return EditCommandHelper.Execute(
                _history,
                new ScoreSnapshotEditCommand(_document, this, _messenger, ApplyAddMeasure, "新增小节"));
        }

        public ScoreEditResult DuplicateMeasures()
        {
            return EditCommandHelper.Execute(
                _history,
                new ScoreSnapshotEditCommand(_document, this, _messenger, ApplyDuplicateMeasures, "复制小节"));
        }

        public (int fromIndex, int toIndex) NormalizeMeasureRange(int fromOneBased, int toOneBased, bool fromChanged)
        {
            _document.EnsureMeasures();
            var from = fromOneBased - 1;
            var to = toOneBased - 1;
            if (from > to)
            {
                if (fromChanged)
                {
                    to = from;
                }
                else
                {
                    from = to;
                }
            }

            return (from, to);
        }

        public ScoreEditResult ApplyMeasureRange(int fromIndex, int toIndex)
        {
            _document.EnsureMeasures();
            fromIndex = Math.Max(0, Math.Min(fromIndex, _document.Score.Measures.Count - 1));
            toIndex = Math.Max(0, Math.Min(toIndex, _document.Score.Measures.Count - 1));

            var message = "已选择第 " + (fromIndex + 1) + " 到第 " + (toIndex + 1) + " 小节";
            _messenger.Send(new StatusChangedMessage(message));
            return new ScoreEditResult
            {
                Changed = true,
                Message = message,
                RequiresScoreRefresh = false,
                SetSelectedMeasureIndices = Enumerable.Range(fromIndex, toIndex - fromIndex + 1).ToList(),
                SetPrimaryMeasureIndex = fromIndex
            };
        }

        public void SyncCurrentMeasureIndex(int index)
        {
            _document.EnsureMeasures();
            index = Math.Max(0, Math.Min(index, _document.Score.Measures.Count - 1));
            CurrentMeasureIndex = index;
        }

        public void SyncCurrentMeasureIndex(int index, int measureCount)
        {
            measureCount = Math.Max(1, measureCount);
            index = Math.Max(0, Math.Min(index, measureCount - 1));
            CurrentMeasureIndex = index;
        }

        private ScoreEditResult ApplyAddMeasure()
        {
            _document.EnsureMeasures();
            var score = ScoreCloneService.Clone(_document.Score);
            score.Measures.Add(new JianpuMeasure());
            var newIndex = score.Measures.Count - 1;
            _document.Score = score;
            CurrentMeasureIndex = newIndex;

            var message = "已新增第 " + score.Measures.Count + " 小节";
            return new ScoreEditResult
            {
                Changed = true,
                Message = message,
                SelectMeasureIndex = newIndex,
                RequiresScoreRefresh = true
            };
        }

        private ScoreEditResult ApplyDuplicateMeasures()
        {
            _document.EnsureMeasures();
            var indices = _selection.SelectedMeasureIndices?.ToList() ?? new List<int>();
            if (indices.Count == 0)
            {
                indices.Add(Math.Max(0, _selection.MeasureIndex));
            }

            var insertAt = indices[indices.Count - 1] + 1;
            var clones = indices.Select(index => MeasureCloneService.Clone(_document.Score.Measures[index])).ToList();
            for (var i = 0; i < clones.Count; i++)
            {
                _document.Score.Measures.Insert(insertAt + i, clones[i]);
            }

            var duplicatedStart = insertAt;
            var duplicatedIndices = Enumerable.Range(duplicatedStart, indices.Count).ToList();
            CurrentMeasureIndex = duplicatedStart;

            var message = "已复制 " + indices.Count + " 个小节到第 " + (duplicatedStart + 1) + " 小节后";
            return new ScoreEditResult
            {
                Changed = true,
                Message = message,
                SetSelectedMeasureIndices = duplicatedIndices,
                SetPrimaryMeasureIndex = duplicatedStart,
                SelectMeasureIndex = duplicatedStart,
                UpdatedMeasureIndex = duplicatedStart
            };
        }
    }
}
