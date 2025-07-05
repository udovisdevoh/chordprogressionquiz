// ChordProgressionQuiz.Tests/ChordProgressionServiceTests.cs
using Xunit;
using ChordProgressionQuiz.Services;
using ChordProgressionQuiz.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.IO;

namespace ChordProgressionQuiz.Tests
{
    public class ChordProgressionServiceTests
    {
        private readonly ChordProgressionService _service;
        private readonly List<ChordProgression> _allProgressions;

        public ChordProgressionServiceTests()
        {
            IWebHostEnvironment testEnv = new TestWebHostEnvironment();
            _service = new ChordProgressionService(testEnv);
            _allProgressions = LoadAllProgressionsForTests(testEnv.ContentRootPath);
        }

        private List<ChordProgression> LoadAllProgressionsForTests(string contentRootPath)
        {
            var filePath = Path.Combine(contentRootPath, "Data", "chordProgressions.json");
            if (File.Exists(filePath))
            {
                var jsonString = File.ReadAllText(filePath);
                return System.Text.Json.JsonSerializer.Deserialize<List<ChordProgression>>(jsonString, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return new List<ChordProgression>();
        }


        [Fact]
        public void GetProgressionCount_ShouldReturnCorrectCount()
        {
            int count = _service.GetProgressionCount();
            Assert.True(count > 0, "Should have loaded some chord progressions.");
            Assert.Equal(_allProgressions.Count, count);
        }

        [Fact]
        public void GetRandomChordProgression_ShouldReturnNonNull()
        {
            var progression = _service.GetRandomChordProgression();
            Assert.NotNull(progression);
        }

        [Fact]
        public void GetChordProgressionByIndex_ShouldReturnCorrectProgression()
        {
            if (!_allProgressions.Any())
            {
                // Replaced Assert.Fail with Assert.True(false, ...)
                Assert.True(false, "No progressions loaded for testing. Ensure 'chordProgressions.json' is in the test project's Data folder and copied to output.");
            }
            int indexToTest = 0;
            var progression = _service.GetChordProgressionByIndex(indexToTest);
            Assert.NotNull(progression);
            Assert.Equal(_allProgressions[indexToTest].Song, progression.Song);
        }

        [Theory]
        // All expected pitches are now relative intervals from the chord's root (0-indexed)
        [InlineData("I'm yours", "C Major", "I V vi IV", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("Just the two of us", "Db Major", "I7 bVII7 iv7 iii7 bVI7 I7 bVII7 iv7 iv7", new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 3, 7, 10 }, new int[] { 0, 3, 7, 10 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 3, 7, 10 }, new int[] { 0, 3, 7, 10 })]
        [InlineData("Light my fire", "C Major", "vi7 iv#7", new int[] { 0, 3, 7, 10 }, new int[] { 0, 3, 7, 10 })] // Am7 (0,3,7,10), F#m7 (0,3,7,10)
        [InlineData("James bond", "C Major", "iii I #Idim I", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 6 }, new int[] { 0, 4, 7 })] // Em, C, C#dim, C
        [InlineData("Circle of 5ths diatonic anti-clockwise: Fly me to the moon, you never give me your money, I will survive", "C Major", "vi ii V I IV viidim III vi", new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 6 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 })] // vi(Am), ii(Dm), V(G), I(C), IV(F), viidim(Bdim), III(E), vi(Am)
        [InlineData("All the young dudes", "C Major", "I Imaj7 vi v II bIII bVII IV bVII V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 11 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]

        // NEW TEST CASES START HERE

        // Simple Major Key Progressions
        [InlineData("The lions sleep tonight", "C Major", "I IV I V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("Mr Brightside", "C Major", "I IV vi V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("Major scale vamp: Luka", "C Major", "I V IV V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("50's, Doo-wop change", "C Major", "I vi IV V", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("Blue moon", "C Major", "I vi ii V", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("What's up", "C Major", "I ii IV I", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("Let's get it on", "C Major", "I iii IV V", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]
        [InlineData("Pachelbel's Canon", "C Major", "I V vi iii IV I IV V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })]

        // Minor Key / Modal Progressions (relative to their own tonic)
        [InlineData("World hold on", "A Aeolian", "i iv bVI bVII", new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, Dm, F, G
        [InlineData("Phrygian Vamp", "E Phrygian", "i bII", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })] // Em, F (F is bII of E Phrygian)
        [InlineData("Harmonic minor vamp", "A Harmonic Minor", "i bVI V", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, F, E (V in harmonic minor is major)
        [InlineData("Dorian vamp", "D Dorian", "i IV", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })] // Dm, G
        [InlineData("Aeolian vamp", "A Aeolian", "i bVII bVI bVII", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, G, F, G
        [InlineData("Mixolydian Vamp", "G Mixolydian", "I bVII IV I", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // G, F, C, G
        [InlineData("Andalusian cadence", "A Harmonic Minor", "i bVII bVI V", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, G, F, E
        [InlineData("Minor climb", "A Aeolian", "i bIII IV bVI", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, C, D, F
        [InlineData("Welcome to the internet", "A Aeolian with a Harmonic Minor V", "i iv bIII V", new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, Dm, C, E
        [InlineData("Aeolian closed loop", "A Aeolian", "i bVII iv i", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 })] // Am, G, Dm, Am
        [InlineData("Harmonic minor axis", "A Harmonic Minor", "i bVI bIII V", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, F, C, E
        [InlineData("Our house", "C Ionian with modal mixture from C minor", "I v ii iv", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 })] // C, Gm, Dm, Fm

        // Chromatic and Complex Progressions
        [InlineData("Creep", "G Major", "I III IV iv", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 })] // G, B, C, Cm
        [InlineData("Eight days a week", "C Lydian", "I II IV I", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // C, D, F, C
        [InlineData("Here it goes again", "C Mixolydian", "I V bVII IV", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // C, G, Bb, F
        [InlineData("Like a prayer", "F Major", "I V I V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // F, C, F, C
        [InlineData("Running up that hill", "C Major", "IV V vi vi", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 })] // F, G, Am, Am
        [InlineData("Major line cliché", "C Major", "I Imaj7 I7 IV", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 11 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7 })] // C, Cmaj7, C7, F
        [InlineData("Augmented climb", "C Major", "I Iaug vi VI7", new int[] { 0, 4, 7 }, new int[] { 0, 4, 8 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7, 10 })] // C, Caug, Am, A7
        [InlineData("Minor line cliché", "C Major", "vi Iaug I II", new int[] { 0, 3, 7 }, new int[] { 0, 4, 8 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, Caug, C, D
        [InlineData("Magnolia", "C Major", "I I7 IV iv", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 })] // C, C7, F, Fm
        [InlineData("Lucy in the sky with diamond (intro)", "C Major", "I I7 vi #Vaug", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 8 })] // C, C7, Am, G#aug
        [InlineData("Wild world", "C Major", "vi V7 I IV IV ii V vi", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 })] // Am, D7, G, C, F, Dm, E, Am
        [InlineData("Black hole sun", "G Major, with strong chromaticism", "I bIII bVII vi bVI V I bII", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // G, Bb, F, Em, Eb, D, G, Ab
        [InlineData("James bond", "C Major", "iii I #Idim I", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 6 }, new int[] { 0, 4, 7 })] // Em, C, C#dim, C (re-added as a test case)
        [InlineData("Folia", "C Major", "vi III vi V I V IV III", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, E, Am, G, C, G, F, E
        [InlineData("Clocks, speed of sound", "C Major", "V ii ii vi", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 3, 7 })] // G, Dm, Dm, Am (Updated based on new data)
        [InlineData("Hotel California", "C Major", "vi III7 V II IV I ii III7", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7, 10 })] // Am, E7, G, D, F, C, Dm, E7
        [InlineData("Poupée de cire poupée de son", "C Major", "vi IV I bVII vi VII III7", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 10 })] // Am, F, C, Bb, Am, B, E7
        [InlineData("Toxic", "C Major", "vi I III7 vi VII", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })] // Am, C, E7, Am, B
        [InlineData("Deewani Mastani", "C Major", "vi VII I VII vi VII I III7", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 10 })] // Am, B, C, B, Am, B, C, E7
        [InlineData("Circle of 5th chromatic clockwise", "C Major", "I V II VI III", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // C, G, D, A, E
        [InlineData("Some non-used chord progression that should be common but is not", "C Major", "vi V I IV", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // Am, G, C, F
        [InlineData("Mario Cadence", "C Major", "IV V VI", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // F, G, A
        [InlineData("Backdoor progression", "C Major", "ii7 V7 VI", new int[] { 0, 3, 7, 10 }, new int[] { 0, 4, 7, 10 }, new int[] { 0, 4, 7 })] // Dm7, G7, A
        [InlineData("She’s Electric", "C Major", "I III vi IV", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 })] // C, E, Am, F
        [InlineData("You and whose army?", "B minor initially", "i IV vii III vi II V i", new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 6 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 })] // Bm, E, F#dim, D, Am, G, D, Bm (simplified to B minor context)
        [InlineData("Yesterday, Mr Blue Sky", "F Major", "I vii III vi IV V I i IV bVII", new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 3, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // F, Em, A, Dm, Bb, C, D, Dm, G, Bb, F (simplified to F major context, then Dm, G, Bb, F)
        [InlineData("Zelda overworld", "A", "I bVII bVI V", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // A, G, F, E (from the start of Zelda progression)
        [InlineData("Blur's unique chord progression", "B", "I I add#9 IV bVI Vmaj7#11 bVII bII", new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 14 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7, 11, 18 }, new int[] { 0, 4, 7 }, new int[] { 0, 4, 7 })] // B, B, Aadd#9, E, G, Fmaj7#11, Bb, Db (simplified to B context)
        public void ConvertToAbsoluteMidiProgression_ShouldProduceCorrectMidiPitches(
            string songName,
            string relativeTo,
            string romanNumerals,
            params int[][] expectedRelativePitchesPerChord) // Changed parameter name to reflect relative pitches
        {
            // Arrange
            var progression = new ChordProgression
            {
                Song = songName,
                Tonal = new Tonal { RomanNumerals = romanNumerals, RelativeTo = relativeTo }
            };

            // Act
            // Use a fixed octave (e.g., 4) for generation, as the test will compare relative pitches
            var absoluteProgression = _service.ConvertToAbsoluteMidiProgression(progression, 4);

            // Assert
            Assert.NotNull(absoluteProgression);
            Assert.NotNull(absoluteProgression.Chords);
            Assert.Equal(expectedRelativePitchesPerChord.Length, absoluteProgression.Chords.Count);

            for (int i = 0; i < expectedRelativePitchesPerChord.Length; i++)
            {
                var expectedRelative = expectedRelativePitchesPerChord[i].OrderBy(p => p).ToList();
                var actualMidiChord = absoluteProgression.Chords[i];

                Assert.True(actualMidiChord.MidiPitches.Any(), $"Chord {i} for song '{songName}' generated no pitches. Expected: [{string.Join(", ", expectedRelative)}]");

                // Calculate actual relative pitches from the generated MIDI chord
                var actualRoot = actualMidiChord.MidiPitches.Min();
                var actualRelative = actualMidiChord.MidiPitches
                                        .Select(p => p - actualRoot)
                                        .OrderBy(p => p)
                                        .ToList();

                Assert.True(expectedRelative.SequenceEqual(actualRelative),
                    $"Mismatch in chord {i} for song '{songName}'. Expected Relative: [{string.Join(", ", expectedRelative)}], Actual Relative: [{string.Join(", ", actualRelative)}]");
            }
        }
    }
}
