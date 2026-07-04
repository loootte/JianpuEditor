using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IMidiImportService
    {
        JianpuScore Import(string path);
    }
}
