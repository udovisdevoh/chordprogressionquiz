// Services/ChordStylingService.cs
using ChordProgressionQuiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Services
{
    public class ChordStylingService
    {
        private readonly Random _random;

        // Configuration for styling
        private const double BaseChordDuration = 1.5; // Base duration for a full chord in seconds
        private const double MinDurationFactor = 1.5; // Minimum factor to multiply BaseChordDuration by
        private const double MaxDurationFactor = 2.5; // Maximum factor to multiply BaseChordDuration by

        private const double ArpeggioNoteDelay = 0.08; // Time between notes in an arpeggio in seconds
        private const double ArpeggioChance = 0.70; // 70% chance a chord will be arpeggiated
        private const int MaxTransposeSemitones = 12; // Max semitones to transpose up/down (one octave)

        // Arpeggiation styles
        private enum ArpeggioStyle
        {
            Up,
            Down,
            UpDown,
            Random
        }

        public ChordStylingService()
        {
            _random = new Random();
        }

        /// <summary>
        /// Applies random styling (transposition, duration, arpeggiation) to an AbsoluteChordProgression.
        /// </summary>
        /// <param name="absoluteProgression">The raw progression with absolute MIDI pitches.</param>
        /// <returns>A StylizedChordProgression ready for playback.</returns>
        public StylizedChordProgression ApplyRandomStyling(AbsoluteChordProgression absoluteProgression)
        {
            if (absoluteProgression == null || !absoluteProgression.Chords.Any())
            {
                return new StylizedChordProgression(new List<StylizedMidiEvent>(), "Empty Stylized Progression");
            }

            var stylizedEvents = new List<StylizedMidiEvent>();
            double currentTime = 0.0;

            // 1. Determine random transposition for the entire progression
            // Offset from -12 to +12 semitones (inclusive)
            int globalTransposeOffset = _random.Next(-MaxTransposeSemitones, MaxTransposeSemitones + 1);

            // Ensure the transposed notes will generally stay within a playable MIDI range (e.g., around C3 to C6)
            // This is a heuristic. A more robust solution might analyze the min/max pitches of the *entire* progression
            // to find a safe transposition range. For now, we'll rely on the note adjustment later.

            foreach (var chord in absoluteProgression.Chords)
            {
                if (!chord.MidiPitches.Any())
                {
                    // If a chord is empty, just advance time
                    currentTime += BaseChordDuration * (_random.NextDouble() * (MaxDurationFactor - MinDurationFactor) + MinDurationFactor);
                    continue;
                }

                // 2. Determine random duration for this chord
                double currentChordDuration = BaseChordDuration * (_random.NextDouble() * (MaxDurationFactor - MinDurationFactor) + MinDurationFactor);
                double noteDuration = currentChordDuration * 0.8; // Notes play for 80% of chord duration or arpeggio time

                // 3. Decide if arpeggiated and choose style
                bool shouldArpeggiate = _random.NextDouble() < ArpeggioChance;
                ArpeggioStyle arpeggioStyle = ArpeggioStyle.Random; // Default, will be chosen if arpeggiated

                if (shouldArpeggiate)
                {
                    arpeggioStyle = (ArpeggioStyle)_random.Next(0, Enum.GetNames(typeof(ArpeggioStyle)).Length);
                    // If Random is selected, re-roll to get a concrete style (Up, Down, UpDown)
                    if (arpeggioStyle == ArpeggioStyle.Random)
                    {
                        arpeggioStyle = (ArpeggioStyle)_random.Next(0, Enum.GetNames(typeof(ArpeggioStyle)).Length - 1); // Exclude Random itself
                    }
                }

                // Get notes for this chord, apply global transpose, and sort for arpeggiation
                var pitchesToPlay = chord.MidiPitches
                                        .Select(p => p + globalTransposeOffset)
                                        .ToList();

                // Apply MIDI range adjustment *after* transposition but before arpeggiation logic
                pitchesToPlay = pitchesToPlay.Select(AdjustMidiPitchToRange).ToList();

                if (shouldArpeggiate && pitchesToPlay.Count > 1)
                {
                    // Sort pitches for consistent arpeggiation direction
                    pitchesToPlay.Sort();

                    double arpeggioTimeOffset = 0.0;
                    List<int> orderedPitches = new List<int>(pitchesToPlay);

                    switch (arpeggioStyle)
                    {
                        case ArpeggioStyle.Up:
                            // Already sorted ascending
                            break;
                        case ArpeggioStyle.Down:
                            orderedPitches.Reverse(); // Sort descending
                            break;
                        case ArpeggioStyle.UpDown:
                            // Play up, then down (excluding the highest note if it's the peak)
                            var upPart = pitchesToPlay;
                            var downPart = pitchesToPlay.Skip(1).Reverse(); // Skip root to avoid double-playing
                            orderedPitches = upPart.Concat(downPart).ToList();
                            break;
                            // ArpeggioStyle.Random is handled by re-rolling
                    }

                    foreach (var pitch in orderedPitches)
                    {
                        stylizedEvents.Add(new StylizedMidiEvent
                        {
                            Pitch = pitch,
                            StartTime = currentTime + arpeggioTimeOffset,
                            Duration = noteDuration // Each note gets the same duration
                        });
                        arpeggioTimeOffset += ArpeggioNoteDelay;
                    }
                    // Advance current time by the total time taken for the arpeggio
                    currentTime += arpeggioTimeOffset;
                }
                else // Play as a block chord
                {
                    foreach (var pitch in pitchesToPlay)
                    {
                        stylizedEvents.Add(new StylizedMidiEvent
                        {
                            Pitch = pitch,
                            StartTime = currentTime,
                            Duration = noteDuration // All notes start at the same time
                        });
                    }
                    currentTime += currentChordDuration; // Advance time by full chord duration
                }
            }

            return new StylizedChordProgression(stylizedEvents, absoluteProgression.Name);
        }

        /// <summary>
        /// Adjusts a MIDI pitch to be within the 0-127 range by shifting octaves.
        /// Prioritizes keeping the pitch near the middle of the range if possible.
        /// </summary>
        private int AdjustMidiPitchToRange(int pitch)
        {
            // If the pitch is too high, shift down by octaves until it's in range or near the middle
            while (pitch > 100 && pitch - 12 >= 0) // Keep it below 100 if possible, but above 0
            {
                pitch -= 12;
            }
            // If the pitch is too low, shift up by octaves until it's in range or near the middle
            while (pitch < 20 && pitch + 12 <= 127) // Keep it above 20 if possible, but below 127
            {
                pitch += 12;
            }

            // Final clamp to ensure it's strictly within MIDI range
            return Math.Clamp(pitch, 0, 127);
        }
    }
}
