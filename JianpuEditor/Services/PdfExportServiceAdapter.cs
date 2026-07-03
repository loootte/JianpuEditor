using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class PdfExportServiceAdapter : IPdfExportService
    {
        public void Export(JianpuScore score, string path, int pageWidth)
        {
            PdfExportService.Export(score, path, pageWidth);
        }
    }
}
