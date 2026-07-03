using JianpuEditor.Services;
using JianpuEditor.Services.EditCommands;
using JianpuEditor.Tests.Helpers;
using JianpuEditor.Tests.ViewModels;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class BulkLyricEditServiceTests
    {
        [Fact]
        public void GetLyricLines_ReturnsTextsForRange()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.MeasureWithLyrics("第一", new[] { "第", "一" }, new[] { 0, 1 }, ScoreTestHelper.Note(1), ScoreTestHelper.Note(2)),
                ScoreTestHelper.MeasureWithLyrics("第二", new[] { "第", "二" }, new[] { 0, 1 }, ScoreTestHelper.Note(1), ScoreTestHelper.Note(2)));

            var lines = BulkLyricEditService.GetLyricLines(score, 0, 1);

            Assert.Equal(2, lines.Count);
            Assert.Equal("第一", lines[0]);
            Assert.Equal("第二", lines[1]);
        }

        [Fact]
        public void BuildCommands_TextChanges_CreateModifyCommandsOnly()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2)),
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2)));
            score.Measures[0].LyricText = "旧一";
            score.Measures[1].LyricText = "旧二";

            var commands = BulkLyricEditService.BuildCommands(
                score,
                messenger,
                0,
                1,
                new[] { "新一", "旧二" },
                realign: false);

            Assert.Single(commands);
            Assert.IsType<ModifyLyricTextCommand>(commands[0]);
        }

        [Fact]
        public void BuildCommands_WithRealign_AddsAlignmentCommands()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2), ScoreTestHelper.Note(3), ScoreTestHelper.Note(4)));
            score.Measures[0].LyricText = "你好世界";

            var commands = BulkLyricEditService.BuildCommands(
                score,
                messenger,
                0,
                0,
                new[] { "你好世界" },
                realign: true);

            Assert.Single(commands);
            Assert.IsType<AlignLyricSyllablesCommand>(commands[0]);
        }

        [Fact]
        public void BuildCommands_NoChanges_ReturnsEmpty()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var score = ScoreTestHelper.CreateScore(ScoreTestHelper.Measure(ScoreTestHelper.Note(1)));
            score.Measures[0].LyricText = "不变";

            var commands = BulkLyricEditService.BuildCommands(
                score,
                messenger,
                0,
                0,
                new[] { "不变" },
                realign: false);

            Assert.Empty(commands);
        }

        [Fact]
        public void ApplyBulkLyrics_UpdatesMultipleMeasuresAndSupportsUndo()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var content = new MeasureContentViewModel(document, navigation, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures.Clear();
            document.Score.Measures.Add(ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2)));
            document.Score.Measures.Add(ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2)));
            document.Score.Measures[0].LyricText = "旧一";
            document.Score.Measures[1].LyricText = "旧二";

            var result = content.ApplyBulkLyrics(0, 1, new[] { "新一", "新二" }, realign: false);

            Assert.True(result.Changed);
            Assert.Equal("新一", document.Score.Measures[0].LyricText);
            Assert.Equal("新二", document.Score.Measures[1].LyricText);
            Assert.True(history.CanUndo);

            history.Undo();

            Assert.Equal("旧一", document.Score.Measures[0].LyricText);
            Assert.Equal("旧二", document.Score.Measures[1].LyricText);
        }

        [Fact]
        public void ApplyBulkLyrics_UnchangedWhenNoEdits()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var content = new MeasureContentViewModel(document, navigation, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].LyricText = "相同";

            var result = content.ApplyBulkLyrics(0, 0, new[] { "相同" }, realign: false);

            Assert.False(result.Changed);
            Assert.False(history.CanUndo);
        }
    }
}
