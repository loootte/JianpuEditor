using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JianpuEditor.Services
{
    public static class SampleLibraryService
    {
        private static readonly string[] SupportedExtensions = { ".jianpu", ".json" };

        public static string GetSampleDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample");
        }

        public static IReadOnlyList<string> ListSampleFiles()
        {
            var directory = GetSampleDirectory();
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            return Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static string GetDisplayName(string filePath)
        {
            return string.IsNullOrWhiteSpace(filePath)
                ? string.Empty
                : Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
