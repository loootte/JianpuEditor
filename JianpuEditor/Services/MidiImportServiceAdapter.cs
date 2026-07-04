using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class MidiImportServiceAdapter : IMidiImportService
    {
        public JianpuScore Import(string path)
        {
            return MidiImportService.Import(path);
        }
    }
}