using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public sealed class JianpuOrnament
    {
        public OrnamentType Type { get; set; } = OrnamentType.Unknown;

        /// <summary>锚定主旋律音符索引（0 为第一个音符）。-1 表示仅按拍位锚定。</summary>
        public int NoteIndex { get; set; } = -1;

        /// <summary>小节内四分拍位置（0 为第 1 拍）。NoteIndex 有效时由归一化逻辑同步。</summary>
        public double BeatPosition { get; set; }

        /// <summary>扩展参数，如倚音音高、滑音方向、反复次数等。</summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}
