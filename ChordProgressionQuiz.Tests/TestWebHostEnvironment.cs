// ChordProgressionQuiz.Tests/TestWebHostEnvironment.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace ChordProgressionQuiz.Tests
{
    /// <summary>
    /// A mock implementation of IWebHostEnvironment for testing purposes.
    /// It sets the ContentRootPath to the test project's output directory,
    /// allowing the ChordProgressionService to find the copied JSON file.
    /// </summary>
    public class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ApplicationName { get; set; }
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }

        public TestWebHostEnvironment()
        {
            // Set ContentRootPath to the directory where the test assembly is located.
            // This is where the chordProgressions.json file will be copied during build.
            ContentRootPath = Directory.GetCurrentDirectory();
            EnvironmentName = "Testing"; // Or "Development"
        }
    }
}
