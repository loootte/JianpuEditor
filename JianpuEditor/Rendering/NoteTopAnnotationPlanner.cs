using System.Collections.Generic;
using JianpuEditor.Models;
using JianpuEditor.Services;

namespace JianpuEditor.Rendering
{
    public static class NoteTopAnnotationPlanner
    {
        private const float AccidentalLeftPadding = 2f;

        private const float AccidentalRightPadding = 2f;

        private const float AccidentalMarkWidth = 8f;

        private const float OctaveLeftOffset = 12f;

        private const float OctaveRightOffset = 8f;

        public static NoteTopAnnotationLayout Plan(
            JianpuNote note,
            int noteX,
            int headWidth,
            IReadOnlyList<JianpuOrnament> ornaments,
            bool compactAccidentals)
        {
            var headCenterX = noteX + headWidth / 2f;
            var layout = new NoteTopAnnotationLayout
            {
                HeadCenterX = headCenterX,
                OctaveDotCenterX = headCenterX - 3f
            };

            if (note == null || note.Type == NoteType.Rest)
            {
                return layout;
            }

            AnalyzeOrnaments(ornaments, layout);
            PlaceAccidental(note, noteX, headWidth, compactAccidentals, layout);
            PlaceOctaveDots(note, noteX, headWidth, layout);
            return layout;
        }

        public static IReadOnlyList<JianpuOrnament> GetOrnamentsForNote(JianpuMeasure measure, int noteIndex)
        {
            if (measure?.Ornaments == null || measure.Ornaments.Count == 0)
            {
                return new List<JianpuOrnament>();
            }

            OrnamentService.NormalizeMeasure(measure);
            var ornaments = new List<JianpuOrnament>();
            foreach (var ornament in measure.Ornaments)
            {
                if (ornament == null || ornament.Type == OrnamentType.Unknown)
                {
                    continue;
                }

                if (OrnamentService.ResolveNoteIndex(measure, ornament) == noteIndex)
                {
                    ornaments.Add(ornament);
                }
            }

            return ornaments;
        }

        private static void AnalyzeOrnaments(IReadOnlyList<JianpuOrnament> ornaments, NoteTopAnnotationLayout layout)
        {
            if (ornaments == null)
            {
                return;
            }

            foreach (var ornament in ornaments)
            {
                if (ornament == null)
                {
                    continue;
                }

                switch (ornament.Type)
                {
                    case OrnamentType.GraceNote:
                        layout.HasGraceOrnament = true;
                        break;
                    case OrnamentType.Fermata:
                        layout.HasFermata = true;
                        break;
                    case OrnamentType.Trill:
                    case OrnamentType.Turn:
                    case OrnamentType.Mordent:
                        layout.HasCenterOrnament = true;
                        break;
                }
            }
        }

        private static void PlaceAccidental(
            JianpuNote note,
            int noteX,
            int headWidth,
            bool compactAccidentals,
            NoteTopAnnotationLayout layout)
        {
            if (!compactAccidentals || note.Accidental == AccidentalKind.None)
            {
                return;
            }

            layout.HasAccidental = true;
            layout.AccidentalKind = note.Accidental;
            var leftX = noteX + AccidentalLeftPadding;
            var rightX = noteX + headWidth - AccidentalMarkWidth - AccidentalRightPadding;
            var preferLeft = note.Accidental == AccidentalKind.Sharp;
            var leftBlocked = layout.HasGraceOrnament;
            var rightBlocked = false;

            if (preferLeft && !leftBlocked)
            {
                layout.AccidentalX = leftX;
                return;
            }

            if (!preferLeft && !rightBlocked)
            {
                layout.AccidentalX = rightX;
                return;
            }

            layout.AccidentalX = preferLeft ? rightX : leftX;
        }

        private static void PlaceOctaveDots(
            JianpuNote note,
            int noteX,
            int headWidth,
            NoteTopAnnotationLayout layout)
        {
            if (note.Octave <= 0)
            {
                return;
            }

            var headCenterX = noteX + headWidth / 2f;
            var centerBlocked = layout.HasCenterOrnament || layout.HasFermata;
            var leftBlocked = layout.HasGraceOrnament
                || (layout.HasAccidental && layout.AccidentalX <= noteX + 4f);
            var rightBlocked = layout.HasAccidental
                && layout.AccidentalX >= noteX + headWidth - AccidentalMarkWidth - 4f;

            if (!centerBlocked && !leftBlocked)
            {
                layout.OctaveDotCenterX = headCenterX - 3f;
                return;
            }

            if (!rightBlocked)
            {
                layout.OctaveDotCenterX = headCenterX + OctaveRightOffset;
                return;
            }

            if (!leftBlocked)
            {
                layout.OctaveDotCenterX = headCenterX - OctaveLeftOffset;
                return;
            }

            layout.OctaveDotCenterX = headCenterX + OctaveRightOffset;
        }
    }
}
