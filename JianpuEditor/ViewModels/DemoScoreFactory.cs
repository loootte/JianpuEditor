using System.Collections.Generic;
using JianpuEditor.Models;
using JianpuEditor.Services;

namespace JianpuEditor.ViewModels
{
    internal static class DemoScoreFactory
    {
        public static JianpuScore CreateOdeToJoy()
        {
            var score = new JianpuScore
            {
                Title = "欢乐颂",
                KeySignature = "1=C",
                Tempo = "中速",
                Bpm = 120,
                Composer = "贝多芬",
                Measures = new List<JianpuMeasure>
                {
                    new JianpuMeasure
                    {
                        MelodyNotes = new List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 3 }, new JianpuNote { Pitch = 3 },
                            new JianpuNote { Pitch = 4 }, new JianpuNote { Pitch = 5 }
                        },
                        ChordMarkers = new List<ChordMarker>
                        {
                            new ChordMarker { Text = "C", BeatPosition = 0 },
                            new ChordMarker { Text = "G", BeatPosition = 2 }
                        },
                        LyricText = "欢乐女神"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 5 }, new JianpuNote { Pitch = 4 },
                            new JianpuNote { Pitch = 3 }, new JianpuNote { Pitch = 2 }
                        },
                        ChordMarkers = new List<ChordMarker>
                        {
                            new ChordMarker { Text = "G", BeatPosition = 0 },
                            new ChordMarker { Text = "C", BeatPosition = 2 }
                        },
                        LyricText = "圣洁美丽"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 1, Dashes = 1 }
                        },
                        ChordMarkers = new List<ChordMarker>
                        {
                            new ChordMarker { Text = "F", BeatPosition = 0 }
                        },
                        LyricText = "灿烂光芒"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 1 }, new JianpuNote { Pitch = 2 },
                            new JianpuNote { Pitch = 3 }, new JianpuNote { Pitch = 2, Dashes = 1 }
                        },
                        ChordMarkers = new List<ChordMarker>
                        {
                            new ChordMarker { Text = "C", BeatPosition = 0 },
                            new ChordMarker { Text = "G", BeatPosition = 2 }
                        },
                        LyricText = "照大地"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 3, Dotted = true, Underlines = 1 },
                            new JianpuNote { Pitch = 3, Underlines = 1 }
                        },
                        ChordMarkers = new List<ChordMarker>
                        {
                            new ChordMarker { Text = "C", BeatPosition = 0 }
                        },
                        LyricText = "我们欢聚"
                    }
                }
            };

            ChordMarkerService.NormalizeScore(score);
            return score;
        }
    }
}
