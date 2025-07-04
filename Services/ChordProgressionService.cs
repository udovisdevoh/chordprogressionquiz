// Services/ChordProgressionService.cs
using ChordProgressionQuiz.Models;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System;
using System.Text.RegularExpressions;

namespace ChordProgressionQuiz.Services
{
    public class ChordProgressionService
    {
        private List<ChordProgression> _chordProgressions;
        private readonly IWebHostEnvironment _env;
        private readonly Random _random;

        private readonly Dictionary<string, int> _noteNameToSemitonesFromC = new Dictionary<string, int>
        {
            {"C", 0}, {"C#", 1}, {"Db", 1}, {"D", 2}, {"D#", 3}, {"Eb", 3}, {"E", 4}, {"F", 5}, {"F#", 6}, {"Gb", 6},
            {"G", 7}, {"G#", 8}, {"Ab", 8}, {"A", 9}, {"A#", 10}, {"Bb", 10}, {"B", 11}
        };

        private readonly Dictionary<string, int> _romanDegreeToSemitones = new Dictionary<string, int>
        {
            {"I", 0}, {"ii", 2}, {"iii", 4}, {"IV", 5}, {"V", 7}, {"vi", 9}, {"vii", 11},
            {"II", 2}, {"III", 4}, {"VI", 9}, {"VII", 11},
            {"bII", 1}, {"bIII", 3}, {"bIV", 4}, {"bV", 6}, {"bVI", 8}, {"bVII", 10},
            {"#I", 1}, {"#II", 3}, {"#IV", 6}, {"#V", 8}, {"#VI", 10},
        };

        private readonly Dictionary<string, int[]> _chordQualityIntervals = new Dictionary<string, int[]>
        {
            {"", new[] {0, 4, 7}},
            {"maj", new[] {0, 4, 7}},
            {"m", new[] {0, 3, 7}},
            {"dim", new[] {0, 3, 6}},
            {"aug", new[] {0, 4, 8}},

            {"7", new[] {0, 4, 7, 10}},
            {"m7", new[] {0, 3, 7, 10}},
            {"Maj7", new[] {0, 4, 7, 11}},
            {"dim7", new[] {0, 3, 6, 9}},
            {"m7b5", new[] {0, 3, 6, 10}},

            {"sus2", new[] {0, 2, 7}},
            {"sus4", new[] {0, 5, 7}},

            {"add9", new[] {0, 4, 7, 14}},
            {"add#9", new[] {0, 4, 7, 15}},
            {"maj7#11", new[] {0, 4, 7, 11, 18}},
        };

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

        /// <summary>
        /// Retrieves a random chord progression from the loaded list.
        /// </summary>
        /// <returns>A random ChordProgression object, or null if no progressions are loaded.</returns>
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
        /// Retrieves a chord progression by its index in the loaded list.
        /// </summary>
        /// <param name="index">The zero-based index of the progression to retrieve.</param>
        /// <returns>The ChordProgression object at the specified index, or null if the index is out of bounds.</returns>
        public ChordProgression GetChordProgressionByIndex(int index)
        {
            if (_chordProgressions == null || index < 0 || index >= _chordProgressions.Count)
            {
                return null;
            }
            return _chordProgressions[index];
        }

        /// <summary>
        /// Gets the total number of chord progressions loaded.
        /// </summary>
        public int GetProgressionCount()
        {
            return _chordProgressions?.Count ?? 0;
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
            string keyScaleType = relativeToParts.Length > 1 ? relativeToParts[1].Trim() : "Major";

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
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                string accidental = match.Groups[1].Value;
                string degreeRoman = match.Groups[2].Value;
                string qualitySuffix = match.Groups[3].Value;

                string standardizedDegree = degreeRoman.ToUpper();
                bool isMinorDegree = char.IsLower(degreeRoman[0]);

                if (!_romanDegreeToSemitones.TryGetValue(standardizedDegree, out int degreeSemitoneOffset))
                {
                    Console.WriteLine($"Warning: Unknown Roman degree '{degreeRoman}' encountered for song '{progression.Song}'. Skipping this chord.");
                    absoluteChords.Add(new MidiChord());
                    continue;
                }

                if (accidental == "b")
                {
                    degreeSemitoneOffset--;
                }
                else if (accidental == "#")
                {
                    degreeSemitoneOffset++;
                }

                int chordRootMidiNote = tonicMidiNote + degreeSemitoneOffset;

                while (chordRootMidiNote < (defaultOctave * 12)) chordRootMidiNote += 12;
                while (chordRootMidiNote > ((defaultOctave + 1) * 12 + 11)) chordRootMidiNote -= 12;


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
