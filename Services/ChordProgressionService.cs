// Services/ChordProgressionService.cs
using ChordProgressionQuiz.Models; // Ensure this is the correct namespace
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System; // Required for Random and Console.WriteLine

namespace ChordProgressionQuiz.Services
{
    public class ChordProgressionService
    {
        private List<ChordProgression> _chordProgressions;
        private readonly IWebHostEnvironment _env;
        private readonly Random _random;

        // MIDI note values for C Major scale (C4 to B4)
        // C=0, C#=1, D=2, D#=3, E=4, F=5, F#=6, G=7, G#=8, A=9, A#=10, B=11 (relative to C)
        private readonly Dictionary<string, int> _noteToMidi = new Dictionary<string, int>
        {
            {"C", 0}, {"C#", 1}, {"Db", 1}, {"D", 2}, {"D#", 3}, {"Eb", 3}, {"E", 4}, {"F", 5}, {"F#", 6}, {"Gb", 6},
            {"G", 7}, {"G#", 8}, {"Ab", 8}, {"A", 9}, {"A#", 10}, {"Bb", 10}, {"B", 11}
        };

        // Intervals for major scale degrees relative to the root (in semitones)
        // Each array represents [Root_interval, Third_interval, Fifth_interval, (Seventh_interval)]
        // All intervals are relative to the *key's tonic* (e.g., C for C Major)
        private readonly Dictionary<string, int[]> _romanToIntervalsMajor = new Dictionary<string, int[]>
        {
            #warning Todo: Add more common Roman numerals and their intervals as needed Chromatic alterations (simplified, might need more robust parsing for complex cases)
            {"I", new[] {0, 4, 7}},      // Major Triad (Root, Major 3rd, Perfect 5th)
            {"ii", new[] {2, 5, 9}},     // Minor Triad (Root, Minor 3rd, Perfect 5th)
            {"iii", new[] {4, 7, 11}},   // Minor Triad
            {"IV", new[] {5, 9, 0}},     // Major Triad (0 is octave higher)
            {"V", new[] {7, 11, 2}},     // Major Triad
            {"vi", new[] {9, 0, 4}},     // Minor Triad
            {"vii", new[] {11, 2, 5}},   // Diminished Triad (Root, Minor 3rd, Diminished 5th)
            {"I7", new[] {0, 4, 7, 10}}, // Major 7th
            {"V7", new[] {7, 11, 2, 5}}, // Dominant 7th
            {"ii7", new[] {2, 5, 9, 0}}, // Minor 7th
            {"vi7", new[] {9, 0, 4, 7}}, // Minor 7th
            {"iii7", new[] {4, 7, 11, 2}}, // Minor 7th
            {"III", new[] {4, 8, 11}},   // Major III (e.g., E Major in C Major)
            {"III7", new[] {4, 8, 11, 2}}, // E7 in C Major (secondary dominant V7/vi) - E-G#-B-D
            {"bIII", new[] {3, 7, 10}},  // Minor bIII (e.g., Eb Major in C Major)
            {"bVII", new[] {10, 2, 5}},  // Major bVII (e.g., Bb Major in C Major)
            {"bVI", new[] {8, 0, 3}},    // Major bVI (e.g., Ab Major in C Major)
            {"iv", new[] {5, 8, 0}},     // Minor iv (e.g., Fm in C Major)
            {"i", new[] {0, 3, 7}},      // Minor i (e.g., Cm in C Major)
            {"v", new[] {7, 10, 2}},     // Minor v (e.g., Gm in C Major)
            {"bII", new[] {1, 5, 8}},    // Neapolitan chord (Major chord on bII)
            {"II", new[] {2, 6, 9}},     // Major II (e.g., D Major in C Major, often V/V)
            {"II7", new[] {2, 6, 9, 0}}, // D7 in C Major (secondary dominant V7/V)
            {"VI", new[] {9, 1, 4}},     // Major VI (e.g., A Major in C Major, often V/ii)
            {"VI7", new[] {9, 1, 4, 7}}, // Dominant 7th on VI
            {"#Vaug", new[] {8, 0, 4}}, // Augmented chord on #V (e.g., G#aug in C) - this is relative to C, so G# is 8 semitones from C.
            {"Imaj7", new[] {0, 4, 7, 11}}, // Major 7th
            {"Iaug", new[] {0, 4, 8}},   // Augmented Triad
            {"#Idim", new[] {1, 4, 7}},  // Diminished chord on #I (e.g., C#dim in C)
            {"#vidim", new[] {10, 1, 4}}, // Diminished chord on #vi (e.g., F#dim in A) - *Note: This is relative to A, not C. Needs careful handling if root changes.*
            {"bVI7", new[] {8, 0, 3, 6}}, // Dominant 7th on bVI
            {"VII", new[] {11, 3, 6}}, // Major VII (e.g., B Major in C Major, a highly chromatic chord)
                                       // This is a temporary fix for the "B" in "Poupée de cire".
                                       // A more robust solution would involve parsing qualities (e.g., "VIImaj", "vii°").
        };


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
            string romanNumerals = null;
            string relativeTo = null;

