namespace JianpuEditor.Models
{
    /// <summary>装饰音与谱面符号类型，供后续工具栏与绘制扩展。</summary>
    public enum OrnamentType
    {
        Unknown = 0,
        GraceNote = 1,
        Trill = 2,
        Mordent = 3,
        Turn = 4,
        Glissando = 5,
        Fermata = 6,
        Staccato = 7,
        Accent = 8,
        Tenuto = 9,
        RepeatStart = 10,
        RepeatEnd = 11,
        Segno = 12,
        Coda = 13,
        Custom = 99
    }
}
