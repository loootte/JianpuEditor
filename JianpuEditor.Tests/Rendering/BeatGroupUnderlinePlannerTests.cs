using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Rendering
{
    public sealed class BeatGroupUnderlinePlannerTests
    {
        [Fact]
        public void CollectSpans_EighthBetweenSixteenths_BreaksSecondUnderline()
        {
            var notes = new List<JianpuNote>
            {
                ScoreTestHelper.Note(1, underlines: 2),
                ScoreTestHelper.Note(2, underlines: 1),
                ScoreTestHelper.Note(3, underlines: 2)
            };
            var groups = BeatGroupUnderlinePlanner.GroupNotesByQuarterBeat(notes);

            Assert.Single(groups);
            Assert.Equal(new[] { 0, 1, 2 }, groups[0]);

            var firstLine = BeatGroupUnderlinePlanner.CollectSpans(groups[0], notes, 0);
            Assert.Single(firstLine);
            Assert.Equal(0, firstLine[0].StartNoteIndex);
            Assert.Equal(2, firstLine[0].EndNoteIndex);

            var secondLine = BeatGroupUnderlinePlanner.CollectSpans(groups[0], notes, 1);
            Assert.Equal(2, secondLine.Count);
            Assert.Equal(0, secondLine[0].StartNoteIndex);
            Assert.Equal(0, secondLine[0].EndNoteIndex);
            Assert.Equal(2, secondLine[1].StartNoteIndex);
            Assert.Equal(2, secondLine[1].EndNoteIndex);
        }

        [Fact]
        public void CollectSpans_SixteenthPairThenEighth_KeepsSecondLineOnPairOnly()
        {
            var notes = new List<JianpuNote>
            {
                ScoreTestHelper.Note(1, underlines: 2),
                ScoreTestHelper.Note(2, underlines: 2),
                ScoreTestHelper.Note(3, underlines: 1)
            };
            var group = BeatGroupUnderlinePlanner.GroupNotesByQuarterBeat(notes)[0];

            var secondLine = BeatGroupUnderlinePlanner.CollectSpans(group, notes, 1);
            Assert.Single(secondLine);
            Assert.Equal(0, secondLine[0].StartNoteIndex);
            Assert.Equal(1, secondLine[0].EndNoteIndex);
        }

        [Fact]
        public void CollectSpans_EighthThenSixteenthPair_KeepsSecondLineOnPairOnly()
        {
            var notes = new List<JianpuNote>
            {
                ScoreTestHelper.Note(1, underlines: 1),
                ScoreTestHelper.Note(2, underlines: 2),
                ScoreTestHelper.Note(3, underlines: 2)
            };
            var group = BeatGroupUnderlinePlanner.GroupNotesByQuarterBeat(notes)[0];

            var firstLine = BeatGroupUnderlinePlanner.CollectSpans(group, notes, 0);
            Assert.Single(firstLine);
            Assert.Equal(0, firstLine[0].StartNoteIndex);
            Assert.Equal(2, firstLine[0].EndNoteIndex);

            var secondLine = BeatGroupUnderlinePlanner.CollectSpans(group, notes, 1);
            Assert.Single(secondLine);
            Assert.Equal(1, secondLine[0].StartNoteIndex);
            Assert.Equal(2, secondLine[0].EndNoteIndex);
        }

        [Fact]
        public void CollectSpans_FourSixteenths_SingleSpanOnBothLevels()
        {
            var notes = new List<JianpuNote>
            {
                ScoreTestHelper.Note(1, underlines: 2),
                ScoreTestHelper.Note(2, underlines: 2),
                ScoreTestHelper.Note(3, underlines: 2),
                ScoreTestHelper.Note(4, underlines: 2)
            };
            var group = BeatGroupUnderlinePlanner.GroupNotesByQuarterBeat(notes)[0];

            var firstLine = BeatGroupUnderlinePlanner.CollectSpans(group, notes, 0);
            var secondLine = BeatGroupUnderlinePlanner.CollectSpans(group, notes, 1);
            Assert.Single(firstLine);
            Assert.Single(secondLine);
            Assert.Equal(0, firstLine[0].StartNoteIndex);
            Assert.Equal(3, firstLine[0].EndNoteIndex);
            Assert.Equal(0, secondLine[0].StartNoteIndex);
            Assert.Equal(3, secondLine[0].EndNoteIndex);
        }

        [Fact]
        public void RenderToBitmap_EighthBetweenSixteenths_DoesNotThrow()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1, underlines: 2),
                ScoreTestHelper.Note(2, underlines: 1),
                ScoreTestHelper.Note(3, underlines: 2),
                ScoreTestHelper.Note(4),
                ScoreTestHelper.Note(5),
                ScoreTestHelper.Note(6));
            var score = ScoreTestHelper.CreateScore(measure);

            using (var renderer = new JianpuRenderer())
            {
                var bitmap = renderer.RenderToBitmap(score, 1280, ScoreLayoutOptions.Editor);
                Assert.NotNull(bitmap);
            }
        }
    }
}
