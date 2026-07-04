using JianpuEditor.Models;
using JianpuEditor.Tests.Helpers;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class ChordEditorViewModelTests
    {
        [Fact]
        public void AddChordMarker_AddsMarkerToCurrentMeasure()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0 });

            var result = chordEditor.AddChordMarker();

            Assert.True(result.Changed);
            Assert.Single(document.Score.Measures[0].ChordMarkers);
        }

        [Fact]
        public void TransposeChords_UpdatesKeySignature()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var transpose = new FakeChordTransposeService();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, transpose, history: history);
            document.EnsureMeasures();
            document.KeySignature = "1=C";
            document.Score.Measures[0].ChordMarkers.Add(new ChordMarker { Text = "C", BeatPosition = 0 });

            var result = chordEditor.TransposeChords("1=G");

            Assert.True(result.Changed);
            Assert.Equal("1=G", document.KeySignature);
        }

        [Fact]
        public void GetHarmonySuggestions_CMajor_ReturnsPrimaryChords()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            document.EnsureMeasures();
            document.KeySignature = "1=C";
            document.Score.Measures[0].MelodyNotes = new System.Collections.Generic.List<JianpuNote>
            {
                ScoreTestHelper.Note(1)
            };

            var suggestions = chordEditor.GetHarmonySuggestions(0, 0);

            Assert.NotEmpty(suggestions);
            Assert.Equal("C", suggestions[0].ChordSymbol);
            Assert.Equal("I", suggestions[0].RomanNumeral);
        }

        [Fact]
        public void ApplyHarmonySuggestion_AddsChordMarkerAtBeat()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, history: history);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });

            var result = chordEditor.ApplyHarmonySuggestion(0, 0, "G");

            Assert.True(result.Changed);
            Assert.Single(document.Score.Measures[0].ChordMarkers);
            Assert.Equal("G", document.Score.Measures[0].ChordMarkers[0].Text);
        }

        [Fact]
        public void TransposeChords_ReturnsErrorWhenServiceFails()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var transpose = new FakeChordTransposeService { ShouldSucceed = false, ErrorMessage = "无效调号" };
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, transpose, history: history);
            document.EnsureMeasures();
            document.KeySignature = "1=C";

            var result = chordEditor.TransposeChords("1=Z");

            Assert.False(result.Changed);
            Assert.Equal("无效调号", result.Message);
            Assert.Equal("1=C", document.KeySignature);
        }
    }
}
