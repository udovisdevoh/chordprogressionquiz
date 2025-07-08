// Pages/ChordPicker.cshtml.cs
using ChordProgprogressionQuiz.Services;
using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChordProgressionQuiz.Pages
{
    public class ChordPickerModel : PageModel
    {
        private readonly ChordProgressionService _chordService;
        private readonly ChordStylingService _chordStylingService;
        private readonly ILogger<ChordPickerModel> _logger;
        private readonly Random _randomLocal;

        public ChordProgression RandomProgression { get; set; }
        public AbsoluteChordProgression AbsoluteProgression { get; set; }
        public StylizedChordProgression StylizedProgression { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentProgressionIndex { get; set; } = -1;

        [BindProperty(SupportsGet = true)]
        public bool EnableStylizedPlayback { get; set; } // This will be bound to the checkbox's state

        [BindProperty(SupportsGet = true)]
        public bool LoopPlayback { get; set; } // NEW: For preserving loop state

        public ChordProgressionService ChordService { get; }

        public ChordPickerModel(ChordProgressionService chordService, ChordStylingService chordStylingService, ILogger<ChordPickerModel> logger)
        {
            _chordService = chordService;
            _chordStylingService = chordStylingService;
            _logger = logger;
            _randomLocal = new Random();
            ChordService = chordService;
        }

        public void OnGet(int? index)
        {
            int totalProgressions = _chordService.GetProgressionCount();

            if (totalProgressions == 0)
            {
                _logger.LogWarning("No chord progressions loaded. The JSON file might be empty or missing.");
                RandomProgression = null;
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

            RandomProgression = _chordService.GetChordProgressionByIndex(CurrentProgressionIndex);

            if (RandomProgression != null)
            {
                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(RandomProgression, 4);

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
            }
            else
            {
                _logger.LogWarning($"Failed to retrieve chord progression at index {CurrentProgressionIndex}. The list might be empty or an invalid index was requested.");
                CurrentProgressionIndex = -1;
                StylizedProgression = null;
            }
        }

        public IActionResult OnPostNext()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int nextIndex = (CurrentProgressionIndex + 1) % totalProgressions;
            return RedirectToPage(new { index = nextIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback }); // NEW: Pass LoopPlayback
        }

        public IActionResult OnPostPrevious()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int previousIndex = (CurrentProgressionIndex - 1 + totalProgressions) % totalProgressions;
            return RedirectToPage(new { index = previousIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback }); // NEW: Pass LoopPlayback
        }

        public IActionResult OnPostFirst()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            return RedirectToPage(new { index = 0, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback }); // NEW: Pass LoopPlayback
        }

        public IActionResult OnPostLast()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            return RedirectToPage(new { index = totalProgressions - 1, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback }); // NEW: Pass LoopPlayback
        }

        /// <summary>
        /// Handles the POST request for toggling stylized playback.
        /// The EnableStylizedPlayback property will already be bound to the new state of the checkbox.
        /// </summary>
        public IActionResult OnPostToggleStylizedPlayback()
        {
            // Simply redirect back, passing the *already bound* EnableStylizedPlayback state.
            // No need to toggle it with '!' here, as it reflects the checkbox's new state.
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback }); // NEW: Pass LoopPlayback
        }

        // NEW: Handler for toggling LoopPlayback.
        // This is necessary because the LoopPlayback checkbox is now part of a form,
        // and we want its state to be persisted via a POST request.
        public IActionResult OnPostToggleLoopPlayback()
        {
            // LoopPlayback is already bound to the new state of the checkbox by ASP.NET Core
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback });
        }
    }
}