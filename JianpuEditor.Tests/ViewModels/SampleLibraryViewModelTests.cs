using JianpuEditor.Core.Messaging;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class SampleLibraryViewModelTests
    {
        [Fact]
        public void RefreshSamples_UsesInjectedService()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var samples = new FakeSampleLibraryService();
            var viewModel = new SampleLibraryViewModel(document, samples, messenger);

            viewModel.RefreshSamples();

            Assert.Single(viewModel.Samples);
            Assert.True(viewModel.HasSamples);
        }

        [Fact]
        public void GetDisplayName_DelegatesToService()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var samples = new FakeSampleLibraryService();
            var viewModel = new SampleLibraryViewModel(document, samples, messenger);

            Assert.Equal("demo", viewModel.GetDisplayName(@"C:\sample\demo.jianpu"));
        }
    }
}
