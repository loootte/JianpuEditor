using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class OrnamentPlaybackServiceTests
    {
        [Fact]
        public void ScheduleMelodyNote_GraceNote_PlaysNeighborBeforeMain()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(3));
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.GraceNote);

            var events = OrnamentPlaybackService.ScheduleMelodyNote(
                measure,
                measure.MelodyNotes[0],
                0,
                0,
                1,
                ScoreMidiSchedule.DefaultTonicMidi,
                ScoreMidiSchedule.MelodyChannel,
                ScoreMidiSchedule.MelodyVelocity);

            Assert.Equal(2, events.Count);
            Assert.Equal(0, events[0].StartQuarter, 3);
            Assert.True(events[0].DurationQuarter < 1);
            Assert.Equal(ScoreMidiSchedule.ToMelodyMidiNote(ScoreTestHelper.Note(2), ScoreMidiSchedule.DefaultTonicMidi), events[0].MidiNote);
            Assert.True(events[1].StartQuarter > events[0].StartQuarter);
            Assert.Equal(ScoreMidiSchedule.ToMelodyMidiNote(ScoreTestHelper.Note(3), ScoreMidiSchedule.DefaultTonicMidi), events[1].MidiNote);
        }

        [Fact]
        public void ScheduleMelodyNote_Trill_AlternatesUpperNeighbor()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(3));
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.Trill);

            var events = OrnamentPlaybackService.ScheduleMelodyNote(
                measure,
                measure.MelodyNotes[0],
                0,
                0,
                1,
                ScoreMidiSchedule.DefaultTonicMidi,
                ScoreMidiSchedule.MelodyChannel,
                ScoreMidiSchedule.MelodyVelocity);

            Assert.True(events.Count >= 4);
            Assert.Equal(
                ScoreMidiSchedule.ToMelodyMidiNote(ScoreTestHelper.Note(3), ScoreMidiSchedule.DefaultTonicMidi),
                events[0].MidiNote);
            Assert.Equal(
                ScoreMidiSchedule.ToMelodyMidiNote(ScoreTestHelper.Note(4), ScoreMidiSchedule.DefaultTonicMidi),
                events[1].MidiNote);
        }

        [Fact]
        public void ScheduleMelodyNote_Fermata_ExtendsDuration()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1));
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.Fermata);

            var events = OrnamentPlaybackService.ScheduleMelodyNote(
                measure,
                measure.MelodyNotes[0],
                0,
                0,
                2,
                ScoreMidiSchedule.DefaultTonicMidi,
                ScoreMidiSchedule.MelodyChannel,
                ScoreMidiSchedule.MelodyVelocity);

            Assert.Single(events);
            Assert.Equal(3, events[0].DurationQuarter, 3);
        }

        [Fact]
        public void Build_IncludesOrnamentExpandedMelodyNotes()
        {
            var score = ScoreTestHelper.CreateScore(ScoreTestHelper.Measure(ScoreTestHelper.Note(3)));
            OrnamentService.TryAddOrnament(score.Measures[0], 0, OrnamentType.Trill);

            var schedule = ScoreMidiSchedule.Build(score);
            var melody = schedule.Notes.Where(note => note.Channel == ScoreMidiSchedule.MelodyChannel).ToList();

            Assert.True(melody.Count >= 4);
        }
    }
}
