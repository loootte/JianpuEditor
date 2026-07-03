using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;

namespace JianpuEditor.Services
{
    public sealed class SampleLibraryServiceAdapter : ISampleLibraryService
    {
        public IReadOnlyList<string> ListSampleFiles()
        {
            return SampleLibraryService.ListSampleFiles();
        }

        public string GetDisplayName(string filePath)
        {
            return SampleLibraryService.GetDisplayName(filePath);
        }
    }
}
