namespace JianpuEditor.Rendering
{
    public sealed class ScoreLayoutOptions
    {
        public static readonly ScoreLayoutOptions Default = new ScoreLayoutOptions();

        public static readonly ScoreLayoutOptions PdfExport = new ScoreLayoutOptions
        {
            MeasuresPerLine = 4,
            EqualizeMeasureWidths = true,
            NoteWidthScale = 1.0,
            HeaderMarginTop = 96,
            TitleFontSize = 36f,
            MetaFontSize = 18f,
            HeaderMetaLeftAligned = true
        };

        public const int PdfRenderWidth = 1280;

        /// <summary>0 = wrap by page width; otherwise fixed measures per line.</summary>
        public int MeasuresPerLine { get; set; }

        public bool EqualizeMeasureWidths { get; set; }

        public double NoteWidthScale { get; set; } = 1.0;

        /// <summary>0 = use renderer default top margin.</summary>
        public int HeaderMarginTop { get; set; }

        public float TitleFontSize { get; set; } = 22f;

        public float MetaFontSize { get; set; } = 11f;

        public bool HeaderMetaLeftAligned { get; set; }
    }
}