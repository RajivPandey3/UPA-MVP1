namespace UPA.Operations;

public static class DefaultOperationCatalog
{
    public static OperationRegistry Create()
    {
        var r = new OperationRegistry();

        r.Register(new(
            "scene.create_gameobject",
            "Create GameObject",
            ExecutorFamily.Scene,
            OperationRisk.Medium,
            new[] { "create gameobject", "create object", "add gameobject", "make gameobject" },
            new[]
            {
                new OperationParameter("name", "string", true, "GameObject name."),
                new OperationParameter("parentId", "globalObjectId", false, "Optional parent."),
            },
            new[]
            {
                new OperationPrecondition(
                    "SCENE-001",
                    "Target scene must be loaded.",
                    true)
            },
            Array.Empty<string>(),
            "Create GameObject '{name}' in the target scene.",
            true,
            true));

        r.Register(new(
            "component.add_rigidbody",
            "Add Rigidbody",
            ExecutorFamily.Component,
            OperationRisk.Medium,
            new[] { "add rigidbody", "add rigid body", "give rigidbody", "add physics body" },
            new[]
            {
                new OperationParameter("targetId", "globalObjectId", true, "Exact GameObject target.")
            },
            new[]
            {
                new OperationPrecondition(
                    "COMP-001",
                    "Target GameObject must resolve uniquely.",
                    true)
            },
            new[] { "scene.create_gameobject" },
            "Add Rigidbody to '{targetId}'.",
            true,
            true));

        r.Register(new(
            "component.configure_rigidbody",
            "Configure Rigidbody",
            ExecutorFamily.Component,
            OperationRisk.Medium,
            new[] { "configure rigidbody", "set gravity", "configure physics body" },
            new[]
            {
                new OperationParameter("targetId", "globalObjectId", true, "Rigidbody target."),
                new OperationParameter("useGravity", "bool", false, "Whether gravity is enabled."),
                new OperationParameter("mass", "float", false, "Mass."),
                new OperationParameter("isKinematic", "bool", false, "Kinematic mode.")
            },
            new[]
            {
                new OperationPrecondition(
                    "COMP-002",
                    "Target must have or be able to receive a Rigidbody.",
                    true)
            },
            new[] { "component.add_rigidbody", "scene.create_gameobject" },
            "Configure Rigidbody on '{targetId}'.",
            true,
            true));

        r.Register(new(
            "component.add_collider",
            "Add Collider",
            ExecutorFamily.Component,
            OperationRisk.Low,
            new[] { "add collider", "add box collider", "add sphere collider" },
            new[]
            {
                new OperationParameter("targetId", "globalObjectId", true, "Exact target."),
                new OperationParameter("shape", "enum", true, "box or sphere.")
            },
            new[]
            {
                new OperationPrecondition(
                    "COMP-003",
                    "Target must resolve uniquely.",
                    true)
            },
            Array.Empty<string>(),
            "Add {shape} collider to '{targetId}'.",
            true,
            true));

        r.Register(new(
            "asset.assign_material",
            "Assign Material",
            ExecutorFamily.Asset,
            OperationRisk.Medium,
            new[] { "assign material", "apply material", "set material", "material" },
            new[]
            {
                new OperationParameter("targetId", "globalObjectId", true, "Renderer target."),
                new OperationParameter("materialPath", "assetPath", true, "Material asset path."),
                new OperationParameter("slot", "int", false, "Renderer material slot.")
            },
            new[]
            {
                new OperationPrecondition(
                    "ASSET-001",
                    "Material asset must resolve to a Material.",
                    true)
            },
            Array.Empty<string>(),
            "Assign material '{materialPath}' to '{targetId}'.",
            true,
            true));

        r.Register(new(
            "prefab.save",
            "Save Prefab",
            ExecutorFamily.Prefab,
            OperationRisk.High,
            new[] { "make prefab", "create prefab", "save prefab" },
            new[]
            {
                new OperationParameter("targetId", "globalObjectId", true, "Root GameObject."),
                new OperationParameter("assetPath", "assetPath", true, "Prefab asset path.")
            },
            new[]
            {
                new OperationPrecondition(
                    "PREFAB-001",
                    "Target must be a valid scene object root or prefab-compatible object.",
                    true)
            },
            Array.Empty<string>(),
            "Save '{targetId}' as prefab '{assetPath}'.",
            true,
            true));

        r.Register(new(
            "settings.physics_gravity",
            "Set Physics Gravity",
            ExecutorFamily.ProjectSettings,
            OperationRisk.High,
            new[] { "set physics gravity", "configure gravity", "change gravity" },
            new[]
            {
                new OperationParameter("gravity", "vector3", true, "Global gravity vector.")
            },
            new[]
            {
                new OperationPrecondition(
                    "SET-001",
                    "Project settings mutation must be explicitly approved.",
                    true)
            },
            Array.Empty<string>(),
            "Set global physics gravity to {gravity}.",
            true,
            true));

        r.Register(new(
            "import.texture_max_size",
            "Set Texture Max Size",
            ExecutorFamily.Importer,
            OperationRisk.Medium,
            new[] { "set texture size", "limit texture size", "change texture max size" },
            new[]
            {
                new OperationParameter("assetPath", "assetPath", true, "Texture asset."),
                new OperationParameter("maxSize", "int", true, "Maximum texture size.")
            },
            new[]
            {
                new OperationPrecondition(
                    "IMPORT-001",
                    "Target must resolve to a TextureImporter.",
                    true)
            },
            Array.Empty<string>(),
            "Set texture '{assetPath}' max size to {maxSize}.",
            true,
            true));

        r.Register(new(
            "validation.run_health",
            "Run Health Validation",
            ExecutorFamily.Validation,
            OperationRisk.Low,
            new[] { "check project health", "validate project", "run health check" },
            Array.Empty<OperationParameter>(),
            Array.Empty<OperationPrecondition>(),
            Array.Empty<string>(),
            "Run UPA project health validation.",
            true,
            false));

        return r;
    }
}
