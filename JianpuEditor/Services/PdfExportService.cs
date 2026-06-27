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
            var renderer = new JianpuRenderer();
            var renderWidth = ScoreLayoutOptions.PdfRenderWidth;
            using (var bitmap = renderer.RenderToBitmap(score, renderWidth, ScoreLayoutOptions.PdfExport))
            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(210);
                page.Height = XUnit.FromMillimeter(297);

                var imageWidthPt = bitmap.Width * 72.0 / 96.0;
                var imageHeightPt = bitmap.Height * 72.0 / 96.0;
                var fitScale = Math.Min(page.Width.Point / imageWidthPt, page.Height.Point / imageHeightPt);
                var drawWidth = imageWidthPt * fitScale;
                var drawHeight = imageHeightPt * fitScale;
                var offsetX = (page.Width.Point - drawWidth) / 2.0;
                var offsetY = (page.Height.Point - drawHeight) / 2.0;

                using (var gfx = XGraphics.FromPdfPage(page))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;
                    using (var image = XImage.FromStream(stream))
                    {
                        gfx.DrawImage(image, offsetX, offsetY, drawWidth, drawHeight);
                    }
                }

                document.Save(path);
            }
        }
    }
}