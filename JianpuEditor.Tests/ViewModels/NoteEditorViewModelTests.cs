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
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
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
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
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
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, InsertIndex = 0 });

            editor.AddRest();

            Assert.Equal(NoteType.Rest, document.Score.Measures[0].MelodyNotes[0].Type);
        }

        [Fact]
        public void AppendNote_WithSelectedNote_AppendsToMeasureEnd()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history, navigation);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 2 });
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            var result = editor.AppendNote(5);

            Assert.Equal(3, document.Score.Measures[0].MelodyNotes.Count);
            Assert.Equal(1, document.Score.Measures[0].MelodyNotes[0].Pitch);
            Assert.Equal(5, document.Score.Measures[0].MelodyNotes[2].Pitch);
            Assert.Equal(2, result.SelectNoteIndex);
        }

        [Fact]
        public void AppendNote_UsesCurrentMeasureIndex()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history, navigation);
            document.EnsureMeasures();
            document.Score.Measures.Add(new JianpuMeasure());
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            document.Score.Measures[1].MelodyNotes.Add(new JianpuNote { Pitch = 2 });
            navigation.SelectMeasure(1);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            var result = editor.AppendNote(6);

            Assert.Equal(2, document.Score.Measures[1].MelodyNotes.Count);
            Assert.Equal(6, document.Score.Measures[1].MelodyNotes[1].Pitch);
            Assert.Equal(1, result.SelectNoteMeasureIndex);
            Assert.Equal(1, result.SelectNoteIndex);
        }

        [Fact]
        public void AppendNote_WithCopyStyle_CopiesPreviousDurationAndOctave()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history, navigation);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote
            {
                Pitch = 3,
                Octave = 1,
                Underlines = 1,
                Dashes = 0,
                Dotted = true
            });

            editor.AppendNote(5, copyPreviousNoteStyle: true);

            var appended = document.Score.Measures[0].MelodyNotes[1];
            Assert.Equal(5, appended.Pitch);
            Assert.Equal(1, appended.Octave);
            Assert.Equal(1, appended.Underlines);
            Assert.True(appended.Dotted);
        }

        [Fact]
        public void AppendRest_AppendsToMeasureEnd()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger, history, navigation);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 4 });
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            editor.AppendRest();

            Assert.Equal(2, document.Score.Measures[0].MelodyNotes.Count);
            Assert.Equal(NoteType.Rest, document.Score.Measures[0].MelodyNotes[1].Type);
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
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
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
        public void TransposePitch_RaisesSelectedNoteWithinKey()
        {
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
            document.EnsureMeasures();
            var note = new JianpuNote { Pitch = 5, Octave = 0 };
            document.Score.Measures[0].MelodyNotes.Add(note);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            editor.TransposePitch(1);

            Assert.Equal(6, note.Pitch);
            Assert.Equal(0, note.Octave);
        }

        [Fact]
        public void DecreaseDuration_StepsThroughTiersDownToSixteenth()
        {
            var (document, selection, messenger, _) = ViewModelTestHelper.CreateDocumentWithSelection();
            var editor = ViewModelTestHelper.CreateNoteEditor(document, selection, messenger);
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
