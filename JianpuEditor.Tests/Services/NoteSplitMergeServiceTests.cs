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
        public void TryMergeNotes_MergesAdjacentQuartersIntoExtension()
        {
            var notes = new[]
            {
                new JianpuNote { Pitch = 1, Underlines = 0 },
                new JianpuNote { Pitch = 2, Underlines = 0 },
                new JianpuNote { Pitch = 3, Underlines = 0 }
            };

            var ok = NoteSplitMergeService.TryMergeNotes(notes, out var merged);

            Assert.True(ok);
            Assert.Equal(1, merged.Pitch);
            Assert.Equal(2, merged.Dashes);
            Assert.Equal(4, NoteEditorViewModel.GetDurationTier(merged));
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
        public void GetAdjacentRuns_GroupsConsecutiveSelections()
        {
            var refs = new[]
            {
                new ScoreNoteRef(0, 1),
                new ScoreNoteRef(0, 2),
                new ScoreNoteRef(0, 4)
            };

            var runs = NoteSplitMergeService.GetAdjacentRuns(refs);

            Assert.Single(runs);
            Assert.Equal(2, runs[0].Count);
            Assert.Equal(1, runs[0][0].NoteIndex);
            Assert.Equal(2, runs[0][1].NoteIndex);
        }
    }
}