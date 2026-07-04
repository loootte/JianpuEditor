using System;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class JianpuPitchService
    {
        public const int MinOctave = -1;
        public const int MaxOctave = 1;
        public const int MinPitch = 1;
        public const int MaxPitch = 7;

        public static bool TryTranspose(JianpuNote note, int delta)
        {
            if (note == null || note.Type == NoteType.Rest || delta == 0)
            {
                return false;
            }

            if (!JianpuPitchCodec.IsNatural(note.Pitch) || note.Accidental != AccidentalKind.None)
            {
                return false;
            }

            if (!TryGetTransposedPitch((int)Math.Round(note.Pitch), note.Octave, delta, out var newPitch, out var newOctave))
            {
                return false;
            }

            JianpuPitchCodec.SetNaturalPitch(note, newPitch);
            note.Octave = newOctave;
            return true;
        }

        public static bool TryGetTransposedPitch(
            int pitch,
            int octave,
            int delta,
            out int newPitch,
            out int newOctave)
        {
            newPitch = pitch;
            newOctave = octave;

            if (delta == 0 || pitch < MinPitch || pitch > MaxPitch)
            {
                return false;
            }

            if (delta > 0)
            {
                if (pitch < MaxPitch)
                {
                    newPitch = pitch + 1;
                    return true;
                }

                if (octave < MaxOctave)
                {
                    newPitch = MinPitch;
                    newOctave = octave + 1;
                    return true;
                }

                return false;
            }

            if (pitch > MinPitch)
            {
                newPitch = pitch - 1;
                return true;
            }

            if (octave > MinOctave)
            {
                newPitch = MaxPitch;
                newOctave = octave - 1;
                return true;
            }

            return false;
        }
    }
}
