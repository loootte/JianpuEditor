using System;
using System.Text.RegularExpressions;

namespace JianpuEditor.Services
{
    public static class KeySignatureService
    {
        private static readonly Regex KeyNamePattern = new Regex(
            @"^([A-Ga-g])([#b♭♯]?)$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryParseTonicPitchClass(string keySignature, out int pitchClass)
        {
            pitchClass = 0;
            var text = NormalizeKeyText(keySignature);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var match = KeyNamePattern.Match(text);
            if (!match.Success)
            {
                return false;
            }

            pitchClass = ParseLetterPitchClass(match.Groups[1].Value);
            if (pitchClass < 0)
            {
                return false;
            }

            var accidentalText = match.Groups[2].Value;
            if (accidentalText == "#" || accidentalText == "＃" || accidentalText == "♯")
            {
                pitchClass = Mod12(pitchClass + 1);
            }
            else if (accidentalText == "b" || accidentalText == "♭")
            {
                pitchClass = Mod12(pitchClass - 1);
            }

            return true;
        }

        public static int GetTransposeSemitones(int sourcePitchClass, int targetPitchClass)
        {
            return Mod12(targetPitchClass - sourcePitchClass);
        }

        public static string FormatKeySignature(int pitchClass)
        {
            return "1=" + PitchClassToNoteName(Mod12(pitchClass));
        }

        public static string PitchClassToNoteName(int pitchClass)
        {
            switch (Mod12(pitchClass))
            {
                case 0: return "C";
                case 1: return "C#";
                case 2: return "D";
                case 3: return "D#";
                case 4: return "E";
                case 5: return "F";
                case 6: return "F#";
                case 7: return "G";
                case 8: return "G#";
                case 9: return "A";
                case 10: return "Bb";
                case 11: return "B";
                default: return "C";
            }
        }

        private static string NormalizeKeyText(string keySignature)
        {
            if (string.IsNullOrWhiteSpace(keySignature))
            {
                return string.Empty;
            }

            var text = keySignature.Trim();
            var equalIndex = text.IndexOf('=');
            if (equalIndex >= 0)
            {
                text = text.Substring(equalIndex + 1).Trim();
            }

            text = text.Replace("大调", string.Empty)
                .Replace("小调", string.Empty)
                .Replace("major", string.Empty)
                .Replace("Major", string.Empty)
                .Replace("minor", string.Empty)
                .Replace("Minor", string.Empty)
                .Replace(" ", string.Empty)
                .Trim();

            return text;
        }

        private static int ParseLetterPitchClass(string letter)
        {
            if (string.IsNullOrEmpty(letter))
            {
                return -1;
            }

            switch (char.ToUpperInvariant(letter[0]))
            {
                case 'C': return 0;
                case 'D': return 2;
                case 'E': return 4;
                case 'F': return 5;
                case 'G': return 7;
                case 'A': return 9;
                case 'B': return 11;
                default: return -1;
            }
        }

        private static int Mod12(int value)
        {
            var result = value % 12;
            return result < 0 ? result + 12 : result;
        }
    }
}