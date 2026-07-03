using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class NoteEditorSplitMergeTests
    {
        [Fact]
        public void SplitSelectedNotes_DividesQuarterIntoTwoEighths()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
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
            Assert.Equal(1, document.Score.Measures[0].MelodyNotes[0].Underlines);
            Assert.Equal(1, document.Score.Measures[0].MelodyNotes[1].Underlines);
        }

        [Fact]
        public void MergeSelectedNotes_CombinesAdjacentQuartersIntoExtension()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1, Underlines = 0 });
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 2, Underlines = 0 });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                NoteIndex = 0,
                SelectedNotes = new[]
                {
                    new ScoreNoteRef(0, 0),
                    new ScoreNoteRef(0, 1)
                }
            });

            editor.MergeSelectedNotes();

            Assert.Single(document.Score.Measures[0].MelodyNotes);
            Assert.Equal(1, document.Score.Measures[0].MelodyNotes[0].Pitch);
            Assert.Equal(1, document.Score.Measures[0].MelodyNotes[0].Dashes);
        }
    }
}