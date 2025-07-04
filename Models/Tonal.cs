// Models/Tonal.cs
using System.Text.Json.Serialization;

namespace ChordProgressionQuiz.Models
{
    public class Tonal
    {
        [JsonPropertyName("roman_numerals")]
        public string RomanNumerals { get; set; }

        [JsonPropertyName("relative_to")]
        public string RelativeTo { get; set; }
    }
}