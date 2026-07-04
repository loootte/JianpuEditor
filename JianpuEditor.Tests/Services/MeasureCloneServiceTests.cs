using System.Collections.Generic;
using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class MeasureCloneServiceTests
    {
        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            var source = ScoreTestHelper.MeasureWithChords(
                new[] { "C", "G" },
                new[] { 0d, 2d },
                ScoreTestHelper.Note(1, dashes: 1, dotted: true),
                ScoreTestHelper.Note(2, underlines: 1));
            source.LyricText = "歌词";
            source.LyricSyllables.Add(new LyricSyllable { Text = "歌", NoteIndex = 0 });
            source.Ornaments.Add(new JianpuOrnament
            {
                Type = OrnamentType.Trill,
                NoteIndex = 0,
                Parameters = new Dictionary<string, string> { [OrnamentService.ParamCount] = "2" }
            });

            var clone = MeasureCloneService.Clone(source);

            Assert.NotSame(source, clone);
            Assert.Equal(source.LyricText, clone.LyricText);
            Assert.Equal(source.MelodyNotes.Count, clone.MelodyNotes.Count);
            Assert.Equal(source.ChordMarkers.Count, clone.ChordMarkers.Count);
            Assert.Equal(source.LyricSyllables.Count, clone.LyricSyllables.Count);
            Assert.Equal(source.Ornaments.Count, clone.Ornaments.Count);
            Assert.NotSame(source.MelodyNotes[0], clone.MelodyNotes[0]);
            Assert.NotSame(source.ChordMarkers[0], clone.ChordMarkers[0]);
            Assert.NotSame(source.LyricSyllables[0], clone.LyricSyllables[0]);
            Assert.NotSame(source.Ornaments[0], clone.Ornaments[0]);

            clone.MelodyNotes[0].Pitch = 7;
            clone.ChordMarkers[0].Text = "Am";
            clone.LyricText = "changed";
            clone.LyricSyllables[0].Text = "词";
            clone.Ornaments[0].Type = OrnamentType.Fermata;

            Assert.Equal(1, source.MelodyNotes[0].Pitch);
            Assert.Equal("C", source.ChordMarkers[0].Text);
            Assert.Equal("歌词", source.LyricText);
            Assert.Equal("歌", source.LyricSyllables[0].Text);
            Assert.Equal(OrnamentType.Trill, source.Ornaments[0].Type);
        }

        [Fact]
        public void Clone_NullSourceReturnsEmptyMeasure()
        {
            var clone = MeasureCloneService.Clone(null);

            Assert.NotNull(clone);
            Assert.Empty(clone.MelodyNotes);
            Assert.Empty(clone.ChordMarkers);
        }
    }
}
