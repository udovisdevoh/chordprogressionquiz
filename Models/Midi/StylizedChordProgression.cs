// Models/Midi/StylizedChordProgression.cs
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Models
{
    /// <summary>
    /// Represents an entire chord progression as a flat list of timed MIDI events,
    /// ready for direct playback after styling has been applied.
    /// </summary>
    public class StylizedChordProgression
    {
        /// <summary>
        /// A sequence of individual MIDI note events, ordered by their StartTime.
        /// </summary>
        public List<StylizedMidiEvent> MidiEvents { get; set; }

        /// <summary>
        /// An optional name or description for this stylized progression.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The global MIDI transpose offset applied to all pitches in this stylized progression.
        /// </summary>
        public int GlobalTransposeOffset { get; set; } = 0; // NEW: Property to store the transpose offset

        public StylizedChordProgression()
        {
            MidiEvents = new List<StylizedMidiEvent>();
        }

        public StylizedChordProgression(IEnumerable<StylizedMidiEvent> midiEvents, string name = null, int globalTransposeOffset = 0)
        {
            MidiEvents = new List<StylizedMidiEvent>(midiEvents);
            Name = name;
            GlobalTransposeOffset = globalTransposeOffset; // NEW: Initialize the offset
        }
    }
}