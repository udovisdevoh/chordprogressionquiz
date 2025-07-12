// D:\users\Anonymous\Documents\C Sharp\ChordProgressionQuiz\Pages\PitchIntervalQuiz.cshtml.cs
using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

        public void OnGet(int? selectedIndex)
        {
            // Get the static definitions of all intervals (name and semitone count)
            var allIntervalDefinitions = _intervalService.GetAllIntervalDefinitions();
            AllIntervalsJson = JsonSerializer.Serialize(allIntervalDefinitions);
            AllIntervalNames = allIntervalDefinitions.Select(i => i.IntervalName).ToList();

            if (selectedIndex.HasValue && selectedIndex.Value >= 0 && selectedIndex.Value < allIntervalDefinitions.Count)
            {
                // Get the definition for the selected interval
                var selectedIntervalDefinition = allIntervalDefinitions[selectedIndex.Value];
                // Generate a NEW, random instance of that interval with a random base pitch
                CurrentInterval = _intervalService.GetRandomIntervalOfSemitone(selectedIntervalDefinition.Semitones);
            }
            else
            {
                // For the very first visit, or if no index is provided, get a single, truly random interval.
                CurrentInterval = _intervalService.GetRandomInterval();
            }
        }
    }
}