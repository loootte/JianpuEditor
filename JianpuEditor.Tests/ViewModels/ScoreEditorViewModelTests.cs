using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class ScoreEditorViewModelTests
    {
        [Fact]
        public void Delete_RemovesSelectedTieFirst()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Ties = new System.Collections.Generic.List<JianpuTie>
            {
                new JianpuTie { StartMeasureIndex = 0, StartNoteIndex = 0, EndMeasureIndex = 0, EndNoteIndex = 1 }
            };
            selection.UpdateFrom(new ScoreSelectionInfo { TieIndex = 0 });
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            var editor = ViewModelTestHelper.CreateScoreEditor(document, selection, navigation, chordEditor, messenger, history);

            var result = editor.Delete();

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Ties);
        }

        [Fact]
        public void Delete_RemovesOrnamentBeforeNote()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 3 });
            OrnamentService.TryAddOrnament(document.Score.Measures[0], 0, OrnamentType.Trill);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            var editor = ViewModelTestHelper.CreateScoreEditor(document, selection, navigation, chordEditor, messenger, history);

            var result = editor.Delete();

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Measures[0].Ornaments);
            Assert.Single(document.Score.Measures[0].MelodyNotes);
        }

        [Fact]
        public void Delete_RemovesSelectedNote()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 3 });
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            var editor = ViewModelTestHelper.CreateScoreEditor(document, selection, navigation, chordEditor, messenger, history);

            var result = editor.Delete();

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Measures[0].MelodyNotes);
        }
    }
}
