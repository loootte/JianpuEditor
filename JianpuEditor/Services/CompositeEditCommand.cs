using System;
using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;

namespace JianpuEditor.Services
{
    public sealed class CompositeEditCommand : IEditCommand
    {
        private readonly IReadOnlyList<IEditCommand> _commands;

        public CompositeEditCommand(string description, IReadOnlyList<IEditCommand> commands)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Description is required.", nameof(description));
            }

            if (commands == null || commands.Count == 0)
            {
                throw new ArgumentException("At least one command is required.", nameof(commands));
            }

            Description = description;
            _commands = commands;
        }

        public string Description { get; }

        public void Execute()
        {
            for (var i = 0; i < _commands.Count; i++)
            {
                _commands[i].Execute();
            }
        }

        public void Undo()
        {
            for (var i = _commands.Count - 1; i >= 0; i--)
            {
                _commands[i].Undo();
            }
        }
    }
}
