using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ChordProgressionQuiz.Pages
{
    public class PitchIntervalQuizModel : PageModel
    {
        private readonly PitchIntervalService _intervalService;
        private readonly Random _random = new Random();

        public PitchInterval CurrentInterval { get; set; }
        public string AllIntervalsJson { get; set; } // MODIFIED: Will hold all possible intervals
        public List<string> AllIntervalNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool LoopPlayback { get; set; } = true;

        [BindProperty(SupportsGet = true)] // NEW: Property for the toggle
        public bool PrioritizeWeaknesses { get; set; } = false;


        public PitchIntervalQuizModel(PitchIntervalService intervalService)
        {
            _intervalService = intervalService;
        }

        // MODIFIED: OnGet now handles selecting a specific interval or a random one
        public void OnGet(int? selectedIndex)
        {
            var allPossibleIntervals = _intervalService.GetAllPossibleIntervals();
            AllIntervalsJson = JsonSerializer.Serialize(allPossibleIntervals);
            AllIntervalNames = _intervalService.GetAllIntervalNames();

            if (selectedIndex.HasValue && selectedIndex.Value >= 0 && selectedIndex.Value < allPossibleIntervals.Count)
            {
                CurrentInterval = allPossibleIntervals[selectedIndex.Value];
            }
            else
            {
                // Default to a completely random one for the very first load
                CurrentInterval = _intervalService.GetRandomInterval();
            }
        }

        // The OnPostNext handler is no longer needed, as the "Next" button will be handled by JavaScript.
    }
}