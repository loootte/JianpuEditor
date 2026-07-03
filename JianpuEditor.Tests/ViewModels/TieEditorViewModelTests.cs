using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class TieEditorViewModelTests
    {
        [Fact]
        public void TryCompleteTie_AddsTieWhenStartAndEndAreValid()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var history = ViewModelTestHelper.CreateHistory(messenger);
            var document = ViewModelTestHelper.CreateDocument(messenger, history);
            var selection = new ScoreSelectionViewModel(document);
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var tieEditor = ViewModelTestHelper.CreateTieEditor(document, navigation, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 1 });
            document.Score.Measures[0].MelodyNotes.Add(new JianpuNote { Pitch = 2 });

            tieEditor.ToggleTieMode();
            tieEditor.TryCompleteTie(0, 0);
            var result = tieEditor.TryCompleteTie(0, 1);

            Assert.True(result.Changed);
            Assert.False(tieEditor.IsTieModeActive);
            Assert.Single(document.Score.Ties);
        }

        [Fact]
        public void CancelTieMode_DeactivatesMode()
        {
            var messenger = ViewModelTestHelper.CreateMessenger();
            var history = ViewModelTestHelper.CreateHistory(messenger);
            var document = ViewModelTestHelper.CreateDocument(messenger, history);
            var selection = new ScoreSelectionViewModel(document);
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            var tieEditor = ViewModelTestHelper.CreateTieEditor(document, navigation, messenger, history);

            tieEditor.ToggleTieMode();
            tieEditor.CancelTieMode();

            Assert.False(tieEditor.IsTieModeActive);
        }
    }
}
