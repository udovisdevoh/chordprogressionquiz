// Models/Midi/StylizedMidiEvent.cs
namespace ChordProgressionQuiz.Models
{
    /// <summary>
    /// Represents a single MIDI note event with its pitch, start time, duration, and instrument.
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

        /// <summary>
        /// The General MIDI Program Change number for this note's instrument (0-127).
        /// </summary>
        public int InstrumentProgram { get; set; } = 0; // Default to Acoustic Grand Piano (GM Program 0/1)
    }
}