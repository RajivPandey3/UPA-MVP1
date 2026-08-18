using UPA.Core;
namespace UPA.Core.Tests;
public class EntityIdTests {
    [Fact] public void NewIds_AreDifferent() => Assert.NotEqual(EntityId.New(), EntityId.New());
}