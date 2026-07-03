using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class ScoreEditorCommandTests
    {
        [Fact]
        public void AddMeasure_Undo_RestoresPreviousCount()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();

            navigation.AddMeasure();
            Assert.Equal(2, navigation.MeasureCount);

            history.Undo();

            Assert.Equal(1, navigation.MeasureCount);
        }

        [Fact]
        public void Delete_Undo_RestoresRemovedNote()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            var editor = ViewModelTestHelper.CreateScoreEditor(document, selection, navigation, chordEditor, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 3 });
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            editor.Delete();
            Assert.Empty(document.Score.Measures[0].MelodyNotes);

            history.Undo();

            Assert.Single(document.Score.Measures[0].MelodyNotes);
            Assert.Equal(3, document.Score.Measures[0].MelodyNotes[0].Pitch);
        }

        [Fact]
        public void TransposeChords_Undo_RestoresKeySignature()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var transpose = new FakeChordTransposeService();
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, transpose, history);
            document.EnsureMeasures();
            document.KeySignature = "1=C";
            document.Score.Measures[0].ChordMarkers.Add(new ChordMarker { Text = "C", BeatPosition = 0 });

            chordEditor.TransposeChords("1=G");
            Assert.Equal("1=G", document.KeySignature);

            history.Undo();

            Assert.Equal("1=C", document.KeySignature);
        }

        [Fact]
        public void TryCompleteTie_Undo_RemovesAddedTie()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var history = ViewModelTestHelper.CreateHistory(messenger);
            var document = ViewModelTestHelper.CreateDocument(messenger, history);
            var selection = new ScoreSelectionViewModel(document);
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var tieEditor = ViewModelTestHelper.CreateTieEditor(document, navigation, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 2 });

            tieEditor.ToggleTieMode();
            tieEditor.TryCompleteTie(0, 0);
            tieEditor.TryCompleteTie(0, 1);

            Assert.Single(document.Score.Ties);

            history.Undo();

            Assert.Empty(document.Score.Ties);
        }
    }
}
