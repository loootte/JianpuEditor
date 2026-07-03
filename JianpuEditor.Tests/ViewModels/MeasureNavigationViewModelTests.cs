using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class MeasureNavigationViewModelTests
    {
        [Fact]
        public void AddMeasure_IncreasesMeasureCount()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = new MeasureNavigationViewModel(document, selection, messenger);
            document.EnsureMeasures();

            navigation.AddMeasure();

            Assert.Equal(2, navigation.MeasureCount);
        }

        [Fact]
        public void DuplicateMeasures_CopiesSelectedRange()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = new MeasureNavigationViewModel(document, selection, messenger);
            document.EnsureMeasures();
            document.Score.Measures.Add(new JianpuMeasure { LyricText = "第二小节" });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                SelectedMeasureIndices = new[] { 0 }
            });

            var result = navigation.DuplicateMeasures();

            Assert.True(result.Changed);
            Assert.Equal(3, document.Score.Measures.Count);
        }

        [Fact]
        public void NormalizeMeasureRange_SwapsWhenFromGreaterThanTo()
        {
            var (document, selection, messenger) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = new MeasureNavigationViewModel(document, selection, messenger);

            var range = navigation.NormalizeMeasureRange(3, 1, fromChanged: true);

            Assert.Equal(2, range.fromIndex);
            Assert.Equal(2, range.toIndex);
        }
    }
}
