using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class MeasureNormalizationServiceTests
    {
        [Fact]
        public void NormalizeMeasures_SplitsLongMeasureIntoFourBeatBars()
        {
            var measures = new List<JianpuMeasure>
            {
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(1, dashes: 3),
                    ScoreTestHelper.Note(2))
            };

            var normalized = MeasureNormalizationService.NormalizeMeasures(measures, 4);

            Assert.True(normalized.Count >= 2);
            Assert.All(normalized, measure =>
                Assert.Equal(4, GetMeasureBeats(measure), 2));
        }

        [Fact]
        public void NormalizeMeasures_PadsShortMeasureWithRests()
        {
            var measures = new List<JianpuMeasure>
            {
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2))
            };

            var normalized = MeasureNormalizationService.NormalizeMeasures(measures, 4);

            Assert.Single(normalized);
            Assert.Equal(4, GetMeasureBeats(normalized[0]), 2);
        }

        [Fact]
        public void NormalizeMeasures_MergesVeryShortTrailingMeasure()
        {
            var measures = new List<JianpuMeasure>
            {
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(4))
            };

            var normalized = MeasureNormalizationService.NormalizeMeasures(measures, 4);

            Assert.Single(normalized);
            Assert.Equal(4, normalized[0].MelodyNotes.Count(note => note.Type == NoteType.Note));
        }

        private static double GetMeasureBeats(JianpuMeasure measure)
        {
            var total = 0.0;
            foreach (var note in measure.MelodyNotes)
            {
                total += JianpuRenderer.GetDurationUnits(note);
            }

            return total;
        }
    }
}
