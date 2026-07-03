using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public class KeySignatureServiceTests
    {
        [Theory]
        [InlineData("1=C", 0)]
        [InlineData("G", 7)]
        [InlineData("1=G", 7)]
        [InlineData("F#", 6)]
        [InlineData("Bb", 10)]
        [InlineData("D大调", 2)]
        public void TryParseTonicPitchClass_ParsesCommonFormats(string input, int expectedPitchClass)
        {
            var ok = KeySignatureService.TryParseTonicPitchClass(input, out var pitchClass);

            Assert.True(ok);
            Assert.Equal(expectedPitchClass, pitchClass);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        public void TryParseTonicPitchClass_RejectsInvalidInput(string input)
        {
            var ok = KeySignatureService.TryParseTonicPitchClass(input, out _);

            Assert.False(ok);
        }

        [Theory]
        [InlineData(0, 7, 7)]
        [InlineData(7, 0, 5)]
        [InlineData(0, 0, 0)]
        public void GetTransposeSemitones_ReturnsExpectedInterval(int source, int target, int expected)
        {
            Assert.Equal(expected, KeySignatureService.GetTransposeSemitones(source, target));
        }

        [Theory]
        [InlineData(0, "1=C")]
        [InlineData(10, "1=Bb")]
        [InlineData(6, "1=F#")]
        public void FormatKeySignature_UsesExpectedSpelling(int pitchClass, string expected)
        {
            Assert.Equal(expected, KeySignatureService.FormatKeySignature(pitchClass));
        }

        [Fact]
        public void PitchClassToNoteName_UsesBbInsteadOfASharp()
        {
            Assert.Equal("Bb", KeySignatureService.PitchClassToNoteName(10));
        }
    }
}
