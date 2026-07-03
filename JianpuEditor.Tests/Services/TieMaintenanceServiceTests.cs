using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class TieMaintenanceServiceTests
    {
        [Fact]
        public void OnNoteRemoved_RemovesTieReferencingDeletedNote()
        {
            var score = ScoreTestHelper.CreateScore(ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3)));
            score.Ties.Add(new JianpuTie { StartMeasureIndex = 0, StartNoteIndex = 0, EndMeasureIndex = 0, EndNoteIndex = 2 });

            TieMaintenanceService.OnNoteRemoved(score, 0, 0);

            Assert.Empty(score.Ties);
        }

        [Fact]
        public void OnNoteRemoved_ShiftsLaterNoteIndicesInSameMeasure()
        {
            var score = ScoreTestHelper.CreateScore(ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4)));
            score.Ties.Add(new JianpuTie { StartMeasureIndex = 0, StartNoteIndex = 0, EndMeasureIndex = 0, EndNoteIndex = 3 });

            TieMaintenanceService.OnNoteRemoved(score, 0, 1);

            Assert.Single(score.Ties);
            Assert.Equal(2, score.Ties[0].EndNoteIndex);
        }

        [Fact]
        public void OnMeasureRemoved_RemovesTiesTouchingMeasureAndShiftsLaterMeasures()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(2)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(3)));
            score.Ties.Add(new JianpuTie { StartMeasureIndex = 0, StartNoteIndex = 0, EndMeasureIndex = 1, EndNoteIndex = 0 });
            score.Ties.Add(new JianpuTie { StartMeasureIndex = 2, StartNoteIndex = 0, EndMeasureIndex = 2, EndNoteIndex = 0 });

            TieMaintenanceService.OnMeasureRemoved(score, 1);

            Assert.Single(score.Ties);
            Assert.Equal(1, score.Ties[0].StartMeasureIndex);
            Assert.Equal(1, score.Ties[0].EndMeasureIndex);
        }
    }
}