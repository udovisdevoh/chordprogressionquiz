// Pages/PitchIntervalQuiz.cshtml.cs
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

        // NEW: Add ReferencePitch property
        [BindProperty(SupportsGet = true)]
        public string ReferencePitch { get; set; } = "F";

        public PitchIntervalQuizModel(PitchIntervalService intervalService)
        {
            _intervalService = intervalService;
        }

        public void OnGet(int? selectedIndex)
        {
            var allIntervalDefinitions = _intervalService.GetAllIntervalDefinitions();
            AllIntervalsJson = JsonSerializer.Serialize(allIntervalDefinitions);
            AllIntervalNames = allIntervalDefinitions.Select(i => i.IntervalName).ToList();

            if (selectedIndex.HasValue && selectedIndex.Value >= 0 && selectedIndex.Value < allIntervalDefinitions.Count)
            {
                var selectedIntervalDefinition = allIntervalDefinitions[selectedIndex.Value];
                // NEW: Pass the reference pitch to the service
                CurrentInterval = _intervalService.GetRandomIntervalOfSemitone(selectedIntervalDefinition.Semitones, ReferencePitch);
            }
            else
            {
                // NEW: Pass the reference pitch to the service
                CurrentInterval = _intervalService.GetRandomInterval(ReferencePitch);
            }
        }
    }
}