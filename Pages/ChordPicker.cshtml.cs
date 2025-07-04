// Pages/ChordPicker.cshtml.cs
using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc; // Add this for [BindProperty] and IActionResult
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System; // Required for Random
using System.Linq; // Ensure this is present for .Any()

namespace ChordProgressionQuiz.Pages
{
    public class ChordPickerModel : PageModel
    {
        private readonly ChordProgressionService _chordService;
        private readonly ILogger<ChordPickerModel> _logger;
        private readonly Random _randomLocal; // <--- NEW: Local Random instance

        public ChordProgression RandomProgression { get; set; }
        public AbsoluteChordProgression AbsoluteProgression { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentProgressionIndex { get; set; } = -1;

        // <--- NEW: Public property to expose ChordService to the Razor page
        public ChordProgressionService ChordService { get; }

        public ChordPickerModel(ChordProgressionService chordService, ILogger<ChordPickerModel> logger)
        {
            _chordService = chordService;
            _logger = logger;
            _randomLocal = new Random(); // <--- NEW: Initialize local Random
            ChordService = chordService; // <--- NEW: Assign to public property
        }

        public void OnGet(int? index) // Make index nullable
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
                // If a valid index is provided, load that specific progression
                CurrentProgressionIndex = index.Value;
            }
            else
            {
                // If no valid index is provided (e.g., first load or "Get Random Progression" clicked),
                // pick a random one and set its index.
                CurrentProgressionIndex = _randomLocal.Next(totalProgressions); // <--- FIX: Use _randomLocal
            }

            // Now load the progression based on the determined CurrentProgressionIndex
            RandomProgression = _chordService.GetChordProgressionByIndex(CurrentProgressionIndex);

            if (RandomProgression != null)
            {
                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(RandomProgression, 4);
            }
            else
            {
                _logger.LogWarning($"Failed to retrieve chord progression at index {CurrentProgressionIndex}. The list might be empty or an invalid index was requested.");
                // Reset index if progression couldn't be found (e.g., if totalProgressions was 0 initially)
                CurrentProgressionIndex = -1;
            }
        }

        public IActionResult OnPostNext()
        {
            int totalProgressions = _chordService.GetProgressionCount();
            if (totalProgressions == 0)
            {
                return RedirectToPage(); // No progressions, just reload the page
            }

            // Increment the index, loop back to 0 if at the end
            int nextIndex = (CurrentProgressionIndex + 1) % totalProgressions;

            // Redirect to the same page with the new index
            return RedirectToPage(new { index = nextIndex });
        }
    }
}
