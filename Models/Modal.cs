// Models/Modal.cs
using System.Text.Json.Serialization;

namespace ChordProgressionQuiz.Models
{
    public class Modal
    {
        [JsonPropertyName("roman_numerals")]
        public string RomanNumerals { get; set; }

        [JsonPropertyName("relative_to")]
        public string RelativeTo { get; set; }
    }
}