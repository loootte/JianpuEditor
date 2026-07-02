using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JianpuEditor.Models;

namespace JianpuEditor.Services
{
    public sealed class ScheduledChordSymbol
    {
        public string Symbol { get; set; }

        public double BeatPosition { get; set; }
    }
    internal static class ChordParser
    {
        private static readonly int[] MajorTriad = { 0, 4, 7 };
        private static readonly int[] MinorTriad = { 0, 3, 7 };
        private static readonly int[] DominantSeventh = { 0, 4, 7, 10 };
        private static readonly int[] MajorSeventh = { 0, 4, 7, 11 };
        private static readonly int[] MinorSeventh = { 0, 3, 7, 10 };

        private static readonly Regex ChordBodyPattern = new Regex(
            @"^([A-Ga-g])([#b♭♯]?)(maj7|min7|m7|maj|min|dim7|dim|aug|sus4|sus2|m|7|\+)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex ChordTokenPattern = new Regex(
            @"^[A-Ga-g](?:#|b|♭|♯)?(?:maj7|min7|m7|maj|min|dim7|dim|aug|sus4|sus2|m|7|\+)?(?:/[A-Ga-g](?:#|b|♭|♯)?)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool IsChordSymbol(string token)
        {
            return !string.IsNullOrWhiteSpace(token) && ChordTokenPattern.IsMatch(token.Trim());
        }

        public static List<ScheduledChordSymbol> ExtractScheduledChords(JianpuMeasure measure)
        {
            ChordMarkerService.NormalizeMeasure(measure);
            var scheduled = new List<ScheduledChordSymbol>();
            if (measure?.ChordMarkers == null)
            {
                return scheduled;
            }

            foreach (var marker in measure.ChordMarkers.OrderBy(item => item.BeatPosition))
            {
                var text = marker.Text?.Trim();
                if (string.IsNullOrWhiteSpace(text) || !IsChordSymbol(text))
                {
                    continue;
                }

                scheduled.Add(new ScheduledChordSymbol
                {
                    Symbol = text,
                    BeatPosition = marker.BeatPosition
                });
            }

            return scheduled;
        }

        public static string TransposeSymbol(string chordSymbol, int semitones)
        {
            if (string.IsNullOrWhiteSpace(chordSymbol) || semitones == 0)
            {
                return chordSymbol?.Trim() ?? string.Empty;
            }

            var text = chordSymbol.Trim();
            string bassPart = null;
            var slashIndex = text.IndexOf('/');
            if (slashIndex >= 0)
            {
                bassPart = text.Substring(slashIndex + 1).Trim();
                text = text.Substring(0, slashIndex).Trim();
            }

            if (!TryParseChordBody(text, out var rootPitchClass, out var quality))
            {
                return chordSymbol;
            }

            var result = KeySignatureService.PitchClassToNoteName(Mod12(rootPitchClass + semitones)) + (quality ?? string.Empty);
            if (!string.IsNullOrEmpty(bassPart))
            {
                if (TryParseChordBody(bassPart, out var bassPitchClass, out _))
                {
                    result += "/" + KeySignatureService.PitchClassToNoteName(Mod12(bassPitchClass + semitones));
                }
                else
                {
                    result += "/" + bassPart;
                }
            }

            return result;
        }

        public static List<string> ExtractChordSymbols(string secondaryText)
        {
            var chords = new List<string>();
            if (string.IsNullOrWhiteSpace(secondaryText))
            {
                return chords;
            }

            var tokens = Regex.Split(secondaryText.Trim(), @"\s+");
            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (!IsChordSymbol(token))
                {
                    return new List<string>();
                }

                chords.Add(token.Trim());
                if (chords.Count >= JianpuMeasure.MaxChordMarkers)
                {
                    break;
                }
            }

            return chords;
        }

        public static List<int> ToBlockChordMidiNotes(string chordSymbol, int rootOctave = 4, int bassOctave = 3)
        {
            if (string.IsNullOrWhiteSpace(chordSymbol))
            {
                return new List<int>();
            }

            var text = chordSymbol.Trim();
            string bassPart = null;
            var slashIndex = text.IndexOf('/');
            if (slashIndex >= 0)
            {
                bassPart = text.Substring(slashIndex + 1).Trim();
                text = text.Substring(0, slashIndex).Trim();
            }

            if (!TryParseChordBody(text, out var rootPitchClass, out var quality))
            {
                return new List<int>();
            }

            var intervals = GetIntervals(quality);
            var notes = new List<int>();
            var rootMidi = PitchClassToMidi(rootPitchClass, rootOctave);
            foreach (var interval in intervals)
            {
                notes.Add(ClampMidi(rootMidi + interval));
            }

            if (!string.IsNullOrWhiteSpace(bassPart))
            {
                var bassMidi = ParseNamedNoteMidi(bassPart, bassOctave);
                if (bassMidi >= 0)
                {
                    notes.Add(bassMidi);
                }
            }

            notes.Sort();
            return Deduplicate(notes);
        }

        private static bool TryParseChordBody(string text, out int rootPitchClass, out string quality)
        {
            rootPitchClass = -1;
            quality = string.Empty;

            var match = ChordBodyPattern.Match(text.Trim());
            if (!match.Success)
            {
                return false;
            }

            var accidental = 0;
            var accidentalText = match.Groups[2].Value;
            if (accidentalText == "#" || accidentalText == "＃" || accidentalText == "♯")
            {
                accidental = 1;
            }
            else if (accidentalText == "b" || accidentalText == "♭")
            {
                accidental = -1;
            }

            rootPitchClass = Mod12(ParseLetterPitchClass(match.Groups[1].Value) + accidental);
            quality = match.Groups[3].Value ?? string.Empty;
            return rootPitchClass >= 0;
        }

        private static int[] GetIntervals(string quality)
        {
            switch ((quality ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "m":
                case "min":
                case "-":
                    return MinorTriad;
                case "7":
                    return DominantSeventh;
                case "maj7":
                    return MajorSeventh;
                case "m7":
                case "min7":
                    return MinorSeventh;
                case "dim":
                case "dim7":
                    return new[] { 0, 3, 6 };
                case "aug":
                case "+":
                    return new[] { 0, 4, 8 };
                case "sus4":
                    return new[] { 0, 5, 7 };
                case "sus2":
                    return new[] { 0, 2, 7 };
                case "maj":
                    return MajorTriad;
                default:
                    return MajorTriad;
            }
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

        private static int ParseNamedNoteMidi(string noteName, int octave)
        {
            if (string.IsNullOrWhiteSpace(noteName))
            {
                return -1;
            }

            if (!TryParseChordBody(noteName.Trim(), out var pitchClass, out _))
            {
                return -1;
            }

            return PitchClassToMidi(pitchClass, octave);
        }

        private static int PitchClassToMidi(int pitchClass, int octave)
        {
            return ClampMidi((octave + 1) * 12 + Mod12(pitchClass));
        }

        private static int Mod12(int value)
        {
            var result = value % 12;
            return result < 0 ? result + 12 : result;
        }

        private static int ClampMidi(int midi)
        {
            return Math.Max(0, Math.Min(127, midi));
        }

        private static List<int> Deduplicate(List<int> notes)
        {
            var unique = new List<int>();
            var seen = new HashSet<int>();
            foreach (var note in notes)
            {
                if (seen.Add(note))
                {
                    unique.Add(note);
                }
            }

            return unique;
        }
    }
}