using System;
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
    public sealed class ChordEditorViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly ScoreSelectionViewModel _selection;
        private readonly MeasureNavigationViewModel _navigation;
        private readonly IChordTransposeService _chordTransposeService;
        private readonly IEditCommandHistory _history;
        private readonly IAppMessenger _messenger;
        private string _selectedChordText = string.Empty;
        private bool _isChordEditorEnabled;
        private bool _suppressSync;

        public ChordEditorViewModel(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            MeasureNavigationViewModel navigation,
            IChordTransposeService chordTransposeService,
            IEditCommandHistory history,
            IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _chordTransposeService = chordTransposeService ?? throw new ArgumentNullException(nameof(chordTransposeService));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            AddChordMarkerCommand = new RelayCommand(() => AddChordMarker());
            TransposeChordsCommand = new RelayCommand<string>(
                targetKey => TransposeChords(targetKey),
                CanTransposeChords);
        }

        public string SelectedChordText
        {
            get { return _selectedChordText; }
            set
            {
                if (_suppressSync)
                {
                    SetProperty(ref _selectedChordText, value);
                    return;
                }

                if (!SetProperty(ref _selectedChordText, value))
                {
                    return;
                }

                ApplyChordText(value);
            }
        }

        public bool IsChordEditorEnabled
        {
            get { return _isChordEditorEnabled; }
            private set { SetProperty(ref _isChordEditorEnabled, value); }
        }

        public RelayCommand AddChordMarkerCommand { get; }

        public RelayCommand<string> TransposeChordsCommand { get; }

        public void SyncFromSelection()
        {
            _suppressSync = true;
            if (_selection.HasChordSelected
                && _selection.ChordMeasureIndex >= 0
                && _selection.ChordMeasureIndex < _document.Score.Measures.Count)
            {
                var measure = _document.Score.Measures[_selection.ChordMeasureIndex];
                ChordMarkerService.NormalizeMeasure(measure);
                if (_selection.ChordMarkerIndex >= 0
                    && _selection.ChordMarkerIndex < measure.ChordMarkers.Count)
                {
                    SelectedChordText = measure.ChordMarkers[_selection.ChordMarkerIndex].Text ?? string.Empty;
                    IsChordEditorEnabled = true;
                    _suppressSync = false;
                    return;
                }
            }

            SelectedChordText = string.Empty;
            IsChordEditorEnabled = false;
            _suppressSync = false;
        }

        public ScoreEditResult AddChordMarker()
        {
            _document.EnsureMeasures();
            var measureIndex = Math.Max(0, _selection.MeasureIndex);
            var measure = _document.Score.Measures[measureIndex];
            if (measure.ChordMarkers.Count >= JianpuMeasure.MaxChordMarkers)
            {
                var status = "当前小节最多 " + JianpuMeasure.MaxChordMarkers + " 个和弦标识";
                _messenger.Send(new StatusChangedMessage(status));
                return ScoreEditResult.Unchanged;
            }

            return EditCommandHelper.Execute(
                _history,
                new ScoreSnapshotEditCommand(
                    _document,
                    _navigation,
                    _messenger,
                    () => ApplyAddChordMarker(measureIndex, 0),
                    "添加和弦标识"));
        }

        public ScoreEditResult AddChordMarkerAtBeat(int measureIndex, double beatPosition)
        {
            return EditCommandHelper.Execute(
                _history,
                new ScoreSnapshotEditCommand(
                    _document,
                    _navigation,
                    _messenger,
                    () => ApplyAddChordMarker(measureIndex, beatPosition),
                    "添加和弦标识"));
        }

        public void ApplyChordText(string text)
        {
            if (!_selection.HasChordSelected)
            {
                return;
            }

            var measureIndex = _selection.ChordMeasureIndex;
            var markerIndex = _selection.ChordMarkerIndex;
            if (measureIndex < 0 || measureIndex >= _document.Score.Measures.Count)
            {
                return;
            }

            var measure = _document.Score.Measures[measureIndex];
            if (markerIndex < 0 || markerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            var newText = text ?? string.Empty;
            var currentText = measure.ChordMarkers[markerIndex].Text ?? string.Empty;
            if (string.Equals(currentText, newText, StringComparison.Ordinal))
            {
                return;
            }

            _history.Execute(new ModifyChordMarkerTextCommand(
                _document.Score,
                _messenger,
                measureIndex,
                markerIndex,
                currentText,
                newText));
        }

        public ScoreEditResult TryRemoveSelectedChord()
        {
            if (!_selection.HasChordSelected)
            {
                return ScoreEditResult.Unchanged;
            }

            var measureIndex = _selection.ChordMeasureIndex;
            var markerIndex = _selection.ChordMarkerIndex;
            var measure = _document.Score.Measures[measureIndex];
            if (!ChordMarkerService.TryRemoveMarker(measure, markerIndex))
            {
                return ScoreEditResult.Unchanged;
            }

            return new ScoreEditResult
            {
                Changed = true,
                Message = "已删除和弦标识",
                ClearChordSelection = true
            };
        }

        public ScoreEditResult TransposeChords(string targetKey)
        {
            if (string.IsNullOrWhiteSpace(targetKey))
            {
                return ScoreEditResult.Unchanged;
            }

            var trimmedTarget = targetKey.Trim();
            if (!_chordTransposeService.TryTransposeChords(
                    ScoreCloneService.Clone(_document.Score),
                    trimmedTarget,
                    out var errorMessage,
                    out _))
            {
                return new ScoreEditResult
                {
                    Changed = false,
                    Message = errorMessage
                };
            }

            return EditCommandHelper.Execute(
                _history,
                new ScoreSnapshotEditCommand(
                    _document,
                    _navigation,
                    _messenger,
                    () => ApplyTransposeChords(trimmedTarget),
                    "和弦转调"));
        }

        public string CurrentKeySignature
        {
            get { return _document.KeySignature; }
        }

        private ScoreEditResult ApplyAddChordMarker(int measureIndex, double beatPosition)
        {
            _document.EnsureMeasures();
            if (measureIndex < 0 || measureIndex >= _document.Score.Measures.Count)
            {
                return ScoreEditResult.Unchanged;
            }

            var measure = _document.Score.Measures[measureIndex];
            if (!ChordMarkerService.TryAddMarker(measure, beatPosition))
            {
                if (beatPosition == 0)
                {
                    var status = "当前小节最多 " + JianpuMeasure.MaxChordMarkers + " 个和弦标识";
                    _messenger.Send(new StatusChangedMessage(status));
                }

                return ScoreEditResult.Unchanged;
            }

            var markerIndex = measure.ChordMarkers.Count - 1;
            return new ScoreEditResult
            {
                Changed = true,
                Message = "已添加和弦标识",
                SelectMeasureIndex = measureIndex,
                SelectChordMeasureIndex = measureIndex,
                SelectChordMarkerIndex = markerIndex
            };
        }

        private ScoreEditResult ApplyTransposeChords(string targetKey)
        {
            if (!_chordTransposeService.TryTransposeChords(
                    _document.Score,
                    targetKey,
                    out var errorMessage,
                    out var transposedCount))
            {
                return new ScoreEditResult
                {
                    Changed = false,
                    Message = errorMessage
                };
            }

            var message = "已将 " + transposedCount + " 个和弦转调到 " + _document.Score.KeySignature;
            OnPropertyChanged(nameof(CurrentKeySignature));
            return ScoreEditResult.WithMessage(message);
        }

        private bool CanTransposeChords(string targetKey)
        {
            return !string.IsNullOrWhiteSpace(targetKey);
        }
    }
}
