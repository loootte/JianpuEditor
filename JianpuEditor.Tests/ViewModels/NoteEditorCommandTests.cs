using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class NoteEditorCommandTests
    {
        [Fact]
        public void AddNote_Undo_RestoresEmptyMeasure()
        {
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var history = new EditCommandHistory(messenger);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, InsertIndex = 0 });

            editor.AddNote(5);
            Assert.Single(document.Score.Measures[0].MelodyNotes);

            history.Undo();

            Assert.Empty(document.Score.Measures[0].MelodyNotes);
        }

        [Fact]
        public void AddNote_Redo_ReappliesInsert()
        {
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var history = new EditCommandHistory(messenger);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, InsertIndex = 0 });

            editor.AddNote(5);
            history.Undo();
            history.Redo();

            Assert.Single(document.Score.Measures[0].MelodyNotes);
            Assert.Equal(5, document.Score.Measures[0].MelodyNotes[0].Pitch);
        }

        [Fact]
        public void ModifySelectedNote_Undo_RestoresOriginalPitch()
        {
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var history = new EditCommandHistory(messenger);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            editor.AddNote(6);
            Assert.Equal(6, document.Score.Measures[0].MelodyNotes[0].Pitch);

            history.Undo();

            Assert.Equal(1, document.Score.Measures[0].MelodyNotes[0].Pitch);
        }

        [Fact]
        public void SplitSelectedNotes_Undo_RestoresSingleNote()
        {
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var history = new EditCommandHistory(messenger);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 4, Underlines = 0 });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                NoteIndex = 0,
                SelectedNotes = new[] { new ScoreNoteRef(0, 0) }
            });

            editor.SplitSelectedNotes();
            Assert.Equal(2, document.Score.Measures[0].MelodyNotes.Count);

            history.Undo();

            Assert.Single(document.Score.Measures[0].MelodyNotes);
            Assert.Equal(0, document.Score.Measures[0].MelodyNotes[0].Underlines);
        }
    }
}
