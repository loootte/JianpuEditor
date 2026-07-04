using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using Xunit;

namespace JianpuEditor.Tests.Rendering
{
    public sealed class AccidentalRenderingTests
    {
        [Fact]
        public void EditorAndPdfExportOptions_EnableStackedTopAnnotations()
        {
            Assert.True(ScoreLayoutOptions.Editor.CompactAccidentalGlyphs);
            Assert.True(ScoreLayoutOptions.PdfExport.CompactAccidentalGlyphs);
            Assert.False(ScoreLayoutOptions.Default.CompactAccidentalGlyphs);
        }

        [Fact]
        public void RenderToBitmap_EditorStackedAnnotations_DoesNotThrow()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1, underlines: 2),
                ScoreTestHelper.Note(2, underlines: 2));
            measure.MelodyNotes[0].Pitch = 1.5;
            measure.MelodyNotes[0].Accidental = AccidentalKind.Sharp;
            measure.MelodyNotes[0].Octave = 1;
            measure.MelodyNotes[1].Pitch = 2.5;
            measure.MelodyNotes[1].Accidental = AccidentalKind.Flat;
            measure.MelodyNotes[1].Octave = 1;
            measure.Ornaments = new List<JianpuOrnament>
            {
                new JianpuOrnament { Type = OrnamentType.GraceNote, NoteIndex = 0 },
                new JianpuOrnament { Type = OrnamentType.Trill, NoteIndex = 1 },
                new JianpuOrnament { Type = OrnamentType.Fermata, NoteIndex = 1 }
            };
            OrnamentService.NormalizeMeasure(measure);

            var score = ScoreTestHelper.CreateScore(measure);
            using (var renderer = new JianpuRenderer())
            {
                var bitmap = renderer.RenderToBitmap(score, 1280, ScoreLayoutOptions.Editor);
                Assert.NotNull(bitmap);
            }
        }

        [Fact]
        public void GetAccidentalMark_ReturnsSharpOrFlat()
        {
            var sharp = new JianpuNote { Type = NoteType.Note, Pitch = 1.5, Accidental = AccidentalKind.Sharp };
            var flat = new JianpuNote { Type = NoteType.Note, Pitch = 2.5, Accidental = AccidentalKind.Flat };

            Assert.Equal("#", JianpuPitchCodec.GetAccidentalMark(sharp));
            Assert.Equal("b", JianpuPitchCodec.GetAccidentalMark(flat));
        }

        [Fact]
        public void RenderPdfPageToBitmap_AccidentalSixteenthNote_DoesNotThrow()
        {
            var measure = ScoreTestHelper.Measure(
                ScoreTestHelper.Note(1, underlines: 2),
                ScoreTestHelper.Note(2, underlines: 2));
            measure.MelodyNotes[0].Pitch = 1.5;
            measure.MelodyNotes[0].Accidental = AccidentalKind.Sharp;
            measure.MelodyNotes[1].Pitch = 2.5;
            measure.MelodyNotes[1].Accidental = AccidentalKind.Flat;
            measure.Ornaments = new List<JianpuOrnament>
            {
                new JianpuOrnament { Type = OrnamentType.GraceNote, NoteIndex = 0 },
                new JianpuOrnament { Type = OrnamentType.Trill, NoteIndex = 1 }
            };
            OrnamentService.NormalizeMeasure(measure);

            var score = ScoreTestHelper.CreateScore(measure);
            using (var renderer = new JianpuRenderer())
            {
                var bitmap = renderer.RenderPdfPageToBitmap(
                    score,
                    ScoreLayoutOptions.PdfRenderWidth,
                    ScoreLayoutOptions.PdfExport,
                    new PdfPageSlice { FirstLineIndex = 0, LineCount = 1, PageNumber = 1, TotalPages = 1 });

                Assert.NotNull(bitmap);
            }
        }
    }
}
