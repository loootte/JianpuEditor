using JianpuEditor.Core.Abstractions;
using JianpuEditor.Services.NoteEditCommands;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.EditCommands
{
    internal static class EditCommandHelper
    {
        public static ScoreEditResult Execute(IEditCommandHistory history, INoteEditCommand command)
        {
            history.Execute(command);
            return command.Result ?? ScoreEditResult.Unchanged;
        }
    }
}
