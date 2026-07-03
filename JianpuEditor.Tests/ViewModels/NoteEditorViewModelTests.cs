using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class NoteEditorViewModelTests
    {
        [Fact]
        public void AddNote_InsertsAtGapSelection()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                InsertIndex = 0
            });

            var result = editor.AddNote(5);

            Assert.True(result.Changed);
            Assert.Equal(5, document.Score.Measures[0].MelodyNotes[0].Pitch);
            Assert.Equal(0, result.SelectNoteIndex);
        }

        [Fact]
        public void AddNote_ModifiesSelectedNote()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                NoteIndex = 0
            });

            editor.AddNote(6);

            Assert.Equal(6, document.Score.Measures[0].MelodyNotes[0].Pitch);
        }

        [Fact]
        public void AddRest_InsertsRestNote()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, InsertIndex = 0 });

            editor.AddRest();

            Assert.Equal(NoteType.Rest, document.Score.Measures[0].MelodyNotes[0].Type);
        }
    }
}
