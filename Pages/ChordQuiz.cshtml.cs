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

        [BindProperty]
        public List<string> SelectedModes { get; set; } = new();

        public IReadOnlyList<string> AvailableModes => _chordService.GetAvailableModes();

        public bool HasProgressions => _chordService.GetProgressionCount() > 0;

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

        public void OnGet(int? index, string modes = null)
        {
            SelectedModes = string.IsNullOrWhiteSpace(modes)
                ? new List<string>()
                : modes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var eligibleProgressionIndices = GetEligibleProgressionIndices();

            if (eligibleProgressionIndices.Count == 0)
            {
                if (_chordService.GetProgressionCount() == 0)
                {
                    _logger.LogWarning("No chord progressions loaded. The JSON file might be empty or missing.");
                }
                QuizProgression = null;
                AbsoluteProgression = null;
                StylizedProgression = null;
                CurrentProgressionIndex = -1;
                return;
            }

            if (index.HasValue && eligibleProgressionIndices.Contains(index.Value))
            {
                CurrentProgressionIndex = index.Value;
            }
            else
            {
                CurrentProgressionIndex = eligibleProgressionIndices[_randomLocal.Next(eligibleProgressionIndices.Count)];
            }

            QuizProgression = _chordService.GetChordProgressionByIndex(CurrentProgressionIndex);

            if (QuizProgression != null)
            {
                var useModalAnswers = QuizProgression.Modal != null && QuizProgression.Modal.Any();
                var answerRomanNumerals = useModalAnswers
                    ? QuizProgression.Modal.First().RomanNumerals
                    : QuizProgression.Tonal?.RomanNumerals ?? string.Empty;
                var answerKey = useModalAnswers
                    ? QuizProgression.Modal.First().RelativeTo
                    : QuizProgression.Tonal?.RelativeTo;
                var sourceKeyParts = answerKey?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                var selectedKey = SelectedKey == "Random" || !AvailableKeys.Contains(SelectedKey)
                    ? AvailableKeys[_randomLocal.Next(AvailableKeys.Count)]
                    : SelectedKey;
                PlayedKey = $"{selectedKey} {string.Join(" ", sourceKeyParts.Skip(1))}".TrimEnd();

                var progressionInPlaybackKey = new ChordProgression
                {
                    Song = QuizProgression.Song,
                    Tonal = new Tonal
                    {
                        RomanNumerals = answerRomanNumerals,
                        RelativeTo = PlayedKey
                    }
                };
                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(progressionInPlaybackKey, 4);

                if (EnableStylizedPlayback)
                {
                    // NEW: Pass PlayArpeggioTwiceAsLong to ChordStylingService
                    StylizedProgression = _chordStylingService.ApplyRandomStyling(AbsoluteProgression, PlayArpeggioTwiceAsLong, false);
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
                    StylizedProgression = new StylizedChordProgression(basicStylizedEvents, AbsoluteProgression.Name, 0);
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

        private IReadOnlyList<int> GetEligibleProgressionIndices()
        {
            var availableModes = _chordService.GetAvailableModes();
            SelectedModes = (SelectedModes ?? new List<string>())
                .Where(mode => availableModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return _chordService.GetProgressionIndicesByModes(SelectedModes);
        }

        private object GetQuizRouteValues(int index)
        {
            return new
            {
                index,
                EnableStylizedPlayback,
                LoopPlayback,
                PlayArpeggioTwiceAsLong,
                SelectedKey,
                modes = string.Join(",", SelectedModes ?? new List<string>())
            };
        }

        public IActionResult OnPostNext()
        {
            var eligibleProgressionIndices = GetEligibleProgressionIndices();
            if (eligibleProgressionIndices.Count == 0) return RedirectToPage();
            var currentPosition = eligibleProgressionIndices.ToList().IndexOf(CurrentProgressionIndex);
            var nextPosition = currentPosition < 0 ? 0 : (currentPosition + 1) % eligibleProgressionIndices.Count;
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(GetQuizRouteValues(eligibleProgressionIndices[nextPosition]));
        }

        public IActionResult OnPostPrevious()
        {
            var eligibleProgressionIndices = GetEligibleProgressionIndices();
            if (eligibleProgressionIndices.Count == 0) return RedirectToPage();
            var currentPosition = eligibleProgressionIndices.ToList().IndexOf(CurrentProgressionIndex);
            var previousPosition = currentPosition < 0
                ? eligibleProgressionIndices.Count - 1
                : (currentPosition - 1 + eligibleProgressionIndices.Count) % eligibleProgressionIndices.Count;
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(GetQuizRouteValues(eligibleProgressionIndices[previousPosition]));
        }

        public IActionResult OnPostFirst()
        {
            var eligibleProgressionIndices = GetEligibleProgressionIndices();
            if (eligibleProgressionIndices.Count == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(GetQuizRouteValues(eligibleProgressionIndices[0]));
        }

        public IActionResult OnPostLast()
        {
            var eligibleProgressionIndices = GetEligibleProgressionIndices();
            if (eligibleProgressionIndices.Count == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(GetQuizRouteValues(eligibleProgressionIndices[^1]));
        }

        public IActionResult OnPostRandom()
        {
            var eligibleProgressionIndices = GetEligibleProgressionIndices();
            if (eligibleProgressionIndices.Count == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(GetQuizRouteValues(eligibleProgressionIndices[_randomLocal.Next(eligibleProgressionIndices.Count)]));
        }

        public IActionResult OnPostToggleStylizedPlayback()
        {
            // NEW: Preserve PlayArpeggioTwiceAsLong on toggle
            return RedirectToPage(GetQuizRouteValues(CurrentProgressionIndex));
        }

        public IActionResult OnPostToggleLoopPlayback()
        {
            // NEW: Preserve PlayArpeggioTwiceAsLong on toggle
            return RedirectToPage(GetQuizRouteValues(CurrentProgressionIndex));
        }

        // NEW: Toggle for PlayArpeggioTwiceAsLong
        public IActionResult OnPostTogglePlayArpeggioTwiceAsLong()
        {
            return RedirectToPage(GetQuizRouteValues(CurrentProgressionIndex));
        }
    }
}