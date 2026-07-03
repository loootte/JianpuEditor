using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IMidiExportService
    {
        void Export(JianpuScore score, string path);
    }
}
