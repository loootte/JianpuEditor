using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class NoteSplitMergeServiceTests
    {
        [Fact]
        public void TrySplitNote_QuarterBecomesTwoEighths()
        {
            var source = new JianpuNote { Pitch = 3, Underlines = 0, Dashes = 0 };

            var ok = NoteSplitMergeService.TrySplitNote(source, out var parts);

            Assert.True(ok);
            Assert.Equal(2, parts.Count);
            Assert.Equal(1, NoteEditorViewModel.GetDurationTier(parts[0]));
            Assert.Equal(3, parts[0].Pitch);
        }

        [Fact]
        public void TrySplitNote_ExtendedNoteBecomesQuarterNotes()
        {
            var source = new JianpuNote { Pitch = 2, Dashes = 2 };

            var ok = NoteSplitMergeService.TrySplitNote(source, out var parts);

            Assert.True(ok);
            Assert.Equal(3, parts.Count);
            Assert.All(parts, part => Assert.Equal(2, NoteEditorViewModel.GetDurationTier(part)));
        }

        [Fact]
        public void TrySplitNote_RejectsSixteenth()
        {
            var source = new JianpuNote { Pitch = 1, Underlines = 2 };

            Assert.False(NoteSplitMergeService.TrySplitNote(source, out _));
        }

        [Fact]
        public void CanMerge_RejectsQuarterAndSixteenthPair()
        {
            var quarter = new JianpuNote { Underlines = 0 };
            var sixteenth = new JianpuNote { Underlines = 2 };

            Assert.False(NoteSplitMergeService.CanMerge(quarter, sixteenth));
        }

        [Fact]
        public void TryMergeNotes_MergesQuarterPairIntoExtension()
        {
            var notes = new[]
            {
                new JianpuNote { Pitch = 1, Underlines = 0 },
                new JianpuNote { Pitch = 2, Underlines = 0 }
            };

            var ok = NoteSplitMergeService.TryMergeNotes(notes, out var merged);

            Assert.True(ok);
            Assert.Equal(1, merged.Pitch);
            Assert.Equal(1, merged.Dashes);
            Assert.Equal(3, NoteEditorViewModel.GetDurationTier(merged));
        }

        [Fact]
        public void TryMergeNotes_MergesEighthPairIntoQuarter()
        {
            var notes = new[]
            {
                new JianpuNote { Pitch = 5, Underlines = 1 },
                new JianpuNote { Pitch = 6, Underlines = 1 }
            };

            var ok = NoteSplitMergeService.TryMergeNotes(notes, out var merged);

            Assert.True(ok);
            Assert.Equal(5, merged.Pitch);
            Assert.Equal(2, NoteEditorViewModel.GetDurationTier(merged));
        }

        [Fact]
        public void ApplyMergePairs_MergesAllPairsWithinMeasure()
        {
            var score = new JianpuScore();
            score.Measures.Add(new JianpuMeasure());
            for (var pitch = 1; pitch <= 5; pitch++)
            {
                score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = pitch, Underlines = 0 });
            }

            var refs = new[]
            {
                new ScoreNoteRef(0, 0),
                new ScoreNoteRef(0, 1),
                new ScoreNoteRef(0, 2),
                new ScoreNoteRef(0, 3),
                new ScoreNoteRef(0, 4)
            };

            var mergedCount = NoteSplitMergeService.ApplyMergePairs(score, refs);

            Assert.Equal(2, mergedCount);
            Assert.Equal(3, score.Measures[0].MelodyNotes.Count);
            Assert.Equal(1, score.Measures[0].MelodyNotes[0].Dashes);
            Assert.Equal(1, score.Measures[0].MelodyNotes[1].Dashes);
            Assert.Equal(5, score.Measures[0].MelodyNotes[2].Pitch);
        }

        [Fact]
        public void GetMergePairs_PairsSelectedNotesTwoByTwo()
        {
            var refs = new[]
            {
                new ScoreNoteRef(0, 1),
                new ScoreNoteRef(0, 2),
                new ScoreNoteRef(0, 4),
                new ScoreNoteRef(0, 5)
            };

            var pairs = NoteSplitMergeService.GetMergePairs(refs);

            Assert.Equal(2, pairs.Count);
            Assert.Equal(1, pairs[0].Left.NoteIndex);
            Assert.Equal(2, pairs[0].Right.NoteIndex);
            Assert.Equal(4, pairs[1].Left.NoteIndex);
            Assert.Equal(5, pairs[1].Right.NoteIndex);
        }
    }
}
