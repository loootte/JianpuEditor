using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JianpuEditor.Models;

namespace JianpuEditor.ViewModels
{
    public sealed class ScoreSelectionViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private int _measureIndex = -1;
        private int _noteIndex = -1;
        private int _insertIndex = -1;
        private int _tieIndex = -1;
        private int _chordMeasureIndex = -1;
        private int _chordMarkerIndex = -1;
        private IReadOnlyList<int> _selectedMeasureIndices = Array.Empty<int>();
        private IReadOnlyList<ScoreNoteRef> _selectedNotes = Array.Empty<ScoreNoteRef>();

        public ScoreSelectionViewModel(ScoreDocumentViewModel document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public int MeasureIndex
        {
            get { return _measureIndex; }
            private set { SetProperty(ref _measureIndex, value); }
        }

        public int NoteIndex
        {
            get { return _noteIndex; }
            private set { SetProperty(ref _noteIndex, value); }
        }

        public int InsertIndex
        {
            get { return _insertIndex; }
            private set { SetProperty(ref _insertIndex, value); }
        }

        public int TieIndex
        {
            get { return _tieIndex; }
            private set { SetProperty(ref _tieIndex, value); }
        }

        public int ChordMeasureIndex
        {
            get { return _chordMeasureIndex; }
            private set { SetProperty(ref _chordMeasureIndex, value); }
        }

        public int ChordMarkerIndex
        {
            get { return _chordMarkerIndex; }
            private set { SetProperty(ref _chordMarkerIndex, value); }
        }

        public IReadOnlyList<int> SelectedMeasureIndices
        {
            get { return _selectedMeasureIndices; }
            private set { SetProperty(ref _selectedMeasureIndices, value); }
        }

        public IReadOnlyList<ScoreNoteRef> SelectedNotes
        {
            get { return _selectedNotes; }
            private set { SetProperty(ref _selectedNotes, value); }
        }

        public bool HasNoteSelected
        {
            get { return SelectedNotes != null && SelectedNotes.Count > 0 || NoteIndex >= 0; }
        }

        public bool HasMultipleNotesSelected
        {
            get { return SelectedNotes != null && SelectedNotes.Count > 1; }
        }

        public bool HasGapSelected
        {
            get { return InsertIndex >= 0; }
        }

        public bool HasTieSelected
        {
            get { return TieIndex >= 0; }
        }

        public bool HasChordSelected
        {
            get { return ChordMarkerIndex >= 0; }
        }

        public void UpdateFrom(ScoreSelectionInfo info)
        {
            if (info == null)
            {
                return;
            }

            MeasureIndex = info.MeasureIndex;
            NoteIndex = info.NoteIndex;
            InsertIndex = info.InsertIndex;
            TieIndex = info.TieIndex;
            ChordMeasureIndex = info.ChordMeasureIndex;
            ChordMarkerIndex = info.ChordMarkerIndex;
            SelectedMeasureIndices = info.SelectedMeasureIndices ?? Array.Empty<int>();
            SelectedNotes = info.SelectedNotes ?? Array.Empty<ScoreNoteRef>();
        }

        public string BuildSelectionDescription()
        {
            if (HasTieSelected
                && _document.Score.Ties != null
                && TieIndex >= 0
                && TieIndex < _document.Score.Ties.Count)
            {
                var tie = _document.Score.Ties[TieIndex];
                return "已选中连音线：第 " + (tie.StartMeasureIndex + 1) + " 小节第 " + (tie.StartNoteIndex + 1) +
                       " 个音符 → 第 " + (tie.EndMeasureIndex + 1) + " 小节第 " + (tie.EndNoteIndex + 1) +
                       " 个音符，点击「删除」可移除";
            }

            if (HasChordSelected
                && ChordMeasureIndex >= 0
                && ChordMeasureIndex < _document.Score.Measures.Count)
            {
                var measure = _document.Score.Measures[ChordMeasureIndex];
                if (ChordMarkerIndex >= 0 && ChordMarkerIndex < measure.ChordMarkers.Count)
                {
                    var marker = measure.ChordMarkers[ChordMarkerIndex];
                    return "已选中和弦标识：第 " + (ChordMeasureIndex + 1) + " 小节第 " +
                           (ChordMarkerIndex + 1) + " 个，拍位 " + (marker.BeatPosition + 1) +
                           "，可拖动 :: 改位置，Delete/「删除」移除";
                }
            }

            if (SelectedMeasureIndices != null && SelectedMeasureIndices.Count > 1)
            {
                return "已选择第 " + (SelectedMeasureIndices.Min() + 1) + " 到第 " +
                       (SelectedMeasureIndices.Max() + 1) + " 小节，可点击「复制小节」";
            }

            if (HasMultipleNotesSelected)
            {
                return "已选中 " + SelectedNotes.Count + " 个音符，可用上方按钮批量修改";
            }

            if (HasNoteSelected)
            {
                return "已选中第 " + (MeasureIndex + 1) + " 小节第 " + (NoteIndex + 1) + " 个音符，可用上方按钮修改";
            }

            if (HasGapSelected)
            {
                return "已选中第 " + (MeasureIndex + 1) + " 小节第 " + (InsertIndex + 1) + " 个插入位置，可用上方按钮插入音符";
            }

            if (MeasureIndex >= 0)
            {
                return "当前编辑第 " + (MeasureIndex + 1) + " 小节，点击副旋律空白拍位添加和弦，点击歌词行编辑文字";
            }

            return string.Empty;
        }

        public (int fromIndex, int toIndex) GetMeasureRangeIndices()
        {
            if (SelectedMeasureIndices == null || SelectedMeasureIndices.Count == 0)
            {
                var index = Math.Max(0, MeasureIndex);
                return (index, index);
            }

            return (SelectedMeasureIndices.Min(), SelectedMeasureIndices.Max());
        }
    }
}
