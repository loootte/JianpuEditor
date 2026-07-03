using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IChordTransposeService
    {
        bool TryTransposeChords(
            JianpuScore score,
            string targetKeySignature,
            out string errorMessage,
            out int transposedCount);
    }
}
