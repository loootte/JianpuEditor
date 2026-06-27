using System.Collections.Generic;

namespace JianpuEditor.Models
{
    public class JianpuScore
    {
        public string Title { get; set; } = "未命名乐曲";

        public string KeySignature { get; set; } = "1=C";

        public string Tempo { get; set; } = "中速";

        public string Composer { get; set; } = string.Empty;

        public List<JianpuMeasure> Measures { get; set; } = new List<JianpuMeasure> { new JianpuMeasure() };

        public List<JianpuTie> Ties { get; set; } = new List<JianpuTie>();
    }
}