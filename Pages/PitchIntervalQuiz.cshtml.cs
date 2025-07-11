using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;

namespace ChordProgressionQuiz.Pages
{
    public class PitchIntervalQuizModel : PageModel
    {
        private readonly PitchIntervalService _intervalService;

        public PitchInterval CurrentInterval { get; set; }
        public string IntervalJson { get; set; }
        public List<string> AllIntervalNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool LoopPlayback { get; set; } = true;

        public PitchIntervalQuizModel(PitchIntervalService intervalService)
        {
            _intervalService = intervalService;
        }

        public void OnGet()
        {
            CurrentInterval = _intervalService.GetRandomInterval();
            IntervalJson = JsonSerializer.Serialize(CurrentInterval);
            AllIntervalNames = _intervalService.GetAllIntervalNames();
        }

        public IActionResult OnPostNext()
        {
            // Redirects to a new quiz page, preserving the user's loop preference
            return RedirectToPage(new { LoopPlayback });
        }
    }
}