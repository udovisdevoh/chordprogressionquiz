// Services/PitchIntervalService.cs
using ChordProgressionQuiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Services
{
    public class PitchIntervalService
    {
        private readonly Random _random;
        private readonly IReadOnlyList<PitchInterval> _intervalDefinitions;

        // Note mapping for reference pitch logic
        private static readonly Dictionary<string, int> NoteNameToMidiClass = new Dictionary<string, int>
        {
            {"C", 0}, {"C#", 1}, {"Db", 1}, {"D", 2}, {"D#", 3}, {"Eb", 3}, {"E", 4}, {"F", 5}, {"F#", 6}, {"Gb", 6},
            {"G", 7}, {"G#", 8}, {"Ab", 8}, {"A", 9}, {"A#", 10}, {"Bb", 10}, {"B", 11}
        };

        private static readonly string[] MidiClassToNoteName = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        private string MidiToNoteName(int midi)
        {
            if (midi < 0 || midi > 127) return "";
            return MidiClassToNoteName[midi % 12];
        }


        public PitchIntervalService()
        {
            _random = new Random();
            _intervalDefinitions = new List<PitchInterval>
            {
                new PitchInterval { Semitones = 1, IntervalName = "Minor Second" },
                new PitchInterval { Semitones = 2, IntervalName = "Major Second" },
                new PitchInterval { Semitones = 3, IntervalName = "Minor Third" },
                new PitchInterval { Semitones = 4, IntervalName = "Major Third" },
                new PitchInterval { Semitones = 5, IntervalName = "Perfect Fourth" },
                new PitchInterval { Semitones = 6, IntervalName = "Tritone" },
                new PitchInterval { Semitones = 7, IntervalName = "Perfect Fifth" },
                new PitchInterval { Semitones = 8, IntervalName = "Minor Sixth" },
                new PitchInterval { Semitones = 9, IntervalName = "Major Sixth" },
                new PitchInterval { Semitones = 10, IntervalName = "Minor Seventh" },
                new PitchInterval { Semitones = 11, IntervalName = "Major Seventh" },
                new PitchInterval { Semitones = 12, IntervalName = "Octave" }
            }.AsReadOnly();
        }

        public PitchInterval GetRandomInterval(string referencePitch = "F")
        {
            int randomIndex = _random.Next(_intervalDefinitions.Count);
            int semitones = _intervalDefinitions[randomIndex].Semitones;
            return GetRandomIntervalOfSemitone(semitones, referencePitch);
        }

        public PitchInterval GetRandomIntervalOfSemitone(int semitones, string referencePitch = "F")
        {
            int startNote = _random.Next(52, 72); // E3 to B4
            bool isAscending = _random.NextDouble() > 0.5;
            int endNote = isAscending ? (startNote + semitones) : (startNote - semitones);
            string referencePitchPosition = null;

            // --- Reference Pitch Logic ---
            if (referencePitch != "Random" && NoteNameToMidiClass.TryGetValue(referencePitch, out int targetMidiClass))
            {
                bool anchorStartNote = _random.NextDouble() > 0.5;

                // FIXED: Determine if the reference note will be the higher or lower one.
                // isAscending means start is low, end is high.
                // anchorStartNote means the reference pitch is applied to the start note.
                bool isRefNoteTheLowerNote = (isAscending && anchorStartNote) || (!isAscending && !anchorStartNote);
                referencePitchPosition = isRefNoteTheLowerNote ? "Low" : "High";

                int noteToAnchor = anchorStartNote ? startNote : endNote;
                int currentMidiClass = noteToAnchor % 12;
                int shift = targetMidiClass - currentMidiClass;

                // Apply the same shift to both notes to preserve the interval
                startNote += shift;
                endNote += shift;
            }

            // Ensure notes are in a playable range after potential shifting
            while ((startNote < 48 || startNote > 84 || endNote < 48 || endNote > 84) && (startNote >= 12 && endNote >= 12 && startNote <= 115 && endNote <= 115))
            {
                if (startNote > 66 || endNote > 66)
                {
                    startNote -= 12;
                    endNote -= 12;
                }
                else
                {
                    startNote += 12;
                    endNote += 12;
                }
            }

            // Final safety clamp
            startNote = Math.Clamp(startNote, 36, 96);
            endNote = Math.Clamp(endNote, 36, 96);


            return new PitchInterval
            {
                StartNoteMidi = startNote,
                EndNoteMidi = endNote,
                StartNoteName = MidiToNoteName(startNote),
                EndNoteName = MidiToNoteName(endNote),
                Semitones = semitones,
                IntervalName = _intervalDefinitions.First(i => i.Semitones == semitones).IntervalName,
                Direction = isAscending ? "Ascending" : "Descending",
                ReferencePitchPosition = referencePitchPosition // Pass position to the client
            };
        }

        public List<string> GetAllIntervalNames()
        {
            return _intervalDefinitions.Select(i => i.IntervalName).ToList();
        }

        public List<PitchInterval> GetAllIntervalDefinitions()
        {
            return _intervalDefinitions.ToList();
        }
    }
}