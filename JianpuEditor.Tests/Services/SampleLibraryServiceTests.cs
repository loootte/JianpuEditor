using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class SampleLibraryServiceTests
    {
        [Fact]
        public void ListSampleFiles_IncludesJianpuFilesInOutputDirectory()
        {
            var directory = SampleLibraryService.GetSampleDirectory();
            Assert.True(Directory.Exists(directory), "sample directory should be copied to output: " + directory);

            var files = SampleLibraryService.ListSampleFiles();
            Assert.Contains(files, path => path.EndsWith("卡农 Canon in D.jianpu", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetDisplayName_StripsExtension()
        {
            Assert.Equal("卡农 Canon in D", SampleLibraryService.GetDisplayName(@"sample\卡农 Canon in D.jianpu"));
        }
    }
}
