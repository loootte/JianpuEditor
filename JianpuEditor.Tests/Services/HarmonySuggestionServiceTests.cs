using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class HarmonySuggestionServiceTests
    {
        [Theory]
        [InlineData(1, "I", "C")]
        [InlineData(5, "V", "G")]
        [InlineData(6, "vi", "Am")]
        [InlineData(4, "IV", "F")]
        public void SuggestForMeasure_CMajor_ReturnsExpectedPrimaryChord(int melodyDegree, string expectedRoman, string expectedSymbol)
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(melodyDegree));
            var suggestions = HarmonySuggestionService.SuggestForMeasure(measure, "1=C", beatPosition: 0);

            Assert.NotEmpty(suggestions);
            Assert.Contains(suggestions, item => item.RomanNumeral == expectedRoman);
            Assert.Equal(expectedSymbol, suggestions[0].ChordSymbol);
        }

        [Fact]
        public void SuggestForMeasure_CMajor_LimitsToThreeSuggestions()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1));
            var suggestions = HarmonySuggestionService.SuggestForMeasure(measure, "1=C", 0);

            Assert.InRange(suggestions.Count, 1, 3);
        }

        [Fact]
        public void RomanNumeralToChordSymbol_CMajor_MapsCommonDegrees()
        {
            Assert.Equal("C", HarmonySuggestionService.RomanNumeralToChordSymbol("I", 0));
            Assert.Equal("G", HarmonySuggestionService.RomanNumeralToChordSymbol("V", 0));
            Assert.Equal("Am", HarmonySuggestionService.RomanNumeralToChordSymbol("vi", 0));
            Assert.Equal("F", HarmonySuggestionService.RomanNumeralToChordSymbol("IV", 0));
            Assert.Equal("Dm", HarmonySuggestionService.RomanNumeralToChordSymbol("ii", 0));
        }

        [Fact]
        public void SuggestForMeasure_UsesMelodyDegreeAtBeatPosition()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(5));
            var suggestions = HarmonySuggestionService.SuggestForMeasure(measure, "1=C", beatPosition: 1);

            Assert.Equal("V", suggestions[0].RomanNumeral);
            Assert.Equal("G", suggestions[0].ChordSymbol);
        }
    }
}
