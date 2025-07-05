// Models/StylizedChordProgression.cs
using System.Collections.Generic;

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

        public StylizedChordProgression()
        {
            MidiEvents = new List<StylizedMidiEvent>();
        }

        public StylizedChordProgression(IEnumerable<StylizedMidiEvent> midiEvents, string name = null)
        {
            MidiEvents = new List<StylizedMidiEvent>(midiEvents);
            Name = name;
        }
    }
}