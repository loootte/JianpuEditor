using JianpuEditor.Models;
using JianpuEditor.Services;
using Xunit;

namespace JianpuEditor.Tests.Services
{
    public sealed class JianpuPitchCodecTests
    {
        [Theory]
        [InlineData(61, 1.5, AccidentalKind.Sharp, "#1", 61)]
        [InlineData(63, 2.5, AccidentalKind.Flat, "b3", 63)]
        [InlineData(60, 1.0, AccidentalKind.None, "1", 60)]
        public void TryMidiToJianpu_MapsAccidentals(
            int midiNote,
            double expectedPitch,
            AccidentalKind expectedAccidental,
            string expectedDisplay,
            int expectedMidi)
        {
            Assert.True(MidiImportService.TryMidiToJianpu(
                midiNote,
                60,
                out var pitch,
                out _,
                out var accidental,
                out _));

            Assert.Equal(expectedPitch, pitch, 3);
            Assert.Equal(expectedAccidental, accidental);

            var note = new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = pitch,
                Accidental = accidental
            };
            Assert.Equal(expectedDisplay, JianpuPitchCodec.GetPitchDisplayText(note));
            Assert.Equal(expectedMidi, JianpuPitchCodec.ToMelodyMidiNote(note, 60));
        }

        [Fact]
        public void ToMelodyMidiNote_NaturalPitch_UsesScaleDegree()
        {
            var note = new JianpuNote { Type = NoteType.Note, Pitch = 5 };
            Assert.Equal(67, JianpuPitchCodec.ToMelodyMidiNote(note, 60));
        }
    }
}
