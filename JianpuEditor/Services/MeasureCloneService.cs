using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class MeasureCloneService
    {
        public static JianpuMeasure Clone(JianpuMeasure source)
        {
            if (source == null)
            {
                return new JianpuMeasure();
            }

            var clone = new JianpuMeasure
            {
                SecondaryText = source.SecondaryText ?? string.Empty,
                LyricText = source.LyricText ?? string.Empty,
                MelodyNotes = new List<JianpuNote>()
            };

            if (source.MelodyNotes != null)
            {
                foreach (var note in source.MelodyNotes)
                {
                    clone.MelodyNotes.Add(CloneNote(note));
                }
            }

            return clone;
        }

        private static JianpuNote CloneNote(JianpuNote source)
        {
            if (source == null)
            {
                return new JianpuNote();
            }

            return new JianpuNote
            {
                Type = source.Type,
                Pitch = source.Pitch,
                Octave = source.Octave,
                Underlines = source.Underlines,
                Dashes = source.Dashes,
                Dotted = source.Dotted
            };
        }
    }
}