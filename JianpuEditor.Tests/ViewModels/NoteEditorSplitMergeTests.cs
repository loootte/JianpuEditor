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
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
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
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
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

        [Fact]
        public void MergeSelectedNotes_PairsAllSelectedNotesWithinMeasure()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
            document.EnsureMeasures();
            var measure = document.Score.Measures[0].MelodyNotes;
            measure.Add(new JianpuNote { Pitch = 1, Underlines = 0 });
            measure.Add(new JianpuNote { Pitch = 2, Underlines = 0 });
            measure.Add(new JianpuNote { Pitch = 3, Underlines = 0 });
            measure.Add(new JianpuNote { Pitch = 4, Underlines = 0 });
            measure.Add(new JianpuNote { Pitch = 5, Underlines = 0 });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                NoteIndex = 0,
                SelectedNotes = new[]
                {
                    new ScoreNoteRef(0, 0),
                    new ScoreNoteRef(0, 1),
                    new ScoreNoteRef(0, 2),
                    new ScoreNoteRef(0, 3),
                    new ScoreNoteRef(0, 4)
                }
            });

            editor.MergeSelectedNotes();

            Assert.Equal(3, measure.Count);
            Assert.Equal(1, measure[0].Dashes);
            Assert.Equal(1, measure[1].Dashes);
            Assert.Equal(5, measure[2].Pitch);
            Assert.Equal(0, measure[2].Dashes);
        }
    }
}
