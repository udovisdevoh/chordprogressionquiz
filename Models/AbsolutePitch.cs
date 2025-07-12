namespace ChordProgressionQuiz.Models
{
    public class AbsolutePitch
    {
        /// <summary>
        /// The MIDI number of the note (e.g., 60 for C4).
        /// </summary>
        public int MidiValue { get; set; }

        /// <summary>
        /// The name of the note (e.g., "C", "F#").
        /// </summary>
        public string NoteName { get; set; }
    }
}