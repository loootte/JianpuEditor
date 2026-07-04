using System;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public static class JianpuPitchCodec
    {
        public const double AccidentalFraction = 0.5;
        private const double PitchEpsilon = 0.001;

        private static readonly int[] MajorScaleOffsets = { 0, 2, 4, 5, 7, 9, 11 };

        public static bool IsNatural(double pitch)
        {
            return Math.Abs(pitch - Math.Round(pitch)) < PitchEpsilon;
        }

        public static bool HasAccidental(JianpuNote note)
        {
            return note != null
                && note.Type == NoteType.Note
                && note.Accidental != AccidentalKind.None;
        }

        public static int GetDisplayDegree(JianpuNote note)
        {
            if (note == null || note.Type == NoteType.Rest)
            {
                return 0;
            }

            if (note.Accidental == AccidentalKind.Sharp)
            {
                return (int)Math.Floor(note.Pitch + PitchEpsilon);
            }

            if (note.Accidental == AccidentalKind.Flat)
            {
                return (int)Math.Ceiling(note.Pitch - PitchEpsilon);
            }

            return (int)Math.Round(note.Pitch);
        }

        public static string GetPitchDisplayText(JianpuNote note)
        {
            if (note == null || note.Type == NoteType.Rest)
            {
                return "0";
            }

            var degree = GetDisplayDegree(note);
            var mark = GetAccidentalMark(note);
            return mark == null ? degree.ToString() : mark + degree;
        }

        public static string GetAccidentalMark(JianpuNote note)
        {
            if (note == null || note.Type == NoteType.Rest)
            {
                return null;
            }

            if (note.Accidental == AccidentalKind.Sharp)
            {
                return "#";
            }

            if (note.Accidental == AccidentalKind.Flat)
            {
                return "b";
            }

            return null;
        }

        public static bool IsValidMelodyPitch(JianpuNote note)
        {
            if (note == null || note.Type == NoteType.Rest)
            {
                return false;
            }

            if (note.Accidental != AccidentalKind.None)
            {
                return Math.Abs(note.Pitch - Math.Floor(note.Pitch) - AccidentalFraction) < PitchEpsilon
                    || Math.Abs(note.Pitch - Math.Ceiling(note.Pitch) - AccidentalFraction) < PitchEpsilon;
            }

            return note.Pitch >= 1 - PitchEpsilon && note.Pitch <= 7 + PitchEpsilon && IsNatural(note.Pitch);
        }

        public static int ToMelodyMidiNote(JianpuNote note, int tonicMidi)
        {
            if (note == null || note.Type == NoteType.Rest || !IsValidMelodyPitch(note))
            {
                return tonicMidi;
            }

            var degree = GetDiatonicDegree(note);
            if (degree < 1 || degree > 7)
            {
                return tonicMidi;
            }

            var midi = tonicMidi + MajorScaleOffsets[degree - 1] + note.Octave * 12;
            if (note.Accidental == AccidentalKind.Sharp)
            {
                midi += 1;
            }
            else if (note.Accidental == AccidentalKind.Flat)
            {
                midi -= 1;
            }

            return Math.Max(0, Math.Min(127, midi));
        }

        public static void SetNaturalPitch(JianpuNote note, int pitch)
        {
            if (note == null)
            {
                return;
            }

            note.Pitch = pitch;
            note.Accidental = AccidentalKind.None;
        }

        public static void SetAccidentalPitch(JianpuNote note, AccidentalKind accidental, int degree)
        {
            if (note == null)
            {
                return;
            }

            note.Accidental = accidental;
            if (accidental == AccidentalKind.Sharp)
            {
                note.Pitch = degree + AccidentalFraction;
                return;
            }

            if (accidental == AccidentalKind.Flat)
            {
                note.Pitch = (degree - 1) + AccidentalFraction;
            }
        }

        private static int GetDiatonicDegree(JianpuNote note)
        {
            if (note.Accidental == AccidentalKind.Sharp)
            {
                return (int)Math.Floor(note.Pitch + PitchEpsilon);
            }

            if (note.Accidental == AccidentalKind.Flat)
            {
                return (int)Math.Ceiling(note.Pitch - PitchEpsilon);
            }

            return (int)Math.Round(note.Pitch);
        }
    }
}
