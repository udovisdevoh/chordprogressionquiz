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
        // This maps "I", "ii", "bIII", etc., to their semitone distance from the tonic.
        private readonly Dictionary<string, int> _romanDegreeToSemitones = new Dictionary<string, int>
        {
            {"I", 0}, {"ii", 2}, {"iii", 4}, {"IV", 5}, {"V", 7}, {"vi", 9}, {"vii", 11},
            {"II", 2}, {"III", 4}, {"VI", 9}, {"VII", 11}, // Major versions (often secondary dominants or borrowed)
            {"bII", 1}, {"bIII", 3}, {"bIV", 4}, {"bV", 6}, {"bVI", 8}, {"bVII", 10}, // Flat versions
            {"#I", 1}, {"#II", 3}, {"#IV", 6}, {"#V", 8}, {"#VI", 10}, // Sharp versions (less common as roots)
            // For minor keys, these are relative to the *major* tonic for consistency,
            // e.g., 'i' in A minor is 'vi' in C Major, but its root is A, 9 semitones from C.
            // When parsing 'i', 'iv', 'v' etc. in a modal context, we'll need to adjust the root calculation.
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
            // Add more as needed, e.g., "9", "11", "13", "m9", "m11", "m13", "Maj9", etc.
        };

        // Regex to parse Roman numerals:
        // Group 1: Optional flat/sharp (b, #)
        // Group 2: Roman numeral (I, V, ii, etc.)
        // Group 3: Optional quality/extension (m, Maj7, 7, dim, aug, etc.)
        private static readonly Regex _romanNumeralRegex = new Regex(
            @"^(b|#)?(I|II|III|IV|V|VI|VII|i|ii|iii|iv|v|vi|vii)(m7b5|m7|Maj7|dim7|dim|aug|sus2|sus4|add9|add#9|maj7#11|7|maj|m)?$",
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

        /// <summary>
        /// Converts a Roman numeral chord progression to an AbsoluteChordProgression (MIDI pitches).
        /// </summary>
        /// <param name="progression">The symbolic ChordProgression object.</param>
        /// <param name="defaultOctave">The base MIDI octave (e.g., 4 for C4).</param>
        /// <returns>An AbsoluteChordProgression with MIDI pitches.</returns>
        public AbsoluteChordProgression ConvertToAbsoluteMidiProgression(ChordProgression progression, int defaultOctave = 4)
        {
            var absoluteChords = new List<MidiChord>();
            string romanNumeralsString = null;
            string relativeTo = null;

            // Prioritize tonal, then modal if tonal is not present or relevant
            if (progression.Tonal != null && !string.IsNullOrEmpty(progression.Tonal.RomanNumerals))
            {
                romanNumeralsString = progression.Tonal.RomanNumerals;
                relativeTo = progression.Tonal.RelativeTo;
            }
            else if (progression.Modal != null && progression.Modal.Any())
            {
                // For simplicity, take the first modal entry
                romanNumeralsString = progression.Modal.First().RomanNumerals;
                relativeTo = progression.Modal.First().RelativeTo;
            }

            if (string.IsNullOrEmpty(romanNumeralsString) || string.IsNullOrEmpty(relativeTo))
            {
                Console.WriteLine($"Warning: Could not convert progression '{progression.Song}' due to missing Roman numerals or relative_to information.");
                return new AbsoluteChordProgression(new List<MidiChord>(), "N/A");
            }

            // Parse root note and scale type from "relative_to" (e.g., "C Major", "A Aeolian")
            var relativeToParts = relativeTo.Split(' ');
            string keyRootNoteName = relativeToParts[0].Trim();
            string keyScaleType = relativeToParts.Length > 1 ? relativeToParts[1].Trim() : "Major"; // Default to Major

            if (!_noteNameToSemitonesFromC.TryGetValue(keyRootNoteName, out int keyRootMidiOffset))
            {
                Console.WriteLine($"Warning: Unknown key root note '{keyRootNoteName}' for progression '{progression.Song}'. Defaulting to C.");
                keyRootMidiOffset = _noteNameToSemitonesFromC["C"]; // Fallback to C
            }

            // The base MIDI note for the *tonic of the key* (e.g., C4 for C Major)
            int tonicMidiNote = (defaultOctave * 12) + keyRootMidiOffset;

            var romanChordStrings = romanNumeralsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var romanChord in romanChordStrings)
            {
                var match = _romanNumeralRegex.Match(romanChord);
                if (!match.Success)
                {
                    Console.WriteLine($"Warning: Could not parse Roman numeral '{romanChord}' for song '{progression.Song}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord()); // Add an empty chord for unknown parsing
                    continue;
                }

                string accidental = match.Groups[1].Value; // "b" or "#"
                string degreeRoman = match.Groups[2].Value; // "I", "ii", "V", etc.
                string qualitySuffix = match.Groups[3].Value; // "m", "7", "Maj7", "dim", etc.

                // Standardize degree Roman (e.g., 'i' to 'I' for lookup, then apply minor quality)
                string standardizedDegree = degreeRoman.ToUpper();
                bool isMinorDegree = char.IsLower(degreeRoman[0]);

                if (!_romanDegreeToSemitones.TryGetValue(standardizedDegree, out int degreeSemitoneOffset))
                {
                    Console.WriteLine($"Warning: Unknown Roman degree '{degreeRoman}' encountered for song '{progression.Song}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                // Adjust for accidental on the root degree (e.g., 'bVII' means 10 semitones from tonic)
                if (accidental == "b")
                {
                    degreeSemitoneOffset--;
                }
                else if (accidental == "#")
                {
                    degreeSemitoneOffset++;
                }

                // Calculate the absolute MIDI pitch for the root of the *current chord*
                int chordRootMidiNote = tonicMidiNote + degreeSemitoneOffset;

                // Ensure the root is within a reasonable octave range for the chord
                while (chordRootMidiNote < (defaultOctave * 12)) chordRootMidiNote += 12;
                while (chordRootMidiNote > ((defaultOctave + 1) * 12 + 11)) chordRootMidiNote -= 12;


                // Determine the chord quality intervals
                int[] intervals;
                if (!string.IsNullOrEmpty(qualitySuffix) && _chordQualityIntervals.TryGetValue(qualitySuffix, out intervals))
                {
                    // Use the specified quality
                }
                else if (isMinorDegree && _chordQualityIntervals.TryGetValue("m", out intervals))
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
                    while (pitch < chordRootMidiNote) pitch += 12; // Ensure notes are at or above the root
                    while (pitch > chordRootMidiNote + 12) pitch -= 12; // Keep within two octaves of the root
                    midiPitches.Add(pitch);
                }
                absoluteChords.Add(new MidiChord(midiPitches));
            }

            return new AbsoluteChordProgression(absoluteChords, $"{progression.Song} ({relativeTo})");
        }
    }
}
