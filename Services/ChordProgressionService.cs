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

        // Intervals for scale degrees relative to the *tonic of the key* (e.g., C for C Major)
        // This maps "I", "ii", "IV", etc., to their diatonic semitone distance from the tonic.
        private readonly Dictionary<string, int> _romanDegreeToSemitones = new Dictionary<string, int>
        {
            {"I", 0}, {"II", 2}, {"III", 4}, {"IV", 5}, {"V", 7}, {"VI", 9}, {"VII", 11},
            {"i", 0}, {"ii", 2}, {"iii", 4}, {"iv", 5}, {"v", 7}, {"vi", 9}, {"vii", 11}
            // Note: Lowercase Roman numerals here only indicate the degree, not the quality.
            // The quality (major/minor/diminished) will be determined by the suffix or implied context.
        };

        // Chord qualities and their intervals *relative to the chord's root*
        private readonly Dictionary<string, int[]> _chordQualityIntervals = new Dictionary<string, int[]>
        {
            {"", new[] {0, 4, 7}},          // Major Triad (default if no suffix)
            {"maj", new[] {0, 4, 7}},       // Major Triad (explicit)
            {"m", new[] {0, 3, 7}},         // Minor Triad
            {"dim", new[] {0, 3, 6}},       // Diminished Triad
            {"aug", new[] {0, 4, 8}},       // Augmented Triad

            {"7", new[] {0, 4, 7, 10}},     // Dominant 7th
            {"m7", new[] {0, 3, 7, 10}},    // Minor 7th
            {"Maj7", new[] {0, 4, 7, 11}},  // Major 7th
            {"dim7", new[] {0, 3, 6, 9}},   // Diminished 7th
            {"m7b5", new[] {0, 3, 6, 10}},  // Half-diminished 7th (Minor 7th flat 5)

            {"sus2", new[] {0, 2, 7}},      // Suspend 2nd
            {"sus4", new[] {0, 5, 7}},      // Suspend 4th

            {"add9", new[] {0, 4, 7, 14}},  // Major triad with added 9 (octave + 2)
            {"add#9", new[] {0, 4, 7, 15}}, // Major triad with added #9 (octave + 3)
            {"maj7#11", new[] {0, 4, 7, 11, 18}}, // Major 7th with #11 (octave + 6)
            // Add more as needed
        };

        // Regex to parse Roman numerals:
        // Group 1: Optional leading accidental (b, #)
        // Group 2: Roman numeral (I, V, ii, etc.) - using a more general pattern for Roman numerals
        // Group 3: Optional trailing accidental (b, #)
        // Group 4: Optional quality/extension (m, Maj7, dim, etc.)
        private static readonly Regex _romanNumeralRegex = new Regex(
            @"^(b|#)?([IVXLCDMivxlcdm]+)(b|#)?(m7b5|m7|Maj7|dim7|dim|aug|sus2|sus4|add9|add#9|maj7#11|7|maj|m)?$",
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

            var relativeToParts = relativeTo.Split(' ');
            string keyRootNoteName = relativeToParts[0].Trim();
            // string keyScaleType = relativeToParts.Length > 1 ? relativeToParts[1].Trim() : "Major"; // Not directly used in this parsing logic

            if (!_noteNameToSemitonesFromC.TryGetValue(keyRootNoteName, out int keyRootMidiOffset))
            {
                Console.WriteLine($"Warning: Unknown key root note '{keyRootNoteName}' for progression '{progression.Song}'. Defaulting to C.");
                keyRootMidiOffset = _noteNameToSemitonesFromC["C"];
            }

            int tonicMidiNote = (defaultOctave * 12) + keyRootMidiOffset;

            var romanChordStrings = romanNumeralsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var romanChord in romanChordStrings)
            {
                var match = _romanNumeralRegex.Match(romanChord);
                if (!match.Success)
                {
                    Console.WriteLine($"Warning: Could not parse Roman numeral '{romanChord}' for song '{progression.Song}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord()); // Add an empty chord
                    continue;
                }

                string leadingAccidental = match.Groups[1].Value;
                string baseRoman = match.Groups[2].Value;
                string trailingAccidental = match.Groups[3].Value;
                string qualitySuffix = match.Groups[4].Value;

                // Determine if the base Roman numeral implies a minor quality by default (e.g., 'ii', 'iii', 'vi', 'i', 'iv', 'v')
                bool isMinorDegreeDefault = baseRoman.All(char.IsLower);

                // Get the diatonic semitone offset for the base Roman numeral
                if (!_romanDegreeToSemitones.TryGetValue(baseRoman, out int degreeSemitoneOffset))
                {
                    Console.WriteLine($"Warning: Unknown base Roman degree '{baseRoman}' encountered for song '{progression.Song}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                // Apply accidentals to the root of the chord
                if (leadingAccidental == "b" || trailingAccidental == "b")
                {
                    degreeSemitoneOffset--;
                }
                else if (leadingAccidental == "#" || trailingAccidental == "#")
                {
                    degreeSemitoneOffset++;
                }

                // Calculate the absolute MIDI pitch for the root of the *current chord*
                int chordRootMidiNote = tonicMidiNote + degreeSemitoneOffset;

                // Ensure the root is within a reasonable octave range
                while (chordRootMidiNote < (defaultOctave * 12)) chordRootMidiNote += 12;
                while (chordRootMidiNote > ((defaultOctave + 1) * 12 + 11)) chordRootMidiNote -= 12;

                // Determine the chord quality intervals
                int[] intervals;
                if (!string.IsNullOrEmpty(qualitySuffix) && _chordQualityIntervals.TryGetValue(qualitySuffix, out intervals))
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
                    Console.WriteLine($"Warning: Unknown chord quality '{qualitySuffix}' for Roman numeral '{romanChord}' in song '{progression.Song}'. Using major triad as fallback.");
                    intervals = _chordQualityIntervals[""]; // Fallback to major triad
                }

                var midiPitches = new List<int>();
                foreach (var interval in intervals)
                {
                    int pitch = chordRootMidiNote + interval;
                    // Minimal octave adjustment to keep notes relatively close to the root of the chord
                    // and prevent extreme high/low notes.
                    while (pitch < chordRootMidiNote) pitch += 12; // Ensure notes are at or above the chord's root octave
                    while (pitch > chordRootMidiNote + 12) pitch -= 12; // Keep within two octaves of the chord's root
                    midiPitches.Add(pitch);
                }
                absoluteChords.Add(new MidiChord(midiPitches));
            }

            return new AbsoluteChordProgression(absoluteChords, $"{progression.Song} ({relativeTo})");
        }
    }
}
