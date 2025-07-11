using ChordProgressionQuiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Services
{
    public class PitchIntervalService
    {
        private readonly Random _random;
        private readonly IReadOnlyDictionary<int, string> _intervalNames;

        public PitchIntervalService()
        {
            _random = new Random();
            _intervalNames = new Dictionary<int, string>
            {
                { 1, "Minor Second" },
                { 2, "Major Second" },
                { 3, "Minor Third" },
                { 4, "Major Third" },
                { 5, "Perfect Fourth" },
                { 6, "Tritone" },
                { 7, "Perfect Fifth" },
                { 8, "Minor Sixth" },
                { 9, "Major Sixth" },
                { 10, "Minor Seventh" },
                { 11, "Major Seventh" },
                { 12, "Octave" }
            };
        }

        // MODIFIED: This function now fully randomizes the base note.
        public PitchInterval GetRandomInterval()
        {
            int semitones = _random.Next(1, 13);
            // Use a wider and more random range for the starting note (E3 to B4)
            int startNote = _random.Next(52, 72);
            bool isAscending = _random.NextDouble() > 0.5;
            int endNote = isAscending ? (startNote + semitones) : (startNote - semitones);

            // This ensures the interval doesn't go too high or low.
            if (endNote < 48 || endNote > 84)
            {
                startNote = 60; // Reset to a safe C4 if out of comfortable range
                endNote = isAscending ? (startNote + semitones) : (startNote - semitones);
            }

            return new PitchInterval
            {
                StartNoteMidi = startNote,
                EndNoteMidi = endNote,
                Semitones = semitones,
                IntervalName = _intervalNames[semitones],
                Direction = isAscending ? "Ascending" : "Descending"
            };
        }

        public List<string> GetAllIntervalNames()
        {
            return _intervalNames.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }

        // MODIFIED: This function now also randomizes the base note for each generated interval.
        public List<PitchInterval> GetAllPossibleIntervals()
        {
            var allIntervals = new List<PitchInterval>();

            foreach (var kvp in _intervalNames.OrderBy(kv => kv.Key))
            {
                int semitones = kvp.Key;
                // Use a wider and more random range for the starting note (E3 to B4)
                int startNote = _random.Next(52, 72);
                bool isAscending = _random.NextDouble() > 0.5;
                int endNote = isAscending ? (startNote + semitones) : (startNote - semitones);

                if (endNote < 48 || endNote > 84)
                {
                    startNote = 60; // Reset to a safe C4 if out of comfortable range
                    endNote = isAscending ? (startNote + semitones) : (startNote - semitones);
                }

                allIntervals.Add(new PitchInterval
                {
                    StartNoteMidi = startNote,
                    EndNoteMidi = endNote,
                    Semitones = semitones,
                    IntervalName = kvp.Value,
                    Direction = isAscending ? "Ascending" : "Descending"
                });
            }
            return allIntervals;
        }
    }
}