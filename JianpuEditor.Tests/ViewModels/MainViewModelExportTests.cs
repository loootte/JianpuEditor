using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class MainViewModelExportTests
    {
        [Fact]
        public void ExportPdf_DelegatesToInjectedService()
        {
            var pdfExport = new FakePdfExportService();
            var midiExport = new FakeMidiExportService();
            var main = ViewModelTestHelper.CreateMainViewModel(pdfExport, midiExport);
            main.Document.Title = "Test Score";

            main.ExportPdf(@"C:\out\test.pdf", 900);

            Assert.Equal(@"C:\out\test.pdf", pdfExport.LastPath);
            Assert.Equal(900, pdfExport.LastPageWidth);
            Assert.Same(main.Document.Score, pdfExport.LastScore);
            Assert.Contains("PDF 已导出", main.StatusMessage);
        }

        [Fact]
        public void ExportMidi_DelegatesToInjectedService()
        {
            var pdfExport = new FakePdfExportService();
            var midiExport = new FakeMidiExportService();
            var main = ViewModelTestHelper.CreateMainViewModel(pdfExport, midiExport);

            main.ExportMidi(@"C:\out\test.mid");

            Assert.Equal(@"C:\out\test.mid", midiExport.LastPath);
            Assert.Same(main.Document.Score, midiExport.LastScore);
            Assert.Contains("MIDI 已导出", main.StatusMessage);
        }
    }
}
