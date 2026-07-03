using JianpuEditor.Core.Abstractions;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Services.NoteEditCommands
{
    internal interface INoteEditCommand : IEditCommand
    {
        ScoreEditResult Result { get; }
    }
}
