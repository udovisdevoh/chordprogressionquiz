// Models/StylizedMidiEvent.cs
namespace ChordProgressionQuiz.Models
{
    /// <summary>
    /// Represents a single MIDI note event with its pitch, start time, and duration.
    /// This is used to build a sequence of individual notes for stylized playback.
    /// </summary>
    public class StylizedMidiEvent
    {
        /// <summary>
        /// The MIDI pitch number (0-127).
        /// </summary>
        public int Pitch { get; set; }

        /// <summary>
        /// The start time of the note in seconds, relative to the beginning of the progression playback.
        /// </summary>
        public double StartTime { get; set; }

        /// <summary>
        /// The duration of the note in seconds.
        /// </summary>
        public double Duration { get; set; }
    }
}