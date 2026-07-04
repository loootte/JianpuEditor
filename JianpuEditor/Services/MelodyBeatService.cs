using System;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Services
{
    public static class MelodyBeatService
    {
        public static double GetNoteStartBeat(JianpuMeasure measure, int noteIndex)
        {
            if (measure?.MelodyNotes == null || noteIndex < 0 || noteIndex >= measure.MelodyNotes.Count)
            {
                return 0;
            }

            var beat = 0.0;
            for (var i = 0; i < noteIndex; i++)
            {
                beat += JianpuRenderer.GetDurationUnits(measure.MelodyNotes[i]);
            }

            return beat;
        }

        public static int FindNoteIndexAtBeat(JianpuMeasure measure, double beatPosition)
        {
            if (measure?.MelodyNotes == null || measure.MelodyNotes.Count == 0)
            {
                return -1;
            }

            var cursor = 0.0;
            for (var i = 0; i < measure.MelodyNotes.Count; i++)
            {
                var duration = JianpuRenderer.GetDurationUnits(measure.MelodyNotes[i]);
                if (beatPosition >= cursor - 0.001 && beatPosition < cursor + duration - 0.001)
                {
                    return i;
                }

                cursor += duration;
            }

            return measure.MelodyNotes.Count - 1;
        }
    }
}
