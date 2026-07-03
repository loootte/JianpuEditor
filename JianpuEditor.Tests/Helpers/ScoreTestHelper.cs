using JianpuEditor.Models;

namespace JianpuEditor.Tests.Helpers
{
    internal static class ScoreTestHelper
    {
        public static JianpuNote Note(int pitch, int dashes = 0, int underlines = 0, bool dotted = false, int octave = 0)
        {
            return new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = pitch,
                Dashes = dashes,
                Underlines = underlines,
                Dotted = dotted,
                Octave = octave
            };
        }

        public static JianpuNote Rest(int underlines = 0)
        {
            return new JianpuNote
            {
                Type = NoteType.Rest,
                Pitch = 0,
                Underlines = underlines
            };
        }

        public static JianpuMeasure Measure(params JianpuNote[] notes)
        {
            return new JianpuMeasure
            {
                MelodyNotes = new List<JianpuNote>(notes)
            };
        }

        public static JianpuMeasure MeasureWithChords(string[] chordTexts, double[] beatPositions, params JianpuNote[] notes)
        {
            var measure = Measure(notes);
            measure.ChordMarkers = new List<ChordMarker>();
            for (var i = 0; i < chordTexts.Length; i++)
            {
                measure.ChordMarkers.Add(new ChordMarker
                {
                    Text = chordTexts[i],
                    BeatPosition = beatPositions[i]
                });
            }

            return measure;
        }

        public static JianpuScore CreateScore(params JianpuMeasure[] measures)
        {
            return new JianpuScore
            {
                Title = "Test Score",
                KeySignature = "1=C",
                Bpm = 120,
                Measures = new List<JianpuMeasure>(measures)
            };
        }
    }
}