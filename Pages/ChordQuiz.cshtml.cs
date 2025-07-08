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

        // Properties to hold the *actual* answers, serialized to JS as hidden data
        public string ActualRomanNumeralsJson { get; set; }
        public string ActualModalRomanNumeralsJson { get; set; } // If a modal exists
        public string ActualKeysExampleJson { get; set; }
        public string ActualSongNameJson { get; set; }
        public string ActualRelativeToJson { get; set; }


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
                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(QuizProgression, 4);

                if (EnableStylizedPlayback)
                {
                    StylizedProgression = _chordStylingService.ApplyRandomStyling(AbsoluteProgression);
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
                    StylizedProgression = new StylizedChordProgression(basicStylizedEvents, AbsoluteProgression.Name);
                }

                // Serialize actual answers for JavaScript, but keep them hidden from direct Razor display
                ActualSongNameJson = JsonSerializer.Serialize(QuizProgression.Song);
                ActualKeysExampleJson = JsonSerializer.Serialize(QuizProgression.KeysExample);
                ActualRelativeToJson = JsonSerializer.Serialize(QuizProgression.Tonal?.RelativeTo); // Assuming Tonal always exists if RomanNumerals are present
                ActualRomanNumeralsJson = JsonSerializer.Serialize(QuizProgression.Tonal?.RomanNumerals?.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                if (QuizProgression.Modal != null && QuizProgression.Modal.Any())
                {
                    // Serialize list of modal objects, then extract roman numerals from them in JS
                    ActualModalRomanNumeralsJson = JsonSerializer.Serialize(
                        QuizProgression.Modal.Select(m => new { RomanNumerals = m.RomanNumerals, RelativeTo = m.RelativeTo }).ToList()
                    );
                }
                else
                {
                    ActualModalRomanNumeralsJson = JsonSerializer.Serialize(new List<object>()); // Empty list if no modal
                }
            }
            else
            {
                _logger.LogWarning($"Failed to retrieve chord progression at index {CurrentProgressionIndex}. The list might be empty or an invalid index was requested.");
                CurrentProgressionIndex = -1;
                StylizedProgression = null;
            }
        }

        // Action methods for navigation (similar to ChordPicker)
        public IActionResult OnPostNext()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int nextIndex = (CurrentProgressionIndex + 1) % totalProgressions;
            return RedirectToPage(new { index = nextIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }

        public IActionResult OnPostPrevious()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int previousIndex = (CurrentProgressionIndex - 1 + totalProgressions) % totalProgressions;
            return RedirectToPage(new { index = previousIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }

        public IActionResult OnPostFirst()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            return RedirectToPage(new { index = 0, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }

        public IActionResult OnPostLast()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            return RedirectToPage(new { index = totalProgressions - 1, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }

        public IActionResult OnPostRandom()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            return RedirectToPage(new { index = _randomLocal.Next(totalProgressions), EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }

        public IActionResult OnPostToggleStylizedPlayback()
        {
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }

        public IActionResult OnPostToggleLoopPlayback()
        {
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }
    }
}