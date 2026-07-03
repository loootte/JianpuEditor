using JianpuEditor.Core.Messaging;
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
            var viewModel = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), new AppMessenger());

            viewModel.Title = "Test Title";

            Assert.Equal("Test Title", viewModel.Score.Title);
            Assert.True(viewModel.IsDirty);
        }

        [Fact]
        public void ResetAsNew_ClearsDirtyState()
        {
            var viewModel = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), new AppMessenger());
            viewModel.Title = "Dirty";

            viewModel.ResetAsNew();

            Assert.False(viewModel.IsDirty);
            Assert.Null(viewModel.CurrentFilePath);
        }

        [Fact]
        public void LoadDemoScore_LoadsOdeToJoyWithoutDirtyFlag()
        {
            var viewModel = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), new AppMessenger());

            viewModel.LoadDemoScore();

            Assert.Equal("欢乐颂", viewModel.Title);
            Assert.False(viewModel.IsDirty);
            Assert.Equal(5, viewModel.Score.Measures.Count);
        }

        [Fact]
        public void EnsureMeasures_CreatesDefaultMeasureWhenEmpty()
        {
            var viewModel = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), new AppMessenger());
            viewModel.Score.Measures.Clear();

            viewModel.EnsureMeasures();

            Assert.Single(viewModel.Score.Measures);
        }
    }
}
