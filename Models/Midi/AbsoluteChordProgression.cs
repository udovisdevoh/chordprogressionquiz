// Models/AbsoluteChordProgression.cs
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Models
{
    /// <summary>
    /// Represents a chord progression as a sequence of MidiChord objects.
    /// This model holds the absolute, pitch-specific representation.
    /// </summary>
    public class AbsoluteChordProgression
    {
        /// <summary>
        /// Gets or sets the list of MidiChord objects that form the progression.
        /// </summary>
        public List<MidiChord> Chords { get; set; }

        /// <summary>
        /// An optional name or description for this specific absolute progression (e.g., "C Major I-IV-V").
        /// </summary>
        public string Name { get; set; }

        public AbsoluteChordProgression()
        {
            Chords = new List<MidiChord>();
        }

        public AbsoluteChordProgression(IEnumerable<MidiChord> chords, string name = null)
        {
            Chords = new List<MidiChord>(chords);
            Name = name;
        }

        /// <summary>
        /// Returns a string representation of the absolute chord progression.
        /// </summary>
        /// <returns>A string showing the sequence of chords.</returns>
        public override string ToString()
        {
            if (!Chords.Any())
            {
                return "Empty Progression";
            }
            return $"{Name ?? "Progression"}: {string.Join(" | ", Chords.Select(c => c.ToString()))}";
        }
    }
}