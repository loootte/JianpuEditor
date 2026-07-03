using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class ScoreMidiScheduleTests
    {
        [Fact]
        public void GetMeasureDurationUnits_UsesNoteDurationsWhenPresent()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1, dashes: 1),
                ScoreTestHelper.Note(2, underlines: 1));

            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);

            Assert.Equal(2.5, duration, 3);
        }

        [Fact]
        public void GetMeasureDurationUnits_FallsBackToDefaultBeatsForEmptyMeasure()
        {
            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(new JianpuMeasure());

            Assert.Equal(ScoreMidiSchedule.DefaultMeasureBeats, duration);
        }

        [Fact]
        public void Build_SchedulesMelodyAndChordNotes()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.MeasureWithChords(
                    new[] { "C" },
                    new[] { 0d },
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)));

            var schedule = ScoreMidiSchedule.Build(score);

            Assert.True(schedule.TotalQuarterLength >= 4);
            Assert.Contains(schedule.Notes, note => note.Channel == ScoreMidiSchedule.MelodyChannel);
            Assert.Contains(schedule.Notes, note => note.Channel == ScoreMidiSchedule.ChordChannel);
        }

        [Fact]
        public void Build_SuppressesTieEndNotes()
        {
            var score = ScoreTestHelper.CreateScore(ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1, dashes: 1),
                ScoreTestHelper.Note(1)));
            score.Ties.Add(new JianpuTie
            {
                StartMeasureIndex = 0,
                StartNoteIndex = 0,
                EndMeasureIndex = 0,
                EndNoteIndex = 1
            });

            var schedule = ScoreMidiSchedule.Build(score);
            var melodyNotes = schedule.Notes.Where(note => note.Channel == ScoreMidiSchedule.MelodyChannel).ToList();

            Assert.Single(melodyNotes);
            Assert.Equal(3, melodyNotes[0].DurationQuarter, 3);
        }

        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 2)]
        [InlineData(0, 1, 0.5)]
        public void GetDurationUnits_MatchesRenderer(int dashes, int underlines, double expected)
        {
            var note = ScoreTestHelper.Note(1, dashes: dashes, underlines: underlines);

            Assert.Equal(expected, JianpuRenderer.GetDurationUnits(note), 3);
        }
    }
}
