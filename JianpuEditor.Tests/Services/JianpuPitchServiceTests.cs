using JianpuEditor.Models;
using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class JianpuPitchServiceTests
    {
        [Theory]
        [InlineData(1, 0, 1, 2, 0)]
        [InlineData(7, 0, 1, 1, 1)]
        [InlineData(1, 1, 1, 2, 1)]
        [InlineData(3, 0, -1, 2, 0)]
        [InlineData(1, 0, -1, 7, -1)]
        public void TryGetTransposedPitch_MovesWithinCurrentKeyScale(
            int pitch,
            int octave,
            int delta,
            int expectedPitch,
            int expectedOctave)
        {
            var ok = JianpuPitchService.TryGetTransposedPitch(
                pitch,
                octave,
                delta,
                out var newPitch,
                out var newOctave);

            Assert.True(ok);
            Assert.Equal(expectedPitch, newPitch);
            Assert.Equal(expectedOctave, newOctave);
        }

        [Fact]
        public void TryGetTransposedPitch_ReturnsFalseAtUpperLimit()
        {
            var ok = JianpuPitchService.TryGetTransposedPitch(7, 1, 1, out _, out _);

            Assert.False(ok);
        }

        [Fact]
        public void TryGetTransposedPitch_ReturnsFalseAtLowerLimit()
        {
            var ok = JianpuPitchService.TryGetTransposedPitch(1, -1, -1, out _, out _);

            Assert.False(ok);
        }

        [Fact]
        public void TryTranspose_SkipsRestNotes()
        {
            var note = new JianpuNote { Type = NoteType.Rest, Pitch = 0 };

            Assert.False(JianpuPitchService.TryTranspose(note, 1));
            Assert.Equal(0, note.Pitch);
        }
    }
}