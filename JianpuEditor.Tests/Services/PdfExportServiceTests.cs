using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using PdfSharp.Pdf.IO;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class PdfExportServiceTests
    {
        [Fact]
        public void Export_32Measures_ProducesMultiplePages()
        {
            var score = CreateScoreWithMeasures(32, "Multi Page Score");
            var path = Path.Combine(Path.GetTempPath(), "jianpu-pdf-" + Guid.NewGuid() + ".pdf");
            try
            {
                PdfExportService.Export(score, path, ScoreLayoutOptions.PdfRenderWidth);

                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 1024);

                using (var document = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                {
                    Assert.True(document.PageCount >= 2);
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void Export_ShortScore_ProducesSinglePage()
        {
            var score = CreateScoreWithMeasures(4, "Short Score");
            var path = Path.Combine(Path.GetTempPath(), "jianpu-pdf-short-" + Guid.NewGuid() + ".pdf");
            try
            {
                PdfExportService.Export(score, path, ScoreLayoutOptions.PdfRenderWidth);

                using (var document = PdfReader.Open(path, PdfDocumentOpenMode.Import))
                {
                    Assert.Equal(1, document.PageCount);
                }
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void PlanPages_EightLines_SplitsAcrossPages()
        {
            var renderWidth = ScoreLayoutOptions.PdfRenderWidth;
            var pages = PdfPagePlanner.PlanPages(
                8,
                ScoreLayoutOptions.PdfExport.HeaderMarginTop,
                PdfPagePlanner.GetPageHeightPixels(renderWidth));

            Assert.True(pages.Count >= 2);
            Assert.Equal(8, pages.Sum(page => page.LineCount));
            Assert.Equal(1, pages[0].PageNumber);
            Assert.Equal(pages.Count, pages[0].TotalPages);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(7, 1)]
        [InlineData(8, 2)]
        public void PlanPages_LineCount_MapsToExpectedPageCount(int lineCount, int expectedPages)
        {
            var pages = PdfPagePlanner.PlanPages(
                lineCount,
                ScoreLayoutOptions.PdfExport.HeaderMarginTop,
                PdfPagePlanner.GetPageHeightPixels(ScoreLayoutOptions.PdfRenderWidth));

            Assert.Equal(expectedPages, pages.Count);
            Assert.Equal(lineCount, pages.Sum(page => page.LineCount));
        }

        private static JianpuScore CreateScoreWithMeasures(int measureCount, string title)
        {
            var measures = new List<JianpuMeasure>();
            for (var i = 0; i < measureCount; i++)
            {
                measures.Add(ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(1 + (i % 7)),
                    ScoreTestHelper.Note(2 + (i % 6)),
                    ScoreTestHelper.Note(3 + (i % 5)),
                    ScoreTestHelper.Note(4 + (i % 4))));
            }

            return new JianpuScore
            {
                Title = title,
                KeySignature = "1=C",
                Tempo = "中速",
                Bpm = 120,
                Composer = "Test",
                Measures = measures
            };
        }
    }
}
