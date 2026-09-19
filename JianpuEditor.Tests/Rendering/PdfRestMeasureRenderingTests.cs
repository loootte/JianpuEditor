using System.Drawing;
using System.Runtime.Versioning;
using JianpuEditor.Rendering;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Rendering
{
    [SupportedOSPlatform("windows")]
    public sealed class PdfRestMeasureRenderingTests
    {
        [Fact]
        public void PdfExportOptions_LeaveRestOnlyMeasuresBlank()
        {
            Assert.True(ScoreLayoutOptions.PdfExport.LeaveRestOnlyMeasuresBlank);
            Assert.False(ScoreLayoutOptions.Editor.LeaveRestOnlyMeasuresBlank);
            Assert.False(ScoreLayoutOptions.Default.LeaveRestOnlyMeasuresBlank);
        }

        [Fact]
        public void RenderPdfPageToBitmap_RestOnlyMeasure_LeavesMelodyRowBlank()
        {
            var restMeasure = ScoreTestHelper.Measure(
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest());
            var noteMeasure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));
            var score = ScoreTestHelper.CreateScore(restMeasure, noteMeasure);

            using (var renderer = new JianpuRenderer())
            {
                var options = ScoreLayoutOptions.PdfExport;
                var bitmap = renderer.RenderPdfPageToBitmap(
                    score,
                    ScoreLayoutOptions.PdfRenderWidth,
                    options,
                    new PdfPageSlice { FirstLineIndex = 0, LineCount = 1, PageNumber = 1, TotalPages = 1 });
                var layouts = renderer.GetMeasureLayouts(score, ScoreLayoutOptions.PdfRenderWidth, options);
                Assert.Equal(2, layouts.Count);

                var restInk = CountDarkPixels(bitmap, MelodySample(layouts[0]));
                var noteInk = CountDarkPixels(bitmap, MelodySample(layouts[1]));

                Assert.True(noteInk > 80);
                Assert.True(restInk < 20);
                Assert.True(restInk < noteInk / 10);
            }
        }

        [Fact]
        public void RenderToBitmap_Editor_StillDrawsRestZeros()
        {
            var restMeasure = ScoreTestHelper.Measure(
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest(),
                ScoreTestHelper.Rest());
            var score = ScoreTestHelper.CreateScore(restMeasure);

            using (var renderer = new JianpuRenderer())
            {
                var bitmap = renderer.RenderToBitmap(score, 1280, ScoreLayoutOptions.Editor);
                var layouts = renderer.GetMeasureLayouts(score, 1280, ScoreLayoutOptions.Editor);
                var restInk = CountDarkPixels(bitmap, MelodySample(layouts[0]));
                Assert.True(restInk > 80);
            }
        }

        private static Rectangle MelodySample(JianpuRenderer.MeasureLayout layout)
        {
            return new Rectangle(
                layout.X + 8,
                layout.BlockTop + 16,
                Math.Max(8, layout.Width - 16),
                JianpuRenderer.MelodyRowHeight - 28);
        }

        private static int CountDarkPixels(Bitmap bitmap, Rectangle bounds)
        {
            var count = 0;
            var maxX = Math.Min(bitmap.Width, bounds.Right);
            var maxY = Math.Min(bitmap.Height, bounds.Bottom);
            var startX = Math.Max(0, bounds.Left);
            var startY = Math.Max(0, bounds.Top);
            for (var y = startY; y < maxY; y++)
            {
                for (var x = startX; x < maxX; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    if (color.R < 200 || color.G < 200 || color.B < 200)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
