using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Tests.ViewModels
{
    internal sealed class FakePdfExportService : IPdfExportService
    {
        public string LastPath { get; private set; }

        public int LastPageWidth { get; private set; }

        public JianpuScore LastScore { get; private set; }

        public void Export(JianpuScore score, string path, int pageWidth)
        {
            LastScore = score;
            LastPath = path;
            LastPageWidth = pageWidth;
        }
    }

    internal sealed class FakeMidiExportService : IMidiExportService
    {
        public string LastPath { get; private set; }

        public JianpuScore LastScore { get; private set; }

        public void Export(JianpuScore score, string path)
        {
            LastScore = score;
            LastPath = path;
        }
    }

    internal sealed class FakeSampleLibraryService : ISampleLibraryService
    {
        public IReadOnlyList<string> Samples { get; set; } = new[] { @"C:\sample\demo.jianpu" };

        public IReadOnlyList<string> ListSampleFiles()
        {
            return Samples;
        }

        public string GetDisplayName(string filePath)
        {
            return System.IO.Path.GetFileNameWithoutExtension(filePath);
        }
    }

    internal sealed class FakeChordTransposeService : IChordTransposeService
    {
        public bool ShouldSucceed { get; set; } = true;

        public string ErrorMessage { get; set; } = "转调失败";

        public int TransposedCount { get; set; } = 1;

        public bool TryTransposeChords(
            JianpuScore score,
            string targetKeySignature,
            out string errorMessage,
            out int transposedCount)
        {
            if (!ShouldSucceed)
            {
                errorMessage = ErrorMessage;
                transposedCount = 0;
                return false;
            }

            score.KeySignature = targetKeySignature;
            errorMessage = null;
            transposedCount = TransposedCount;
            return true;
        }
    }
}
