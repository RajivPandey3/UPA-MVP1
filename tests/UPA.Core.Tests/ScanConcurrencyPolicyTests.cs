using System;
using Xunit;

namespace UPA.Core.Tests;

public sealed class ScanConcurrencyPolicyTests
{
    [Fact]
    public void AcceptsSafeRange() => Assert.Equal(4, new ScanConcurrencyPolicy(4).MaxDegreeOfParallelism);

    [Theory]
    [InlineData(0)]
    [InlineData(65)]
    public void RejectsUnsafeRange(int value) => Assert.Throws<ArgumentOutOfRangeException>(() => new ScanConcurrencyPolicy(value));
}
