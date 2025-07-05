// ChordProgressionQuiz.Tests/ChordProgressionServiceTests.cs
using Xunit;
using ChordProgressionQuiz.Services;
using ChordProgressionQuiz.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.IO; // Required for Path.Combine and File.Exists
using System;

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
        // 1. songName: Name of the song for easy identification in test results.
        // 2. relativeTo: The key/mode string (e.g., "C Major", "A Aeolian").
        // 3. romanNumerals: The Roman numeral string from the JSON (e.g., "I V vi IV").
        // 4. expectedPitchesRelativeToKeyTonic: An array of object arrays. Each inner object array represents
        //    the expected *MIDI pitches for that chord, normalized so the key's tonic is 0*.
        //    (e.g., for C Major, C is 0, D is 2, E is 4, F is 5, G is 7, A is 9, B is 11.
        //    Pitches can go above 11 for notes in higher octaves, e.g., C an octave up is 12).
        // 5. expectedRootSemitoneOffsetsFromTonic: An array of integers. Each integer represents the
        //    expected *semitone offset of the chord's root from the key's tonic*.
        //    This is a redundant check but explicitly verifies the root calculation.

        // Core Test Cases (All pitches are relative to the KEY'S TONIC, where tonic = 0)
        [InlineData("I'm yours", "C Major", "I V vi IV",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major: C-E-G) -> relative to C: [0, 4, 7]
                new int[] { 7, 11, 14 },    // V (G Major: G-B-D) -> relative to C: [7, 11, 14] (D is 14 semitones from C)
                new int[] { 9, 12, 16 },    // vi (A Minor: A-C-E) -> relative to C: [9, 12, 16] (C is 12, E is 16 semitones from C)
                new int[] { 5, 9, 12 }      // IV (F Major: F-A-C) -> relative to C: [5, 9, 12] (C is 12 semitones from C)
            },
            new int[] { 0, 7, 9, 5 })] // Roots: C, G, A, F (semitones from C)

        [InlineData("Just the two of us", "Db Major", "I7 bVII7 iv7 iii7 bVI7 I7 bVII7 iv7 iv7",
            new object[] {
                new int[] { 0, 4, 7, 10 },  // I7 (Db7) -> relative to Db: [0, 4, 7, 10]
                new int[] { 11, 15, 18, 21 }, // bVII7 (C7) -> relative to Db: [11, 15, 18, 21] (C is 11, E is 15, G is 18, Bb is 21 semitones from Db)
                new int[] { 5, 8, 12, 15 }, // iv7 (Fm7) -> relative to Db: [5, 8, 12, 15] (F is 5, Ab is 8, C is 12, Eb is 15 semitones from Db)
                new int[] { 3, 6, 10, 13 }, // iii7 (Ebm7) -> relative to Db: [3, 6, 10, 13] (Eb is 3, Gb is 6, Bb is 10, Db is 13 semitones from Db)
                new int[] { 8, 12, 15, 18 }, // bVI7 (Ab7) -> relative to Db: [8, 12, 15, 18] (Ab is 8, C is 12, Eb is 15, Gb is 18 semitones from Db)
                new int[] { 0, 4, 7, 10 },  // I7 (Db7) -> relative to Db: [0, 4, 7, 10]
                new int[] { 11, 15, 18, 21 }, // bVII7 (C7) -> relative to Db: [11, 15, 18, 21]
                new int[] { 5, 8, 12, 15 }, // iv7 (Fm7) -> relative to Db: [5, 8, 12, 15]
                new int[] { 5, 8, 12, 15 }  // iv7 (Fm7) -> relative to Db: [5, 8, 12, 15]
            },
            new int[] { 0, 11, 5, 3, 8, 0, 11, 5, 5 })] // Roots: Db, C, F, Eb, Ab, Db, C, F, F (semitones from Db)

        [InlineData("Light my fire", "C Major", "vi7 iv#7",
            new object[] {
                new int[] { 9, 12, 16, 19 }, // vi7 (Am7) -> relative to C: [9, 12, 16, 19] (A, C, E, G)
                new int[] { 6, 10, 13, 16 }  // iv#7 (F#7) -> relative to C: [6, 10, 13, 16] (F#, A#, C#, E)
            },
            new int[] { 9, 6 })] // Roots: A, F# (semitones from C)

        [InlineData("James bond", "C Major", "iii I #Idim I",
            new object[] {
                new int[] { 4, 7, 11 },     // iii (E Minor) -> relative to C: [4, 7, 11] (E, G, B)
                new int[] { 0, 4, 7 },      // I (C Major) -> relative to C: [0, 4, 7] (C, E, G)
                new int[] { 1, 4, 7 },      // #Idim (C# Diminished) -> relative to C: [1, 4, 7] (C#, E, G)
                new int[] { 0, 4, 7 }       // I (C Major) -> relative to C: [0, 4, 7] (C, E, G)
            },
            new int[] { 4, 0, 1, 0 })] // Roots: E, C, C#, C (semitones from C)

        [InlineData("Circle of 5ths diatonic anti-clockwise: Fly me to the moon, you never give me your money, I will survive", "C Major", "vi ii V I IV viidim III vi",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor) -> relative to C: [9, 12, 16]
                new int[] { 2, 5, 9 },      // ii (D Minor) -> relative to C: [2, 5, 9]
                new int[] { 7, 11, 14 },    // V (G Major) -> relative to C: [7, 11, 14]
                new int[] { 0, 4, 7 },      // I (C Major) -> relative to C: [0, 4, 7]
                new int[] { 5, 9, 12 },     // IV (F Major) -> relative to C: [5, 9, 12]
                new int[] { 11, 14, 17 },   // viidim (B Diminished) -> relative to C: [11, 14, 17]
                new int[] { 4, 8, 11 },     // III (E Major) -> relative to C: [4, 8, 11]
                new int[] { 9, 12, 16 }     // vi (A Minor) -> relative to C: [9, 12, 16]
            },
            new int[] { 9, 2, 7, 0, 5, 11, 4, 9 })] // Roots: A, D, G, C, F, B, E, A (semitones from C)

        [InlineData("All the young dudes", "C Major", "I Imaj7 vi v II bIII bVII IV bVII V",
            new object[] {
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
            new int[] { 0, 0, 9, 7, 2, 3, 10, 5, 10, 7 })] // Roots: C, C, A, G, D, Eb, Bb, F, Bb, G (semitones from C)

        // Simple Major Key Progressions
        [InlineData("The lions sleep tonight", "C Major", "I IV I V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 5, 0, 7 })] // Roots: C, F, C, G (semitones from C)

        [InlineData("Mr Brightside", "C Major", "I IV vi V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 5, 9, 7 })] // Roots: C, F, A, G (semitones from C)

        [InlineData("Major scale vamp: Luka", "C Major", "I V IV V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 7, 5, 7 })] // Roots: C, G, F, G (semitones from C)

        [InlineData("50's, Doo-wop change", "C Major", "I vi IV V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 9, 5, 7 })] // Roots: C, A, F, G (semitones from C)

        [InlineData("Blue moon", "C Major", "I vi ii V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 9, 2, 7 })] // Roots: C, A, D, G (semitones from C)

        [InlineData("What's up", "C Major", "I ii IV I",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            new int[] { 0, 2, 5, 0 })] // Roots: C, D, F, C (semitones from C)

        [InlineData("Let's get it on", "C Major", "I iii IV V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 4, 5, 7 })] // Roots: C, E, F, G (semitones from C)

        [InlineData("Pachelbel's Canon", "C Major", "I V vi iii IV I IV V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 4, 7, 11 },     // iii (E Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 }     // V (G Major)
            },
            new int[] { 0, 7, 9, 4, 5, 0, 5, 7 })] // Roots: C, G, A, E, F, C, F, G (semitones from C)

        // Minor Key / Modal Progressions (all pitches relative to the KEY'S TONIC)
        // NOTE: For these, the Tonal Roman Numerals are used as per the service's current logic.
        // The expected pitches are relative to the *key's tonic* (e.g., A=0 for A Aeolian).
        [InlineData("World hold on", "A Aeolian", "vi ii IV V", // Tonal: vi ii IV V relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 5, 8, 12 },     // ii (Dm) -> relative to A: [5, 8, 12]
                new int[] { 8, 12, 15 },    // IV (F) -> relative to A: [8, 12, 15] (F is 8 semitones from A)
                new int[] { 10, 14, 17 }    // V (G) -> relative to A: [10, 14, 17] (G is 10 semitones from A)
            },
            new int[] { 0, 5, 8, 10 })] // Roots: A, D, F, G (semitones from A)

        [InlineData("Phrygian Vamp", "E Phrygian", "iii IV", // Tonal: iii IV relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // iii (Em) -> relative to E: [0, 3, 7]
                new int[] { 1, 5, 8 }       // IV (F) -> relative to E: [1, 5, 8]
            },
            new int[] { 0, 1 })] // Roots: E, F (semitones from E)

        [InlineData("Harmonic minor vamp", "A Harmonic Minor", "vi IV III", // Tonal: vi IV III relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 5, 9, 12 },     // IV (F) -> relative to A: [5, 9, 12]
                new int[] { 4, 8, 11 }      // III (E) -> relative to A: [4, 8, 11]
            },
            new int[] { 0, 5, 7 })] // Roots: A, F, E (semitones from A)

        [InlineData("Dorian vamp", "D Dorian", "ii V", // Tonal: ii V relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // ii (Dm) -> relative to D: [0, 3, 7]
                new int[] { 5, 9, 12 }      // V (G) -> relative to D: [5, 9, 12]
            },
            new int[] { 0, 5 })] // Roots: D, G (semitones from D)

        [InlineData("Aeolian vamp", "A Aeolian", "vi V IV V", // Tonal: vi V IV V relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 10, 14, 17 },   // V (G) -> relative to A: [10, 14, 17]
                new int[] { 8, 12, 15 },    // IV (F) -> relative to A: [8, 12, 15]
                new int[] { 10, 14, 17 }    // V (G) -> relative to A: [10, 14, 17]
            },
            new int[] { 0, 10, 8, 10 })] // Roots: A, G, F, G (semitones from A)

        [InlineData("Mixolydian Vamp", "G Mixolydian", "V IV I V", // Tonal: V IV I V relative to C Major
            new object[] {
                new int[] { 0, 4, 7 },      // V (G) -> relative to G: [0, 4, 7]
                new int[] { 10, 14, 17 },   // IV (F) -> relative to G: [10, 14, 17]
                new int[] { 5, 9, 12 },     // I (C) -> relative to G: [5, 9, 12]
                new int[] { 0, 4, 7 }       // V (G) -> relative to G: [0, 4, 7]
            },
            new int[] { 0, 10, 5, 0 })] // Roots: G, F, C, G (semitones from G)

        [InlineData("Andalusian cadence", "A Harmonic Minor", "vi V IV III", // Tonal: vi V IV III relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 10, 14, 17 },   // V (G) -> relative to A: [10, 14, 17]
                new int[] { 8, 12, 15 },    // IV (F) -> relative to A: [8, 12, 15]
                new int[] { 7, 11, 14 }     // III (E) -> relative to A: [7, 11, 14]
            },
            new int[] { 0, 10, 8, 7 })] // Roots: A, G, F, E (semitones from A)

        [InlineData("Minor climb", "A Aeolian", "vi I II IV", // Tonal: vi I II IV relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 3, 7, 10 },     // I (C) -> relative to A: [3, 7, 10]
                new int[] { 5, 9, 12 },     // II (D) -> relative to A: [5, 9, 12]
                new int[] { 8, 12, 15 }     // IV (F) -> relative to A: [8, 12, 15]
            },
            new int[] { 0, 3, 5, 8 })] // Roots: A, C, D, F (semitones from A)

        [InlineData("Welcome to the internet", "A Aeolian with a Harmonic Minor V", "vi ii I III", // Tonal: vi ii I III relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 5, 8, 12 },     // ii (Dm) -> relative to A: [5, 8, 12]
                new int[] { 3, 7, 10 },     // I (C) -> relative to A: [3, 7, 10]
                new int[] { 7, 11, 14 }     // III (E) -> relative to A: [7, 11, 14]
            },
            new int[] { 0, 5, 3, 7 })] // Roots: A, D, C, E (semitones from A)

        [InlineData("Aeolian closed loop", "A Aeolian", "vi V ii vi", // Tonal: vi V ii vi relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 10, 14, 17 },   // V (G) -> relative to A: [10, 14, 17]
                new int[] { 5, 8, 12 },     // ii (Dm) -> relative to A: [5, 8, 12]
                new int[] { 0, 3, 7 }       // vi (Am) -> relative to A: [0, 3, 7]
            },
            new int[] { 0, 10, 5, 0 })] // Roots: A, G, D, A (semitones from A)

        [InlineData("Harmonic minor axis", "A Harmonic Minor", "vi IV I III", // Tonal: vi IV I III relative to C Major
            new object[] {
                new int[] { 0, 3, 7 },      // vi (Am) -> relative to A: [0, 3, 7]
                new int[] { 8, 12, 15 },    // IV (F) -> relative to A: [8, 12, 15]
                new int[] { 3, 7, 10 },     // I (C) -> relative to A: [3, 7, 10]
                new int[] { 7, 11, 14 }     // III (E) -> relative to A: [7, 11, 14]
            },
            new int[] { 0, 8, 3, 7 })] // Roots: A, F, C, E (semitones from A)

        [InlineData("Our house", "C Major", "I v ii iv", // Tonal: I v ii iv relative to C Major
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 10, 14 },    // v (G Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 5, 8, 12 }      // iv (F Minor)
            },
            new int[] { 0, 7, 2, 5 })] // Roots: C, G, D, F (semitones from C)

        // Chromatic and Complex Progressions
        [InlineData("Creep", "G Major", "I III IV iv",
            new object[] {
                new int[] { 0, 4, 7 },      // I (G Major) -> relative to G: [0, 4, 7]
                new int[] { 4, 8, 11 },     // III (B Major) -> relative to G: [4, 8, 11]
                new int[] { 5, 9, 12 },     // IV (C Major) -> relative to G: [5, 9, 12]
                new int[] { 5, 8, 12 }      // iv (C Minor) -> relative to G: [5, 8, 12]
            },
            new int[] { 0, 4, 5, 5 })] // Roots: G, B, C, C (semitones from G)

        [InlineData("Eight days a week", "C Major", "I II IV I", // Tonal: I II IV I relative to C Major
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 }       // I (C Major)
            },
            new int[] { 0, 2, 5, 0 })] // Roots: C, D, F, C (semitones from C)

        [InlineData("Here it goes again", "C Major", "I V bVII IV", // Tonal: I V bVII IV relative to C Major
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 10, 14, 17 },   // bVII (Bb Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            new int[] { 0, 7, 10, 5 })] // Roots: C, G, Bb, F (semitones from C)

        [InlineData("Like a prayer", "F Major", "I V I V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (F Major)
                new int[] { 7, 11, 14 },    // V (C Major)
                new int[] { 0, 4, 7 },      // I (F Major)
                new int[] { 7, 11, 14 }     // V (C Major)
            },
            new int[] { 0, 7, 0, 7 })] // Roots: F, C, F, C (semitones from F)

        [InlineData("Running up that hill", "C Major", "IV V vi vi",
            new object[] {
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            new int[] { 5, 7, 9, 9 })] // Roots: F, G, A, A (semitones from C)

        [InlineData("Major line cliché", "C Major", "I Imaj7 I7 IV",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 11 },  // Imaj7 (Cmaj7)
                new int[] { 0, 4, 7, 10 },  // I7 (C7)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            new int[] { 0, 0, 0, 5 })] // Roots: C, C, C, F (semitones from C)

        [InlineData("Augmented climb", "C Major", "I Iaug vi VI7",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 8 },      // Iaug (C Augmented)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 9, 13, 16, 19 } // VI7 (A7) (A, C#, E, G)
            },
            new int[] { 0, 0, 9, 9 })] // Roots: C, C, A, A (semitones from C)

        [InlineData("Minor line cliché", "C Major", "vi Iaug I II",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 8 },      // Iaug (C Augmented)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 6, 9 }       // II (D Major)
            },
            new int[] { 9, 0, 0, 2 })] // Roots: A, C, C, D (semitones from C)

        [InlineData("Magnolia", "C Major", "I I7 IV iv",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 10 },  // I7 (C7)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 5, 8, 12 }      // iv (F Minor)
            },
            new int[] { 0, 0, 5, 5 })] // Roots: C, C, F, F (semitones from C)

        [InlineData("Lucy in the sky with diamond (intro)", "C Major", "I I7 vi #Vaug",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 0, 4, 7, 10 },  // I7 (C7)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 8, 12, 16 }     // #Vaug (G# Augmented)
            },
            new int[] { 0, 0, 9, 8 })] // Roots: C, C, A, G# (semitones from C)

        [InlineData("Wild world", "C Major", "vi V7 I IV IV ii V vi",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 2, 6, 9, 12 },  // V7 (D7)
                new int[] { 7, 11, 14 },    // I (G Major)
                new int[] { 0, 4, 7 },      // IV (C Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 4, 8, 11 },     // V (E Major)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            new int[] { 9, 2, 7, 0, 5, 2, 4, 9 })] // Roots: A, D, G, C, F, D, E, A (semitones from C)

        [InlineData("Black hole sun", "G Major, with strong chromaticism", "I bIII bVII vi bVI V I bII",
            new object[] {
                new int[] { 0, 4, 7 },      // I (G Major)
                new int[] { 3, 7, 10 },     // bIII (Bb Major)
                new int[] { 10, 14, 17 },   // bVII (F Major)
                new int[] { 9, 12, 16 },    // vi (E Minor)
                new int[] { 8, 12, 15 },    // bVI (Eb Major)
                new int[] { 7, 11, 14 },    // V (D Major)
                new int[] { 0, 4, 7 },      // I (G Major)
                new int[] { 1, 5, 8 }       // bII (Ab Major)
            },
            new int[] { 0, 3, 10, 9, 8, 7, 0, 1 })] // Roots: G, Bb, F, E, Eb, D, G, Ab (semitones from G)

        [InlineData("Folia", "C Major", "vi III vi V I V IV III",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 4, 8, 11 },     // III (E Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            new int[] { 9, 4, 9, 7, 0, 7, 5, 4 })] // Roots: A, E, A, G, C, G, F, E (semitones from C)

        [InlineData("Clocks, speed of sound", "C Major", "V ii ii vi",
            new object[] {
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 9, 12, 16 }     // vi (A Minor)
            },
            new int[] { 7, 2, 2, 9 })] // Roots: G, D, D, A (semitones from C)

        [InlineData("Hotel California", "C Major", "vi III7 V II IV I ii III7",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 4, 8, 11, 14 }, // III7 (E7)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 2, 5, 9 },      // ii (D Minor)
                new int[] { 4, 8, 11, 14 }  // III7 (E7)
            },
            new int[] { 9, 4, 7, 2, 5, 0, 2, 4 })] // Roots: A, E, G, D, F, C, D, E (semitones from C)

        [InlineData("Poupée de cire poupée de son", "C Major", "vi IV I bVII vi VII III7",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 10, 14, 17 },   // bVII (Bb Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 4, 8, 11, 14 }  // III7 (E7)
            },
            new int[] { 9, 5, 0, 10, 9, 11, 4 })] // Roots: A, F, C, Bb, A, B, E (semitones from C)

        [InlineData("Toxic", "C Major", "vi I III7 vi VII",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11, 14 }, // III7 (E7)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 }    // VII (B Major)
            },
            new int[] { 9, 0, 4, 9, 11 })] // Roots: A, C, E, A, B (semitones from C)

        [InlineData("Deewani Mastani", "C Major", "vi VII I VII vi VII I III7",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 11, 15, 18 },   // VII (B Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11, 14 }  // III7 (E7)
            },
            new int[] { 9, 11, 0, 11, 9, 11, 0, 4 })] // Roots: A, B, C, B, A, B, C, E (semitones from C)

        [InlineData("Circle of 5th chromatic clockwise", "C Major", "I V II VI III",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 2, 6, 9 },      // II (D Major)
                new int[] { 9, 13, 16 },    // VI (A Major)
                new int[] { 4, 8, 11 }      // III (E Major)
            },
            new int[] { 0, 7, 2, 9, 4 })] // Roots: C, G, D, A, E (semitones from C)

        [InlineData("Some non-used chord progression that should be common but is not", "C Major", "vi V I IV",
            new object[] {
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            new int[] { 9, 7, 0, 5 })] // Roots: A, G, C, F (semitones from C)

        [InlineData("Mario Cadence", "C Major", "IV V VI",
            new object[] {
                new int[] { 5, 9, 12 },     // IV (F Major)
                new int[] { 7, 11, 14 },    // V (G Major)
                new int[] { 9, 13, 16 }     // VI (A Major)
            },
            new int[] { 5, 7, 9 })] // Roots: F, G, A (semitones from C)

        [InlineData("Backdoor progression", "C Major", "ii7 V7 VI",
            new object[] {
                new int[] { 2, 5, 9, 12 },  // ii7 (Dm7)
                new int[] { 7, 11, 14, 17 },// V7 (G7)
                new int[] { 9, 13, 16 }     // VI (A Major)
            },
            new int[] { 2, 7, 9 })] // Roots: D, G, A (semitones from C)

        [InlineData("She’s Electric", "C Major", "I III vi IV",
            new object[] {
                new int[] { 0, 4, 7 },      // I (C Major)
                new int[] { 4, 8, 11 },     // III (E Major)
                new int[] { 9, 12, 16 },    // vi (A Minor)
                new int[] { 5, 9, 12 }      // IV (F Major)
            },
            new int[] { 0, 4, 9, 5 })] // Roots: C, E, A, F (semitones from C)

        [InlineData("You and whose army?", "B minor initially", "i IV vii III vi II V i",
            new object[] {
                new int[] { 0, 3, 7 },      // i (B Minor) -> relative to B: [0, 3, 7]
                new int[] { 5, 9, 12 },     // IV (E Major) -> relative to B: [5, 9, 12]
                new int[] { 10, 13, 16 },   // vii (A Minor) -> relative to B: [10, 13, 16] (A, C, E)
                new int[] { 3, 7, 10 },     // III (D Major) -> relative to B: [3, 7, 10]
                new int[] { 8, 11, 15 },    // vi (G Minor) -> relative to B: [8, 11, 15]
                new int[] { 1, 5, 8 },      // II (C Major) -> relative to B: [1, 5, 8]
                new int[] { 6, 10, 13 },    // V (F Major) -> relative to B: [6, 10, 13]
                new int[] { 0, 3, 7 }       // i (B Minor) -> relative to B: [0, 3, 7]
            },
            new int[] { 0, 5, 10, 3, 8, 1, 6, 0 })] // Roots: B, E, A, D, G, C, F, B (semitones from B)

        [InlineData("Yesterday, Mr Blue Sky", "F Major", "I vii III vi IV V I i IV bVII",
            new object[] {
                new int[] { 0, 4, 7 },      // I (F Major)
                new int[] { 11, 14, 18 },   // vii (E Minor) -> relative to F: [11, 14, 18]
                new int[] { 4, 8, 11 },     // III (A Major) -> relative to F: [4, 8, 11]
                new int[] { 9, 12, 16 },    // vi (D Minor) -> relative to F: [9, 12, 16]
                new int[] { 7, 11, 14 },    // IV (Bb Major) -> relative to F: [7, 11, 14]
                new int[] { 2, 6, 9 },      // V (C Major) -> relative to F: [2, 6, 9]
                new int[] { 0, 4, 7 },      // I (F Major) -> relative to F: [0, 4, 7]
                new int[] { 9, 12, 16 },    // i (Dm) - D is 9 semitones from F
                new int[] { 7, 11, 14 },    // IV (Bb) - G is 2 semitones from F, Bb is 7 semitones from F
                new int[] { 10, 14, 17 }    // bVII (Eb) - Bb is 7 semitones from F, Eb is 10 semitones from F
            },
            new int[] { 0, 11, 4, 9, 7, 2, 0, 9, 2, 7 })] // Roots: F, E, A, D, Bb, C, F, D, G, Bb (semitones from F)
                                                          // Corrected expectedRootSemitoneOffsetsFromTonic for Yesterday:
                                                          // The original JSON's keys_example for Yesterday was "F Em A Dm Bb C D Dm G Bb F"
                                                          // The Roman numeral "I vii III vi IV V I i IV bVII" relative to F Major implies:
                                                          // F (I), Em (vii), A (III), Dm (vi), Bb (IV), C (V), F (I), Dm (i), G (IV), Bb (bVII)
                                                          // My previous interpretation of the last few chords was slightly off. Correcting now.
                                                          // F=0, Em=11, A=4, Dm=9, Bb=7, C=2, F=0, Dm=9, G=2, Bb=7 (semitones from F)
                                                          // This means the last part of the expectedRootSemitoneOffsetsFromTonic should be:
                                                          // ..., 0 (F), 9 (Dm), 2 (G), 7 (Bb)

        [InlineData("Zelda overworld", "A", "I bVII bVI V",
            new object[] {
                new int[] { 0, 4, 7 },      // I (A Major)
                new int[] { 10, 14, 17 },   // bVII (G Major)
                new int[] { 8, 12, 15 },    // bVI (F Major)
                new int[] { 7, 11, 14 }     // V (E Major)
            },
            new int[] { 0, 10, 8, 7 })] // Roots: A, G, F, E (semitones from A)

        [InlineData("Blur's unique chord progression", "B", "I I add#9 IV bVI Vmaj7#11 bVII bII",
            new object[] {
                new int[] { 0, 4, 7 },      // I (B Major)
                new int[] { 0, 4, 7 },      // I (B Major)
                new int[] { 10, 14, 17, 24 },// Aadd#9 -> relative to B: [10, 14, 17, 24] (A, C#, E, B)
                new int[] { 5, 9, 12 },     // IV (E Major) -> relative to B: [5, 9, 12]
                new int[] { 8, 12, 15 },    // bVI (G Major) -> relative to B: [8, 12, 15]
                new int[] { 6, 10, 13, 17, 24 }, // Vmaj7#11 (F#maj7#11) -> relative to B: [6, 10, 13, 17, 24] (F#, A#, C#, E#, B#)
                new int[] { 10, 14, 17 },   // bVII (Bb Major) -> relative to B: [10, 14, 17]
                new int[] { 1, 5, 8 }       // bII (Db Major) -> relative to B: [1, 5, 8]
            },
            new int[] { 0, 0, 10, 5, 8, 6, 10, 1 })] // Roots: B, B, A, E, G, F#, Bb, Db (semitones from B)

        // END TEST CASES
        public void ConvertToAbsoluteMidiProgression_ShouldProduceCorrectMidiPitches(
            string songName,
            string relativeTo,
            string romanNumerals,
            object[] expectedPitchesRelativeToKeyTonic, // Changed to object[]
            object expectedRootSemitoneOffsetsFromTonic // Changed from params int[] to object
            )
        {
            // Arrange
            var progression = new ChordProgression
            {
                Song = songName,
                Tonal = new Tonal { RomanNumerals = romanNumerals, RelativeTo = relativeTo }
            };

            // Act
            var absoluteProgression = _service.ConvertToAbsoluteMidiProgression(progression, 4);

            // Assert
            Assert.NotNull(absoluteProgression);
            Assert.NotNull(absoluteProgression.Chords);

            // Cast the object arrays back to int[]
            var expectedRootOffsetsArray = (int[])expectedRootSemitoneOffsetsFromTonic; // <--- CAST HERE

            Assert.Equal(expectedPitchesRelativeToKeyTonic.Length, absoluteProgression.Chords.Count);
            Assert.Equal(expectedRootOffsetsArray.Length, absoluteProgression.Chords.Count); // Use the cast array length

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

            for (int i = 0; i < expectedPitchesRelativeToKeyTonic.Length; i++)
            {
                // Cast the object array back to int[]
                var expectedPitches = ((int[])expectedPitchesRelativeToKeyTonic[i]).OrderBy(p => p).ToList();
                var expectedRootOffset = expectedRootOffsetsArray[i]; // Use the cast array
                var actualMidiChord = absoluteProgression.Chords[i];

                Assert.True(actualMidiChord.MidiPitches.Any(), $"Chord {i} for song '{songName}' generated no pitches. Expected Pitches (relative to tonic): [{string.Join(", ", expectedPitches)}]");

                var actualPitchesRelativeToTonic = actualMidiChord.MidiPitches
                                                        .Select(p => p - tonicMidiNote)
                                                        .OrderBy(p => p)
                                                        .ToList();

                Assert.True(expectedPitches.SequenceEqual(actualPitchesRelativeToTonic),
                    $"Mismatch in chord {i} pitches for song '{songName}'. Expected Pitches (relative to tonic): [{string.Join(", ", expectedPitches)}], Actual Pitches (relative to tonic): [{string.Join(", ", actualPitchesRelativeToTonic)}]");

                int actualRootOffsetFromTonic = (actualMidiChord.MidiPitches.Min() - tonicMidiNote) % 12;
                if (actualRootOffsetFromTonic < 0) actualRootOffsetFromTonic += 12;

                Assert.True(expectedRootOffset == actualRootOffsetFromTonic,
                    $"Mismatch in chord {i} root for song '{songName}'. Expected Root Offset from Tonic: {expectedRootOffset}, Actual Root Offset from Tonic: {actualRootOffsetFromTonic}");
            }
        }
    }
}
