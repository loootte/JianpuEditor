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
                LyricText = source.LyricText ?? string.Empty,
                MelodyNotes = new List<JianpuNote>(),
                ChordMarkers = new List<ChordMarker>(),
                LyricSyllables = new List<LyricSyllable>(),
                Ornaments = new List<JianpuOrnament>()
            };

            if (source.MelodyNotes != null)
            {
                foreach (var note in source.MelodyNotes)
                {
                    clone.MelodyNotes.Add(CloneNote(note));
                }
            }

            if (source.ChordMarkers != null)
            {
                foreach (var marker in source.ChordMarkers)
                {
                    clone.ChordMarkers.Add(new ChordMarker
                    {
                        Text = marker.Text ?? string.Empty,
                        BeatPosition = marker.BeatPosition
                    });
                }
            }

            if (source.LyricSyllables != null)
            {
                foreach (var syllable in source.LyricSyllables)
                {
                    clone.LyricSyllables.Add(new LyricSyllable
                    {
                        Text = syllable.Text ?? string.Empty,
                        NoteIndex = syllable.NoteIndex,
                        BeatPosition = syllable.BeatPosition
                    });
                }
            }

            if (source.Ornaments != null)
            {
                foreach (var ornament in source.Ornaments)
                {
                    if (ornament == null)
                    {
                        continue;
                    }

                    clone.Ornaments.Add(new JianpuOrnament
                    {
                        Type = ornament.Type,
                        NoteIndex = ornament.NoteIndex,
                        BeatPosition = ornament.BeatPosition,
                        Parameters = ornament.Parameters == null
                            ? new Dictionary<string, string>()
                            : new Dictionary<string, string>(ornament.Parameters)
                    });
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
                Accidental = source.Accidental,
                Octave = source.Octave,
                Underlines = source.Underlines,
                Dashes = source.Dashes,
                Dotted = source.Dotted
            };
        }
    }
}
