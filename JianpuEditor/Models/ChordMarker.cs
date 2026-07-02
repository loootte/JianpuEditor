namespace JianpuEditor.Models
{
    public sealed class ChordMarker
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>小节内四分拍位置（0 为第 1 拍），与主旋律四分音符对齐。</summary>
        public double BeatPosition { get; set; }
    }
}