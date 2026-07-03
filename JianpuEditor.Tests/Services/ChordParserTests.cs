using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class ChordParserTests
    {
        [Theory]
        [InlineData("C", true)]
        [InlineData("Bm7", true)]
        [InlineData("G/D", true)]
        [InlineData("F#maj7", true)]
        [InlineData("间奏", false)]
        [InlineData("Cmaj", true)]
        public void IsChordSymbol_DetectsValidSymbols(string token, bool expected)
        {
            Assert.Equal(expected, ChordParser.IsChordSymbol(token));
        }

        [Theory]
        [InlineData("C", 7, "G")]
        [InlineData("Am", 7, "Em")]
        [InlineData("G/D", 7, "D/A")]
        [InlineData("Bb", 2, "C")]
        public void TransposeSymbol_TransposesRootAndSlashBass(string chord, int semitones, string expected)
        {
            Assert.Equal(expected, ChordParser.TransposeSymbol(chord, semitones));
        }

        [Fact]
        public void TransposeSymbol_ReturnsOriginalWhenSemitonesZero()
        {
            Assert.Equal("Dm7", ChordParser.TransposeSymbol("Dm7", 0));
        }

        [Fact]
        public void ToBlockChordMidiNotes_BuildsMajorTriad()
        {
            var notes = ChordParser.ToBlockChordMidiNotes("C");

            Assert.Equal(3, notes.Count);
            Assert.Equal(new[] { 60, 64, 67 }, notes);
        }

        [Fact]
        public void ExtractScheduledChords_ReturnsOnlyValidChordMarkers()
        {
            var measure = ScoreTestHelper.MeasureWithChords(
                new[] { "C", "间奏", "G" },
                new[] { 0d, 1d, 2d },
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));

            var scheduled = ChordParser.ExtractScheduledChords(measure);

            Assert.Equal(2, scheduled.Count);
            Assert.Equal("C", scheduled[0].Symbol);
            Assert.Equal(0, scheduled[0].BeatPosition);
            Assert.Equal("G", scheduled[1].Symbol);
            Assert.Equal(2, scheduled[1].BeatPosition);
        }
    }
}