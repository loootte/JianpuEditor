using JianpuEditor.Models;
using Xunit;

namespace JianpuEditor.Tests.Models
{
    public class NoteSelectionRangeTests
    {
        [Fact]
        public void Enumerate_ReturnsNotesWithinSingleMeasure()
        {
            var score = new JianpuScore();
            score.Measures.Add(new JianpuMeasure());
            score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 2 });
            score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 3 });

            var range = NoteSelectionRange.Enumerate(
                score,
                new ScoreNoteRef(0, 0),
                new ScoreNoteRef(0, 2));

            Assert.Equal(3, range.Count);
            Assert.Equal(new ScoreNoteRef(0, 0), range[0]);
            Assert.Equal(new ScoreNoteRef(0, 2), range[2]);
        }

        [Fact]
        public void Enumerate_SupportsCrossMeasureRange()
        {
            var score = new JianpuScore();
            score.Measures.Add(new JianpuMeasure());
            score.Measures.Add(new JianpuMeasure());
            score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 2 });
            score.Measures[1].MelodyNotes.Add(new JianpuNote { Pitch = 3 });

            var range = NoteSelectionRange.Enumerate(
                score,
                new ScoreNoteRef(0, 1),
                new ScoreNoteRef(1, 0));

            Assert.Equal(2, range.Count);
            Assert.Equal(new ScoreNoteRef(0, 1), range[0]);
            Assert.Equal(new ScoreNoteRef(1, 0), range[1]);
        }

        [Fact]
        public void ContainsAll_DetectsFullySelectedRange()
        {
            var selected = new[]
            {
                new ScoreNoteRef(0, 0),
                new ScoreNoteRef(0, 1),
                new ScoreNoteRef(0, 2)
            };
            var range = new[]
            {
                new ScoreNoteRef(0, 1),
                new ScoreNoteRef(0, 2)
            };

            Assert.True(NoteSelectionRange.ContainsAll(selected, range));
            Assert.False(NoteSelectionRange.ContainsAll(selected, new[] { new ScoreNoteRef(0, 0), new ScoreNoteRef(1, 0) }));
        }
    }
}