using JianpuEditor.Models;
using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class MeasurePlaceholderServiceTests
    {
        [Fact]
        public void CreateMeasureWithPlaceholders_AddsFourQuarterNotes()
        {
            var measure = MeasurePlaceholderService.CreateMeasureWithPlaceholders();

            Assert.Equal(4, measure.MelodyNotes.Count);
            foreach (var note in measure.MelodyNotes)
            {
                Assert.Equal(NoteType.Note, note.Type);
                Assert.Equal(1, note.Pitch);
                Assert.Equal(0, note.Octave);
                Assert.Equal(0, note.Underlines);
                Assert.Equal(0, note.Dashes);
                Assert.False(note.Dotted);
            }
        }

        [Fact]
        public void CreateMeasureWithPlaceholders_RespectsCustomCount()
        {
            var measure = MeasurePlaceholderService.CreateMeasureWithPlaceholders(2);

            Assert.Equal(2, measure.MelodyNotes.Count);
        }
    }
}
