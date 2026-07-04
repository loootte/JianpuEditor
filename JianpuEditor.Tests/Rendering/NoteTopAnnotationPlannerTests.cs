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
        public void Plan_GraceAndSharp_PlacesAccidentalClosestToNoteAndGraceAbove()
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
            Assert.Equal(NoteTopAnnotationLayout.AccidentalBandY, layout.AccidentalY);
            Assert.True(layout.AccidentalX < layout.HeadCenterX);
            Assert.True(layout.HasGraceOrnament);
            Assert.Equal(layout.HeadCenterX, layout.GetOrnamentAnchorX(OrnamentType.GraceNote, 100, 28), 1);
            Assert.True(layout.OctaveDotBaseY < layout.AccidentalY);
            Assert.True(layout.GetOrnamentY(OrnamentType.GraceNote) < layout.OctaveDotBaseY);
        }

        [Fact]
        public void Plan_TrillHighOctaveAndFlat_StacksMarkersFromNoteUpward()
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
            Assert.Equal(NoteTopAnnotationLayout.AccidentalBandY, layout.AccidentalY);
            Assert.True(layout.AccidentalX < layout.HeadCenterX);
            Assert.True(layout.OctaveDotCenterX > layout.AccidentalX);
            Assert.Equal(NoteTopAnnotationLayout.OctaveDotBandY, layout.OctaveDotBaseY);
            Assert.Equal(NoteTopAnnotationLayout.DefaultOrnamentBandY, layout.GetOrnamentY(OrnamentType.Trill));
            Assert.True(layout.AccidentalY > layout.OctaveDotBaseY);
            Assert.True(layout.OctaveDotBaseY > layout.GetOrnamentY(OrnamentType.Trill));
        }

        [Fact]
        public void Plan_FermataAndSharp_PlacesFermataTopmostAndAccidentalClosestToNote()
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
            Assert.Equal(NoteTopAnnotationLayout.FermataBandY, layout.GetOrnamentY(OrnamentType.Fermata));
            Assert.Equal(NoteTopAnnotationLayout.AccidentalBandY, layout.AccidentalY);
            Assert.True(layout.AccidentalX < layout.OctaveDotCenterX);
            Assert.True(layout.GetOrnamentY(OrnamentType.Fermata) < layout.OctaveDotBaseY);
        }

        [Fact]
        public void Plan_OctaveOnly_PlacesDotsClosestToNote()
        {
            var note = new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 1,
                Octave = 1
            };

            var layout = NoteTopAnnotationPlanner.Plan(note, 100, 28, new List<JianpuOrnament>(), compactAccidentals: true);

            Assert.Equal(NoteTopAnnotationLayout.OctaveDotBandYWithoutAccidental, layout.OctaveDotBaseY);
            Assert.Equal(layout.HeadCenterX - 3f, layout.OctaveDotCenterX, 1);
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
