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
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var chordEditor = new ChordEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0 });

            var result = chordEditor.AddChordMarker();

            Assert.True(result.Changed);
            Assert.Single(document.Score.Measures[0].ChordMarkers);
        }

        [Fact]
        public void TransposeChords_UpdatesKeySignature()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var chordEditor = new ChordEditorViewModel(document, selection, messenger);
            document.EnsureMeasures();
            document.KeySignature = "1=C";
            document.Score.Measures[0].ChordMarkers.Add(new ChordMarker { Text = "C", BeatPosition = 0 });

            var result = chordEditor.TransposeChords("1=G");

            Assert.True(result.Changed);
            Assert.Equal("1=G", document.KeySignature);
        }
    }
}
