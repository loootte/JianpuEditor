using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class ScoreEditorViewModelTests
    {
        [Fact]
        public void Delete_RemovesSelectedTieFirst()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Ties = new System.Collections.Generic.List<JianpuTie>
            {
                new JianpuTie { StartMeasureIndex = 0, StartNoteIndex = 0, EndMeasureIndex = 0, EndNoteIndex = 1 }
            };
            selection.UpdateFrom(new ScoreSelectionInfo { TieIndex = 0 });
            var navigation = new MeasureNavigationViewModel(document, selection, messenger);
            var chordEditor = new ChordEditorViewModel(document, selection, messenger);
            var editor = new ScoreEditorViewModel(document, selection, navigation, chordEditor, messenger);

            var result = editor.Delete();

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Ties);
        }

        [Fact]
        public void Delete_RemovesSelectedNote()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 3 });
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var navigation = new MeasureNavigationViewModel(document, selection, messenger);
            var chordEditor = new ChordEditorViewModel(document, selection, messenger);
            var editor = new ScoreEditorViewModel(document, selection, navigation, chordEditor, messenger);

            var result = editor.Delete();

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Measures[0].MelodyNotes);
        }
    }
}
