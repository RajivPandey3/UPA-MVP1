using UPA.Operations;

namespace UPA.Operations.Tests;

public class OperationRegistryTests
{
    [Fact]
    public void DefaultCatalogResolvesNaturalLanguageAliases()
    {
        var registry = DefaultOperationCatalog.Create();
        var matcher = new IntentOperationMatcher(registry);

        var matches = matcher.Match(
            "Create a GameObject and add Rigidbody, collider and material.");

        Assert.Contains(matches, x => x.Definition.Id == "scene.create_gameobject");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_rigidbody");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_collider");
        Assert.Contains(matches, x => x.Definition.Id == "asset.assign_material");
    }

    [Fact]
    public void AliasCollisionIsRejected()
    {
        var registry = new OperationRegistry();

        registry.Register(new OperationDefinition(
            "a",
            "A",
            ExecutorFamily.Scene,
            OperationRisk.Low,
            new[] { "same phrase" },
            Array.Empty<OperationParameter>(),
            Array.Empty<OperationPrecondition>(),
            Array.Empty<string>(),
            "A",
            true,
            false));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new OperationDefinition(
                "b",
                "B",
                ExecutorFamily.Scene,
                OperationRisk.Low,
                new[] { "same phrase" },
                Array.Empty<OperationParameter>(),
                Array.Empty<OperationPrecondition>(),
                Array.Empty<string>(),
                "B",
                true,
                false)));
    }

    [Fact]
    public void CompilerReportsMissingDependency()
    {
        var registry = DefaultOperationCatalog.Create();
        var matcher = new IntentOperationMatcher(registry);

        var matches = matcher.Match("Configure Rigidbody.");
        var compiled = new OperationPlanCompiler().Compile(matches);

        Assert.Contains(
            compiled.Warnings,
            x => x.Contains("scene.create_gameobject"));
    }
    [Fact]
    public void CompilerReportsTransitiveMissingDependency()
    {
        var registry = DefaultOperationCatalog.Create();
        var matcher = new IntentOperationMatcher(registry);

        var matches = matcher.Match("Configure Rigidbody.");
        var compiled = new OperationPlanCompiler().Compile(matches);

        Assert.Contains(compiled.Warnings,
            x => x.Contains("scene.create_gameobject", StringComparison.Ordinal));
    }


    [Fact]
    public void NaturalLanguageMatcherIgnoresPunctuation()
    {
        var registry = DefaultOperationCatalog.Create();
        var matches = new IntentOperationMatcher(registry)
            .Match("Create a GameObject and add Rigidbody, collider and material.");

        Assert.Contains(matches, x => x.Definition.Id == "scene.create_gameobject");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_rigidbody");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_collider");
        Assert.Contains(matches, x => x.Definition.Id == "asset.assign_material");
    }

    [Fact]
    public void CompilerReportsFullMissingDependencyClosure()
    {
        var registry = DefaultOperationCatalog.Create();
        var matches = new IntentOperationMatcher(registry).Match("Configure Rigidbody.");
        var compiled = new OperationPlanCompiler().Compile(matches);

        Assert.Contains(compiled.Warnings,
            x => x.Contains("component.add_rigidbody", StringComparison.Ordinal));
        Assert.Contains(compiled.Warnings,
            x => x.Contains("scene.create_gameobject", StringComparison.Ordinal));
    }

    [Fact]
    public void NaturalLanguageMatcherResolvesStopWords()
    {
        var registry = DefaultOperationCatalog.Create();
        var matches = new IntentOperationMatcher(registry)
            .Match("Create a GameObject and add Rigidbody, collider and material.");

        Assert.Contains(matches, x => x.Definition.Id == "scene.create_gameobject");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_rigidbody");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_collider");
        Assert.Contains(matches, x => x.Definition.Id == "asset.assign_material");
    }


    [Fact]
    public void MatcherFindsAliasesInsideMultiOperationIntent()
    {
        var registry = DefaultOperationCatalog.Create();
        var matches = new IntentOperationMatcher(registry)
            .Match("Create a GameObject and add Rigidbody, collider and material.");

        Assert.Contains(matches, x => x.Definition.Id == "scene.create_gameobject");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_rigidbody");
        Assert.Contains(matches, x => x.Definition.Id == "component.add_collider");
        Assert.Contains(matches, x => x.Definition.Id == "asset.assign_material");
    }
}
