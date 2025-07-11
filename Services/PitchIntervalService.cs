using ChordProgressionQuiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Services
{
    /// <summary>
    /// Generates random musical intervals for the pitch interval quiz.
    /// </summary>
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

        /// <summary>
        /// Generates a single random interval, used for the initial page load.
        /// </summary>
        /// <returns>A PitchInterval object for the quiz.</returns>
        public PitchInterval GetRandomInterval()
        {
            // Pick a random interval size from 1 to 12 semitones
            int semitones = _random.Next(1, 13); // From 1 (m2) to 12 (P8)

            // Pick a random starting note within a comfortable range (C4 to C5)
            int startNote = _random.Next(60, 73);

            // Pick a random direction
            bool isAscending = _random.NextDouble() > 0.5;

            int endNote = isAscending ? (startNote + semitones) : (startNote - semitones);

            // If the calculated end note goes too far, reset the start note to a safe default.
            if (endNote < 48 || endNote > 84) // Keep notes roughly between C3 and C6
            {
                startNote = 60; // Reset to a safe C4
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

        /// <summary>
        /// Gets an ordered list of all possible interval names for generating UI buttons.
        /// </summary>
        /// <returns>A list of interval name strings.</returns>
        public List<string> GetAllIntervalNames()
        {
            return _intervalNames.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }

        /// <summary>
        /// NEW: Generates a list of all 12 possible interval types.
        /// This is used by the front-end to perform its own weighted random selection.
        /// </summary>
        /// <returns>A list containing one of each possible PitchInterval.</returns>
        public List<PitchInterval> GetAllPossibleIntervals()
        {
            var allIntervals = new List<PitchInterval>();
            int startNote = 60; // Use a consistent C4 for all base notes

            foreach (var kvp in _intervalNames.OrderBy(kv => kv.Key))
            {
                int semitones = kvp.Key;
                bool isAscending = _random.NextDouble() > 0.5;
                int endNote = isAscending ? (startNote + semitones) : (startNote - semitones);

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