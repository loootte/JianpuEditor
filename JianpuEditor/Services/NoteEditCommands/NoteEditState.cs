using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Services.NoteEditCommands
{
    internal static class NoteEditState
    {
        public static JianpuNote CloneNote(JianpuNote source)
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

        public static List<JianpuNote> CloneMelodyNotes(IReadOnlyList<JianpuNote> notes)
        {
            var clone = new List<JianpuNote>();
            if (notes == null)
            {
                return clone;
            }

            for (var i = 0; i < notes.Count; i++)
            {
                clone.Add(CloneNote(notes[i]));
            }

            return clone;
        }

        public static void CopyNoteProperties(JianpuNote target, JianpuNote source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.Type = source.Type;
            target.Pitch = source.Pitch;
            target.Octave = source.Octave;
            target.Underlines = source.Underlines;
            target.Dashes = source.Dashes;
            target.Dotted = source.Dotted;
        }

        public static Dictionary<int, List<JianpuNote>> CaptureAllMeasuresMelody(JianpuScore score)
        {
            var backup = new Dictionary<int, List<JianpuNote>>();
            if (score?.Measures == null)
            {
                return backup;
            }

            for (var i = 0; i < score.Measures.Count; i++)
            {
                backup[i] = CloneMelodyNotes(score.Measures[i].MelodyNotes);
            }

            return backup;
        }

        public static void RestoreMeasuresMelody(JianpuScore score, Dictionary<int, List<JianpuNote>> backup)
        {
            if (score?.Measures == null || backup == null)
            {
                return;
            }

            foreach (var entry in backup)
            {
                if (entry.Key < 0 || entry.Key >= score.Measures.Count)
                {
                    continue;
                }

                var measure = score.Measures[entry.Key];
                measure.MelodyNotes.Clear();
                if (entry.Value == null)
                {
                    continue;
                }

                for (var i = 0; i < entry.Value.Count; i++)
                {
                    measure.MelodyNotes.Add(CloneNote(entry.Value[i]));
                }
            }
        }
    }
}
