using JianpuEditor.Controls;
using JianpuEditor.Glue;
using JianpuEditor.Models;
using Xunit;

namespace JianpuEditor.Tests.Glue
{
    public class ScoreSelectionMapperTests
    {
        [Fact]
        public void FromCanvas_MapsAllFields()
        {
            var args = new ScoreSelectionChangedEventArgs
            {
                MeasureIndex = 2,
                NoteIndex = 3,
                InsertIndex = 4,
                TieIndex = 1,
                ChordMeasureIndex = 2,
                ChordMarkerIndex = 0,
                SelectedMeasureIndices = new[] { 1, 2, 3 }
            };

            var info = ScoreSelectionMapper.FromCanvas(args);

            Assert.Equal(2, info.MeasureIndex);
            Assert.Equal(3, info.NoteIndex);
            Assert.Equal(4, info.InsertIndex);
            Assert.Equal(1, info.TieIndex);
            Assert.Equal(2, info.ChordMeasureIndex);
            Assert.Equal(0, info.ChordMarkerIndex);
            Assert.Equal(new[] { 1, 2, 3 }, info.SelectedMeasureIndices);
        }

        [Fact]
        public void FromCanvas_ReturnsEmptyInfoWhenArgsNull()
        {
            var info = ScoreSelectionMapper.FromCanvas(null);

            Assert.NotNull(info);
            Assert.Equal(-1, info.MeasureIndex);
        }
    }
}
