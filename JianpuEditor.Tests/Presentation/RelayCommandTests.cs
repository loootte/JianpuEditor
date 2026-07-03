using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace JianpuEditor.Tests.Presentation
{
    public class RelayCommandTests
    {
        [Fact]
        public void Execute_RunsAction()
        {
            var executed = false;
            var command = new RelayCommand(() => executed = true);

            command.Execute(null);

            Assert.True(executed);
        }

        [Fact]
        public void CanExecute_RespectsPredicate()
        {
            var enabled = false;
            var command = new RelayCommand(() => { }, () => enabled);

            Assert.False(command.CanExecute(null));
            enabled = true;
            Assert.True(command.CanExecute(null));
        }
    }
}
