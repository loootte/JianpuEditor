namespace JianpuEditor.Models
{
    public sealed class LyricSyllable
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>小节内主旋律音符索引（0 为第一个音符）。-1 表示仅按拍位对齐。</summary>
        public int NoteIndex { get; set; } = -1;

        /// <summary>小节内四分拍位置（0 为第 1 拍）。NoteIndex 有效时由归一化逻辑同步。</summary>
        public double BeatPosition { get; set; }
    }
}
