// Services/ChordProgressionService.cs
using ChordProgressionQuiz.Models; // Ensure this is the correct namespace
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System; // Required for Random and Console.WriteLine
using System.Text.RegularExpressions; // For parsing Roman numerals

namespace ChordProgressionQuiz.Services
{
    public class ChordProgressionService
    {
        private List<ChordProgression> _chordProgressions;
        private readonly IWebHostEnvironment _env;
        private readonly Random _random;

        // MIDI note values for C (0) to B (11)
        private readonly Dictionary<string, int> _noteNameToSemitonesFromC = new Dictionary<string, int>
        {
            {"C", 0}, {"C#", 1}, {"Db", 1}, {"D", 2}, {"D#", 3}, {"Eb", 3}, {"E", 4}, {"F", 5}, {"F#", 6}, {"Gb", 6},
            {"G", 7}, {"G#", 8}, {"Ab", 8}, {"A", 9}, {"A#", 10}, {"Bb", 10}, {"B", 11}
        };

        // Diatonic intervals for various scales relative to their *own root* (in semitones)
        private readonly Dictionary<string, int[]> _scaleIntervals = new Dictionary<string, int[]>
        {
            {"Major", new[] {0, 2, 4, 5, 7, 9, 11}},
            {"Ionian", new[] {0, 2, 4, 5, 7, 9, 11}},
            {"Minor", new[] {0, 2, 3, 5, 7, 8, 10}},
            {"Aeolian", new[] {0, 2, 3, 5, 7, 8, 10}},
            {"Dorian", new[] {0, 2, 3, 5, 7, 9, 10}},
            {"Phrygian", new[] {0, 1, 3, 5, 7, 8, 10}},
            {"Mixolydian", new[] {0, 2, 4, 5, 7, 9, 10}},
            {"Harmonic Minor", new[] {0, 2, 3, 5, 7, 8, 11}},
        };

        // Map Roman numeral degree to its 0-indexed position in the scale intervals array.
        private readonly Dictionary<string, int> _romanDegreeToIndex = new Dictionary<string, int>
        {
            {"I", 0}, {"II", 1}, {"III", 2}, {"IV", 3}, {"V", 4}, {"VI", 5}, {"VII", 6},
            {"i", 0}, {"ii", 1}, {"iii", 2}, {"iv", 3}, {"v", 4}, {"vi", 5}, {"vii", 6}
        };

        // Chord qualities and their intervals *relative to the chord's root*
        // All keys are now LOWERCASE for consistent lookup
        private readonly Dictionary<string, int[]> _chordQualityIntervals = new Dictionary<string, int[]>
        {
            {"", new[] {0, 4, 7}},          // Major Triad (default if no suffix)
            {"maj", new[] {0, 4, 7}},       // Major Triad (explicit)
            {"m", new[] {0, 3, 7}},         // Minor Triad
            {"dim", new[] {0, 3, 6}},       // Diminished Triad
            {"aug", new[] {0, 4, 8}},       // Augmented Triad

            {"7", new[] {0, 4, 7, 10}},     // Dominant 7th
            {"m7", new[] {0, 3, 7, 10}},    // Minor 7th
            {"maj7", new[] {0, 4, 7, 11}},  // <--- CHANGED: Key is now lowercase "maj7"
            {"dim7", new[] {0, 3, 6, 9}},   // Diminished 7th
            {"m7b5", new[] {0, 3, 6, 10}},  // Half-diminished 7th

            {"sus2", new[] {0, 2, 7}},      // Suspend 2nd
            {"sus4", new[] {0, 5, 7}},      // Suspend 4th

            {"add9", new[] {0, 4, 7, 14}},
            {"add#9", new[] {0, 4, 7, 15}},
            {"maj7#11", new[] {0, 4, 7, 11, 18}}, // <--- CHANGED: Key is now lowercase "maj7#11"
        };

        // Regex to parse Roman numerals: (remains unchanged from previous fix, which used explicit list)
        private static readonly Regex _romanNumeralRegex = new Regex(
            @"^(b|#)?(I|II|III|IV|V|VI|VII|i|ii|iii|iv|v|vi|vii)(b|#)?(m7b5|m7|Maj7|dim7|dim|aug|sus2|sus4|add9|add#9|maj7#11|7|maj|m)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );


        public ChordProgressionService(IWebHostEnvironment env)
        {
            _env = env;
            _random = new Random();
            LoadChordProgressions();
        }

        private void LoadChordProgressions()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "chordProgressions.json");

            if (File.Exists(filePath))
            {
                var jsonString = File.ReadAllText(filePath);
                _chordProgressions = JsonSerializer.Deserialize<List<ChordProgression>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                _chordProgressions = new List<ChordProgression>();
                Console.WriteLine($"Warning: chordProgressions.json not found at {filePath}");
            }
        }

        public ChordProgression GetRandomChordProgression()
        {
            if (_chordProgressions == null || !_chordProgressions.Any())
            {
                return null;
            }

            var index = _random.Next(_chordProgressions.Count);
            return _chordProgressions[index];
        }

        public ChordProgression GetChordProgressionByIndex(int index)
        {
            if (_chordProgressions == null || index < 0 || index >= _chordProgressions.Count)
            {
                return null;
            }
            return _chordProgressions[index];
        }

        public int GetProgressionCount()
        {
            return _chordProgressions?.Count ?? 0;
        }

