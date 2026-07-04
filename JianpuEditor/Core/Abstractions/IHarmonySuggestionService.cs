using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Core.Abstractions
{
    public interface IHarmonySuggestionService
    {
        IReadOnlyList<HarmonySuggestion> SuggestForMeasure(
            JianpuMeasure measure,
            string keySignature,
            double beatPosition);

        IReadOnlyList<HarmonyProgressionSuggestion> SuggestForMeasureRange(
            IReadOnlyList<JianpuMeasure> measures,
            string keySignature);
    }
}
