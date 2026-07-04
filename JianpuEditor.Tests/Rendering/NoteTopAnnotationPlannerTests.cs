using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Rendering
{
    public sealed class NoteTopAnnotationPlannerTests
    {
        [Fact]
        public void Plan_GraceAndSharp_PlacesAccidentalOnRightAndGraceOnLeft()
        {
            var note = new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 1.5,
                Accidental = AccidentalKind.Sharp,
                Octave = 1
            };
            var ornaments = new List<JianpuOrnament>
            {
                new JianpuOrnament { Type = OrnamentType.GraceNote, NoteIndex = 0 }
            };

            var layout = NoteTopAnnotationPlanner.Plan(note, 100, 28, ornaments, compactAccidentals: true);

            Assert.True(layout.HasAccidental);
            Assert.True(layout.AccidentalX > 110);
            Assert.True(layout.HasGraceOrnament);
            Assert.True(layout.GetOrnamentAnchorX(OrnamentType.GraceNote, 100, 28) < layout.HeadCenterX);
        }

        [Fact]
        public void Plan_TrillHighOctaveAndFlat_SeparatesMarkersHorizontally()
        {
            var note = new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 2.5,
                Accidental = AccidentalKind.Flat,
                Octave = 1
            };
            var ornaments = new List<JianpuOrnament>
            {
                new JianpuOrnament { Type = OrnamentType.Trill, NoteIndex = 0 }
            };

            var layout = NoteTopAnnotationPlanner.Plan(note, 100, 28, ornaments, compactAccidentals: true);

            Assert.True(layout.HasCenterOrnament);
            Assert.True(layout.HasAccidental);
            Assert.True(layout.OctaveDotCenterX < layout.HeadCenterX);
            Assert.True(layout.AccidentalX > layout.HeadCenterX);
        }

        [Fact]
        public void Plan_FermataAndSharp_KeepsFermataCenteredAndAccidentalOnSide()
        {
            var note = new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 1.5,
                Accidental = AccidentalKind.Sharp,
                Octave = 1
            };
            var ornaments = new List<JianpuOrnament>
            {
                new JianpuOrnament { Type = OrnamentType.Fermata, NoteIndex = 0 }
            };

            var layout = NoteTopAnnotationPlanner.Plan(note, 100, 28, ornaments, compactAccidentals: true);

            Assert.Equal(layout.HeadCenterX, layout.GetOrnamentAnchorX(OrnamentType.Fermata, 100, 28), 1);
            Assert.NotEqual(layout.HeadCenterX, layout.OctaveDotCenterX, 1);
        }

        [Fact]
        public void GetOrnamentsForNote_ReturnsOnlyMatchingOrnaments()
        {
            var measure = ScoreTestHelper.Measure(ScoreTestHelper.Note(1), ScoreTestHelper.Note(2));
            OrnamentService.TryAddOrnament(measure, 0, OrnamentType.GraceNote);
            OrnamentService.TryAddOrnament(measure, 1, OrnamentType.Trill);

            var ornaments = NoteTopAnnotationPlanner.GetOrnamentsForNote(measure, 0);

            Assert.Single(ornaments);
            Assert.Equal(OrnamentType.GraceNote, ornaments[0].Type);
        }
    }
}
