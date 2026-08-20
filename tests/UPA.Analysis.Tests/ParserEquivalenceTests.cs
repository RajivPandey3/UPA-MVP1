using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Xunit;
using UPA.Analysis;
using UPA.Core;
using System.Linq;

namespace UPA.Analysis.Tests
{
    public class ParserEquivalenceTests
    {
        private static CSharpScriptModel RunLegacyScanFile(string root, string path)
        {
            var method = typeof(CSharpScanner).GetMethod("ScanFile", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new Exception("ScanFile not found!");
            return (CSharpScriptModel)method.Invoke(null, new object[] { root, path })!;
        }

        private static CSharpScriptModel RunFastScanFile(string root, string path)
        {
            return CSharpFastParser.ParseFile(root, path);
        }

        [Fact]
        public void ComplexScript_ParsersAreSemanticallyEquivalent()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string projectRoot = Path.GetFullPath(Path.Combine(currentDir, "../../.."));
            string fixturePath = Path.Combine(projectRoot, "Fixtures", "ComplexScript.cs.txt");
            
            Assert.True(File.Exists(fixturePath), $"Fixture not found at {fixturePath}");

            var legacyModel = RunLegacyScanFile(projectRoot, fixturePath);
            var fastModel = RunFastScanFile(projectRoot, fixturePath);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string legacyJson = JsonSerializer.Serialize(legacyModel, options);
            string fastJson = JsonSerializer.Serialize(fastModel, options);

            Assert.Equal(legacyJson, fastJson);
        }
    }
}
