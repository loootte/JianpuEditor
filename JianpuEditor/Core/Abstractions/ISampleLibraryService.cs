using System.Collections.Generic;

namespace JianpuEditor.Core.Abstractions
{
    public interface ISampleLibraryService
    {
        IReadOnlyList<string> ListSampleFiles();

        string GetDisplayName(string filePath);
    }
}
