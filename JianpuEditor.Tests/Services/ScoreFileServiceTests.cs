using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class ScoreFileServiceTests
    {
        [Fact]
        public void SaveAndLoad_RoundTripsScoreWithoutExtraMeasure()
        {
            var score = ScoreTestHelper.CreateScore(
                ScoreTestHelper.MeasureWithChords(
                    new[] { "C", "G" },
                    new[] { 0d, 2d },
                    ScoreTestHelper.Note(1),
                    ScoreTestHelper.Note(2),
                    ScoreTestHelper.Note(3),
                    ScoreTestHelper.Note(4)));
            score.Title = "Round Trip";
            var path = Path.Combine(Path.GetTempPath(), "jianpu-roundtrip-" + Guid.NewGuid() + ".json");

            try
            {
                ScoreFileService.Save(score, path);
                var loaded = ScoreFileService.Load(path);

                Assert.Equal("Round Trip", loaded.Title);
                Assert.Single(loaded.Measures);
                Assert.Equal(2, loaded.Measures[0].ChordMarkers.Count);
                Assert.Equal(4, loaded.Measures[0].MelodyNotes.Count);
                Assert.DoesNotContain(loaded.Measures, measure => measure.MelodyNotes.Count == 0 && measure.ChordMarkers.Count == 0 && string.IsNullOrEmpty(measure.LyricText));
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
        public void SaveAndLoad_RoundTripsLyricSyllables()
        {
            var measure = ScoreTestHelper.MeasureWithLyrics(
                "欢乐女神",
                new[] { "欢", "乐", "女", "神" },
                new[] { 0, 1, 2, 3 },
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3),
                ScoreTestHelper.Note(4));
            var score = ScoreTestHelper.CreateScore(measure);
            var path = Path.Combine(Path.GetTempPath(), "jianpu-lyrics-" + Guid.NewGuid() + ".json");

            try
            {
                ScoreFileService.Save(score, path);
                var loaded = ScoreFileService.Load(path);

                Assert.Equal("欢乐女神", loaded.Measures[0].LyricText);
                Assert.Equal(4, loaded.Measures[0].LyricSyllables.Count);
                Assert.Equal("欢", loaded.Measures[0].LyricSyllables[0].Text);
                Assert.Equal(0, loaded.Measures[0].LyricSyllables[0].NoteIndex);
                Assert.Equal(3, loaded.Measures[0].LyricSyllables[3].NoteIndex);
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
        public void Load_PreservesLegacyLyricTextWithoutSyllables()
        {
            var path = Path.Combine(Path.GetTempPath(), "jianpu-legacy-lyric-" + Guid.NewGuid() + ".json");
            var json = @"{
  ""Title"": ""Legacy Lyric"",
  ""KeySignature"": ""1=C"",
  ""Measures"": [
    {
      ""MelodyNotes"": [{ ""Pitch"": 1 }, { ""Pitch"": 2 }],
      ""LyricText"": ""照大地""
    }
  ]
}";

            try
            {
                File.WriteAllText(path, json);
                var loaded = ScoreFileService.Load(path);

                Assert.Equal("照大地", loaded.Measures[0].LyricText);
                Assert.Empty(loaded.Measures[0].LyricSyllables);
                Assert.Equal("照大地", LyricSyllableService.GetDisplayText(loaded.Measures[0]));
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
        public void Load_ImportsLegacySecondaryTextIntoChordMarkers()
        {
            var path = Path.Combine(Path.GetTempPath(), "jianpu-legacy-" + Guid.NewGuid() + ".json");
            var json = @"{
  ""Title"": ""Legacy"",
  ""KeySignature"": ""1=C"",
  ""Measures"": [
    {
      ""MelodyNotes"": [{ ""Pitch"": 1 }, { ""Pitch"": 2 }, { ""Pitch"": 3 }, { ""Pitch"": 4 }],
      ""SecondaryText"": ""D Bm7""
    }
  ]
}";

            try
            {
                File.WriteAllText(path, json);
                var loaded = ScoreFileService.Load(path);

                Assert.Equal(2, loaded.Measures[0].ChordMarkers.Count);
                Assert.Equal("D", loaded.Measures[0].ChordMarkers[0].Text);
                Assert.Equal("Bm7", loaded.Measures[0].ChordMarkers[1].Text);
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
