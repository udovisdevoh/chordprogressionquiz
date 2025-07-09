// D:\users\Anonymous\Documents\C Sharp\ChordProgressionQuiz\Pages\ChordPicker.cshtml.cs
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
        public bool LoopPlayback { get; set; } // For preserving loop state

        [BindProperty(SupportsGet = true)] // NEW: Add PlayArpeggioTwiceAsLong property
        public bool PlayArpeggioTwiceAsLong { get; set; } = false;


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
                    // FIXED: Pass the new playArpeggioTwiceAsLong parameter to ApplyRandomStyling
                    StylizedProgression = _chordStylingService.ApplyRandomStyling(AbsoluteProgression, PlayArpeggioTwiceAsLong);
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
                    // When not stylized, globalTransposeOffset is 0
                    StylizedProgression = new StylizedChordProgression(basicStylizedEvents, AbsoluteProgression.Name, 0);
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
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = nextIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }

        public IActionResult OnPostPrevious()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            int previousIndex = (CurrentProgressionIndex - 1 + totalProgressions) % totalProgressions;
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = previousIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }

        public IActionResult OnPostFirst()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = 0, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }

        public IActionResult OnPostLast()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0) return RedirectToPage();
            // NEW: Preserve PlayArpeggioTwiceAsLong on navigation
            return RedirectToPage(new { index = totalProgressions - 1, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }

        public IActionResult OnPostToggleStylizedPlayback()
        {
            // NEW: Preserve PlayArpeggioTwiceAsLong on toggle
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }

        public IActionResult OnPostToggleLoopPlayback()
        {
            // NEW: Preserve PlayArpeggioTwiceAsLong on toggle
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }

        // NEW: Toggle for PlayArpeggioTwiceAsLong (for ChordPicker if feature added later)
        public IActionResult OnPostTogglePlayArpeggioTwiceAsLong()
        {
            return RedirectToPage(new { index = CurrentProgressionIndex, EnableStylizedPlayback = EnableStylizedPlayback, LoopPlayback = LoopPlayback, PlayArpeggioTwiceAsLong = PlayArpeggioTwiceAsLong });
        }
    }
}