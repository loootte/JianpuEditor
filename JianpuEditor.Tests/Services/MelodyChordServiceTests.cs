using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class MelodyChordServiceTests
    {
        [Fact]
        public void NormalizeMeasure_BuildsChordsFromLegacyMelodyNotes()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3));

            MelodyChordService.NormalizeMeasure(measure);

            Assert.Equal(3, measure.Chords.Count);
            Assert.Equal(0, measure.Chords[0].BeatPosition, 3);
            Assert.Equal(1, measure.Chords[1].BeatPosition, 3);
            Assert.Equal(2, measure.Chords[2].BeatPosition, 3);
            Assert.Single(measure.Chords[0].Notes);
            Assert.Equal(1, measure.Chords[0].Notes[0].Pitch, 3);
        }

        [Fact]
        public void NormalizeMeasure_SyncsMelodyNotesFromMultiNoteChord()
        {
            var measure = new JianpuMeasure
            {
                Chords = new List<JianpuChord>
                {
                    new JianpuChord
                    {
                        BeatPosition = 0,
                        Notes = new List<JianpuNote>
                        {
                            ScoreTestHelper.Note(1),
                            ScoreTestHelper.Note(3, octave: 1)
                        }
                    }
                }
            };

            MelodyChordService.NormalizeMeasure(measure);

            Assert.Single(measure.MelodyNotes);
            Assert.Equal(2, measure.Chords[0].Notes.Count);
            Assert.Equal(1, measure.MelodyNotes[0].Pitch, 3);
        }

        [Fact]
        public void GetNotesAtSlot_ReturnsAllSimultaneousNotes()
        {
            var measure = new JianpuMeasure
            {
                Chords = new List<JianpuChord>
                {
                    new JianpuChord
                    {
                        BeatPosition = 0,
                        Notes = new List<JianpuNote>
                        {
                            ScoreTestHelper.Note(1),
                            ScoreTestHelper.Note(3)
                        }
                    },
                    new JianpuChord
                    {
                        BeatPosition = 1,
                        Notes = new List<JianpuNote> { ScoreTestHelper.Note(5) }
                    }
                }
            };

            MelodyChordService.NormalizeMeasure(measure);

            Assert.Equal(2, MelodyChordService.GetNotesAtSlot(measure, 0).Count);
            Assert.Single(MelodyChordService.GetNotesAtSlot(measure, 1));
            Assert.True(MelodyChordService.HasSimultaneousNotes(measure, 0));
            Assert.False(MelodyChordService.HasSimultaneousNotes(measure, 1));
        }

        [Fact]
        public void Build_SchedulesAllNotesInSimultaneousChord()
        {
            var measure = new JianpuMeasure
            {
                Chords = new List<JianpuChord>
                {
                    new JianpuChord
                    {
                        BeatPosition = 0,
                        Notes = new List<JianpuNote>
                        {
                            ScoreTestHelper.Note(1),
                            ScoreTestHelper.Note(3)
                        }
                    }
                }
            };
            MelodyChordService.NormalizeMeasure(measure);
            var score = ScoreTestHelper.CreateScore(measure);

            var schedule = ScoreMidiSchedule.Build(score);
            var melody = schedule.Notes.Where(note => note.Channel == ScoreMidiSchedule.MelodyChannel).ToList();

            Assert.Equal(2, melody.Count);
            Assert.Equal(0, melody[0].StartQuarter, 3);
            Assert.Equal(0, melody[1].StartQuarter, 3);
            Assert.NotEqual(melody[0].MidiNote, melody[1].MidiNote);
        }
    }
}