using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class ScoreUndoServiceTests
    {
        [Fact]
        public void RecordSnapshot_AllowsUndo()
        {
            var messenger = new AppMessenger(new CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger());
            var undo = new ScoreUndoService(messenger);
            var score = new JianpuScore { Title = "A" };

            undo.RecordSnapshot(score);
            score.Title = "B";

            Assert.True(undo.CanUndo);
            var restored = undo.PopSnapshot();
            Assert.Equal("A", restored.Title);
            Assert.False(undo.CanUndo);
        }

        [Fact]
        public void DiscardLastSnapshot_RemovesUnusedSnapshot()
        {
            var messenger = new AppMessenger(new CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger());
            var undo = new ScoreUndoService(messenger);
            var score = new JianpuScore();

            undo.RecordSnapshot(score);
            undo.DiscardLastSnapshot();

            Assert.False(undo.CanUndo);
        }

        [Fact]
        public void ScoreLoadedMessage_ClearsStack()
        {
            var messenger = new AppMessenger(new CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger());
            var undo = new ScoreUndoService(messenger);
            undo.RecordSnapshot(new JianpuScore());

            messenger.Send(new ScoreLoadedMessage(new JianpuScore(), null));

            Assert.False(undo.CanUndo);
        }

        [Fact]
        public void RecordSnapshot_DoesNotPushWhileRestoring()
        {
            var messenger = new AppMessenger(new CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger());
            var undo = new ScoreUndoService(messenger);
            undo.RecordSnapshot(new JianpuScore { Title = "A" });
            undo.EnterRestore();
            undo.RecordSnapshot(new JianpuScore { Title = "B" });
            undo.LeaveRestore();

            Assert.True(undo.CanUndo);
            Assert.Equal("A", undo.PopSnapshot().Title);
        }
    }
}
