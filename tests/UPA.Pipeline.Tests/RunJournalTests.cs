namespace UPA.Pipeline.Tests;

public sealed class RunJournalTests
{
    [Theory]
    [InlineData("Prepared")]
    [InlineData("Executing")]
    [InlineData("Verifying")]
    public void InterruptedStagesRequireReview(string stage)
    {
        var root = Directory.CreateTempSubdirectory("upa-journal-");
        try
        {
            var journal = new RunJournal(root.FullName, "run", "hash", Array.Empty<OutputExpectation>());
            journal.Write(stage, Array.Empty<PipelineEvent>());
            var record = Assert.Single(RunJournal.Inspect(root.FullName));
            Assert.True(RunJournal.RequiresReview(record));
            Assert.Equal(stage, record.Status);
            Assert.Throws<IOException>(() => new RunJournal(root.FullName, "run", "hash", Array.Empty<OutputExpectation>()));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void TruncatedRecordIsNeverReportedCompleted()
    {
        var root = Directory.CreateTempSubdirectory("upa-journal-");
        try
        {
            var directory = Directory.CreateDirectory(Path.Combine(root.FullName, ".upa", "runs"));
            File.WriteAllText(Path.Combine(directory.FullName, "broken.json"), "{\"RunId\":");
            var record = Assert.Single(RunJournal.Inspect(root.FullName));
            Assert.Equal("Corrupt", record.Status);
            Assert.True(RunJournal.RequiresReview(record));
        }
        finally { root.Delete(true); }
    }
}
