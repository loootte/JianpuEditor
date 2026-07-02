namespace JianpuEditor.Rendering
{
    public sealed class ScoreLayoutOptions
    {
        public static readonly ScoreLayoutOptions Default = new ScoreLayoutOptions();

        public static readonly ScoreLayoutOptions Editor = new ScoreLayoutOptions
        {
            ChordMarkersTextOnly = true,
            ShowChordEditorAffordances = true
        };

        public static readonly ScoreLayoutOptions PdfExport = new ScoreLayoutOptions
        {
            MeasuresPerLine = 4,
            EqualizeMeasureWidths = true,
            NoteWidthScale = 1.0,
            HeaderMarginTop = 96,
            TitleFontSize = 36f,
            MetaFontSize = 18f,
            HeaderMetaLeftAligned = true,
            ChordMarkersTextOnly = true
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

        /// <summary>PDF 等导出场景：仅绘制和弦文字，不绘制编辑边框与手柄。</summary>
        public bool ChordMarkersTextOnly { get; set; }

        /// <summary>编辑界面：显示拍位网格、提示，以及选中态的操作标记。</summary>
        public bool ShowChordEditorAffordances { get; set; }
    }
}