using System;
using System.Collections.Generic;
using JianpuEditor.Models;
using JianpuEditor.Services;

namespace JianpuEditor.Rendering
{
    public static class NoteTopAnnotationPlanner
    {
        private const float AccidentalLeftPadding = 2f;

        private const float AccidentalToOctaveGap = 4f;

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
                OctaveDotCenterX = headCenterX - 3f,
                OrnamentY = NoteTopAnnotationLayout.OrnamentBandYWithoutLowerLayers,
                FermataY = NoteTopAnnotationLayout.FermataBandY
            };

            if (note == null || note.Type == NoteType.Rest)
            {
                return layout;
            }

            AnalyzeOrnaments(ornaments, layout);
            PlaceAccidental(note, noteX, compactAccidentals, layout);
            PlaceOctaveDots(note, layout);
            PlaceOrnamentBands(layout);
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
            bool compactAccidentals,
            NoteTopAnnotationLayout layout)
        {
            if (!compactAccidentals || note.Accidental == AccidentalKind.None)
            {
                return;
            }

            layout.HasAccidental = true;
            layout.AccidentalKind = note.Accidental;
            layout.AccidentalX = noteX + AccidentalLeftPadding;
            layout.AccidentalY = NoteTopAnnotationLayout.AccidentalBandY;
        }

        private static void PlaceOctaveDots(JianpuNote note, NoteTopAnnotationLayout layout)
        {
            if (note.Octave <= 0)
            {
                return;
            }

            layout.HasHighOctaveDots = true;
            layout.OctaveDotBaseY = layout.HasAccidental
                ? NoteTopAnnotationLayout.OctaveDotBandY
                : NoteTopAnnotationLayout.OctaveDotBandYWithoutAccidental;

            if (layout.HasAccidental)
            {
                var minCenterX = layout.AccidentalX
                    + NoteTopAnnotationLayout.AccidentalMarkWidth
                    + AccidentalToOctaveGap
                    + NoteTopAnnotationLayout.OctaveDotDiameter / 2f;
                layout.OctaveDotCenterX = Math.Max(layout.HeadCenterX - 3f, minCenterX);
            }
            else
            {
                layout.OctaveDotCenterX = layout.HeadCenterX - 3f;
            }
        }

        private static void PlaceOrnamentBands(NoteTopAnnotationLayout layout)
        {
            var hasUpperOrnament = layout.HasGraceOrnament || layout.HasCenterOrnament;
            var hasLowerLayers = layout.HasAccidental || layout.HasHighOctaveDots;

            if (hasUpperOrnament)
            {
                layout.OrnamentY = hasLowerLayers || layout.HasFermata
                    ? NoteTopAnnotationLayout.DefaultOrnamentBandY
                    : NoteTopAnnotationLayout.OrnamentBandYWithoutLowerLayers;
            }

            if (layout.HasFermata)
            {
                layout.FermataY = NoteTopAnnotationLayout.FermataBandY;
            }
        }
    }
}
