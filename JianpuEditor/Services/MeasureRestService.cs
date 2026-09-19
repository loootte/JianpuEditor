using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class MeasureRestService
    {
        public static bool IsSoundingNote(JianpuNote note)
        {
            return note != null
                && note.Type == NoteType.Note
                && JianpuPitchCodec.IsValidMelodyPitch(note);
        }

        public static bool IsRestOnly(JianpuMeasure measure)
        {
            if (measure?.MelodyNotes == null || measure.MelodyNotes.Count == 0)
            {
                return true;
            }

            foreach (var note in measure.MelodyNotes)
            {
                if (IsSoundingNote(note))
                {
                    return false;
                }
            }

            if (measure.Chords == null)
            {
                return true;
            }

            foreach (var chord in measure.Chords)
            {
                if (chord?.Notes == null)
                {
                    continue;
                }

                foreach (var note in chord.Notes)
                {
                    if (IsSoundingNote(note))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
