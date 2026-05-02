using MiKiNuo.Mvi.SourceGen;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.SourceGen.Tests;

public sealed class SourceGenProjectSmokeTests
{
    [Test]
    public async Task Assembly_exposes_source_generation_marker()
    {
        await Assert.That(SourceGenerationMarker.Name).IsEqualTo("MiKiNuo.Mvi.SourceGen");
    }
}
