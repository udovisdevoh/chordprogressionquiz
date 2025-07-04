// Pages/ChordPicker.cshtml.cs
using ChordProgressionQuiz.Models; // Ensure this is the correct namespace
using ChordProgressionQuiz.Services; // Ensure this is the correct namespace
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ChordProgressionQuiz.Pages
{
    public class ChordPickerModel : PageModel
    {
        private readonly ChordProgressionService _chordService;
        private readonly ILogger<ChordPickerModel> _logger;

        public ChordProgression RandomProgression { get; set; }
        public AbsoluteChordProgression AbsoluteProgression { get; set; } // New property for MIDI pitches

        public ChordPickerModel(ChordProgressionService chordService, ILogger<ChordPickerModel> logger)
        {
            _chordService = chordService;
            _logger = logger;
        }

        public void OnGet()
        {
            RandomProgression = _chordService.GetRandomChordProgression();

            if (RandomProgression != null)
            {
                // Convert the random symbolic progression to an absolute MIDI progression
                // We'll use octave 4 as a default. You can make this configurable later.
                AbsoluteProgression = _chordService.ConvertToAbsoluteMidiProgression(RandomProgression, 4);
            }
            else
            {
                _logger.LogWarning("Failed to retrieve a random chord progression. The list might be empty or the JSON file could not be loaded.");
            }
        }
    }
}