            // Prioritize tonal, then modal if tonal is not present or relevant
            if (progression.Tonal != null && !string.IsNullOrEmpty(progression.Tonal.RomanNumerals))
            {
                romanNumerals = progression.Tonal.RomanNumerals;
                relativeTo = progression.Tonal.RelativeTo;
            }
            else if (progression.Modal != null && progression.Modal.Any())
            {
                // For simplicity, take the first modal entry
                romanNumerals = progression.Modal.First().RomanNumerals;
                relativeTo = progression.Modal.First().RelativeTo;
            }

            if (string.IsNullOrEmpty(romanNumerals) || string.IsNullOrEmpty(relativeTo))
            {
                Console.WriteLine($"Warning: Could not convert progression '{progression.Song}' due to missing Roman numerals or relative_to information.");
                return new AbsoluteChordProgression(new List<MidiChord>(), "N/A");
            }

            // Parse root note and scale type from "relative_to" (e.g., "C Major", "A Aeolian")
            var parts = relativeTo.Split(' ');
            string rootNoteName = parts[0].Trim();
            // The scaleType is mostly for context; the Roman numeral dictionary is primarily major-key centric
            // and handles chromaticism relative to the major tonic.
            string scaleType = parts.Length > 1 ? parts[1].Trim() : "Major";

            if (!_noteToMidi.TryGetValue(rootNoteName, out int rootMidiOffset))
            {
                Console.WriteLine($"Warning: Unknown root note '{rootNoteName}' for progression '{progression.Song}'. Defaulting to C.");
                rootMidiOffset = _noteToMidi["C"]; // Fallback to C
            }

            // The base MIDI note for the *tonic of the key* (e.g., C4 for C Major)
            int tonicMidiNote = (defaultOctave * 12) + rootMidiOffset;

            var romanChordNames = romanNumerals.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var roman in romanChordNames)
            {
                if (_romanToIntervalsMajor.TryGetValue(roman, out int[] intervals))
                {
                    var midiPitches = new List<int>();
                    foreach (var interval in intervals)
                    {
                        // Add the interval to the tonic MIDI note
                        int pitch = tonicMidiNote + interval;
                        // Adjust octave if necessary to keep pitches within a reasonable range
                        // This simple adjustment assumes upward intervals.
                        while (pitch < defaultOctave * 12) pitch += 12; // Ensure it's at least in the default octave
                        while (pitch > (defaultOctave + 1) * 12 + 11) pitch -= 12; // Prevent going too high
                        midiPitches.Add(pitch);
                    }
                    absoluteChords.Add(new MidiChord(midiPitches));
                }
                else
                {
                    Console.WriteLine($"Warning: Unknown Roman numeral '{roman}' encountered for song '{progression.Song}'. Adding root note only.");
                    // Fallback: Add just the root note of the chord (calculated from the Roman numeral's degree)
                    // This is a more intelligent fallback than just the tonic.
                    int chordRootOffset = 0; // Default to tonic
                    if (roman.StartsWith("I")) chordRootOffset = 0;
                    else if (roman.StartsWith("ii")) chordRootOffset = 2;
                    else if (roman.StartsWith("iii")) chordRootOffset = 4;
                    else if (roman.StartsWith("IV")) chordRootOffset = 5;
                    else if (roman.StartsWith("V")) chordRootOffset = 7;
                    else if (roman.StartsWith("vi")) chordRootOffset = 9;
                    else if (roman.StartsWith("vii")) chordRootOffset = 11;
                    // Handle flats/sharps for root if not in dictionary
                    else if (roman.StartsWith("bII")) chordRootOffset = 1;
                    else if (roman.StartsWith("bIII")) chordRootOffset = 3;
                    else if (roman.StartsWith("bVI")) chordRootOffset = 8;
                    else if (roman.StartsWith("bVII")) chordRootOffset = 10;
                    else if (roman.StartsWith("II")) chordRootOffset = 2; // Major II
                    else if (roman.StartsWith("III")) chordRootOffset = 4; // Major III
                    else if (roman.StartsWith("VI")) chordRootOffset = 9; // Major VI
                    else if (roman.Contains("#I")) chordRootOffset = 1;
                    else if (roman.Contains("#V")) chordRootOffset = 8;
                    else if (roman.Contains("#vi")) chordRootOffset = 10;


                    int pitch = tonicMidiNote + chordRootOffset;
                    while (pitch < defaultOctave * 12) pitch += 12;
                    while (pitch > (defaultOctave + 1) * 12 + 11) pitch -= 12;

                    absoluteChords.Add(new MidiChord(new[] { pitch }));
                }
            }

            return new AbsoluteChordProgression(absoluteChords, $"{progression.Song} ({relativeTo})");
        }
    }
}
