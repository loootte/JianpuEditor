using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Services;

namespace JianpuEditor.ViewModels
{
    public sealed class ScoreEditorViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly ScoreSelectionViewModel _selection;
        private readonly MeasureNavigationViewModel _navigation;
        private readonly ChordEditorViewModel _chordEditor;
        private readonly IAppMessenger _messenger;

        public ScoreEditorViewModel(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            MeasureNavigationViewModel navigation,
            ChordEditorViewModel chordEditor,
            IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _chordEditor = chordEditor ?? throw new ArgumentNullException(nameof(chordEditor));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            DeleteCommand = new RelayCommand(() => Delete());
            ClearScoreCommand = new RelayCommand(() => ClearScore());
        }

        public RelayCommand DeleteCommand { get; }

        public RelayCommand ClearScoreCommand { get; }

        public ScoreEditResult Delete()
        {
            if (_selection.HasTieSelected
                && _document.Score.Ties != null
                && _selection.TieIndex >= 0
                && _selection.TieIndex < _document.Score.Ties.Count)
            {
                _document.Score.Ties.RemoveAt(_selection.TieIndex);
                _messenger.Send(new ScoreEditedMessage("已删除连音线"));
                return new ScoreEditResult
                {
                    Changed = true,
                    Message = "已删除连音线",
                    ClearTieSelection = true
                };
            }

            var chordResult = _chordEditor.RemoveSelectedChord();
            if (chordResult.Changed)
            {
                return chordResult;
            }

            _document.EnsureMeasures();
            var measureIndex = Math.Max(0, _selection.MeasureIndex);
            var measure = _document.Score.Measures[measureIndex];

            if (_selection.HasNoteSelected
                && _selection.NoteIndex >= 0
                && _selection.NoteIndex < measure.MelodyNotes.Count)
            {
                var noteIndex = _selection.NoteIndex;
                measure.MelodyNotes.RemoveAt(noteIndex);
                TieMaintenanceService.OnNoteRemoved(_document.Score, measureIndex, noteIndex);
                _messenger.Send(new ScoreEditedMessage("已删除选中音符"));
                return new ScoreEditResult
                {
                    Changed = true,
                    Message = "已删除选中音符",
                    ClearMelodySelection = true,
                    ClearTieSelection = true
                };
            }

            if (measure.MelodyNotes.Count > 0)
            {
                var noteIndex = measure.MelodyNotes.Count - 1;
                measure.MelodyNotes.RemoveAt(noteIndex);
                TieMaintenanceService.OnNoteRemoved(_document.Score, measureIndex, noteIndex);
                _messenger.Send(new ScoreEditedMessage("已删除当前小节最后一个音符"));
                return new ScoreEditResult
                {
                    Changed = true,
                    Message = "已删除当前小节最后一个音符",
                    ClearMelodySelection = true,
                    ClearTieSelection = true
                };
            }

            if (_document.Score.Measures.Count > 1)
            {
                var removedMeasureIndex = measureIndex;
                TieMaintenanceService.OnMeasureRemoved(_document.Score, removedMeasureIndex);
                _document.Score.Measures.RemoveAt(removedMeasureIndex);
                var newIndex = Math.Max(0, removedMeasureIndex - 1);
                _navigation.SyncCurrentMeasureIndex(newIndex);
                _messenger.Send(new ScoreEditedMessage("已删除空小节"));
                return new ScoreEditResult
                {
                    Changed = true,
                    Message = "已删除空小节",
                    ClearTieSelection = true,
                    SelectMeasureIndex = newIndex
                };
            }

            return ScoreEditResult.Unchanged;
        }

        public ScoreEditResult ClearScore()
        {
            _document.ClearMeasures();
            _navigation.SyncCurrentMeasureIndex(0);
            _messenger.Send(new ScoreEditedMessage("谱面已清空"));
            return new ScoreEditResult
            {
                Changed = true,
                Message = "谱面已清空",
                SelectMeasureIndex = 0
            };
        }
    }
}
