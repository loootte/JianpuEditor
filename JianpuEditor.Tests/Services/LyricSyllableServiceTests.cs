using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class LyricSyllableServiceTests
    {
        [Fact]
        public void ImportLegacyLyricText_SplitsChineseCharactersAcrossNotes()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));
            measure.LyricText = "欢乐女神";

            LyricSyllableService.ImportLegacyLyricText(measure);

            Assert.Equal(4, measure.LyricSyllables.Count);
            Assert.Equal("欢", measure.LyricSyllables[0].Text);
            Assert.Equal(0, measure.LyricSyllables[0].NoteIndex);
            Assert.Equal("乐", measure.LyricSyllables[1].Text);
            Assert.Equal(1, measure.LyricSyllables[1].NoteIndex);
            Assert.Equal("女", measure.LyricSyllables[2].Text);
            Assert.Equal("神", measure.LyricSyllables[3].Text);
            Assert.Equal("欢乐女神", measure.LyricText);
        }

        [Fact]
        public void ImportLegacyLyricText_SplitsWhitespaceSeparatedTokens()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3));
            measure.LyricText = "hello world";

            LyricSyllableService.ImportLegacyLyricText(measure);

            Assert.Equal(2, measure.LyricSyllables.Count);
            Assert.Equal("hello", measure.LyricSyllables[0].Text);
            Assert.Equal(0, measure.LyricSyllables[0].NoteIndex);
            Assert.Equal("world", measure.LyricSyllables[1].Text);
            Assert.Equal(1, measure.LyricSyllables[1].NoteIndex);
        }

        [Fact]
        public void GetDisplayText_UsesLyricTextWhenNoStructuredLyrics()
        {
            var measure = new JianpuMeasure { LyricText = "照大地" };

            Assert.False(LyricSyllableService.HasStructuredLyrics(measure));
            Assert.Equal("照大地", LyricSyllableService.GetDisplayText(measure));
            Assert.Equal("照大地", LyricSyllableService.GetFallbackText(measure));
        }

        [Fact]
        public void GetDisplayText_JoinsStructuredSyllables()
        {
            var measure = ScoreTestHelper.MeasureWithLyrics(
                "欢乐女神",
                new[] { "欢", "乐", "女", "神" },
                new[] { 0, 1, 2, 3 },
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));
            LyricSyllableService.NormalizeMeasure(measure);

            Assert.True(LyricSyllableService.HasStructuredLyrics(measure));
            Assert.Equal("欢乐女神", LyricSyllableService.GetDisplayText(measure));
            Assert.Equal("欢乐女神", LyricSyllableService.GetFallbackText(measure));
        }

        [Fact]
        public void NormalizeMeasure_SyncsBeatPositionFromNoteIndex()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2, dashes: 1),
                ScoreTestHelper.Note(3));
            measure.LyricSyllables.Add(new LyricSyllable { Text = "啊", NoteIndex = 2 });

            LyricSyllableService.NormalizeMeasure(measure);

            Assert.Equal(3, measure.LyricSyllables[0].BeatPosition);
        }

        [Fact]
        public void ResolveNoteIndex_FindsNoteFromBeatPosition()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2, dashes: 1),
                ScoreTestHelper.Note(3));
            var syllable = new LyricSyllable { Text = "啦", BeatPosition = 3 };

            Assert.Equal(2, LyricSyllableService.ResolveNoteIndex(measure, syllable));
        }

        [Fact]
        public void ImportLegacyLyricText_DoesNotOverwriteExistingSyllables()
        {
            var measure = ScoreTestHelper.MeasureWithLyrics(
                "旧歌词",
                new[] { "新" },
                new[] { 0 },
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2));

            LyricSyllableService.ImportLegacyLyricText(measure);

            Assert.Single(measure.LyricSyllables);
            Assert.Equal("新", measure.LyricSyllables[0].Text);
            Assert.Equal("旧歌词", measure.LyricText);
        }
    }
}
