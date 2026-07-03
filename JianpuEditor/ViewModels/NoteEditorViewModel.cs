using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;

namespace JianpuEditor.ViewModels
{
    public sealed class NoteEditorViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly ScoreSelectionViewModel _selection;
        private readonly IAppMessenger _messenger;
        private JianpuNote _pendingNote = CreateDefaultNote();

        public NoteEditorViewModel(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            AddNoteCommand = new RelayCommand<int>(pitch => AddNote(pitch));
            AddRestCommand = new RelayCommand(() => AddRest());
            SetOctaveUpCommand = new RelayCommand(() => SetOctave(1));
            SetOctaveDownCommand = new RelayCommand(() => SetOctave(-1));
            ToggleDottedCommand = new RelayCommand(() => ToggleDotted());
            CycleDurationCommand = new RelayCommand(() => CycleDuration());
            CycleExtensionCommand = new RelayCommand(() => CycleExtension());
        }

        public RelayCommand<int> AddNoteCommand { get; }

        public RelayCommand AddRestCommand { get; }

        public RelayCommand SetOctaveUpCommand { get; }

        public RelayCommand SetOctaveDownCommand { get; }

        public RelayCommand ToggleDottedCommand { get; }

        public RelayCommand CycleDurationCommand { get; }

        public RelayCommand CycleExtensionCommand { get; }

        public JianpuNote PendingNote
        {
            get { return _pendingNote; }
        }

        public ScoreEditResult AddNote(int pitch)
        {
            var selected = GetSelectedNote();
            if (selected != null)
            {
                selected.Type = NoteType.Note;
                selected.Pitch = pitch;
                return PublishEdit("已修改选中音符为 " + pitch);
            }

            var note = ClonePendingNote();
            note.Type = NoteType.Note;
            note.Pitch = pitch;
            return InsertMelodyNote(note, "已插入音符 " + pitch);
        }

        public ScoreEditResult AddRest()
        {
            var selected = GetSelectedNote();
            if (selected != null)
            {
                selected.Type = NoteType.Rest;
                selected.Pitch = 0;
                return PublishEdit("已修改选中音符为休止符");
            }

            var note = ClonePendingNote();
            note.Type = NoteType.Rest;
            note.Pitch = 0;
            return InsertMelodyNote(note, "已插入休止符");
        }

        public ScoreEditResult SetOctave(int octave)
        {
            var selected = GetSelectedNote();
            if (selected != null)
            {
                selected.Octave = selected.Octave == octave ? 0 : octave;
                return PublishEdit("已修改选中音符八度");
            }

            _pendingNote.Octave = _pendingNote.Octave == octave ? 0 : octave;
            var label = _pendingNote.Octave > 0 ? "高音" : _pendingNote.Octave < 0 ? "低音" : "中音";
            _messenger.Send(new StatusChangedMessage("当前八度: " + label));
            return ScoreEditResult.Unchanged;
        }

        public ScoreEditResult ToggleDotted()
        {
            var selected = GetSelectedNote();
            if (selected != null)
            {
                selected.Dotted = !selected.Dotted;
                return PublishEdit(selected.Dotted ? "已为选中音符添加附点" : "已移除选中音符附点");
            }

            _pendingNote.Dotted = !_pendingNote.Dotted;
            _messenger.Send(new StatusChangedMessage(_pendingNote.Dotted ? "下一音符将带附点" : "已取消附点"));
            return ScoreEditResult.Unchanged;
        }

        public ScoreEditResult CycleDuration()
        {
            var selected = GetSelectedNote();
            if (selected != null)
            {
                selected.Underlines = (selected.Underlines + 1) % 3;
                selected.Dashes = 0;
                return PublishEdit("时值: " + GetDurationLabel(selected.Underlines));
            }

            _pendingNote.Underlines = (_pendingNote.Underlines + 1) % 3;
            _pendingNote.Dashes = 0;
            _messenger.Send(new StatusChangedMessage("下一音符时值: " + GetDurationLabel(_pendingNote.Underlines)));
            return ScoreEditResult.Unchanged;
        }

        public ScoreEditResult CycleExtension()
        {
            var selected = GetSelectedNote();
            if (selected != null)
            {
                selected.Dashes = (selected.Dashes + 1) % 4;
                if (selected.Dashes > 0)
                {
                    selected.Underlines = 0;
                }

                return PublishEdit("延长: " + GetExtensionLabel(selected.Dashes));
            }

            _pendingNote.Dashes = (_pendingNote.Dashes + 1) % 4;
            if (_pendingNote.Dashes > 0)
            {
                _pendingNote.Underlines = 0;
            }

            _messenger.Send(new StatusChangedMessage("下一音符延长: " + GetExtensionLabel(_pendingNote.Dashes)));
            return ScoreEditResult.Unchanged;
        }

        public void ResetPendingModifiers()
        {
            _pendingNote = CreateDefaultNote();
        }

        private ScoreEditResult InsertMelodyNote(JianpuNote note, string message)
        {
            _document.EnsureMeasures();
            var measureIndex = Math.Max(0, _selection.MeasureIndex);
            var measure = _document.Score.Measures[measureIndex];
            int insertIndex;

            if (_selection.HasGapSelected)
            {
                insertIndex = _selection.InsertIndex;
            }
            else
            {
                insertIndex = measure.MelodyNotes.Count;
            }

            measure.MelodyNotes.Insert(insertIndex, note);
            ResetPendingModifiers();

            var result = PublishEdit(message);
            result.SelectNoteMeasureIndex = measureIndex;
            result.SelectNoteIndex = insertIndex;
            return result;
        }

        private JianpuNote GetSelectedNote()
        {
            if (!_selection.HasNoteSelected)
            {
                return null;
            }

            _document.EnsureMeasures();
            var measureIndex = _selection.MeasureIndex;
            if (measureIndex < 0 || measureIndex >= _document.Score.Measures.Count)
            {
                return null;
            }

            var notes = _document.Score.Measures[measureIndex].MelodyNotes;
            if (_selection.NoteIndex < 0 || _selection.NoteIndex >= notes.Count)
            {
                return null;
            }

            return notes[_selection.NoteIndex];
        }

        private JianpuNote ClonePendingNote()
        {
            return new JianpuNote
            {
                Type = _pendingNote.Type,
                Pitch = _pendingNote.Pitch,
                Octave = _pendingNote.Octave,
                Underlines = _pendingNote.Underlines,
                Dashes = _pendingNote.Dashes,
                Dotted = _pendingNote.Dotted
            };
        }

        private static JianpuNote CreateDefaultNote()
        {
            return new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 1,
                Octave = 0,
                Underlines = 0,
                Dashes = 0,
                Dotted = false
            };
        }

        private static string GetDurationLabel(int underlines)
        {
            switch (underlines)
            {
                case 1: return "八分音符";
                case 2: return "十六分音符";
                default: return "四分音符";
            }
        }

        private static string GetExtensionLabel(int dashes)
        {
            switch (dashes)
            {
                case 1: return "延一拍";
                case 2: return "延两拍";
                case 3: return "延三拍";
                default: return "不延长";
            }
        }

        private ScoreEditResult PublishEdit(string message)
        {
            _messenger.Send(new ScoreEditedMessage(message));
            return ScoreEditResult.WithMessage(message);
        }
    }
}
