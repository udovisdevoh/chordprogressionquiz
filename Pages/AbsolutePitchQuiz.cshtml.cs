using ChordProgressionQuiz.Models;
using ChordProgressionQuiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;

namespace ChordProgressionQuiz.Pages
{
    public class AbsolutePitchQuizModel : PageModel
    {
        private readonly AbsolutePitchQuizService _pitchService;

        public AbsolutePitch CurrentNote { get; set; }
        public string CurrentNoteJson { get; set; }
        public List<string> AllNoteNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool LoopPlayback { get; set; } = true;

        public AbsolutePitchQuizModel(AbsolutePitchQuizService pitchService)
        {
            _pitchService = pitchService;
        }

        public void OnGet()
        {
            CurrentNote = _pitchService.GetRandomNote();
            AllNoteNames = _pitchService.GetAllNoteNames();
            CurrentNoteJson = JsonSerializer.Serialize(CurrentNote);
        }
    }
}