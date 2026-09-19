using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class MeasureRestServiceTests
    {
        [Fact]
        public void IsRestOnly_EmptyMeasure_IsTrue()
        {
            Assert.True(MeasureRestService.IsRestOnly(new JianpuMeasure()));
            Assert.True(MeasureRestService.IsRestOnly(null));
        }

        [Fact]
        public void IsRestOnly_AllRests_IsTrue()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(underlines: 1),
                ScoreTestHelper.Rest(underlines: 2),
                ScoreTestHelper.Rest());

            Assert.True(MeasureRestService.IsRestOnly(measure));
        }

        [Fact]
        public void IsRestOnly_ContainsMelodyNote_IsFalse()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest());

            Assert.False(MeasureRestService.IsRestOnly(measure));
        }

        [Fact]
        public void IsRestOnly_SimultaneousSoundingNote_IsFalse()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest());
            measure.Chords = new List<JianpuChord>
            {
                new JianpuChord
                {
                    BeatPosition = 0,
                    Notes = new List<JianpuNote> { ScoreTestHelper.Note(5) }
                }
            };

            Assert.False(MeasureRestService.IsRestOnly(measure));
        }
    }
}
