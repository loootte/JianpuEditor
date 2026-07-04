using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class OrnamentServiceTests
    {
        [Fact]
        public void NormalizeMeasure_SyncsBeatPositionFromNoteIndex()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2, dashes: 1),
                ScoreTestHelper.Note(3));
            measure.Ornaments.Add(new JianpuOrnament
            {
                Type = OrnamentType.Trill,
                NoteIndex = 2
            });

            OrnamentService.NormalizeMeasure(measure);

            Assert.Single(measure.Ornaments);
            Assert.Equal(3, measure.Ornaments[0].BeatPosition);
            Assert.Equal(OrnamentType.Trill, measure.Ornaments[0].Type);
        }

        [Fact]
        public void NormalizeMeasure_DropsUnknownOrnaments()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1));
            measure.Ornaments.Add(new JianpuOrnament { Type = OrnamentType.Unknown });
            measure.Ornaments.Add(new JianpuOrnament { Type = OrnamentType.Staccato, NoteIndex = 0 });

            OrnamentService.NormalizeMeasure(measure);

            Assert.Single(measure.Ornaments);
            Assert.Equal(OrnamentType.Staccato, measure.Ornaments[0].Type);
        }

        [Fact]
        public void ResolveNoteIndex_FindsNoteFromBeatPosition()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2, dashes: 1),
                ScoreTestHelper.Note(3));
            var ornament = new JianpuOrnament { Type = OrnamentType.Fermata, BeatPosition = 3 };

            Assert.Equal(2, OrnamentService.ResolveNoteIndex(measure, ornament));
        }

        [Fact]
        public void TryAddOrnament_ReplacesSameTypeOnSameNote()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2));
            OrnamentService.TryAddOrnament(measure, 1, OrnamentType.Trill);
            OrnamentService.TryAddOrnament(measure, 1, OrnamentType.Trill);

            Assert.Single(measure.Ornaments);
            Assert.Equal(OrnamentType.Trill, measure.Ornaments[0].Type);
            Assert.Equal(1, measure.Ornaments[0].NoteIndex);
        }

        [Fact]
        public void TryRemoveForNote_RemovesAllOrnamentsOnNote()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2));
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.Trill);
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.Turn);

            Assert.True(OrnamentService.TryRemoveForNote(measure, 0));
            Assert.Empty(measure.Ornaments);
        }

        [Fact]
        public void OnNoteRemoved_ShiftsLaterNoteIndices()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3));
            OrnamentService.TryAddOrnament(measure, 2, OrnamentType.Fermata);

            OrnamentService.OnNoteRemoved(measure, 0);

            Assert.Single(measure.Ornaments);
            Assert.Equal(1, measure.Ornaments[0].NoteIndex);
        }

        [Fact]
        public void GetPlaceholderGlyph_ReturnsToolbarLabel()
        {
            Assert.Equal("倚", OrnamentService.GetPlaceholderGlyph(OrnamentType.GraceNote));
            Assert.Equal("tr", OrnamentService.GetPlaceholderGlyph(OrnamentType.Trill));
            Assert.Equal("回", OrnamentService.GetPlaceholderGlyph(OrnamentType.Turn));
            Assert.Equal("延", OrnamentService.GetPlaceholderGlyph(OrnamentType.Fermata));
        }

        [Fact]
        public void GetParameter_ReturnsStoredValue()
        {
            var ornament = new JianpuOrnament
            {
                Type = OrnamentType.GraceNote,
                Parameters = new Dictionary<string, string>
                {
                    [OrnamentService.ParamPitch] = "3",
                    [OrnamentService.ParamDirection] = "up"
                }
            };

            Assert.Equal("3", OrnamentService.GetParameter(ornament, OrnamentService.ParamPitch));
            Assert.Equal("up", OrnamentService.GetParameter(ornament, OrnamentService.ParamDirection));
            Assert.Equal(string.Empty, OrnamentService.GetParameter(ornament, "missing"));
        }
    }
}
