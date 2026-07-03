using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class MeasurePlaceholderService
    {
        public const int DefaultPlaceholderCount = 4;

        public static JianpuMeasure CreateMeasureWithPlaceholders(int count = DefaultPlaceholderCount)
        {
            var measure = new JianpuMeasure();
            for (var i = 0; i < count; i++)
            {
                measure.MelodyNotes.Add(CreateQuarterNotePlaceholder());
            }

            return measure;
        }

        public static JianpuNote CreateQuarterNotePlaceholder()
        {
            return new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 1,
                Octave = 0,
                Underlines = 0,
                Dashes = 0,
                Dotted = false
            };
        }
    }
}
