﻿// ChordProgressionQuiz.Tests/ChordProgressionServiceTests.cs
using Xunit;
using ChordProgressionQuiz.Services;
using ChordProgressionQuiz.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.IO; // Required for Path.Combine and File.Exists
using System; // Required for StringSplitOptions

namespace ChordProgressionQuiz.Tests
{
    public class ChordProgressionServiceTests
    {
        private readonly ChordProgressionService _service;
        private readonly List<ChordProgression> _allProgressions;

        public ChordProgressionServiceTests()
        {
            // Set up a mock IWebHostEnvironment to point to the test project's Data folder
            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            // ContentRootPath needs to be the directory where the test assembly runs,
            // which is also where the 'Data' folder with the JSON is copied.
            mockWebHostEnv.Setup(m => m.ContentRootPath).Returns(Directory.GetCurrentDirectory());
            mockWebHostEnv.Setup(m => m.EnvironmentName).Returns("Testing");

            _service = new ChordProgressionService(mockWebHostEnv.Object);
            _allProgressions = LoadAllProgressionsForTests(mockWebHostEnv.Object.ContentRootPath);
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
                Assert.True(false, "No progressions loaded for testing. Ensure 'chordProgressions.json' is in the test project's Data folder and copied to output.");
            }
            int indexToTest = 0;
            var progression = _service.GetChordProgressionByIndex(indexToTest);
            Assert.NotNull(progression);
            Assert.Equal(_allProgressions[indexToTest].Song, progression.Song);
        }

