// Models/MidiChord.cs
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Models
{
    /// <summary>
    /// Represents a chord as a collection of MIDI pitches (0-127).
    /// </summary>
    public class MidiChord
    {
        /// <summary>
        /// Gets or sets the list of MIDI pitches that form the chord.
        /// Each integer should be between 0 and 127, inclusive.
        /// </summary>
        public List<int> MidiPitches { get; set; }

        public MidiChord()
        {
            MidiPitches = new List<int>();
        }

        public MidiChord(IEnumerable<int> pitches)
        {
            // Ensure pitches are within the valid MIDI range (0-127)
            MidiPitches = pitches.Where(p => p >= 0 && p <= 127).ToList();
        }

        /// <summary>
        /// Returns a string representation of the MIDI chord (e.g., "60, 64, 67").
        /// </summary>
        /// <returns>A comma-separated string of MIDI pitches.</returns>
        public override string ToString()
        {
            return string.Join(", ", MidiPitches);
        }

        /// <summary>
        /// Returns a formatted string representation with a label (e.g., "MIDI Chord: [60, 64, 67]").
        /// </summary>
        /// <returns>A formatted string.</returns>
        public string ToFormattedString()
        {
            return $"MIDI Chord: [{string.Join(", ", MidiPitches)}]";
        }
    }
}