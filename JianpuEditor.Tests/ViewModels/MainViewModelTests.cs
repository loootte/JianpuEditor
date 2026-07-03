using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class MainViewModelTests
    {
        [Fact]
        public void NewScoreCommand_ResetsDocumentAndStatus()
        {
            var main = ViewModelTestHelper.CreateMainViewModel();
            main.Document.Title = "Old";

            main.NewScoreCommand.Execute(null);

            Assert.Equal("未命名乐曲", main.Document.Title);
            Assert.Equal("已新建谱面", main.StatusMessage);
        }

        [Fact]
        public void HandleSelectionChanged_UpdatesStatusForNoteSelection()
        {
            var main = ViewModelTestHelper.CreateMainViewModel();
            main.Document.EnsureMeasures();

            main.HandleSelectionChanged(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                NoteIndex = 1
            });

            Assert.Contains("第 1 小节第 2 个音符", main.StatusMessage);
        }
    }
}