        public AbsoluteChordProgression ConvertToAbsoluteMidiProgression(ChordProgression progression, int defaultOctave = 4)
        {
            var absoluteChords = new List<MidiChord>();
            string romanNumeralsString = null;
            string relativeTo = null;

            if (progression.Tonal != null && !string.IsNullOrEmpty(progression.Tonal.RomanNumerals))
            {
                romanNumeralsString = progression.Tonal.RomanNumerals;
                relativeTo = progression.Tonal.RelativeTo;
            }
            else if (progression.Modal != null && progression.Modal.Any())
            {
                romanNumeralsString = progression.Modal.First().RomanNumerals;
                relativeTo = progression.Modal.First().RelativeTo;
            }

            if (string.IsNullOrEmpty(romanNumeralsString) || string.IsNullOrEmpty(relativeTo))
            {
                Console.WriteLine($"Warning: Could not convert progression '{progression.Song}' due to missing Roman numerals or relative_to information.");
                return new AbsoluteChordProgression(new List<MidiChord>(), "N/A");
            }

            var relativeToParts = relativeTo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string keyRootNoteName = relativeToParts[0].Trim();
            string keyScaleType = relativeToParts.Length > 1 ? string.Join(" ", relativeToParts.Skip(1)).Trim() : "Major";

            if (keyScaleType.Contains(","))
            {
                keyScaleType = keyScaleType.Split(',')[0].Trim();
            }
            if (keyScaleType.Contains(" with "))
            {
                keyScaleType = keyScaleType.Split(new[] { " with " }, StringSplitOptions.None)[0].Trim();
            }

            if (!_noteNameToSemitonesFromC.TryGetValue(keyRootNoteName, out int keyRootMidiOffset))
            {
                Console.WriteLine($"Warning: Unknown key root note '{keyRootNoteName}' for progression '{progression.Song}'. Defaulting to C.");
                keyRootMidiOffset = _noteNameToSemitonesFromC["C"];
            }

            int tonicMidiNote = (defaultOctave * 12) + keyRootMidiOffset;

            var romanChordStrings = romanNumeralsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var romanChord in romanChordStrings)
            {
                Console.WriteLine($"DEBUG: Processing Roman chord: '{romanChord}' for song '{progression.Song}'");
                var match = _romanNumeralRegex.Match(romanChord);
                if (!match.Success)
                {
                    Console.WriteLine($"DEBUG:   Regex failed for '{romanChord}'. Adding empty chord.");
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                string leadingAccidental = match.Groups[1].Value;
                string baseRoman = match.Groups[2].Value;
                string trailingAccidental = match.Groups[3].Value;
                string qualitySuffix = match.Groups[4].Value;

                Console.WriteLine($"DEBUG:   Parsed: Leading='{leadingAccidental}', Base='{baseRoman}', Trailing='{trailingAccidental}', Quality='{qualitySuffix}'");

                bool isMinorDegreeDefault = baseRoman.All(char.IsLower);

                int[] currentScaleIntervals;
                if (!_scaleIntervals.TryGetValue(keyScaleType, out currentScaleIntervals))
                {
                    Console.WriteLine($"Warning: Unknown scale type '{keyScaleType}' for progression '{progression.Song}'. Defaulting to Major scale intervals.");
                    currentScaleIntervals = _scaleIntervals["Major"];
                }

                if (!_romanDegreeToIndex.TryGetValue(baseRoman, out int degreeIndex))
                {
                    Console.WriteLine($"Warning: Unknown base Roman degree '{baseRoman}' encountered for song '{progression.Song}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                if (degreeIndex >= currentScaleIntervals.Length)
                {
                    Console.WriteLine($"Warning: Roman degree index {degreeIndex} out of bounds for scale type '{keyScaleType}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                int diatonicRootSemitoneOffset = currentScaleIntervals[degreeIndex];

                int chordRootSemitoneOffset = diatonicRootSemitoneOffset;
                if (leadingAccidental == "b" || trailingAccidental == "b")
                {
                    chordRootSemitoneOffset--;
                }
                else if (leadingAccidental == "#" || trailingAccidental == "#")
                {
                    chordRootSemitoneOffset++;
                }

                int chordRootMidiNote = tonicMidiNote + chordRootSemitoneOffset;

                while (chordRootMidiNote < (defaultOctave * 12)) chordRootMidiNote += 12;
                while (chordRootMidiNote > ((defaultOctave + 1) * 12 + 11)) chordRootMidiNote -= 12;


                int[] intervals;
                // Convert qualitySuffix to lowercase for consistent dictionary lookup
                string normalizedQualitySuffix = qualitySuffix.ToLowerInvariant(); // <--- NEW LINE

                if (!string.IsNullOrEmpty(normalizedQualitySuffix) && _chordQualityIntervals.TryGetValue(normalizedQualitySuffix, out intervals)) // <--- Use normalized suffix
                {
                    // Use the explicitly specified quality
                }
                else if (isMinorDegreeDefault && _chordQualityIntervals.TryGetValue("m", out intervals))
                {
                    // If it's a lowercase Roman numeral (e.g., 'ii', 'iii', 'vi', 'i', 'iv', 'v'), default to minor triad
                }
                else if (_chordQualityIntervals.TryGetValue("", out intervals))
                {
                    // Default to major triad if no specific quality or lowercase indicates minor
                }
                else
                {
                    Console.WriteLine($"Warning: Unknown chord quality '{qualitySuffix}' (normalized: '{normalizedQualitySuffix}') for Roman numeral '{romanChord}' in song '{progression.Song}'. Using major triad as fallback.");
                    intervals = _chordQualityIntervals[""];
                }

                var midiPitches = new List<int>();
                foreach (var interval in intervals)
                {
                    int pitch = chordRootMidiNote + interval;
                    while (pitch < chordRootMidiNote) pitch += 12;
                    while (pitch > chordRootMidiNote + 12) pitch -= 12;
                    midiPitches.Add(pitch);
                }
                absoluteChords.Add(new MidiChord(midiPitches));
            }

            return new AbsoluteChordProgression(absoluteChords, $"{progression.Song} ({relativeTo})");
        }
    }
}
