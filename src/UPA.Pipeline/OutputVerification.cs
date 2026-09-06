using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UPA.Execution;
using UPA.Planning;

namespace UPA.Pipeline;

public static class OutputVerification
{
    public static OutputVerifierRegistry CreateRegistry() => new();
    public static string Fingerprint(UpaPlan plan, string preview, IReadOnlyList<OutputExpectation> outputs)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { plan, preview, outputs }))));

    public static IReadOnlyList<string> Verify(string root, IReadOnlyList<OutputExpectation> outputs)
    {
        if (outputs.Count == 0) throw new InvalidOperationException("No output expectations were approved.");
        var evidence = new List<string>();
        foreach (var expected in outputs)
        {
            var path = new PathSandbox(root).Resolve(expected.RelativePath);
            var bytes = File.ReadAllBytes(path);
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = reader.ReadToEnd();
            if (expected.Kind == "text")
            {
                if (content != expected.Value) throw new InvalidOperationException("File content differs from the approved output.");
            }
            else if (expected.Kind == "unity-player")
            {
                var blocks = Regex.Matches(content, @"(?ms)^--- !u!(?<type>\d+) &(?<id>\d+)\r?\n(?<body>.*?)(?=^--- !u!|\z)");
                var objects = blocks.Cast<Match>().Where(block => block.Groups["type"].Value == "1").ToArray();
                var bodies = blocks.Cast<Match>().Where(block => block.Groups["type"].Value == "54").ToArray();
                var transforms = blocks.Cast<Match>().Where(block => block.Groups["type"].Value == "4").ToArray();
                if (objects.Length != 1 || bodies.Length != 1 || transforms.Length != 1)
                    throw new InvalidOperationException("Saved scene does not contain exactly the approved object and components.");
                var gameObject = objects[0];
                var objectBody = gameObject.Groups["body"].Value;
                var objectId = gameObject.Groups["id"].Value;
                var references = Regex.Matches(objectBody, @"component: \{fileID: (?<id>\d+)\}")
                    .Select(reference => reference.Groups["id"].Value).ToArray();
                if (!Regex.IsMatch(objectBody, @"(?m)^  m_Name: " + Regex.Escape(expected.Value) + @"\r?$") ||
                    references.Length != 2 || !references.Contains(bodies[0].Groups["id"].Value) ||
                    !references.Contains(transforms[0].Groups["id"].Value) ||
                    !bodies[0].Groups["body"].Value.Contains("m_GameObject: {fileID: " + objectId + "}") ||
                    !transforms[0].Groups["body"].Value.Contains("m_GameObject: {fileID: " + objectId + "}"))
                    throw new InvalidOperationException("Saved scene object/component references differ from the approved output.");
            }
            else throw new InvalidOperationException("Unsupported output verifier: " + expected.Kind);
            evidence.Add($"Read back {expected.RelativePath}; SHA256={Convert.ToHexString(SHA256.HashData(bytes))}");
        }
        return evidence;
    }
}
