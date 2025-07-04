// Pages/ChordPicker.cshtml.cs
using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ChordProgressionQuiz.Pages
{
    public class ChordPickerModel : PageModel
    {
        private readonly ChordProgressionService _chordService;
        private readonly ILogger<ChordPickerModel> _logger;

        public ChordPickerModel(ChordProgressionService chordService, ILogger<ChordPickerModel> logger)
        {
            _chordService = chordService;
            _logger = logger;
        }

        public void OnGet()
        {
            RandomProgression = _chordService.GetRandomChordProgression();

            if (RandomProgression == null)
            {
                _logger.LogWarning("Failed to retrieve a random chord progression. The list might be empty or the JSON file could not be loaded.");
            }
        }

        public ChordProgression RandomProgression { get; set; }
    }
}