// Services/ChordStylingService.cs
using ChordProgressionQuiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgprogressionQuiz.Services
{
    public class ChordStylingService
    {
        private readonly Random _random;

        // Configuration for styling
        private const double BaseChordDuration = 1.5; // Base duration for a full chord in seconds
        // MinDurationFactor and MaxDurationFactor are removed as duration will now be constant.

        // ArpeggioNoteDelay is now a dynamic calculation, but we keep a small minimum for very short chords
        private const double MinArpeggioNoteDuration = 0.05; // Minimum duration for an individual arpeggio note
        private const double MinTimeBetweenArpeggioNotes = 0.02; // Minimum time between start of arpeggio notes

        // Max semitones to transpose up/down (one octave) - adjusted to ensure range safety
        private const int MaxTransposeSemitones = 12;

        public ChordStylingService()
        {
            _random = new Random();
        }

        /// <summary>
        /// Applies random styling (transposition, duration, arpeggiation) to an AbsoluteChordProgression.
        /// Arpeggiation is now always 'Up' and consistent when applied.
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
            double currentPlaybackTime = 0.0; // Tracks the current time in the overall stylized progression

            // 1. Determine random transposition for the entire progression
            int globalTransposeOffset = _random.Next(-MaxTransposeSemitones, MaxTransposeSemitones + 1);

            foreach (var chord in absoluteProgression.Chords)
            {
                if (!chord.MidiPitches.Any())
                {
                    // If a chord is empty, just advance time by a default duration
                    currentPlaybackTime += BaseChordDuration; // Use fixed BaseChordDuration
                    continue;
                }

                // 2. Determine fixed duration for THIS chord's slot
                double allocatedChordDuration = BaseChordDuration; // Fixed duration as per request

                // Get notes for this chord, apply global transpose, and sort ascending for 'Up' arpeggio
                var pitchesToProcess = chord.MidiPitches
                                        .Select(p => p + globalTransposeOffset)
                                        .ToList();

                // Apply MIDI range adjustment *after* transposition but before arpeggiation logic
                pitchesToProcess = pitchesToProcess.Select(AdjustMidiPitchToRange).ToList();
                pitchesToProcess.Sort(); // Always sort ascending for consistent 'Up' arpeggio

                // Determine if arpeggiated (always if more than one note) or block
                bool isArpeggiated = pitchesToProcess.Count > 1;

                if (isArpeggiated)
                {
                    // Arpeggiated playback (always Up)
                    double currentArpeggioNoteOffset = 0.0;

                    // Calculate the time slot for each note, spreading them evenly across the allocated chord duration.
                    // This is the time from the start of one note to the start of the next.
                    double dynamicNoteStartDelay = allocatedChordDuration / pitchesToProcess.Count;

                    // Calculate individual note duration (shorter than the start delay to prevent overlap)
                    // Ensure a small gap between notes for clarity.
                    double individualNoteDuration = dynamicNoteStartDelay * 0.8; // Note plays for 80% of its slot
                    individualNoteDuration = Math.Max(MinArpeggioNoteDuration, individualNoteDuration); // Ensure minimum duration

                    foreach (var pitch in pitchesToProcess) // Already sorted ascending
                    {
                        stylizedEvents.Add(new StylizedMidiEvent
                        {
                            Pitch = pitch,
                            StartTime = currentPlaybackTime + currentArpeggioNoteOffset,
                            Duration = individualNoteDuration
                        });
                        currentArpeggioNoteOffset += dynamicNoteStartDelay; // Advance offset by dynamic delay
                    }
                    // Advance current time by the full allocated duration for this chord
                    currentPlaybackTime += allocatedChordDuration;
                }
                else // Block chord playback (or single note)
                {
                    double individualNoteDuration = allocatedChordDuration * 0.9; // Notes play for 90% of the allocated time

                    foreach (var pitch in pitchesToProcess)
                    {
                        stylizedEvents.Add(new StylizedMidiEvent
                        {
                            Pitch = pitch,
                            StartTime = currentPlaybackTime,
                            Duration = individualNoteDuration
                        });
                    }
                    currentPlaybackTime += allocatedChordDuration; // Advance time by the full allocated duration
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
            // Aim to keep pitches roughly within C3 (48) to C6 (84) for piano
            const int lowerBound = 48; // C3
            const int upperBound = 84; // C6

            while (pitch < lowerBound && pitch + 12 <= 127)
            {
                pitch += 12;
            }
            while (pitch > upperBound && pitch - 12 >= 0)
            {
                pitch -= 12;
            }

            // Final clamp to ensure it's strictly within MIDI range
            return Math.Clamp(pitch, 0, 127);
        }
    }
}
