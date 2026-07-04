using System.Collections.Generic;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class HarmonySuggestionServiceAdapter : IHarmonySuggestionService
    {
        public IReadOnlyList<HarmonySuggestion> SuggestForMeasure(
            JianpuMeasure measure,
            string keySignature,
            double beatPosition)
        {
            return HarmonySuggestionService.SuggestForMeasure(measure, keySignature, beatPosition);
        }
    }
}
