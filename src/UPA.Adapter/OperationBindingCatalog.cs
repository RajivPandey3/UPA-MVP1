namespace UPA.Adapter;

public sealed record OperationBinding(
    string OperationId,
    AdapterExecutor Executor,
    IReadOnlyList<(string Name, string Type, bool Required)> Parameters,
    Func<OperationArguments, IReadOnlyList<AdapterPrecondition>> Preconditions);

public sealed class OperationBindingCatalog
{
    private readonly Dictionary<string, OperationBinding> _bindings =
        new(StringComparer.OrdinalIgnoreCase);

    public OperationBindingCatalog Register(OperationBinding binding)
    {
        if (!_bindings.TryAdd(binding.OperationId, binding))
            throw new InvalidOperationException(
                $"Binding already exists: {binding.OperationId}");

        return this;
    }

    public OperationBinding Get(string operationId)
        => _bindings.TryGetValue(operationId, out var binding)
            ? binding
            : throw new KeyNotFoundException(
                $"No executor binding exists for '{operationId}'.");

    public static OperationBindingCatalog CreateDefault()
    {
        var c = new OperationBindingCatalog();

        c.Register(new OperationBinding(
            "scene.create_gameobject",
            AdapterExecutor.Scene,
            new[]
            {
                ("name", "string", true),
                ("parentId", "globalObjectId", false)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-SCENE-001",
                    "Target scene must be explicitly selected and loaded.",
                    true)
            }));

        c.Register(new OperationBinding(
            "component.add_rigidbody",
            AdapterExecutor.Component,
            new[]
            {
                ("targetId", "globalObjectId", true)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-TARGET-001",
                    "Target must resolve to exactly one GameObject.",
                    true)
            }));

        c.Register(new OperationBinding(
            "component.configure_rigidbody",
            AdapterExecutor.Component,
            new[]
            {
                ("targetId", "globalObjectId", true),
                ("useGravity", "bool", false),
                ("mass", "float", false),
                ("isKinematic", "bool", false)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-RB-001",
                    "Target must contain or safely receive a Rigidbody.",
                    true)
            }));

        c.Register(new OperationBinding(
            "component.add_collider",
            AdapterExecutor.Component,
            new[]
            {
                ("targetId", "globalObjectId", true),
                ("shape", "enum", true)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-COLLIDER-001",
                    "Target must resolve uniquely.",
                    true)
            }));

        c.Register(new OperationBinding(
            "asset.assign_material",
            AdapterExecutor.Asset,
            new[]
            {
                ("targetId", "globalObjectId", true),
                ("materialPath", "assetPath", true),
                ("slot", "int", false)
            },
            args =>
            {
                var slot = args.Values.TryGetValue("slot", out var value)
                    ? value
                    : null;

                return new[]
                {
                    new AdapterPrecondition(
                        "UNITY-MATERIAL-001",
                        "Material asset must resolve to a Material.",
                        true),
                    new AdapterPrecondition(
                        "UNITY-MATERIAL-002",
                        $"Material slot must be non-negative. Current: {slot ?? 0}.",
                        false)
                };
            }));

        c.Register(new OperationBinding(
            "prefab.save",
            AdapterExecutor.Prefab,
            new[]
            {
                ("targetId", "globalObjectId", true),
                ("assetPath", "assetPath", true)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-PREFAB-001",
                    "Prefab destination must be inside Assets/.",
                    true)
            }));

        c.Register(new OperationBinding(
            "settings.physics_gravity",
            AdapterExecutor.Settings,
            new[]
            {
                ("gravity", "vector3", true)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-SETTINGS-001",
                    "Project-settings mutation requires explicit approval.",
                    true)
            }));

        c.Register(new OperationBinding(
            "import.texture_max_size",
            AdapterExecutor.Importer,
            new[]
            {
                ("assetPath", "assetPath", true),
                ("maxSize", "int", true)
            },
            _ => new[]
            {
                new AdapterPrecondition(
                    "UNITY-IMPORT-001",
                    "Asset must resolve to a TextureImporter.",
                    true)
            }));

        return c;
    }
}
