using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class MidiExportServiceAdapter : IMidiExportService
    {
        public void Export(JianpuScore score, string path)
        {
            MidiExportService.Export(score, path);
        }
    }
}
