namespace JianpuEditor.Rendering
{
    public sealed class PdfPageSlice
    {
        public int FirstLineIndex { get; set; }

        public int LineCount { get; set; }

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }
    }
}
