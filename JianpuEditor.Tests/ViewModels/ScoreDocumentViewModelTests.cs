using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class ScoreDocumentViewModelTests
    {
        [Fact]
        public void Title_ChangeMarksDocumentDirty()
        {
            var viewModel = new ScoreDocumentViewModel(new ScoreFileServiceAdapter());

            viewModel.Title = "Test Title";

            Assert.Equal("Test Title", viewModel.Score.Title);
            Assert.True(viewModel.IsDirty);
        }

        [Fact]
        public void ResetAsNew_ClearsDirtyState()
        {
            var viewModel = new ScoreDocumentViewModel(new ScoreFileServiceAdapter());
            viewModel.Title = "Dirty";

            viewModel.ResetAsNew();

            Assert.False(viewModel.IsDirty);
            Assert.Null(viewModel.CurrentFilePath);
        }
    }
}
