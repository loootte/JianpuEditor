using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;

namespace JianpuEditor.ViewModels
{
    public sealed class TieEditorViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly IAppMessenger _messenger;
        private bool _isTieModeActive;
        private int _tieStartMeasureIndex = -1;
        private int _tieStartNoteIndex = -1;

        public TieEditorViewModel(ScoreDocumentViewModel document, IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            ToggleTieModeCommand = new RelayCommand(ToggleTieMode);
            CancelTieModeCommand = new RelayCommand(CancelTieMode);
        }

        public bool IsTieModeActive
        {
            get { return _isTieModeActive; }
            private set { SetProperty(ref _isTieModeActive, value); }
        }

        public RelayCommand ToggleTieModeCommand { get; }

        public RelayCommand CancelTieModeCommand { get; }

        public void ToggleTieMode()
        {
            if (IsTieModeActive)
            {
                CancelTieMode();
                return;
            }

            IsTieModeActive = true;
            _tieStartMeasureIndex = -1;
            _tieStartNoteIndex = -1;
            _messenger.Send(new StatusChangedMessage("连音线：请选择起始音符"));
        }

        public void CancelTieMode()
        {
            IsTieModeActive = false;
            _tieStartMeasureIndex = -1;
            _tieStartNoteIndex = -1;
            _messenger.Send(new StatusChangedMessage("已取消连音线"));
        }

        public ScoreEditResult TryCompleteTie(int endMeasureIndex, int endNoteIndex)
        {
            if (!IsTieModeActive)
            {
                return ScoreEditResult.Unchanged;
            }

            if (_tieStartMeasureIndex < 0)
            {
                _tieStartMeasureIndex = endMeasureIndex;
                _tieStartNoteIndex = endNoteIndex;
                _messenger.Send(new StatusChangedMessage("连音线：请选择结束音符"));
                return ScoreEditResult.Unchanged;
            }

            if (_tieStartMeasureIndex == endMeasureIndex && _tieStartNoteIndex == endNoteIndex)
            {
                _messenger.Send(new StatusChangedMessage("连音线：结束音符不能与起始音符相同"));
                return ScoreEditResult.Unchanged;
            }

            if (!IsNoteAfter(_tieStartMeasureIndex, _tieStartNoteIndex, endMeasureIndex, endNoteIndex))
            {
                _tieStartMeasureIndex = endMeasureIndex;
                _tieStartNoteIndex = endNoteIndex;
                _messenger.Send(new StatusChangedMessage("连音线：结束音符须晚于起始音符，请重新选择结束音符"));
                return ScoreEditResult.Unchanged;
            }

            if (_document.Score.Ties == null)
            {
                _document.Score.Ties = new List<JianpuTie>();
            }

            _document.Score.Ties.Add(new JianpuTie
            {
                StartMeasureIndex = _tieStartMeasureIndex,
                StartNoteIndex = _tieStartNoteIndex,
                EndMeasureIndex = endMeasureIndex,
                EndNoteIndex = endNoteIndex
            });

            CancelTieMode();
            _messenger.Send(new ScoreEditedMessage("已添加连音线"));
            return ScoreEditResult.WithMessage("已添加连音线");
        }

        private static bool IsNoteAfter(int measureA, int noteA, int measureB, int noteB)
        {
            if (measureA != measureB)
            {
                return measureB > measureA;
            }

            return noteB > noteA;
        }
    }
}
