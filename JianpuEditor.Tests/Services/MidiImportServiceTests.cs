using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class MidiImportServiceTests
    {
        [Fact]
        public void Import_ExportedMelody_ProducesEditableScore()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)));
            score.Title = "Round Trip";
            score.KeySignature = "1=C";
            score.Bpm = 120;

            var path = Path.Combine(Path.GetTempPath(), "jianpu-import-" + Guid.NewGuid() + ".mid");
            try
            {
                MidiExportService.Export(score, path);
                var imported = MidiImportService.Import(path);

                Assert.Equal(Path.GetFileNameWithoutExtension(path), imported.Title);
                Assert.StartsWith("1=", imported.KeySignature);
                Assert.True(imported.Measures.Count > 0);
                Assert.Contains(imported.Measures, measure => measure.MelodyNotes.Count > 0);
                Assert.All(imported.Measures, measure =>
                    Assert.True(measure.MelodyNotes.Count > 0 || imported.Measures.Count == 1));
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
        public void Import_ExportedScore_PreservesPitchCount()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4),
                    ScoreTestHelper.Note(5)));
            score.KeySignature = "1=C";

            var path = Path.Combine(Path.GetTempPath(), "jianpu-import-pitch-" + Guid.NewGuid() + ".mid");
            try
            {
                MidiExportService.Export(score, path);
                var imported = MidiImportService.Import(path);
                var noteCount = imported.Measures.Sum(measure =>
                    measure.MelodyNotes.Count(note => note.Type == NoteType.Note));

                Assert.Equal(3, noteCount);
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
        public void TryMidiToJianpu_MapsMiddleC()
        {
            Assert.True(MidiImportService.TryMidiToJianpu(
                60,
                60,
                out var pitch,
                out var octave,
                out var accidental,
                out var error));
            Assert.Equal(1, pitch);
            Assert.Equal(0, octave);
            Assert.Equal(AccidentalKind.None, accidental);
            Assert.Equal(0, error);
        }

        [Fact]
        public void Import_NormalizesMeasuresToFourBeats()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)),
                ScoreTestHelper.Measure(
                    ScoreTestHelper.Note(5),
                    ScoreTestHelper.Note(6),
                    ScoreTestHelper.Note(7)));
            score.KeySignature = "1=C";

            var path = Path.Combine(Path.GetTempPath(), "jianpu-import-normalize-" + Guid.NewGuid() + ".mid");
            try
            {
                MidiExportService.Export(score, path);
                var imported = MidiImportService.Import(path);

                Assert.True(imported.Measures.Count >= 2);
                foreach (var measure in imported.Measures)
                {
                    var beats = measure.MelodyNotes.Sum(note => JianpuRenderer.GetDurationUnits(note));
                    Assert.Equal(4, beats, 2);
                }
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
        public void ApplyDurationUnits_EncodesCommonValues()
        {
            var quarter = new JianpuNote { Type = NoteType.Note, Pitch = 1 };
            Assert.True(MidiImportService.ApplyDurationUnits(quarter, 1.0));
            Assert.Equal(0, quarter.Underlines);
            Assert.Equal(0, quarter.Dashes);

            var eighth = new JianpuNote { Type = NoteType.Note, Pitch = 1 };
            Assert.True(MidiImportService.ApplyDurationUnits(eighth, 0.5));
            Assert.Equal(1, eighth.Underlines);

            var half = new JianpuNote { Type = NoteType.Note, Pitch = 1 };
            Assert.True(MidiImportService.ApplyDurationUnits(half, 2.0));
            Assert.Equal(1, half.Dashes);
        }

        [Fact]
        public void Import_ThrowsForMissingFile()
        {
            Assert.Throws<FileNotFoundException>(() =>
                MidiImportService.Import(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mid")));
        }
    }
}
