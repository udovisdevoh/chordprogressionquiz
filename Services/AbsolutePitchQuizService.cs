using ChordProgressionQuiz.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Services
{
    public class AbsolutePitchQuizService
    {
        private readonly Random _random;
        private readonly IReadOnlyList<string> _noteNames;

        public AbsolutePitchQuizService()
        {
            _random = new Random();
            _noteNames = new List<string>
            {
                "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
            }.AsReadOnly();
        }

        /// <summary>
        /// Gets a single random note within a playable range.
        /// </summary>
        /// <returns>An AbsolutePitch object representing the random note.</returns>
        public AbsolutePitch GetRandomNote()
        {
            // MIDI range from C3 (48) to B5 (83) - a common and comfortable range for piano
            int midiValue = _random.Next(48, 84);
            string noteName = _noteNames[midiValue % 12];

            return new AbsolutePitch
            {
                MidiValue = midiValue,
                NoteName = noteName
            };
        }

        /// <summary>
        /// Gets the list of all 12 unique note names.
        /// </summary>
        /// <returns>A list of strings representing the note names.</returns>
        public List<string> GetAllNoteNames()
        {
            return _noteNames.ToList();
        }
    }
}