        [Theory]
        // Parameters:
        // 1. string romanNumerals (e.g., "I V vi IV")
        // 2. object[] expectedPitchesRelativeToKeyTonic (an array of int[] for each chord, normalized so key's tonic is 0)
        // 3. string songName (for test readability/error messages)
        // 4. string relativeTo (key/mode string, e.g., "C Major")
        [InlineData("I V vi IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major: C-E-G)
                new int[] { 7, 11, 14 },    // V (G Major: G-B-D)
                new int[] { 9, 12, 16 },    // vi (A Minor: A-C-E)
                new int[] { 5, 9, 12 }      // IV (F Major: F-A-C)
            },
            "I'm yours", "C Major")] // songName, relativeTo

        /*
        [InlineData("I7 VII7 iii7 ii7 V7 I7 VII7 iii7", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },  // I7 (C)
                new int[] { 11, 15, 18, 21 }, // VII7 (B7)
                new int[] { 4, 7, 11, 14 }, // iii7 (Em7)
                new int[] { 2, 5, 9, 12 },  // ii7 (Dm7)
                new int[] { 7, 11, 14, 17 }, // V7 (G7)
                new int[] { 0, 4, 7 },  // I7 (C)
                new int[] { 11, 15, 18, 21 }, // VII7 (B7)
                new int[] { 4, 7, 11, 14 }  // iii7 (Em7)
            },
            "Just the two of us", "C Major")] // songName, relativeTo  */

        [InlineData("vi7 iv#7", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16, 19 }, // vi7 (Am7)
                new int[] { 6, 9, 13, 16 }  // iv#7 (F#m7)
            },
            "Light my fire", "C Major")] // songName, relativeTo

        [InlineData("iii I #Idim I", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 1, 4, 7 },      // #Idim (C# Diminished)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            "James bond", "C Major")] // songName, relativeTo

        [InlineData("vi ii V I IV viidim III vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 11, 14, 17 },   // viidim (B Diminished)
                new int[] { 4, 8, 11 },     // III (E Major)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Circle of 5ths diatonic anti-clockwise: Fly me to the moon, you never give me your money, I will survive", "C Major")] // songName, relativeTo

        /*[InlineData("I Imaj7 vi v II bIII bVII IV bVII V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 11 },  // Imaj7 (C Major 7)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 10, 14 },    // v (G Minor)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 3, 7, 10 },     // bIII (Eb Major)
                new int[] { 10, 14, 17 },   // bVII (Bb Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 10, 14, 17 },   // bVII (Bb Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "All the young dudes", "C Major")] // songName, relativeTo */

        // Simple Major Key Progressions
        [InlineData("I IV I V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "The lions sleep tonight", "C Major")] // songName, relativeTo

        [InlineData("I IV vi V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Mr Brightside", "C Major")] // songName, relativeTo

        [InlineData("I V IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Major scale vamp: Luka", "C Major")] // songName, relativeTo

        [InlineData("I vi IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "50's, Doo-wop change", "C Major")] // songName, relativeTo

        [InlineData("I vi ii V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Blue moon", "C Major")] // songName, relativeTo

        [InlineData("I ii IV I", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            "What's up", "C Major")] // songName, relativeTo

        [InlineData("I iii IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Let's get it on", "C Major")] // songName, relativeTo

        [InlineData("I V vi iii IV I IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 }, new int[] { 7, 11, 14 }, new int[] { 9, 12, 16 }, new int[] { 4, 7, 11 },
                new int[] { 5, 9, 12 }, new int[] { 0, 4, 7 }, new int[] { 5, 9, 12 }, new int[] { 7, 11, 14 }
            },
            "Pachelbel's Canon", "C Major")] // songName, relativeTo

        // Minor Key / Modal Progressions (all pitches relative to the KEY'S TONIC)
        // NOTE: For these, the Tonal Roman Numerals are used as per the service's current logic.
        // The expected pitches are relative to the *key's tonic* (e.g., A=0 for A Aeolian).
        [InlineData("vi ii IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "World hold on", "C Major")] // songName, relativeTo

        [InlineData("iii IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 4, 7, 11 },      // iii (Em) -> relative to E: [0, 3, 7]
                new int[] { 5, 9, 12 }       // IV (F) -> relative to E: [1, 5, 8]
            },
            "Phrygian Vamp", "E Phrygian")] // songName, relativeTo

        [InlineData("vi IV III", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            "Harmonic minor vamp: sweet dreams, toxicity, smooth", "C Major")] // songName, relativeTo

        [InlineData("ii V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 2, 5, 9 },      // ii (Dm) -> relative to D: [0, 3, 7]
                new int[] { 7, 11, 14 }      // V (G) -> relative to D: [5, 9, 12]
            },
            "Dorian vamp", "D Dorian")] // songName, relativeTo

        [InlineData("ii IV vi V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Get Lucky", "C Major")] // songName, relativeTo

        [InlineData("vi V IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Aeolian vamp", "C Major")] // songName, relativeTo

        [InlineData("V IV I V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 7, 11, 14 },      // V (G) -> relative to G: [0, 4, 7]
                new int[] { 5, 9, 12 },   // IV (F) -> relative to G: [10, 14, 17]
                new int[] { 0, 4, 7 },     // I (C) -> relative to G: [5, 9, 12]
                new int[] { 7, 11, 14 }       // V (G) -> relative to G: [0, 4, 7]
            },
            "Mixolydian Vamp", "G Mixolydian")] // songName, relativeTo

        [InlineData("vi V IV III", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            "Andalusian cadence: Hit the road jack, Genie in a bottle", "C Major")] // songName, relativeTo

        [InlineData("vi I II IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Minor climb: House of the rising sun, Ecstacy of Gold, Call me, Talk talk, Birdie, My Sharona", "C Major")] // songName, relativeTo

        [InlineData("vi ii I III", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            "Welcome to the internet: I want the world to stop", "C Major")] // songName, relativeTo

        [InlineData("vi V ii vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Aeolian closed loop: Dani California, Apart, True colors, Yet another movie", "C Major")] // songName, relativeTo

        [InlineData("vi IV I III", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            "Harmonic minor axis: Lonely day, (You're welcome (moana))", "C Major")] // songName, relativeTo

        [InlineData("I v ii iv", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 10, 14 },    // v (G Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 8, 12 }      // iv (F Minor)
            },
            "Our house", "C Major")] // songName, relativeTo

        // Chromatic and Complex Progressions
        [InlineData("I III IV iv", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (G Major)
                new int[] { 4, 8, 11 },     // III (B Major)
                new int[] { 5, 9, 12 },     // IV (C Major)
                new int[] { 5, 8, 12 }      // iv (C Minor)
            },
            "Creep", "G Major")] // songName, relativeTo

        [InlineData("I II IV I", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            "Eight days a week", "C Major")] // songName, relativeTo

        [InlineData("I V bVII IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 10, 14, 17 },   // bVII (Bb Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Here it goes again", "C Major")] // songName, relativeTo

        [InlineData("I V I V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (F Major)
                new int[] { 7, 11, 14 },    // V (C Major)
                new int[] { 0, 4, 7 },      // I (F Major)
                new int[] { 7, 11, 14 }     // V (C Major)
            },
            "Like a prayer", "F Major")] // songName, relativeTo

        [InlineData("IV V vi vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Running up that hill", "C Major")] // songName, relativeTo

        [InlineData("I Imaj7 I7 IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 11 },  // Imaj7 (Cmaj7)
                new int[] { 0, 4, 7, 10 },  // I7 (C7)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Major line cliché", "C Major")] // songName, relativeTo

        [InlineData("I Iaug vi VI7", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 8 },      // Iaug (C Augmented)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 9, 13, 16, 19 } // VI7 (A7) (A, C#, E, G)
            },
            "Augmented climb", "C Major")] // songName, relativeTo

        [InlineData("vi Iaug I II", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 8 },      // Iaug (C Augmented)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 6, 9 }       // II (D Major)
            },
            "Minor line cliché", "C Major")] // songName, relativeTo

        [InlineData("I I7 IV iv", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 10 },  // I7 (C7)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 5, 8, 12 }      // iv (F Minor)
            },
            "Magnolia", "C Major")] // songName, relativeTo

        [InlineData("I I7 vi #Vaug", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 10 },  // I7 (C7)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 8, 12, 16 }     // #Vaug (G# Augmented)
            },
            "Lucy in the sky with diamond (intro)", "C Major")] // songName, relativeTo

        [InlineData("vi V7 I IV IV ii V vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14, 17 },// V7 (G7)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Wild world", "C Major")] // songName, relativeTo

        [InlineData("I bIII bVII vi bVI V I bII", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (G Major)
                new int[] { 3, 7, 10 },     // bIII (Bb Major)
                new int[] { 10, 14, 17 },   // bVII (F Major)
                new int[] { 9, 12, 16 },    // vi (E Minor)
                new int[] { 8, 12, 15 },    // bVI (Eb Major)
                new int[] { 7, 11, 14 },    // V (D Major)
                new int[] { 0, 4, 7 },      // I (G Major)
                new int[] { 1, 5, 8 }       // bII (Ab Major)
            },
            "Black hole sun", "G Major, with strong chromaticism")] // songName, relativeTo

        [InlineData("vi III vi V I V IV III", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 4, 8, 11 },     // III (E Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            "Folia", "C Major")] // songName, relativeTo

        [InlineData("V ii ii vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Clocks, speed of sound", "C Major")] // songName, relativeTo

        [InlineData("vi III7 V II IV I ii III7", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 4, 8, 11, 14 }, // III7 (E7)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 4, 8, 11, 14 }  // III7 (E7)
            },
            "Hotel California", "C Major")] // songName, relativeTo

        [InlineData("vi IV I bVII vi VII III7", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 10, 14, 17 },   // bVII (Bb Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 4, 8, 11, 14 }  // III7 (E7)
            },
            "Poupée de cire poupée de son", "C Major")] // songName, relativeTo

        [InlineData("vi I III7 vi VII", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11, 14 }, // III7 (E7)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 }    // VII (B Major)
            },
            "Toxic", "C Major")] // songName, relativeTo

        [InlineData("vi VII I VII vi VII I III7", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11, 14 }  // III7 (E7)
            },
            "Deewani Mastani", "C Major")] // songName, relativeTo

        [InlineData("I V II VI III", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 9, 13, 16 },    // VI (A Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            "Circle of 5th chromatic clockwise", "C Major")] // songName, relativeTo

        [InlineData("vi V I IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Some non-used chord progression that should be common but is not", "C Major")] // songName, relativeTo

        [InlineData("IV V VI", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 13, 16 }     // VI (A Major)
            },
            "Mario Cadence", "C Major")] // songName, relativeTo

        [InlineData("ii7 V7 VI", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 2, 5, 9, 12 },  // ii7 (Dm7)
                new int[] { 7, 11, 14, 17 },// V7 (G7)
                new int[] { 9, 13, 16 }     // VI (A Major)
            },
            "Backdoor progression", "C Major")] // songName, relativeTo

        [InlineData("I III vi IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11 },     // III (E Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "She’s Electric", "C Major")] // songName, relativeTo

        /* [InlineData("i IV bvii bIII bvi bII bV i", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 3, 7 },      // i (B Minor)
                new int[] { 5, 9, 12 },     // IV (E Major)
                new int[] { 10, 13, 17 },   // bvii (A Minor)
                new int[] { 3, 7, 10 },     // bIII (D Major)
                new int[] { 8, 11, 15 },    // bvi (G Minor)
                new int[] { 1, 5, 8 },      // bII (C Major)
                new int[] { 6, 10, 13 },    // bV (F Major)
                new int[] { 0, 3, 7 }       // i (B Minor)
            },
            "You and whose army?", "B minor initially")] // songName, relativeTo */

        /* [InlineData("I vii III vi IV V VI vi II IV I", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (F Major)
                new int[] { 11, 14, 18 },   // vii (E Minor)
                new int[] { 4, 8, 11 },     // III (A Major)
                new int[] { 9, 12, 16 },    // vi (D Minor)
                new int[] { 5, 9, 12 },     // IV (Bb Major)
                new int[] { 7, 11, 14 },    // V (C Major)
                new int[] { 9, 13, 16 },    // VI (D Major)
                new int[] { 9, 12, 16 },    // vi (D Minor)
                new int[] { 2, 6, 9 },      // II (G Major)
                new int[] { 5, 9, 12 },     // IV (Bb Major)
                new int[] { 0, 4, 7 }       // I (F Major)
            },
            "Yesterday, Mr Blue Sky", "F Major")] // songName, relativeTo */

        [InlineData("I bVII bVI V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (A Major)
                new int[] { 10, 14, 17 },   // bVII (G Major)
                new int[] { 8, 12, 15 },    // bVI (F Major)
                new int[] { 7, 11, 14 }     // V (E Major)
            },
            "Zelda overworld (first 4 chords only)", "A")] // songName, relativeTo

        [InlineData("I bVII bVI V I v bVI bIII bII i II7 V I v bVI #vidim V bVI7 #vidim V bII i II7 V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (A Major)
                new int[] { 10, 14, 17 },   // bVII (G Major)
                new int[] { 8, 12, 15 },    // bVI (F Major)
                new int[] { 7, 11, 14 },    // V (E Major)
                new int[] { 0, 4, 7 },      // I (A Major)
                new int[] { 7, 10, 14 },    // v (E minor)
                new int[] { 8, 12, 15 },    // bVI (F Major)
                new int[] { 3, 7, 10 },     // bIII (C Major)
                new int[] { 1, 5, 8 },      // bII (Bb Major)
                new int[] { 0, 3, 7 },      // i (A minor)
                new int[] { 2, 6, 9, 12 },  // II7 (B7)
                new int[] { 7, 11, 14 },    // V (E Major)
                new int[] { 0, 4, 7 },      // I (A Major)
                new int[] { 7, 10, 14 },    // v (E minor)
                new int[] { 8, 12, 15 },    // bVI (F Major)
                new int[] { 10, 13, 16 },   // #vidim (G diminished, based on service logic for #vi)
                new int[] { 7, 11, 14 },    // V (E Major)
                new int[] { 8, 12, 15, 18 },// bVI7 (F7)
                new int[] { 10, 13, 16 },   // #vidim (G diminished, based on service logic for #vi)
                new int[] { 7, 11, 14 },    // V (E Major)
                new int[] { 1, 5, 8 },      // bII (Bb Major)
                new int[] { 0, 3, 7 },      // i (A minor)
                new int[] { 2, 6, 9, 12 },  // II7 (B7)
                new int[] { 7, 11, 14 }     // V (E Major)
            },
            "Zelda overworld", "A Major")] // songName, relativeTo - (assuming "A" defaults to "A Major" for scale calculation)

        [InlineData("vi V iii IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Can't stop: RDI, Étoiles Fillantes, Bollywood Dance", "C Major")]

        [InlineData("vi I V IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Bad touch, counting stars", "C Major")]

        [InlineData("vi IV V iii", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 4, 7, 11 }      // iii (E Minor)
            },
            "One Must Fall 2097, Think about the way, Touch me (F G Em Am)", "C Major")]

        [InlineData("IV V iii vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Royal road", "C Major")]

        [InlineData("vi I IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Move my body (F G Am C)", "C Major")]

        [InlineData("IV V vi I", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            "Tyler", "C Major")]

        [InlineData("vi IV I V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Axis of Awesome: Despacito, Kids aren't alright, (C G Am F, F C G Am, G Am F C)", "C Major")]

        [InlineData("IV I V vi", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            "Dragostea Din Tei, Midnight rain", "C Major")]

        [InlineData("I V vi IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "I'm yours, With or without you, Dammit, Song for a winter's night", "C Major")]

        [InlineData("V vi IV I", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            "Angels, Hurt, September Song", "C Major")]

        [InlineData("I ii IV V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Major scale climb, My girl, 99 Luftballons, Love will keep us together", "C Major")]

        [InlineData("I IV V IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "5 years time, Minority, Cheerleader, Summer Nights, Walking on sunshine", "C Major")]

        [InlineData("vi iii V ii", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 5, 9 }       // ii (D Minor)
            },
            "The deep blue", "C Major")]

        [InlineData("ii IV I V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Plagal Cascade: Wonderall, Mad world, Boulevard of broken dreams, How you remind me", "C Major")]

        [InlineData("I V ii IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Closing time, Believe (Cher), All star, I'm like a bird, Wildest dreams", "C Major")]

        [InlineData("ii V I IV", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            "Isn't she lovely", "C Major")]

        [InlineData("V I IV ii", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 2, 5, 9 }       // ii (D Minor)
            },
            "Pride in the name of love", "C Major")]

        [InlineData("ii vi I V", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            "Blinding Lights, Watermelon Sugar", "C Major")]

        /*[InlineData("I bVII IV bVI Vmaj7 bVII bII", // romanNumerals
            new object[] { // expectedPitchesRelativeToKeyTonic
                new int[] { 0, 4, 7 },      // I (B Major)
                new int[] { 10, 14, 17 },   // bVII (A Major)
                new int[] { 5, 9, 12 },     // IV (E Major)
                new int[] { 8, 12, 15 },    // bVI (G Major)
                new int[] { 7, 11, 14, 18 },// Vmaj7 (F#maj7)
                new int[] { 10, 14, 17 },   // bVII (A Major)
                new int[] { 1, 5, 8 }       // bII (C Major)
            },
            "Blur's unique chord progression", "B")]*/

        public void ConvertToAbsoluteMidiProgression_ShouldProduceCorrectMidiPitches(
            string romanNumerals,
            object[] expectedPitchesRelativeToKeyTonic,
            string songName, // Moved to end
            string relativeTo) // Moved to end
        {
            // Convert object[] of int[] to int[][]
            int[][] expectedPitchesRelativeToKeyTonicArray = expectedPitchesRelativeToKeyTonic
                                                            .Select(x => (int[])x)
                                                            .ToArray();

            // Arrange
            // Find the full ChordProgression object from _allProgressions based on romanNumerals
            var progression = _allProgressions.FirstOrDefault(p =>
                (p.Tonal != null && p.Tonal.RomanNumerals == romanNumerals) ||
                (p.Modal != null && p.Modal.Any() && p.Modal.First().RomanNumerals == romanNumerals));

            Assert.NotNull(progression); // Ensure a matching progression was found
            // Update songName and relativeTo from the found progression for accurate error messages
            songName = progression.Song;
            relativeTo = progression.Tonal?.RelativeTo ?? progression.Modal?.FirstOrDefault()?.RelativeTo;
            Assert.NotNull(relativeTo); // Ensure relativeTo is not null for the test to proceed


            // Act
            var absoluteProgression = _service.ConvertToAbsoluteMidiProgression(progression, 4);

            // Assert
            Assert.NotNull(absoluteProgression);
            Assert.NotNull(absoluteProgression.Chords);
            Assert.Equal(expectedPitchesRelativeToKeyTonicArray.Length, absoluteProgression.Chords.Count);

            // Parse key root from relativeTo for calculating tonicMidiNote in the test
            var relativeToParts = relativeTo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string keyRootNoteName = relativeToParts[0].Trim();
            int keyRootMidiOffset = 0;
            var noteToSemitonesFromC = new Dictionary<string, int>
            {
                {"C", 0}, {"C#", 1}, {"Db", 1}, {"D", 2}, {"D#", 3}, {"Eb", 3}, {"E", 4}, {"F", 5}, {"F#", 6}, {"Gb", 6},
                {"G", 7}, {"G#", 8}, {"Ab", 8}, {"A", 9}, {"A#", 10}, {"Bb", 10}, {"B", 11}
            };
            if (!noteToSemitonesFromC.TryGetValue(keyRootNoteName, out keyRootMidiOffset))
            {
                keyRootMidiOffset = noteToSemitonesFromC["C"];
            }
            int tonicMidiNote = (4 * 12) + keyRootMidiOffset;

            for (int i = 0; i < expectedPitchesRelativeToKeyTonicArray.Length; i++)
            {
                var expectedPitches = expectedPitchesRelativeToKeyTonicArray[i].OrderBy(p => p).ToList();
                var actualMidiChord = absoluteProgression.Chords[i];

                Assert.True(actualMidiChord.MidiPitches.Any(), $"Chord {i} for song '{songName}' generated no pitches. Expected Pitches (relative to tonic): [{string.Join(", ", expectedPitches)}]");

                var actualPitchesRelativeToTonic = actualMidiChord.MidiPitches
                                                        .Select(p => p - tonicMidiNote)
                                                        .OrderBy(p => p)
                                                        .ToList();

                Assert.True(expectedPitches.SequenceEqual(actualPitchesRelativeToTonic),
                    $"Mismatch in chord {i} pitches for song '{songName}'. Expected Pitches (relative to tonic): [{string.Join(", ", expectedPitches)}], Actual Pitches (relative to tonic): [{string.Join(", ", actualPitchesRelativeToTonic)}]");

                int expectedRootOffset = expectedPitches.Any() ? expectedPitches[0] % 12 : -1;
                if (expectedRootOffset < 0) expectedRootOffset += 12;

                int actualRootOffsetFromTonic = (actualMidiChord.MidiPitches.Min() - tonicMidiNote) % 12;
                if (actualRootOffsetFromTonic < 0) actualRootOffsetFromTonic += 12;

                Assert.True(expectedRootOffset == actualRootOffsetFromTonic,
                    $"Mismatch in chord {i} root for song '{songName}'. Expected Root Offset from Tonic: {expectedRootOffset}, Actual Root Offset from Tonic: {actualRootOffsetFromTonic}");
            }
        }
    }
}
