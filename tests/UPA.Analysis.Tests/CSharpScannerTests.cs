using UPA.Analysis;
using UPA.Core;

namespace UPA.Analysis.Tests;

public class CSharpScannerTests
{
    [Fact]
    public void Scan_DiscoversUnityClassAndLifecycle()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var assets = Path.Combine(root.FullName, "Assets", "Scripts");
            Directory.CreateDirectory(assets);
            File.WriteAllText(Path.Combine(assets, "Player.cs"),
@"using UnityEngine;
namespace Game.Player;
[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    void Awake() {}
    void Update() {}
}");

            var result = new CSharpScanner().Scan(new ScanContext(root.FullName));
            var type = Assert.Single(Assert.Single(result).Types);

            Assert.Equal("Player", type.Name);
            Assert.Equal("Game.Player", type.Namespace);
            Assert.Equal("MonoBehaviour", type.BaseType);
            Assert.Contains("Awake", type.UnityLifecycleMethods);
            Assert.Contains("Update", type.UnityLifecycleMethods);
            Assert.Contains("Rigidbody", type.RequiredComponents);
            Assert.Single(type.SerializedFields);
        }
        finally { root.Delete(true); }
    }
}
