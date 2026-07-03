using System;
using System.Collections.Generic;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.NoteEditCommands
{
    internal sealed class ModifyMelodyNotesCommand : INoteEditCommand
    {
        private readonly JianpuScore _score;
        private readonly IAppMessenger _messenger;
        private readonly List<NoteBackup> _backups;
        private readonly Action _apply;
        private readonly string _message;
        private readonly ScoreEditResult _resultTemplate;

        public ModifyMelodyNotesCommand(
            JianpuScore score,
            IAppMessenger messenger,
            IReadOnlyList<ScoreNoteRef> noteRefs,
            Action apply,
            string message,
            ScoreEditResult resultTemplate = null)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            if (messenger == null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }

            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required.", nameof(message));
            }

            _score = score;
            _messenger = messenger;
            _apply = apply;
            _message = message;
            _resultTemplate = resultTemplate;
            _backups = CaptureBackups(score, noteRefs);
            Description = message;
        }

        public string Description { get; }

        public ScoreEditResult Result { get; private set; }

        public void Execute()
        {
            _apply();
            PublishEdit();
        }

        public void Undo()
        {
            for (var i = 0; i < _backups.Count; i++)
            {
                var backup = _backups[i];
                if (backup.MeasureIndex < 0 || backup.MeasureIndex >= _score.Measures.Count)
                {
                    continue;
                }

                var notes = _score.Measures[backup.MeasureIndex].MelodyNotes;
                if (backup.NoteIndex < 0 || backup.NoteIndex >= notes.Count)
                {
                    continue;
                }

                NoteEditState.CopyNoteProperties(notes[backup.NoteIndex], backup.Note);
            }

            _messenger.Send(new ScoreEditedMessage("已撤回: " + Description));
            Result = BuildResult("已撤回: " + Description);
        }

        private void PublishEdit()
        {
            _messenger.Send(new ScoreEditedMessage(_message));
            Result = BuildResult(_message);
        }

        private ScoreEditResult BuildResult(string message)
        {
            if (_resultTemplate == null)
            {
                return ScoreEditResult.WithMessage(message);
            }

            return new ScoreEditResult
            {
                Changed = true,
                Message = message,
                RequiresScoreRefresh = _resultTemplate.RequiresScoreRefresh,
                SelectMeasureIndex = _resultTemplate.SelectMeasureIndex,
                SelectNoteMeasureIndex = _resultTemplate.SelectNoteMeasureIndex,
                SelectNoteIndex = _resultTemplate.SelectNoteIndex,
                SetSelectedMeasureIndices = _resultTemplate.SetSelectedMeasureIndices,
                SetPrimaryMeasureIndex = _resultTemplate.SetPrimaryMeasureIndex,
                ClearMelodySelection = _resultTemplate.ClearMelodySelection,
                ClearTieSelection = _resultTemplate.ClearTieSelection,
                ClearChordSelection = _resultTemplate.ClearChordSelection,
                SelectChordMeasureIndex = _resultTemplate.SelectChordMeasureIndex,
                SelectChordMarkerIndex = _resultTemplate.SelectChordMarkerIndex,
                UpdatedMeasureIndex = _resultTemplate.UpdatedMeasureIndex
            };
        }

        private static List<NoteBackup> CaptureBackups(JianpuScore score, IReadOnlyList<ScoreNoteRef> noteRefs)
        {
            var backups = new List<NoteBackup>();
            if (noteRefs == null || noteRefs.Count == 0)
            {
                return backups;
            }

            for (var i = 0; i < noteRefs.Count; i++)
            {
                var noteRef = noteRefs[i];
                if (noteRef.MeasureIndex < 0 || noteRef.MeasureIndex >= score.Measures.Count)
                {
                    continue;
                }

                var notes = score.Measures[noteRef.MeasureIndex].MelodyNotes;
                if (noteRef.NoteIndex < 0 || noteRef.NoteIndex >= notes.Count)
                {
                    continue;
                }

                backups.Add(new NoteBackup(
                    noteRef.MeasureIndex,
                    noteRef.NoteIndex,
                    NoteEditState.CloneNote(notes[noteRef.NoteIndex])));
            }

            return backups;
        }

        private readonly struct NoteBackup
        {
            public NoteBackup(int measureIndex, int noteIndex, JianpuNote note)
            {
                MeasureIndex = measureIndex;
                NoteIndex = noteIndex;
                Note = note;
            }

            public int MeasureIndex { get; }

            public int NoteIndex { get; }

            public JianpuNote Note { get; }
        }
    }
}
