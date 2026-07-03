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
            var messenger = CreateMessenger();
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
            var messenger = CreateMessenger();
            var undo = new ScoreUndoService(messenger);
            var score = new JianpuScore();

            undo.RecordSnapshot(score);
            undo.DiscardLastSnapshot();

            Assert.False(undo.CanUndo);
        }

        [Fact]
        public void ScoreLoadedMessage_ClearsHistory()
        {
            var messenger = CreateMessenger();
            var undo = new ScoreUndoService(messenger);
            undo.RecordSnapshot(new JianpuScore());
            undo.PopSnapshotForUndo(new JianpuScore { Title = "After" });

            messenger.Send(new ScoreLoadedMessage(new JianpuScore(), null));

            Assert.False(undo.CanUndo);
            Assert.False(undo.CanRedo);
        }

        [Fact]
        public void RecordSnapshot_DoesNotPushWhileRestoring()
        {
            var messenger = CreateMessenger();
            var undo = new ScoreUndoService(messenger);
            undo.RecordSnapshot(new JianpuScore { Title = "A" });
            undo.EnterRestore();
            undo.RecordSnapshot(new JianpuScore { Title = "B" });
            undo.LeaveRestore();

            Assert.True(undo.CanUndo);
            Assert.Equal("A", undo.PopSnapshot().Title);
        }

        [Fact]
        public void PopSnapshotForUndo_EnablesRedoWithPriorState()
        {
            var messenger = CreateMessenger();
            var undo = new ScoreUndoService(messenger);
            undo.RecordSnapshot(new JianpuScore { Title = "Before" });
            var current = new JianpuScore { Title = "After" };

            var restored = undo.PopSnapshotForUndo(current);

            Assert.Equal("Before", restored.Title);
            Assert.False(undo.CanUndo);
            Assert.True(undo.CanRedo);

            var redone = undo.PopSnapshotForRedo(new JianpuScore { Title = "Before" });
            Assert.Equal("After", redone.Title);
            Assert.True(undo.CanUndo);
            Assert.False(undo.CanRedo);
        }

        [Fact]
        public void RecordSnapshot_ClearsRedoStack()
        {
            var messenger = CreateMessenger();
            var undo = new ScoreUndoService(messenger);
            undo.RecordSnapshot(new JianpuScore { Title = "A" });
            undo.PopSnapshotForUndo(new JianpuScore { Title = "B" });
            Assert.True(undo.CanRedo);

            undo.RecordSnapshot(new JianpuScore { Title = "C" });

            Assert.False(undo.CanRedo);
        }

        [Fact]
        public void PopSnapshotForUndo_ReturnsNullWhenUndoStackEmpty()
        {
            var messenger = CreateMessenger();
            var undo = new ScoreUndoService(messenger);

            Assert.Null(undo.PopSnapshotForUndo(new JianpuScore()));
            Assert.False(undo.CanRedo);
        }

        private static AppMessenger CreateMessenger()
        {
            return new AppMessenger(new CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger());
        }
    }
}
