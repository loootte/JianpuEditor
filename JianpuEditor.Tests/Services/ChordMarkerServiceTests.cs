using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class ChordMarkerServiceTests
    {
        [Fact]
        public void ImportLegacyChordText_SplitsTokensAcrossMeasureDuration()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));

            ChordMarkerService.ImportLegacyChordText(measure, "D    Bm7");

            Assert.Equal(2, measure.ChordMarkers.Count);
            Assert.Equal("D", measure.ChordMarkers[0].Text);
            Assert.Equal(0, measure.ChordMarkers[0].BeatPosition);
            Assert.Equal("Bm7", measure.ChordMarkers[1].Text);
            Assert.Equal(2, measure.ChordMarkers[1].BeatPosition);
        }

        [Fact]
        public void TryAddMarker_EnforcesMaxMarkersPerMeasure()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2), ScoreTestHelper.Note(3), ScoreTestHelper.Note(4));
            for (var i = 0; i < JianpuMeasure.MaxChordMarkers; i++)
            {
                Assert.True(ChordMarkerService.TryAddMarker(measure, i, "C"));
            }

            Assert.False(ChordMarkerService.TryAddMarker(measure, 0, "G"));
            Assert.Equal(JianpuMeasure.MaxChordMarkers, measure.ChordMarkers.Count);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1.4, 1)]
        [InlineData(3.6, 3)]
        public void SnapBeatPosition_ClampsAndRounds(double input, double expected)
        {
            Assert.Equal(expected, ChordMarkerService.SnapBeatPosition(input, 4));
        }

        [Fact]
        public void SetMarkerBeat_SwapsWhenTargetBeatOccupied()
        {
            var measure = ScoreTestHelper.MeasureWithChords(
                new[] { "C", "G" },
                new[] { 0d, 2d },
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));

            ChordMarkerService.SetMarkerBeat(measure, 0, 2);

            Assert.Equal(2, measure.ChordMarkers.Single(marker => marker.Text == "C").BeatPosition);
            Assert.Equal(0, measure.ChordMarkers.Single(marker => marker.Text == "G").BeatPosition);
        }
    }
}