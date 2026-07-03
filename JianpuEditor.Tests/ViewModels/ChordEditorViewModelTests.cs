using JianpuEditor.Models;
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
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, transpose, history);
            document.EnsureMeasures();
            document.KeySignature = "1=C";
            document.Score.Measures[0].ChordMarkers.Add(new ChordMarker { Text = "C", BeatPosition = 0 });

            var result = chordEditor.TransposeChords("1=G");

            Assert.True(result.Changed);
            Assert.Equal("1=G", document.KeySignature);
        }

        [Fact]
        public void TransposeChords_ReturnsErrorWhenServiceFails()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var transpose = new FakeChordTransposeService { ShouldSucceed = false, ErrorMessage = "无效调号" };
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var chordEditor = ViewModelTestHelper.CreateChordEditor(document, selection, navigation, messenger, transpose, history);
            document.EnsureMeasures();
            document.KeySignature = "1=C";

            var result = chordEditor.TransposeChords("1=Z");

            Assert.False(result.Changed);
            Assert.Equal("无效调号", result.Message);
            Assert.Equal("1=C", document.KeySignature);
        }
    }
}
