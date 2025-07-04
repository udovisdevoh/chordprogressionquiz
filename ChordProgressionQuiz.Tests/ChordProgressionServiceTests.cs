// ChordProgressionQuiz.Tests/ChordProgressionServiceTests.cs
using Xunit;
using ChordProgressionQuiz.Services;
using ChordProgressionQuiz.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting; // Needed for IWebHostEnvironment
using System.IO;

namespace ChordProgressionQuiz.Tests
{
    public class ChordProgressionServiceTests
    {
        private readonly ChordProgressionService _service;
        private readonly List<ChordProgression> _allProgressions;

        public ChordProgressionServiceTests()
        {
            // Use our custom TestWebHostEnvironment to point to the test project's output directory
            // where chordProgressions.json is copied.
            IWebHostEnvironment testEnv = new TestWebHostEnvironment();
            _service = new ChordProgressionService(testEnv);

            // Load all progressions once for easy lookup in tests
            // In a real scenario, ChordProgressionService might expose a GetAll() method.
            // For now, we'll re-read the JSON directly for test setup.
            // This is a bit redundant but ensures tests are independent of service's internal state.
            _allProgressions = LoadAllProgressionsForTests(testEnv.ContentRootPath);
        }

        // Helper method to load JSON for test setup
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
            // Arrange
            // Service is initialized in constructor

            // Act
            int count = _service.GetProgressionCount();

            // Assert
            Assert.True(count > 0, "Should have loaded some chord progressions.");
            Assert.Equal(_allProgressions.Count, count);
        }

        [Fact]
        public void GetRandomChordProgression_ShouldReturnNonNull()
        {
            // Arrange
            // Service is initialized in constructor

            // Act
            var progression = _service.GetRandomChordProgression();

            // Assert
            Assert.NotNull(progression);
        }

        [Fact]
        public void GetChordProgressionByIndex_ShouldReturnCorrectProgression()
        {
            // Arrange
            if (!_allProgressions.Any())
            {
                Assert.Fail("No progressions loaded for testing.");
            }
            int indexToTest = 0; // Test the first one

            // Act
            var progression = _service.GetChordProgressionByIndex(indexToTest);

            // Assert
            Assert.NotNull(progression);
            Assert.Equal(_allProgressions[indexToTest].Song, progression.Song);
        }

        [Theory]
        [InlineData("I'm yours", "C Major", "I V vi IV", new int[] { 60, 64, 67 }, new int[] { 67, 71, 74 }, new int[] { 69, 72, 76 }, new int[] { 65, 69, 72 })]
        [InlineData("Just the two of us", "Db Major", "I7 bVII7 iv7 iii7 bVI7 I7 bVII7 iv7 iv7", new int[] { 61, 65, 68, 71 }, new int[] { 60, 64, 67, 70 }, new int[] { 65, 68, 71, 74 }, new int[] { 63, 66, 69, 72 }, new int[] { 68, 72, 75, 78 }, new int[] { 61, 65, 68, 71 }, new int[] { 60, 64, 67, 70 }, new int[] { 65, 68, 71, 74 }, new int[] { 65, 68, 71, 74 })]
        [InlineData("Light my fire", "C Major", "vi7 iv#7", new int[] { 57, 61, 64, 67 }, new int[] { 54, 57, 61, 64 })] // Am7 (A3-C4-E4-G4), F#m7 (F#3-A3-C#4-E4)
        [InlineData("James bond", "C Major", "iii I #Idim I", new int[] { 52, 55, 59 }, new int[] { 48, 52, 55 }, new int[] { 49, 52, 55 }, new int[] { 48, 52, 55 })] // Em, C, C#dim, C
        [InlineData("Circle of 5ths diatonic anti-clockwise: Fly me to the moon, you never give me your money, I will survive", "C Major", "vi ii V I IV viidim III vi", new int[] { 57, 60, 64 }, new int[] { 50, 53, 57 }, new int[] { 55, 59, 62 }, new int[] { 48, 52, 55 }, new int[] { 53, 57, 60 }, new int[] { 59, 62, 65 }, new int[] { 52, 56, 59 }, new int[] { 57, 60, 64 })] // vi(Am), ii(Dm), V(G), I(C), IV(F), viidim(Bdim), III(E), vi(Am)
        [InlineData("All the young dudes", "C Major", "I Imaj7 vi v II bIII bVII IV bVII V", new int[] { 48, 52, 55 }, new int[] { 48, 52, 55, 59 }, new int[] { 57, 60, 64 }, new int[] { 55, 58, 62 }, new int[] { 50, 54, 57 }, new int[] { 51, 55, 58 }, new int[] { 58, 62, 65 }, new int[] { 53, 57, 60 }, new int[] { 58, 62, 65 }, new int[] { 55, 59, 62 })] // C, Cmaj7, Am, Gm, D, Eb, Bb, F, Bb, G
        public void ConvertToAbsoluteMidiProgression_ShouldProduceCorrectMidiPitches(
            string songName,
            string relativeTo,
            string romanNumerals,
            params int[][] expectedMidiPitchesPerChord)
        {
            // Arrange
            var progression = new ChordProgression
            {
                Song = songName,
                Tonal = new Tonal { RomanNumerals = romanNumerals, RelativeTo = relativeTo }
            };

            // Act
            var absoluteProgression = _service.ConvertToAbsoluteMidiProgression(progression, 4); // Use octave 4 for consistency

            // Assert
            Assert.NotNull(absoluteProgression);
            Assert.NotNull(absoluteProgression.Chords);
            Assert.Equal(expectedMidiPitchesPerChord.Length, absoluteProgression.Chords.Count);

            for (int i = 0; i < expectedMidiPitchesPerChord.Length; i++)
            {
                var expected = expectedMidiPitchesPerChord[i].OrderBy(p => p).ToList();
                var actual = absoluteProgression.Chords[i].MidiPitches.OrderBy(p => p).ToList();

                // Use CollectionAssert.AreEquivalent or check elements one by one
                // Assert.Equal(expected, actual); // This requires exact order, which might not always be guaranteed by generation
                Assert.True(expected.SequenceEqual(actual),
                    $"Mismatch in chord {i} for song '{songName}'. Expected: [{string.Join(", ", expected)}], Actual: [{string.Join(", ", actual)}]");
            }
        }
    }
}
