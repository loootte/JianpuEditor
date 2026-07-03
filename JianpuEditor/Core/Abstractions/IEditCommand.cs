namespace JianpuEditor.Core.Abstractions
{
    public interface IEditCommand
    {
        string Description { get; }

        void Execute();

        void Undo();
    }
}
