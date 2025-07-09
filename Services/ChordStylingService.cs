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

        private const double MinArpeggioNoteDuration = 0.05; // Minimum duration for an individual arpeggio note

        // Max semitones to transpose up/down (one octave) - adjusted to ensure range safety
        private const int MaxTransposeSemitones = 12;

        // Octave shift for the "left hand" notes
        private const int LeftHandOctaveShift = -24; // Two octaves down

        // MIDI Program Change numbers for instruments
        private const int PianoProgram = 0; // General MIDI Program 0 is Acoustic Grand Piano (often 1 in DAWs)
        private const int StringEnsembleProgram = 48; // General MIDI Program 48 is String Ensemble 1 (often 49 in DAWs)

        public ChordStylingService()
        {
            _random = new Random();
        }

        /// <summary>
        /// Applies random styling (transposition, duration, arpeggiation) to an AbsoluteChordProgression.
        /// If the number of chords is odd, the last chord is repeated once to make it even.
        /// Arpeggiation is always "twice as fast and repeated twice".
        /// Random chord inversion is applied per chord.
        /// Arpeggio direction (up/down) is chosen globally for the entire progression.
        /// A "left hand" accompaniment of the two lowest chord notes (two octaves lower, held, String Ensemble) is added.
        /// The "right hand" arpeggios are played with an Acoustic Grand Piano.
        /// </summary>
        /// <param name="absoluteProgression">The raw progression with absolute MIDI pitches.</param>
        /// <returns>A StylizedChordProgression ready for playback.</returns>
        public StylizedChordProgression ApplyRandomStyling(AbsoluteChordProgression absoluteProgression)
        {
            if (absoluteProgression == null || !absoluteProgression.Chords.Any())
            {
                return new StylizedChordProgression(new List<StylizedMidiEvent>(), "Empty Stylized Progression");
            }

            // NEW FIX: Duplicate the last chord if the count is odd
            var processedChords = new List<MidiChord>(absoluteProgression.Chords);
            if (processedChords.Count % 2 != 0) // If odd number of chords
            {
                var lastChord = processedChords.Last();
                var duplicatedChord = new MidiChord(lastChord.MidiPitches);
                processedChords.Add(duplicatedChord);
                Console.WriteLine($"Duplicated last chord '{lastChord}' to make progression even. New count: {processedChords.Count}");
            }

            var stylizedEvents = new List<StylizedMidiEvent>();
            double currentPlaybackTime = 0.0;

            // Determine random transposition for the entire progression
            int globalTransposeOffset = _random.Next(-MaxTransposeSemitones, MaxTransposeSemitones + 1); // This is the value we need to pass

            bool applyFastRepeatedArpeggioGlobally = true;

            // Determine globally if arpeggios should move down
            bool arpeggioDirectionIsDown = _random.NextDouble() < 0.5;

            foreach (var chord in processedChords)
            {
                if (!chord.MidiPitches.Any())
                {
                    currentPlaybackTime += BaseChordDuration;
                    continue;
                }

                double allocatedChordDuration = BaseChordDuration;

                var originalSortedPitches = chord.MidiPitches.OrderBy(p => p).ToList();

                var leftHandPitches = originalSortedPitches
                                        .Take(2)
                                        .Select(p => AdjustMidiPitchToRange(p + globalTransposeOffset + LeftHandOctaveShift))
                                        .ToList();

                foreach (var lhPitch in leftHandPitches)
                {
                    stylizedEvents.Add(new StylizedMidiEvent
                    {
                        Pitch = lhPitch,
                        StartTime = currentPlaybackTime,
                        Duration = allocatedChordDuration * 0.95,
                        InstrumentProgram = StringEnsembleProgram
                    });
                }


                var pitchesToProcess = originalSortedPitches
                                        .Select(p => p + globalTransposeOffset)
                                        .ToList();

                pitchesToProcess = pitchesToProcess.Select(AdjustMidiPitchToRange).ToList();
                pitchesToProcess.Sort();

                bool isArpeggiated = pitchesToProcess.Count > 1;

                if (isArpeggiated)
                {
                    if (pitchesToProcess.Count >= 3)
                    {
                        int inversionType = _random.Next(0, 3);

                        for (int i = 0; i < inversionType; i++)
                        {
                            int lowestPitch = pitchesToProcess[0];
                            pitchesToProcess.RemoveAt(0);
                            pitchesToProcess.Add(AdjustMidiPitchToRange(lowestPitch + 12));
                            pitchesToProcess.Sort();
                        }
                    }

                    List<int> arpeggioPitches = new List<int>();
                    int originalNoteCount = pitchesToProcess.Count;
                    const int desiredArpeggioNotes = 4;

                    for (int i = 0; i < desiredArpeggioNotes; i++)
                    {
                        int basePitch = pitchesToProcess[i % originalNoteCount];
                        int octaveShift = (i / originalNoteCount) * 12;
                        arpeggioPitches.Add(AdjustMidiPitchToRange(basePitch + octaveShift));
                    }
                    arpeggioPitches.Sort();

                    if (arpeggioDirectionIsDown)
                    {
                        arpeggioPitches.Reverse();
                    }

                    int numberOfRepetitions = applyFastRepeatedArpeggioGlobally ? 2 : 1;

                    for (int rep = 0; rep < numberOfRepetitions; rep++)
                    {
                        double currentArpeggioNoteOffset = 0.0;
                        double singlePassDuration = allocatedChordDuration / (applyFastRepeatedArpeggioGlobally ? 2.0 : 1.0);

                        double dynamicNoteStartDelay = singlePassDuration / arpeggioPitches.Count;

                        if (dynamicNoteStartDelay < MinArpeggioNoteDuration)
                        {
                            dynamicNoteStartDelay = MinArpeggioNoteDuration;
                            singlePassDuration = dynamicNoteStartDelay * arpeggioPitches.Count;
                        }

                        double individualNoteDuration = dynamicNoteStartDelay * 0.8;
                        individualNoteDuration = Math.Max(MinArpeggioNoteDuration, individualNoteDuration);

                        foreach (var pitch in arpeggioPitches)
                        {
                            stylizedEvents.Add(new StylizedMidiEvent
                            {
                                Pitch = pitch,
                                StartTime = currentPlaybackTime + currentArpeggioNoteOffset,
                                Duration = individualNoteDuration,
                                InstrumentProgram = PianoProgram
                            });
                            currentArpeggioNoteOffset += dynamicNoteStartDelay;
                        }
                        currentPlaybackTime += singlePassDuration;
                    }
                }
                else // Block chord playback (or single note)
                {
                    double individualNoteDuration = allocatedChordDuration * 0.9;

                    foreach (var pitch in pitchesToProcess)
                    {
                        stylizedEvents.Add(new StylizedMidiEvent
                        {
                            Pitch = pitch,
                            StartTime = currentPlaybackTime,
                            Duration = individualNoteDuration,
                            InstrumentProgram = PianoProgram
                        });
                    }
                    currentPlaybackTime += allocatedChordDuration;
                }
            }

            stylizedEvents.Sort((e1, e2) => e1.StartTime.CompareTo(e2.StartTime));


            return new StylizedChordProgression(stylizedEvents, absoluteProgression.Name, globalTransposeOffset); // NEW: Pass globalTransposeOffset
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

            // A final clamp to ensure it doesn't go below 0 or above 127
            return Math.Clamp(pitch, 0, 127);
        }
    }
}