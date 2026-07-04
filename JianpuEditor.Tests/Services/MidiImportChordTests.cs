using System.IO;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class MidiImportChordTests
    {
        [Fact]
        public void Import_RoundTripPreservesSimultaneousChordFromExport()
        {
            var measure = new JianpuMeasure
            {
                Chords = new List<JianpuChord>
                {
                    new JianpuChord
                    {
                        BeatPosition = 0,
                        Notes = new List<JianpuNote>
                        {
                            ScoreTestHelper.Note(1),
                            ScoreTestHelper.Note(3)
                        }
                    }
                }
            };
            MelodyChordService.NormalizeMeasure(measure);
            var score = ScoreTestHelper.CreateScore(measure);
            score.KeySignature = "1=C";

            var path = Path.Combine(Path.GetTempPath(), "jianpu-import-chord-" + System.Guid.NewGuid() + ".mid");
            try
            {
                MidiExportService.Export(score, path);
                var imported = MidiImportService.Import(path);
                MelodyChordService.NormalizeMeasure(imported.Measures[0]);

                Assert.NotEmpty(imported.Measures[0].Chords);
                Assert.Contains(
                    imported.Measures[0].Chords,
                    chord => chord.Notes.Count(note => note.Type == NoteType.Note) > 1);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void SaveAndLoad_RoundTripsMultiNoteChords()
        {
            var measure = new JianpuMeasure
            {
                Chords = new List<JianpuChord>
                {
                    new JianpuChord
                    {
                        BeatPosition = 0,
                        Notes = new List<JianpuNote>
                        {
                            ScoreTestHelper.Note(1),
                            ScoreTestHelper.Note(3, octave: 1)
                        }
                    },
                    new JianpuChord
                    {
                        BeatPosition = 1,
                        Notes = new List<JianpuNote> { ScoreTestHelper.Note(5) }
                    }
                }
            };
            MelodyChordService.NormalizeMeasure(measure);
            var score = ScoreTestHelper.CreateScore(measure);
            var path = Path.Combine(Path.GetTempPath(), "jianpu-chord-roundtrip-" + System.Guid.NewGuid() + ".json");

            try
            {
                ScoreFileService.Save(score, path);
                var loaded = ScoreFileService.Load(path);

                MelodyChordService.NormalizeMeasure(loaded.Measures[0]);
                Assert.Equal(2, loaded.Measures[0].Chords.Count);
                Assert.Equal(2, loaded.Measures[0].Chords[0].Notes.Count);
                Assert.Equal(1, loaded.Measures[0].Chords[0].Notes[0].Pitch, 3);
                Assert.Equal(3, loaded.Measures[0].Chords[0].Notes[1].Pitch, 3);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

    }
}