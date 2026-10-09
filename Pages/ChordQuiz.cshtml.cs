// D:\users\Anonymous\Documents\C Sharp\ChordProgressionQuiz\Pages\ChordQuiz.cshtml.cs
using ChordProgprogressionQuiz.Services;
using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // For Json.Serialize

namespace ChordProgressionQuiz.Pages
{
    public class ChordQuizModel : PageModel
    {
        private readonly ChordProgressionService _chordService;
        private readonly ChordStylingService _chordStylingService;
        private readonly ILogger<ChordQuizModel> _logger;
        private readonly Random _randomLocal;

        public ChordProgression QuizProgression { get; set; }
        public AbsoluteChordProgression AbsoluteProgression { get; set; }
        public StylizedChordProgression StylizedProgression { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentProgressionIndex { get; set; } = -1;

        [BindProperty(SupportsGet = true)]
        public bool EnableStylizedPlayback { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public bool LoopPlayback { get; set; } = true;

        [BindProperty(SupportsGet = true)] // NEW: Option to play arpeggio twice as long
        public bool PlayArpeggioTwiceAsLong { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public string SelectedKey { get; set; } = "Random";

        public string PlayedKey { get; private set; }

        public static IReadOnlyList<string> AvailableKeys { get; } = new[]
        {
            "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"
        };


        // Properties to hold the *actual* answers, serialized to JS as hidden data
        public string ActualTonalRomanNumeralsJson { get; set; }
        public string ActualModalRomanNumeralsJson { get; set; }
        public string ActualKeysExampleJson { get; set; }
        public string ActualSongNameJson { get; set; }
        public string ActualRelativeToJson { get; set; }

        // This will hold the preferred answer sequence for the quiz check
        public string ActualQuizAnswersJson { get; set; }


        public ChordQuizModel(ChordProgressionService chordService, ChordStylingService chordStylingService, ILogger<ChordQuizModel> logger)
        {
            _chordService = chordService;
            _chordStylingService = chordStylingService;
            _logger = logger;
            _randomLocal = new Random();
        }

        public void OnGet(int? index)
        {
            int totalProgressions = _chordService.GetProgressionCount();

            if (totalProgressions == 0)
            {
                _logger.LogWarning("No chord progressions loaded. The JSON file might be empty or missing.");
                QuizProgression = null;
                AbsoluteProgression = null;
                StylizedProgression = null;
                CurrentProgressionIndex = -1;
                return;
            }

            if (index.HasValue && index.Value >= 0 && index.Value < totalProgressions)
            {
                CurrentProgressionIndex = index.Value;
            }
            else
            {
                CurrentProgressionIndex = _randomLocal.Next(totalProgressions);
            }

            QuizProgression = _chordService.GetChordProgressionByIndex(CurrentProgressionIndex);

            if (QuizProgression != null)
            {
                var sourceKeyParts = QuizProgression.Tonal?.RelativeTo?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var sourceKey = sourceKeyParts.FirstOrDefault() ?? "C";
                var selectedKey = SelectedKey == "Random" || !AvailableKeys.Contains(SelectedKey)
                    ? AvailableKeys[_randomLocal.Next(AvailableKeys.Count)]
                    : SelectedKey;
                PlayedKey = $"{selectedKey} {string.Join(" ", sourceKeyParts.Skip(1))}".TrimEnd();
                var keyTransposeOffset = GetKeySemitones(selectedKey) - GetKeySemitones(sourceKey);

                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(QuizProgression, 4);
                foreach (var chord in AbsoluteProgression.Chords)
                {
                    chord.MidiPitches = chord.MidiPitches
                        .Select(pitch => Math.Clamp(pitch + keyTransposeOffset, 0, 127))
                        .ToList();
                }

                if (EnableStylizedPlayback)
                {
                    // NEW: Pass PlayArpeggioTwiceAsLong to ChordStylingService
                    StylizedProgression = _chordStylingService.ApplyRandomStyling(AbsoluteProgression, PlayArpeggioTwiceAsLong);
                    StylizedProgression.GlobalTransposeOffset += keyTransposeOffset;
                }
                else
                {
                    var basicStylizedEvents = new List<StylizedMidiEvent>();
                    double currentTime = 0.0;
                    const double defaultNoteDuration = 1.0;

                    foreach (var chord in AbsoluteProgression.Chords)
                    {
                        if (chord.MidiPitches.Any())
                        {
                            foreach (var pitch in chord.MidiPitches)
                            {
                                basicStylizedEvents.Add(new StylizedMidiEvent
                                {
                                    Pitch = pitch,
                                    StartTime = currentTime,
                                    Duration = defaultNoteDuration
                                });
                            }
                        }
                        currentTime += defaultNoteDuration;
                    }
                    // Constructor now requires globalTransposeOffset (default to 0 if not stylized)
                    StylizedProgression = new StylizedChordProgression(basicStylizedEvents, AbsoluteProgression.Name, keyTransposeOffset);
                }

                // Serialize all answers for JavaScript to reveal/display later
                ActualSongNameJson = JsonSerializer.Serialize(QuizProgression.Song);
                ActualKeysExampleJson = JsonSerializer.Serialize(QuizProgression.KeysExample);
                ActualRelativeToJson = JsonSerializer.Serialize(QuizProgression.Tonal?.RelativeTo);

                ActualTonalRomanNumeralsJson = JsonSerializer.Serialize(QuizProgression.Tonal?.RomanNumerals?.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                if (QuizProgression.Modal != null && QuizProgression.Modal.Any())
                {
                    ActualModalRomanNumeralsJson = JsonSerializer.Serialize(
                        QuizProgression.Modal.Select(m => new { RomanNumerals = m.RomanNumerals, RelativeTo = m.RelativeTo }).ToList()
                    );
                }
                else
                {
                    ActualModalRomanNumeralsJson = JsonSerializer.Serialize(new List<object>());
                }

                string[] quizAnswerNumerals;
                if (QuizProgression.Modal != null && QuizProgression.Modal.Any())
                {
                    quizAnswerNumerals = QuizProgression.Modal.First().RomanNumerals.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                }
                else if (QuizProgression.Tonal != null && !string.IsNullOrEmpty(QuizProgression.Tonal.RomanNumerals))
                {
                    quizAnswerNumerals = QuizProgression.Tonal.RomanNumerals.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    quizAnswerNumerals = new string[] { };
                }
                ActualQuizAnswersJson = JsonSerializer.Serialize(quizAnswerNumerals);
            }
            else
            {
                _logger.LogWarning($"Failed to retrieve chord progression at index {CurrentProgressionIndex}. The list might be empty or an invalid index was requested.");
                CurrentProgressionIndex = -1;
                StylizedProgression = null;
            }
        }

        private static int GetKeySemitones(string key)
        {
            return key switch
            {
                "C" => 0, "C#" or "Db" => 1, "D" => 2, "D#" or "Eb" => 3,
                "E" => 4, "F" => 5, "F#" or "Gb" => 6, "G" => 7,
                "G#" or "Ab" => 8, "A" => 9, "A#" or "Bb" => 10, "B" => 11,
                _ => 0
            };
        }

        public IActionResult OnPostNext()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int nextIndex = (CurrentProgressionIndex + 1) % totalProgressions;
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = nextIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        public IActionResult OnPostPrevious()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int previousIndex = (CurrentProgressionIndex - 1 + totalProgressions) % totalProgressions;
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = previousIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        public IActionResult OnPostFirst()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = 0, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        public IActionResult OnPostLast()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = totalProgressions - 1, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        public IActionResult OnPostRandom()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = _randomLocal.Next(totalProgressions), EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        public IActionResult OnPostToggleStylizedPlayback()
        {
            // NEW: Preserve PlayArpeggioTwiceAsLong on toggle
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        public IActionResult OnPostToggleLoopPlayback()
        {
            // NEW: Preserve PlayArpeggioTwiceAsLong on toggle
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }

        // NEW: Toggle for PlayArpeggioTwiceAsLong
        public IActionResult OnPostTogglePlayArpeggioTwiceAsLong()
        {
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong, SelectedKey = SelectedKey });
        }
    }
}