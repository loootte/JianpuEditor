using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class ChordTransposeServiceTests
    {
        [Fact]
        public void TryTransposeChords_TransposesChordSymbolsAndKeySignature()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.MeasureWithChords(
                    new[] { "C", "Am", "间奏" },
                    new[] { 0d, 2d, 3d },
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)));
            score.KeySignature = "1=C";

            var ok = ChordTransposeService.TryTransposeChords(score, "G", out var error, out var count);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(2, count);
            Assert.Equal("1=G", score.KeySignature);
            Assert.Equal("G", score.Measures[0].ChordMarkers[0].Text);
            Assert.Equal("Em", score.Measures[0].ChordMarkers[1].Text);
            Assert.Equal("间奏", score.Measures[0].ChordMarkers[2].Text);
        }

        [Fact]
        public void TryTransposeChords_RejectsSameKey()
        {
            var score = ScoreTestHelper.CreateScore(ScoreTestHelper.Measure(ScoreTestHelper.Note(1)));
            score.KeySignature = "1=C";

            var ok = ChordTransposeService.TryTransposeChords(score, "C", out var error, out var count);

            Assert.False(ok);
            Assert.Contains("无需转调", error);
            Assert.Equal(0, count);
        }

        [Fact]
        public void TryTransposeChords_RejectsNullScore()
        {
            var ok = ChordTransposeService.TryTransposeChords(null, "G", out var error, out var count);

            Assert.False(ok);
            Assert.NotNull(error);
            Assert.Equal(0, count);
        }
    }
}
