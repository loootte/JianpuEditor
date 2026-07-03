using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class ChordTransposeServiceAdapter : IChordTransposeService
    {
        public bool TryTransposeChords(
            JianpuScore score,
            string targetKeySignature,
            out string errorMessage,
            out int transposedCount)
        {
            return ChordTransposeService.TryTransposeChords(
                score,
                targetKeySignature,
                out errorMessage,
                out transposedCount);
        }
    }
}
