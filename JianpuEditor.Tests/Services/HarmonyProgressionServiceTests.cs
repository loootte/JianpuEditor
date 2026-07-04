using System.Linq;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class HarmonyProgressionServiceTests
    {
        [Fact]
        public void SuggestForMeasureRange_FourMeasuresWithBass1451_PrefersClassicalCadence()
        {
            var measures = new[]
            {
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(4)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(5)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1))
            };

            var suggestions = HarmonySuggestionService.SuggestForMeasureRange(measures, "1=C");

            Assert.NotEmpty(suggestions);
            Assert.Equal("I - IV - V - I", suggestions[0].Label);
            Assert.Equal(4, suggestions[0].Steps.Count);
            Assert.Equal("C", suggestions[0].Steps[0].ChordSymbol);
            Assert.Equal("F", suggestions[0].Steps[1].ChordSymbol);
            Assert.Equal("G", suggestions[0].Steps[2].ChordSymbol);
            Assert.Equal("C", suggestions[0].Steps[3].ChordSymbol);
            Assert.Contains("低音走向 1→4→5→1", suggestions[0].BassLineSummary);
        }

        [Fact]
        public void SuggestForMeasureRange_ThreeMeasures_ReturnsUpToThreeProgressions()
        {
            var measures = new[]
            {
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(6)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(4))
            };

            var suggestions = HarmonySuggestionService.SuggestForMeasureRange(measures, "1=C");

            Assert.InRange(suggestions.Count, 1, 3);
            Assert.Equal(3, suggestions[0].Steps.Count);
            Assert.Contains(suggestions[0].Steps.Select(step => step.ChordSymbol), symbol => symbol == "C" || symbol == "Am" || symbol == "F");
        }

        [Fact]
        public void SuggestForMeasureRange_SingleMeasure_ReturnsEmpty()
        {
            var measures = new[] { ScoreTestHelper.Measure(ScoreTestHelper.Note(1)) };
            var suggestions = HarmonySuggestionService.SuggestForMeasureRange(measures, "1=C");
            Assert.Empty(suggestions);
        }
    }
}
