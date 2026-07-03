using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class ScoreSelectionViewModelTests
    {
        [Fact]
        public void BuildSelectionDescription_DescribesSelectedNote()
        {
            var document = ViewModelTestHelper.CreateDocument();
            var selection = new ScoreSelectionViewModel(document);
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                NoteIndex = 2
            });

            var description = selection.BuildSelectionDescription();

            Assert.Contains("第 1 小节第 3 个音符", description);
        }

        [Fact]
        public void BuildSelectionDescription_DescribesMultiMeasureRange()
        {
            var document = ViewModelTestHelper.CreateDocument();
            var selection = new ScoreSelectionViewModel(document);
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                SelectedMeasureIndices = new[] { 0, 1, 2 }
            });

            var description = selection.BuildSelectionDescription();

            Assert.Contains("第 1 到第 3 小节", description);
        }
    }
}
