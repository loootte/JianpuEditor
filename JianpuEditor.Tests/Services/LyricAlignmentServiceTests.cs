using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using JianpuEditor.Tests.ViewModels;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class LyricAlignmentServiceTests
    {
        [Fact]
        public void TryBuildAlignment_MapsFourCharactersToFourNotes()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));

            var success = LyricAlignmentService.TryBuildAlignment(
                measure,
                "你好世界",
                ties: null,
                measureIndex: 0,
                out var syllables,
                out var message);

            Assert.True(success);
            Assert.Equal(4, syllables.Count);
            Assert.Equal("你", syllables[0].Text);
            Assert.Equal(0, syllables[0].NoteIndex);
            Assert.Equal("好", syllables[1].Text);
            Assert.Equal(1, syllables[1].NoteIndex);
            Assert.Equal("世", syllables[2].Text);
            Assert.Equal(3, syllables[3].NoteIndex);
            Assert.Contains("已对齐 4 个音节", message);
        }

        [Fact]
        public void TryBuildAlignment_SkipsTieContinuationNotes()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3));
            var ties = new[]
            {
                new JianpuTie { StartMeasureIndex = 0, StartNoteIndex = 0, EndMeasureIndex = 0, EndNoteIndex = 1 }
            };

            var success = LyricAlignmentService.TryBuildAlignment(
                measure,
                "你好",
                ties,
                measureIndex: 0,
                out var syllables,
                out _);

            Assert.True(success);
            Assert.Equal(2, syllables.Count);
            Assert.Equal(0, syllables[0].NoteIndex);
            Assert.Equal(2, syllables[1].NoteIndex);
        }

        [Fact]
        public void TryBuildAlignment_SkipsRestNotes()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Note(3));

            var success = LyricAlignmentService.TryBuildAlignment(
                measure,
                "你好",
                ties: null,
                measureIndex: 0,
                out var syllables,
                out _);

            Assert.True(success);
            Assert.Equal(2, syllables.Count);
            Assert.Equal(0, syllables[0].NoteIndex);
            Assert.Equal(2, syllables[1].NoteIndex);
        }

        [Fact]
        public void TryBuildAlignment_OneSyllablePerNoteWithDottedDuration()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1, dotted: true),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3));

            var success = LyricAlignmentService.TryBuildAlignment(
                measure,
                "你好啊",
                ties: null,
                measureIndex: 0,
                out var syllables,
                out _);

            Assert.True(success);
            Assert.Equal(3, syllables.Count);
            Assert.Equal(0, syllables[0].NoteIndex);
            Assert.Equal(1, syllables[1].NoteIndex);
            Assert.Equal(2, syllables[2].NoteIndex);
        }

        [Fact]
        public void AlignLyricsToNotes_UpdatesMeasureAndSupportsUndo()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var content = new MeasureContentViewModel(document, navigation, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.AddRange(new[]
            {
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4)
            });
            document.Score.Measures[0].LyricText = "你好世界";

            var result = content.AlignLyricsToNotes();

            Assert.True(result.Changed);
            Assert.Equal(4, document.Score.Measures[0].LyricSyllables.Count);
            Assert.Equal("你", document.Score.Measures[0].LyricSyllables[0].Text);
            Assert.True(history.CanUndo);

            history.Undo();

            Assert.Empty(document.Score.Measures[0].LyricSyllables);
        }
    }
}
