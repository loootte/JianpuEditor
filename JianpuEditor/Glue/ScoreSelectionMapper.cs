using JianpuEditor.Controls;
using JianpuEditor.Models;

namespace JianpuEditor.Glue
{
    internal static class ScoreSelectionMapper
    {
        public static ScoreSelectionInfo FromCanvas(ScoreSelectionChangedEventArgs args)
        {
            if (args == null)
            {
                return new ScoreSelectionInfo();
            }

            return new ScoreSelectionInfo
            {
                MeasureIndex = args.MeasureIndex,
                NoteIndex = args.NoteIndex,
                InsertIndex = args.InsertIndex,
                TieIndex = args.TieIndex,
                ChordMeasureIndex = args.ChordMeasureIndex,
                ChordMarkerIndex = args.ChordMarkerIndex,
                SelectedMeasureIndices = args.SelectedMeasureIndices
            };
        }
    }
}
