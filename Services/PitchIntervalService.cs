// D:\users\Anonymous\Documents\C Sharp\ChordProgressionQuiz\Services\PitchIntervalService.cs
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

        public PitchIntervalService()
        {
            _random = new Random();
            // This list is now a static definition of all possible intervals.
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

        /// <summary>
        /// Gets a fully randomized interval from all possibilities.
        /// </summary>
        public PitchInterval GetRandomInterval()
        {
            int randomIndex = _random.Next(_intervalDefinitions.Count);
            int semitones = _intervalDefinitions[randomIndex].Semitones;
            return GetRandomIntervalOfSemitone(semitones);
        }

        /// <summary>
        /// Creates a PitchInterval with a random base note for a specific semitone difference.
        /// </summary>
        public PitchInterval GetRandomIntervalOfSemitone(int semitones)
        {
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
                IntervalName = _intervalDefinitions.First(i => i.Semitones == semitones).IntervalName,
                Direction = isAscending ? "Ascending" : "Descending"
            };
        }

        public List<string> GetAllIntervalNames()
        {
            return _intervalDefinitions.Select(i => i.IntervalName).ToList();
        }

        /// <summary>
        /// Returns the static list of all interval definitions (name and semitones).
        /// </summary>
        public List<PitchInterval> GetAllIntervalDefinitions()
        {
            return _intervalDefinitions.ToList();
        }
    }
}