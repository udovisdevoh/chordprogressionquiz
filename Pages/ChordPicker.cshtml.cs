// Pages/ChordPicker.cshtml.cs
using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace ChordProgressionQuiz.Pages
{
    public class ChordPickerModel : PageModel
    {
        private readonly ChordProgressionService _chordService;
        private readonly ILogger<ChordPickerModel> _logger;
        private readonly Random _randomLocal;

        public ChordProgression RandomProgression { get; set; }
        public AbsoluteChordProgression AbsoluteProgression { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentProgressionIndex { get; set; } = -1;

        public ChordProgressionService ChordService { get; } // Public property for Razor page access

        public ChordPickerModel(ChordProgressionService chordService, ILogger<ChordPickerModel> logger)
        {
            _chordService = chordService;
            _logger = logger;
            _randomLocal = new Random();
            ChordService = chordService; // Assign to the public property
        }

        public void OnGet(int? index)
        {
            int totalProgressions = _chordService.GetProgressionCount();

            if (totalProgressions == 0)
            {
                _logger.LogWarning("No chord progressions loaded. The JSON file might be empty or missing.");
                RandomProgression = null;
                AbsoluteProgression = null;
                CurrentProgressionIndex = -1;
                return;
            }

            if (index.HasValue && index.Value >= 0 && index.Value < totalProgressions)
            {
                CurrentProgressionIndex = index.Value;
            }
            else
            {
                // If no valid index is provided (e.g., first load or "Get Random Progression" clicked),
                // pick a random one and set its index.
                CurrentProgressionIndex = _randomLocal.Next(totalProgressions);
            }

            RandomProgression = _chordService.GetChordProgressionByIndex(CurrentProgressionIndex);

            if (RandomProgression != null)
            {
                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(RandomProgression, 4);
            }
            else
            {
                _logger.LogWarning($"Failed to retrieve chord progression at index {CurrentProgressionIndex}. The list might be empty or an invalid index was requested.");
                CurrentProgressionIndex = -1;
            }
        }

        public IActionResult OnPostNext()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0)
            {
                return RedirectToPage();
            }

            // Increment the index, loop back to 0 if at the end
            int nextIndex = (CurrentProgressionIndex + 1) % totalProgressions;
            return RedirectToPage(new { index = nextIndex });
        }

        /// <summary>
        /// Handles the POST request for navigating to the previous chord progression.
        /// </summary>
        public IActionResult OnPostPrevious()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0)
            {
                return RedirectToPage();
            }

            // Decrement the index, loop to the last if at the beginning
            int previousIndex = (CurrentProgressionIndex - 1 + totalProgressions) % totalProgressions;
            return RedirectToPage(new { index = previousIndex });
        }

        /// <summary>
        /// Handles the POST request for navigating to the first chord progression.
        /// </summary>
        public IActionResult OnPostFirst()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0)
            {
                return RedirectToPage();
            }

            // Navigate to the first index (0)
            return RedirectToPage(new { index = 0 });
        }

        /// <summary>
        /// Handles the POST request for navigating to the last chord progression.
        /// </summary>
        public IActionResult OnPostLast()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0)
            {
                return RedirectToPage();
            }

            // Navigate to the last index (totalProgressions - 1)
            return RedirectToPage(new { index = totalProgressions - 1 });
        }
    }
}
