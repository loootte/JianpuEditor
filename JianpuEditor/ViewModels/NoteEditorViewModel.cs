using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services;

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
            DecreaseDurationCommand = new RelayCommand(() => DecreaseDuration());
            IncreaseDurationCommand = new RelayCommand(() => IncreaseDuration());
            TransposePitchUpCommand = new RelayCommand(() => TransposePitch(1));
            TransposePitchDownCommand = new RelayCommand(() => TransposePitch(-1));
        }

        public RelayCommand<int> AddNoteCommand { get; }

        public RelayCommand AddRestCommand { get; }

        public RelayCommand SetOctaveUpCommand { get; }

        public RelayCommand SetOctaveDownCommand { get; }

        public RelayCommand ToggleDottedCommand { get; }

        public RelayCommand DecreaseDurationCommand { get; }

        public RelayCommand IncreaseDurationCommand { get; }

        public RelayCommand TransposePitchUpCommand { get; }

        public RelayCommand TransposePitchDownCommand { get; }

        public JianpuNote PendingNote
        {
            get { return _pendingNote; }
        }

        public ScoreEditResult AddNote(int pitch)
        {
            var selectedNotes = GetSelectedNotes();
            if (selectedNotes.Count > 0)
            {
                foreach (var selected in selectedNotes)
                {
                    selected.Type = NoteType.Note;
                    selected.Pitch = pitch;
                }

                return PublishEdit(selectedNotes.Count > 1
                    ? "已修改 " + selectedNotes.Count + " 个选中音符为 " + pitch
                    : "已修改选中音符为 " + pitch);
            }

            var note = ClonePendingNote();
            note.Type = NoteType.Note;
            note.Pitch = pitch;
            return InsertMelodyNote(note, "已插入音符 " + pitch);
        }

        public ScoreEditResult AddRest()
        {
            var selectedNotes = GetSelectedNotes();
            if (selectedNotes.Count > 0)
            {
                foreach (var selected in selectedNotes)
                {
                    selected.Type = NoteType.Rest;
                    selected.Pitch = 0;
                }

                return PublishEdit(selectedNotes.Count > 1
                    ? "已修改 " + selectedNotes.Count + " 个选中音符为休止符"
                    : "已修改选中音符为休止符");
            }

            var note = ClonePendingNote();
            note.Type = NoteType.Rest;
            note.Pitch = 0;
            return InsertMelodyNote(note, "已插入休止符");
        }

        public ScoreEditResult SetOctave(int octave)
        {
            var selectedNotes = GetSelectedNotes();
            if (selectedNotes.Count > 0)
            {
                foreach (var selected in selectedNotes)
                {
                    selected.Octave = selected.Octave == octave ? 0 : octave;
                }

                return PublishEdit(selectedNotes.Count > 1
                    ? "已修改 " + selectedNotes.Count + " 个选中音符八度"
                    : "已修改选中音符八度");
            }

            _pendingNote.Octave = _pendingNote.Octave == octave ? 0 : octave;
            var label = _pendingNote.Octave > 0 ? "高音" : _pendingNote.Octave < 0 ? "低音" : "中音";
            _messenger.Send(new StatusChangedMessage("当前八度: " + label));
            return ScoreEditResult.Unchanged;
        }

        public ScoreEditResult ToggleDotted()
        {
            var selectedNotes = GetSelectedNotes();
            if (selectedNotes.Count > 0)
            {
                foreach (var selected in selectedNotes)
                {
                    selected.Dotted = !selected.Dotted;
                }

                var dotted = selectedNotes[0].Dotted;
                return PublishEdit(selectedNotes.Count > 1
                    ? (dotted ? "已为 " + selectedNotes.Count + " 个选中音符添加附点" : "已移除 " + selectedNotes.Count + " 个选中音符附点")
                    : (dotted ? "已为选中音符添加附点" : "已移除选中音符附点"));
            }

            _pendingNote.Dotted = !_pendingNote.Dotted;
            _messenger.Send(new StatusChangedMessage(_pendingNote.Dotted ? "下一音符将带附点" : "已取消附点"));
            return ScoreEditResult.Unchanged;
        }

        public ScoreEditResult DecreaseDuration()
        {
            return StepDuration(-1);
        }

        public ScoreEditResult IncreaseDuration()
        {
            return StepDuration(1);
        }

        public ScoreEditResult TransposePitch(int delta)
        {
            var selectedNotes = GetSelectedNotes();
            if (selectedNotes.Count > 0)
            {
                var changedCount = 0;
                foreach (var selected in selectedNotes)
                {
                    if (JianpuPitchService.TryTranspose(selected, delta))
                    {
                        changedCount++;
                    }
                }

                if (changedCount == 0)
                {
                    return PublishEdit(delta > 0 ? "已达最高音" : "已达最低音");
                }

                var direction = delta > 0 ? "升" : "降";
                return PublishEdit(changedCount > 1
                    ? "已" + direction + "key " + changedCount + " 个选中音符"
                    : "已" + direction + "key选中音符");
            }

            if (!JianpuPitchService.TryTranspose(_pendingNote, delta))
            {
                _messenger.Send(new StatusChangedMessage(delta > 0 ? "下一音符已达最高音" : "下一音符已达最低音"));
                return ScoreEditResult.Unchanged;
            }

            _messenger.Send(new StatusChangedMessage(
                "下一音符音高: " + _pendingNote.Pitch +
                (_pendingNote.Octave > 0 ? "·" : _pendingNote.Octave < 0 ? ".." : string.Empty)));
            return ScoreEditResult.Unchanged;
        }

        private ScoreEditResult StepDuration(int delta)
        {
            var selectedNotes = GetSelectedNotes();
            if (selectedNotes.Count > 0)
            {
                var tier = GetDurationTier(selectedNotes[0]);
                var nextTier = Math.Max(MinDurationTier, Math.Min(MaxDurationTier, tier + delta));
                if (nextTier == tier)
                {
                    var limit = delta > 0 ? "已达最长时值" : "已达最短时值";
                    return PublishEdit(limit + "（" + GetDurationTierLabel(tier) + "）");
                }

                foreach (var selected in selectedNotes)
                {
                    ApplyDurationTier(selected, nextTier);
                }

                return PublishEdit(selectedNotes.Count > 1
                    ? "时值: " + GetDurationTierLabel(nextTier) + "（" + selectedNotes.Count + " 个音符）"
                    : "时值: " + GetDurationTierLabel(nextTier));
            }

            var pendingTier = GetDurationTier(_pendingNote);
            var nextPendingTier = Math.Max(MinDurationTier, Math.Min(MaxDurationTier, pendingTier + delta));
            ApplyDurationTier(_pendingNote, nextPendingTier);
            _messenger.Send(new StatusChangedMessage("下一音符时值: " + GetDurationTierLabel(nextPendingTier)));
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

        private List<JianpuNote> GetSelectedNotes()
        {
            var result = new List<JianpuNote>();
            if (!_selection.HasNoteSelected)
            {
                return result;
            }

            _document.EnsureMeasures();
            if (_selection.SelectedNotes != null && _selection.SelectedNotes.Count > 0)
            {
                foreach (var selected in _selection.SelectedNotes)
                {
                    if (selected.MeasureIndex < 0 || selected.MeasureIndex >= _document.Score.Measures.Count)
                    {
                        continue;
                    }

                    var notes = _document.Score.Measures[selected.MeasureIndex].MelodyNotes;
                    if (selected.NoteIndex < 0 || selected.NoteIndex >= notes.Count)
                    {
                        continue;
                    }

                    result.Add(notes[selected.NoteIndex]);
                }

                return result;
            }

            var measureIndex = _selection.MeasureIndex;
            if (measureIndex < 0 || measureIndex >= _document.Score.Measures.Count)
            {
                return result;
            }

            var melodyNotes = _document.Score.Measures[measureIndex].MelodyNotes;
            if (_selection.NoteIndex < 0 || _selection.NoteIndex >= melodyNotes.Count)
            {
                return result;
            }

            result.Add(melodyNotes[_selection.NoteIndex]);
            return result;
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

        private const int MinDurationTier = 0;
        private const int MaxDurationTier = 5;

        internal static int GetDurationTier(JianpuNote note)
        {
            if (note == null)
            {
                return 2;
            }

            if (note.Dashes > 0)
            {
                return Math.Min(MaxDurationTier, 2 + note.Dashes);
            }

            return Math.Max(MinDurationTier, 2 - Math.Min(2, note.Underlines));
        }

        internal static void ApplyDurationTier(JianpuNote note, int tier)
        {
            if (note == null)
            {
                return;
            }

            tier = Math.Max(MinDurationTier, Math.Min(MaxDurationTier, tier));
            if (tier <= 2)
            {
                note.Dashes = 0;
                note.Underlines = 2 - tier;
                return;
            }

            note.Underlines = 0;
            note.Dashes = tier - 2;
        }

        private static string GetDurationTierLabel(int tier)
        {
            switch (tier)
            {
                case 0:
                    return "1/16";
                case 1:
                    return "1/8";
                case 3:
                    return "延1拍";
                case 4:
                    return "延2拍";
                case 5:
                    return "延3拍";
                default:
                    return "1/4";
            }
        }

        private ScoreEditResult PublishEdit(string message)
        {
            _messenger.Send(new ScoreEditedMessage(message));
            return ScoreEditResult.WithMessage(message);
        }
    }
}
