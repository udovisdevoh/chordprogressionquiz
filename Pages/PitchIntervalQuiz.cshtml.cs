using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Http; // Required for Session
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // Required for JSON serialization

namespace ChordProgressionQuiz.Pages
{
    public class PitchIntervalQuizModel : PageModel
    {
        private readonly PitchIntervalService _intervalService;

        public PitchInterval CurrentInterval { get; set; }
        public string AllIntervalsJson { get; set; }
        public List<string> AllIntervalNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool LoopPlayback { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public bool PrioritizeWeaknesses { get; set; } = false;


        public PitchIntervalQuizModel(PitchIntervalService intervalService)
        {
            _intervalService = intervalService;
        }

        // MODIFIED: This method now uses the session to maintain a stable list of intervals
        public void OnGet(int? selectedIndex)
        {
            string sessionIntervalsKey = "PitchIntervalQuiz_Intervals";
            List<PitchInterval> allPossibleIntervals;
            var intervalsJsonFromSession = HttpContext.Session.GetString(sessionIntervalsKey);

            if (string.IsNullOrEmpty(intervalsJsonFromSession))
            {
                // If no list exists in the session, generate a new randomized list...
                allPossibleIntervals = _intervalService.GetAllPossibleIntervals();
                var jsonToStore = JsonSerializer.Serialize(allPossibleIntervals);
                // ...and store it in the session for future requests.
                HttpContext.Session.SetString(sessionIntervalsKey, jsonToStore);
            }
            else
            {
                // If a list already exists, retrieve that stable list from the session.
                allPossibleIntervals = JsonSerializer.Deserialize<List<PitchInterval>>(intervalsJsonFromSession);
            }

            AllIntervalsJson = JsonSerializer.Serialize(allPossibleIntervals);
            AllIntervalNames = _intervalService.GetAllIntervalNames();

            if (selectedIndex.HasValue && selectedIndex.Value >= 0 && selectedIndex.Value < allPossibleIntervals.Count)
            {
                // Select the specific interval the user chose
                CurrentInterval = allPossibleIntervals[selectedIndex.Value];
            }
            else
            {
                // For the very first visit, get a single, truly random interval
                CurrentInterval = _intervalService.GetRandomInterval();
            }
        }
    }
}