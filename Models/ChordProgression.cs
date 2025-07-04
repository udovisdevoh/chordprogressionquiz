// Models/ChordProgression.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChordProgressionQuiz.Models
{
    public class ChordProgression
    {
        [JsonPropertyName("song")]
        public string Song { get; set; }

        [JsonPropertyName("keys_example")]
        public string KeysExample { get; set; }

        [JsonPropertyName("tonal")]
        public Tonal Tonal { get; set; }

        [JsonPropertyName("modal")]
        public List<Modal> Modal { get; set; }

        [JsonPropertyName("substitution_group")]
        public int? SubstitutionGroup { get; set; }

        [JsonPropertyName("palindromic_group")]
        public int? PalindromicGroup { get; set; }

        [JsonPropertyName("rotation_group")]
        public int? RotationGroup { get; set; }
    }
}