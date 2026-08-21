using UPA.Core;
using UPA.ProjectModel;
using Xunit;

namespace UPA.Analysis.Tests;

public class UnifiedProjectModelCompositionTests
{
    [Fact]
    public void Composer_ProducesUnifiedModelFromScannerResults()
    {
        var scan = new ScanResult(
            EntityId.FromStableKey(@"C:\Soul-Hunter"),
            DateTimeOffset.UtcNow,
            Array.Empty<Diagnostic>())
        {
            ProjectName = "Soul-Hunter",
            ProjectRoot = @"C:\Soul-Hunter",
            UnityVersion = "6000.0.36f1",
            RenderPipelineHint = "URP"
        };

        var scripts = new[]
        {
            new CSharpScriptModel(
                EntityId.FromStableKey("Assets/Test.cs"),
                "Assets/Test.cs",
                "SoulHunter.Test",
                Array.Empty<CSharpTypeModel>(),
                Array.Empty<Diagnostic>())
        };

        var assemblies = new AssemblyScanResult(
            Array.Empty<AssemblyDefinitionModel>(),
            Array.Empty<AssemblyDependencyModel>(),
            Array.Empty<string>(),
            Array.Empty<Diagnostic>());

        var model = new UnifiedProjectModelComposer()
            .Compose(scan, scripts, assemblies);

        Assert.Equal("Soul-Hunter", model.ProjectName);
        Assert.Equal(@"C:\Soul-Hunter", model.RootPath);
        Assert.Equal("6000.0.36f1", model.UnityVersion);
        Assert.Equal("URP", model.RenderPipeline);

        Assert.Equal(1, model.Counts.Scripts);
        Assert.Equal(0, model.Counts.Types);
        Assert.Equal(0, model.Counts.Assemblies);
        Assert.Equal(0, model.Counts.References);
        Assert.Equal(0, model.Counts.UnresolvedReferences);

        Assert.Empty(ProjectModelIntegrity.Validate(model));
    }
}