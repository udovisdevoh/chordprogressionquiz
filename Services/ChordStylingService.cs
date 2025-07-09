// D:\users\Anonymous\Documents\C Sharp\ChordProgressionQuiz\Services\ChordStylingService.cs
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
        private const double BaseChordDuration = 1.5; // Base duration for one 'slot' for a full chord

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
        /// Arpeggiation is "twice as fast and repeated twice" by default.
        /// If playArpeggioTwiceAsLong is true, the *total duration* for each arpeggiated chord is doubled,
        /// and the arpeggio is repeated more times at its *original speed* to fill this doubled duration.
        /// Random chord inversion is applied per chord.
        /// Arpeggio direction (up/down) is chosen globally for the entire progression.
        /// A "left hand" accompaniment of the two lowest chord notes (two octaves lower, held, String Ensemble) is added.
        /// The "right hand" arpeggios are played with an Acoustic Grand Piano.
        /// </summary>
        /// <param name="absoluteProgression">The raw progression with absolute MIDI pitches.</param>
        /// <param name="playArpeggioTwiceAsLong">If true, each arpeggio's total playback duration is doubled, with more repetitions at original speed.</param>
        /// <returns>A StylizedChordProgression ready for playback.</returns>
        public StylizedChordProgression ApplyRandomStyling(AbsoluteChordProgression absoluteProgression, bool playArpeggioTwiceAsLong)
        {
            if (absoluteProgression == null || !absoluteProgression.Chords.Any())
            {
                return new StylizedChordProgression(new List<StylizedMidiEvent>(), "Empty Stylized Progression");
            }

            // Duplicate the last chord if the count is odd
            var processedChords = new List<MidiChord>(absoluteProgression.Chords);
            if (processedChords.Count % 2 != 0)
            {
                var lastChord = processedChords.Last();
                var duplicatedChord = new MidiChord(lastChord.MidiPitches);
                processedChords.Add(duplicatedChord);
                Console.WriteLine($"Duplicated last chord '{lastChord}' to make progression even. New count: {processedChords.Count}");
            }

            var stylizedEvents = new List<StylizedMidiEvent>();
            double currentPlaybackTime = 0.0;

            int globalTransposeOffset = _random.Next(-MaxTransposeSemitones, MaxTransposeSemitones + 1);

            bool applyFastRepeatedArpeggioGlobally = true; // This makes arpeggios play twice within their original slot duration

            bool arpeggioDirectionIsDown = _random.NextDouble() < 0.5;

            foreach (var chord in processedChords)
            {
                if (!chord.MidiPitches.Any())
                {
                    // If chord is empty, still advance time appropriately for a chord slot
                    currentPlaybackTime += BaseChordDuration * (playArpeggioTwiceAsLong ? 2.0 : 1.0); // Account for doubled slot duration
                    continue;
                }

                // Determine the total duration this *chord slot* will occupy
                double currentChordSlotDuration = BaseChordDuration * (playArpeggioTwiceAsLong ? 2.0 : 1.0);

                // --- Left Hand (held notes) ---
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
                        Duration = currentChordSlotDuration * 0.95, // Hold for almost the full chord slot duration
                        InstrumentProgram = StringEnsembleProgram
                    });
                }

                // --- Right Hand (arpeggio or block) ---
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
                    const int desiredArpeggioNotes = 4; // Ensure at least 4 notes for arpeggiation patterns

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

                    // This is the duration for ONE complete pass of the arpeggio (e.g., C-E-G-C')
                    // It's based on the ORIGINAL BaseChordDuration and the applyFastRepeatedArpeggioGlobally flag.
                    // This is what defines the "note speed".
                    double durationOfOneArpeggioPass = BaseChordDuration / (applyFastRepeatedArpeggioGlobally ? 2.0 : 1.0);

                    // Adjust if individual notes become too short
                    double dynamicNoteStartDelay = durationOfOneArpeggioPass / arpeggioPitches.Count;
                    if (dynamicNoteStartDelay < MinArpeggioNoteDuration)
                    {
                        dynamicNoteStartDelay = MinArpeggioNoteDuration;
                        durationOfOneArpeggioPass = dynamicNoteStartDelay * arpeggioPitches.Count;
                    }
                    double individualNoteDuration = dynamicNoteStartDelay * 0.8;
                    individualNoteDuration = Math.Max(MinArpeggioNoteDuration, individualNoteDuration);


                    // Calculate the total number of times the fixed-speed arpeggio pass needs to repeat
                    // This fills the 'currentChordSlotDuration' at the 'durationOfOneArpeggioPass' speed.
                    int totalArpeggioRepetitions = (int)Math.Round(currentChordSlotDuration / durationOfOneArpeggioPass);
                    // Ensure it's at least 1, and positive.
                    totalArpeggioRepetitions = Math.Max(1, totalArpeggioRepetitions);

                    // Console.WriteLine($"Chord: {chord.MidiPitches.Count}, SlotDur: {currentChordSlotDuration:F2}s, PassDur: {durationOfOneArpeggioPass:F2}s, Reps: {totalArpeggioRepetitions}");


                    for (int rep = 0; rep < totalArpeggioRepetitions; rep++)
                    {
                        double currentArpeggioNoteOffset = 0.0; // Offset *within* this single pass

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
                        // Advance currentPlaybackTime by the duration of one pass for the next repetition
                        currentPlaybackTime += durationOfOneArpeggioPass;
                    }
                }
                else // Block chord playback (or single note)
                {
                    double individualNoteDuration = currentChordSlotDuration * 0.9; // Block chords fill the entire slot duration

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
                    // Advance playback time by the full chord slot duration
                    currentPlaybackTime += currentChordSlotDuration;
                }
            }

            stylizedEvents.Sort((e1, e2) => e1.StartTime.CompareTo(e2.StartTime));


            return new StylizedChordProgression(stylizedEvents, absoluteProgression.Name, globalTransposeOffset);
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