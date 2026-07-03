using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IPdfExportService
    {
        void Export(JianpuScore score, string path, int pageWidth);
    }
}
