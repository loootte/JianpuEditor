using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using JianpuEditor.Tests.ViewModels;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.Rendering
{
    public sealed class LyricRenderingTests
    {
        [Fact]
        public void TryGetSyllableAnchorX_PlacesEachSyllableUnderItsNote()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.MeasureWithLyrics(
                    "你好世界",
                    new[] { "你", "好", "世", "界" },
                    new[] { 0, 1, 2, 3 },
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)));
            var measure = score.Measures[0];
            var layout = new JianpuRenderer.MeasureLayout
            {
                MeasureIndex = 0,
                X = JianpuRenderer.MarginLeft,
                BlockTop = JianpuRenderer.MarginTop,
                Width = JianpuRenderer.NoteCellWidth * measure.MelodyNotes.Count
            };
            layout.ComputeNoteLayout(measure);
            layout.ApplyMelodyScale(layout.Width);

            var anchors = new List<float>();
            foreach (var syllable in measure.LyricSyllables)
            {
                Assert.True(JianpuRenderer.TryGetSyllableAnchorX(layout, measure, syllable, out var centerX));
                anchors.Add(centerX);
            }

            Assert.Equal(4, anchors.Count);
            Assert.True(anchors[0] < anchors[1]);
            Assert.True(anchors[1] < anchors[2]);
            Assert.True(anchors[2] < anchors[3]);
        }

        [Fact]
        public void ApplyBulkLyrics_WithRealign_ProducesStructuredLyricsForRendering()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var content = new MeasureContentViewModel(document, navigation, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures.Clear();
            document.Score.Measures.Add(ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4)));
            document.Score.Measures[0].LyricText = "你好世界";

            var result = content.ApplyBulkLyrics(0, 0, new[] { "你好世界" }, realign: true);

            Assert.True(result.Changed);
            Assert.True(LyricSyllableService.HasStructuredLyrics(document.Score.Measures[0]));
            Assert.Equal(4, document.Score.Measures[0].LyricSyllables.Count);
        }
    }
}
