using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class EditCommandHistoryTests
    {
        [Fact]
        public void Execute_PushesCommandOntoUndoStack()
        {
            var history = CreateHistory();
            var command = new TestEditCommand("append", () => { }, () => { });

            history.Execute(command);

            Assert.True(history.CanUndo);
            Assert.False(history.CanRedo);
            Assert.Equal(1, history.UndoCount);
        }

        [Fact]
        public void Undo_InvokesCommandUndoAndEnablesRedo()
        {
            var history = CreateHistory();
            var state = 0;
            var command = new TestEditCommand(
                "increment",
                () => state++,
                () => state--);

            history.Execute(command);
            history.Undo();

            Assert.Equal(0, state);
            Assert.False(history.CanUndo);
            Assert.True(history.CanRedo);
        }

        [Fact]
        public void Redo_ReappliesCommand()
        {
            var history = CreateHistory();
            var state = 0;
            var command = new TestEditCommand(
                "increment",
                () => state++,
                () => state--);

            history.Execute(command);
            history.Undo();
            history.Redo();

            Assert.Equal(1, state);
            Assert.True(history.CanUndo);
            Assert.False(history.CanRedo);
        }

        [Fact]
        public void Execute_ClearsRedoStack()
        {
            var history = CreateHistory();
            var first = new TestEditCommand("first", () => { }, () => { });
            var second = new TestEditCommand("second", () => { }, () => { });

            history.Execute(first);
            history.Undo();
            Assert.True(history.CanRedo);

            history.Execute(second);

            Assert.False(history.CanRedo);
            Assert.Equal(1, history.UndoCount);
        }

        [Fact]
        public void MaxDepth_TrimsOldestUndoEntry()
        {
            var history = new EditCommandHistory(CreateMessenger(), maxDepth: 2);

            history.Execute(new TestEditCommand("one", () => { }, () => { }));
            history.Execute(new TestEditCommand("two", () => { }, () => { }));
            history.Execute(new TestEditCommand("three", () => { }, () => { }));

            Assert.Equal(2, history.UndoCount);
            history.Undo();
            history.Undo();
            Assert.False(history.CanUndo);
        }

        [Fact]
        public void CompositeEditCommand_UndoRunsChildrenInReverseOrder()
        {
            var events = new List<string>();
            var composite = new CompositeEditCommand(
                "batch",
                new IEditCommand[]
                {
                    new TestEditCommand("a", () => events.Add("execute-a"), () => events.Add("undo-a")),
                    new TestEditCommand("b", () => events.Add("execute-b"), () => events.Add("undo-b"))
                });

            composite.Execute();
            composite.Undo();

            Assert.Equal(new[] { "execute-a", "execute-b", "undo-b", "undo-a" }, events);
        }

        [Fact]
        public void ScoreLoadedMessage_ClearsHistory()
        {
            var messenger = CreateMessenger();
            var history = new EditCommandHistory(messenger);
            history.Execute(new TestEditCommand("edit", () => { }, () => { }));
            history.Undo();

            messenger.Send(new ScoreLoadedMessage(new JianpuScore(), null));

            Assert.False(history.CanUndo);
            Assert.False(history.CanRedo);
        }

        [Fact]
        public void HistoryChanged_FiresOnExecuteUndoAndClear()
        {
            var history = CreateHistory();
            var changes = 0;
            history.HistoryChanged += (s, e) => changes++;

            history.Execute(new TestEditCommand("edit", () => { }, () => { }));
            history.Undo();
            history.Clear();

            Assert.Equal(3, changes);
        }

        private static EditCommandHistory CreateHistory()
        {
            return new EditCommandHistory(CreateMessenger());
        }

        private static AppMessenger CreateMessenger()
        {
            return new AppMessenger(new CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger());
        }

        private sealed class TestEditCommand : IEditCommand
        {
            private readonly System.Action _execute;
            private readonly System.Action _undo;

            public TestEditCommand(string description, System.Action execute, System.Action undo)
            {
                Description = description;
                _execute = execute;
                _undo = undo;
            }

            public string Description { get; }

            public void Execute()
            {
                _execute();
            }

            public void Undo()
            {
                _undo();
            }
        }
    }
}
