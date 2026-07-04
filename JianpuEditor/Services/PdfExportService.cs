using System;
using System.Drawing;
using System.IO;
using JianpuEditor.Models;
using JianpuEditor.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace JianpuEditor.Services
{
    public static class PdfExportService
    {
        public static void Export(JianpuScore score, string path, int pageWidth)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", nameof(path));
            }

            var options = ScoreLayoutOptions.PdfExport;
            var renderWidth = ScoreLayoutOptions.PdfRenderWidth;
            using (var renderer = new JianpuRenderer())
            using (var document = new PdfDocument())
            {
                var totalLines = renderer.GetStaffLineCount(score, renderWidth, options);
                var pageHeightPixels = PdfPagePlanner.GetPageHeightPixels(renderWidth);
                var firstPageHeaderHeight = GetFirstPageHeaderHeight(options);
                var slices = PdfPagePlanner.PlanPages(totalLines, firstPageHeaderHeight, pageHeightPixels);
                if (slices.Count == 0)
                {
                    slices.Add(new PdfPageSlice
                    {
                        FirstLineIndex = 0,
                        LineCount = 0,
                        PageNumber = 1,
                        TotalPages = 1
                    });
                }

                foreach (var slice in slices)
                {
                    using (var bitmap = renderer.RenderPdfPageToBitmap(score, renderWidth, options, slice))
                    {
                        AddBitmapPage(document, bitmap);
                    }
                }

                document.Save(path);
            }
        }

        private static int GetFirstPageHeaderHeight(ScoreLayoutOptions options)
        {
            return options.HeaderMarginTop > 0 ? options.HeaderMarginTop : JianpuRenderer.MarginTop;
        }

        private static void AddBitmapPage(PdfDocument document, Bitmap bitmap)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromMillimeter(210);
            page.Height = XUnit.FromMillimeter(297);

            var imageWidthPt = bitmap.Width * 72.0 / 96.0;
            var imageHeightPt = bitmap.Height * 72.0 / 96.0;
            var scale = page.Width.Point / imageWidthPt;
            var drawWidth = page.Width.Point;
            var drawHeight = imageHeightPt * scale;

            using (var gfx = XGraphics.FromPdfPage(page))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                using (var image = XImage.FromStream(stream))
                {
                    gfx.DrawImage(image, 0, 0, drawWidth, drawHeight);
                }
            }
        }
    }
}
