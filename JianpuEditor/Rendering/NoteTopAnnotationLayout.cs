using JianpuEditor.Models;

namespace JianpuEditor.Rendering
{
    public sealed class NoteTopAnnotationLayout
    {
        public const float DigitTextY = 18f;

        public const float AccidentalBandY = 12f;

        public const float OctaveDotBandY = 7f;

        public const float OctaveDotBandYWithoutAccidental = 12f;

        public const float DefaultOrnamentBandY = 2f;

        public const float OrnamentBandYWithoutLowerLayers = 12f;

        public const float FermataBandY = 0f;

        public const float OctaveDotDiameter = 6f;

        public const float OctaveDotStackSpacing = 6f;

        public const float AccidentalMarkWidth = 8f;

        public float HeadCenterX { get; set; }

        public float OctaveDotCenterX { get; set; }

        public float OctaveDotBaseY { get; set; }

        public bool HasAccidental { get; set; }

        public bool HasHighOctaveDots { get; set; }

        public float AccidentalX { get; set; }

        public float AccidentalY { get; set; }

        public AccidentalKind AccidentalKind { get; set; }

        public bool HasGraceOrnament { get; set; }

        public bool HasCenterOrnament { get; set; }

        public bool HasFermata { get; set; }

        public float OrnamentY { get; set; }

        public float FermataY { get; set; }

        public float GetOrnamentAnchorX(OrnamentType type, int noteX, int headWidth)
        {
            _ = type;
            _ = noteX;
            _ = headWidth;
            return HeadCenterX;
        }

        public float GetOrnamentY(OrnamentType type)
        {
            return type == OrnamentType.Fermata ? FermataY : OrnamentY;
        }
    }
}
