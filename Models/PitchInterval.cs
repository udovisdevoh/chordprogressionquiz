namespace ChordProgressionQuiz.Models
{
    /// <summary>
    /// Represents a musical interval with its start/end notes and name.
    /// </summary>
    public class PitchInterval
    {
        public int StartNoteMidi { get; set; }
        public int EndNoteMidi { get; set; }
        public string StartNoteName { get; set; }
        public string EndNoteName { get; set; }
        public string IntervalName { get; set; }
        public int Semitones { get; set; }
        public string Direction { get; set; } // "Ascending" or "Descending"
        public string ReferencePitchPosition { get; set; } // NEW: "High" or "Low"
    }
}