using System;
using System.Collections.Generic;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class PdfPagePlanner
    {
        public const int CompactHeaderHeight = 44;

        public const int BottomMargin = 48;

        public static int GetPageHeightPixels(int renderWidth)
        {
            return (int)Math.Round(renderWidth * 297.0 / 210.0);
        }

        public static List<PdfPageSlice> PlanPages(int totalLines, int firstPageHeaderHeight, int pageHeightPixels)
        {
            var pages = new List<PdfPageSlice>();
            if (totalLines <= 0)
            {
                pages.Add(new PdfPageSlice
                {
                    FirstLineIndex = 0,
                    LineCount = 0,
                    PageNumber = 1,
                    TotalPages = 1
                });
                return pages;
            }

            var lineIndex = 0;
            while (lineIndex < totalLines)
            {
                var isFirstPage = pages.Count == 0;
                var headerHeight = isFirstPage ? firstPageHeaderHeight : CompactHeaderHeight;
                var availableHeight = pageHeightPixels - headerHeight - BottomMargin;
                var lineCount = CountLinesThatFit(availableHeight, totalLines - lineIndex);
                pages.Add(new PdfPageSlice
                {
                    FirstLineIndex = lineIndex,
                    LineCount = lineCount,
                    PageNumber = pages.Count + 1
                });
                lineIndex += lineCount;
            }

            var totalPages = pages.Count;
            foreach (var page in pages)
            {
                page.TotalPages = totalPages;
            }

            return pages;
        }

        internal static int CountLinesThatFit(int availableHeight, int maxLines)
        {
            if (maxLines <= 0)
            {
                return 0;
            }

            for (var lineCount = maxLines; lineCount >= 1; lineCount--)
            {
                var height = GetLinesHeight(lineCount);
                if (height <= availableHeight)
                {
                    return lineCount;
                }
            }

            return 1;
        }

        internal static int GetLinesHeight(int lineCount)
        {
            if (lineCount <= 0)
            {
                return 0;
            }

            return lineCount * JianpuRenderer.StaffBlockHeight
                + Math.Max(0, lineCount - 1) * JianpuRenderer.StaffBlockSpacing;
        }
    }
}
