using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class MidiExportServiceTests
    {
        [Fact]
        public void Export_WritesValidMidiHeader()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.MeasureWithChords(
                    new[] { "C", "G" },
                    new[] { 0d, 2d },
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)));
            var path = Path.Combine(Path.GetTempPath(), "jianpu-export-" + Guid.NewGuid() + ".mid");

            try
            {
                MidiExportService.Export(score, path);

                var bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 14);
                Assert.Equal("MThd", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
                Assert.Equal("MTrk", System.Text.Encoding.ASCII.GetString(bytes, 14, 4));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void Export_ThrowsForNullScore()
        {
            Assert.Throws<ArgumentNullException>(() => MidiExportService.Export(null, "test.mid"));
        }
    }
}
