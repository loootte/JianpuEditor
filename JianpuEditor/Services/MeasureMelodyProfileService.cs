using System;
using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    internal sealed class MeasureMelodyProfile
    {
        public int PrimaryDegree { get; set; }

        public int EndDegree { get; set; }

        public int BassDegree { get; set; }
    }

    internal static class MeasureMelodyProfileService
    {
        public static IReadOnlyList<MeasureMelodyProfile> AnalyzeMeasures(
            IReadOnlyList<JianpuMeasure> measures,
            int tonicMidi)
        {
            var profiles = new List<MeasureMelodyProfile>();
            if (measures == null)
            {
                return profiles;
            }

            foreach (var measure in measures)
            {
                profiles.Add(AnalyzeMeasure(measure, tonicMidi));
            }

            return profiles;
        }

        private static MeasureMelodyProfile AnalyzeMeasure(JianpuMeasure measure, int tonicMidi)
        {
            var profile = new MeasureMelodyProfile();
            if (measure?.MelodyNotes == null || measure.MelodyNotes.Count == 0)
            {
                profile.PrimaryDegree = 1;
                profile.EndDegree = 1;
                profile.BassDegree = 1;
                return profile;
            }

            var degrees = new List<int>();
            var lowestMidi = int.MaxValue;
            var bassDegree = 1;
            foreach (var note in measure.MelodyNotes)
            {
                if (note == null || note.Type == NoteType.Rest || !JianpuPitchCodec.IsValidMelodyPitch(note))
                {
                    continue;
                }

                var degree = GetDiatonicDegree(note);
                if (degree >= 1 && degree <= 7)
                {
                    degrees.Add(degree);
                }

                var midi = JianpuPitchCodec.ToMelodyMidiNote(note, tonicMidi);
                if (midi < lowestMidi)
                {
                    lowestMidi = midi;
                    bassDegree = degree >= 1 && degree <= 7 ? degree : bassDegree;
                }
            }

            if (degrees.Count == 0)
            {
                profile.PrimaryDegree = 1;
                profile.EndDegree = 1;
                profile.BassDegree = 1;
                return profile;
            }

            profile.PrimaryDegree = degrees[0];
            profile.EndDegree = degrees[degrees.Count - 1];
            profile.BassDegree = bassDegree;
            return profile;
        }

        private static int GetDiatonicDegree(JianpuNote note)
        {
            if (note.Accidental == AccidentalKind.Sharp)
            {
                return (int)Math.Floor(note.Pitch + 0.001);
            }

            if (note.Accidental == AccidentalKind.Flat)
            {
                return (int)Math.Ceiling(note.Pitch - 0.001);
            }

            return (int)Math.Round(note.Pitch);
        }
    }
}
