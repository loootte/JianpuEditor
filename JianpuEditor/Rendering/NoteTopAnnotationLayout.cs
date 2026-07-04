using System;
using System.Collections.Generic;
using JianpuEditor.Models;

namespace JianpuEditor.Rendering
{
    public sealed class NoteTopAnnotationLayout
    {
        public const float OrnamentFermataY = 0f;

        public const float OrnamentDefaultY = 2f;

        public const float OrnamentGraceY = 0f;

        public const float AccidentalY = 5f;

        public const float OctaveDotBaseY = 4f;

        public const float OctaveDotDiameter = 6f;

        public float HeadCenterX { get; set; }

        public float OctaveDotCenterX { get; set; }

        public bool HasAccidental { get; set; }

        public float AccidentalX { get; set; }

        public AccidentalKind AccidentalKind { get; set; }

        public bool HasGraceOrnament { get; set; }

        public bool HasCenterOrnament { get; set; }

        public bool HasFermata { get; set; }

        public float GetOrnamentAnchorX(OrnamentType type, int noteX, int headWidth)
        {
            var headCenterX = noteX + headWidth / 2f;
            if (type == OrnamentType.GraceNote)
            {
                return noteX + Math.Min(14f, headWidth * 0.25f);
            }

            if (type == OrnamentType.Fermata)
            {
                return headCenterX;
            }

            if (HasAccidental && AccidentalKind == AccidentalKind.Sharp && AccidentalX <= noteX + 4f)
            {
                return headCenterX + Math.Min(8f, headWidth * 0.15f);
            }

            return headCenterX;
        }

        public float GetOrnamentY(OrnamentType type)
        {
            if (type == OrnamentType.Fermata)
            {
                return OrnamentFermataY;
            }

            if (type == OrnamentType.GraceNote)
            {
                return HasFermata || HasCenterOrnament ? OrnamentGraceY : OrnamentDefaultY;
            }

            return OrnamentDefaultY;
        }
    }
}
