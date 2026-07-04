using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Rendering
{
    public sealed class OrnamentRenderingTests
    {
        [Fact]
        public void TryGetOrnamentAnchorX_PlacesGlyphAboveBoundNote()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3));
            OrnamentService.TryAddOrnament(measure, 1, OrnamentType.Trill);
            var layout = CreateLayout(measure, 0);

            Assert.True(JianpuRenderer.TryGetOrnamentAnchorX(
                layout,
                measure,
                measure.Ornaments[0],
                out var anchorX));

            Assert.True(JianpuRenderer.TryGetSyllableAnchorX(
                layout,
                measure,
                new LyricSyllable { Text = "x", NoteIndex = 1 },
                out var noteCenterX));
            Assert.Equal(noteCenterX, anchorX, 1);
        }

        [Fact]
        public void TryGetOrnamentAnchorX_GraceNoteAnchorsLeftOfHead()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(5));
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.GraceNote);
            var layout = CreateLayout(measure, 0);

            Assert.True(JianpuRenderer.TryGetOrnamentAnchorX(
                layout,
                measure,
                measure.Ornaments[0],
                out var anchorX));
            Assert.True(JianpuRenderer.TryGetSyllableAnchorX(
                layout,
                measure,
                new LyricSyllable { Text = "x", NoteIndex = 0 },
                out var noteCenterX));
            Assert.True(anchorX < noteCenterX);
        }

        private static JianpuRenderer.MeasureLayout CreateLayout(JianpuMeasure measure, int measureIndex)
        {
            var layout = new JianpuRenderer.MeasureLayout
            {
                MeasureIndex = measureIndex,
                X = JianpuRenderer.MarginLeft,
                BlockTop = JianpuRenderer.MarginTop,
                Width = JianpuRenderer.NoteCellWidth * measure.MelodyNotes.Count
            };
            layout.ComputeNoteLayout(measure);
            layout.ApplyMelodyScale(layout.Width);
            return layout;
        }

    }
}
