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

        [Theory]
        [InlineData(0, 2, 0)]
        [InlineData(1, 1, 0)]
        [InlineData(2, 0, 0)]
        [InlineData(3, 0, 1)]
        [InlineData(4, 0, 2)]
        [InlineData(5, 0, 3)]
        public void ApplyDurationTier_SetsUnderlinesAndDashes(int tier, int underlines, int dashes)
        {
            var note = new JianpuNote();

            NoteEditorViewModel.ApplyDurationTier(note, tier);

            Assert.Equal(underlines, note.Underlines);
            Assert.Equal(dashes, note.Dashes);
            Assert.Equal(tier, NoteEditorViewModel.GetDurationTier(note));
        }

        [Fact]
        public void IncreaseDuration_StepsThroughTiersUpToExtension3()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            var note = new JianpuNote { Underlines = 2 };
            document.Score.Measures[0].MelodyNotes.Add(note);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            editor.IncreaseDuration();
            Assert.Equal(1, note.Underlines);
            Assert.Equal(0, note.Dashes);

            editor.IncreaseDuration();
            Assert.Equal(0, note.Underlines);
            Assert.Equal(0, note.Dashes);

            editor.IncreaseDuration();
            Assert.Equal(0, note.Underlines);
            Assert.Equal(1, note.Dashes);

            editor.IncreaseDuration();
            Assert.Equal(0, note.Underlines);
            Assert.Equal(2, note.Dashes);

            editor.IncreaseDuration();
            Assert.Equal(0, note.Underlines);
            Assert.Equal(3, note.Dashes);

            var atMax = editor.IncreaseDuration();
            Assert.Equal(3, note.Dashes);
            Assert.Contains("已达最长时值", atMax.Message);
        }

        [Fact]
        public void DecreaseDuration_StepsThroughTiersDownToSixteenth()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = new NoteEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            var note = new JianpuNote { Dashes = 3 };
            document.Score.Measures[0].MelodyNotes.Add(note);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            editor.DecreaseDuration();
            Assert.Equal(2, note.Dashes);

            editor.DecreaseDuration();
            Assert.Equal(1, note.Dashes);

            editor.DecreaseDuration();
            Assert.Equal(0, note.Underlines);
            Assert.Equal(0, note.Dashes);

            editor.DecreaseDuration();
            Assert.Equal(1, note.Underlines);
            Assert.Equal(0, note.Dashes);

            editor.DecreaseDuration();
            Assert.Equal(2, note.Underlines);
            Assert.Equal(0, note.Dashes);

            var atMin = editor.DecreaseDuration();
            Assert.Equal(2, note.Underlines);
            Assert.Contains("已达最短时值", atMin.Message);
        }
    }
}
