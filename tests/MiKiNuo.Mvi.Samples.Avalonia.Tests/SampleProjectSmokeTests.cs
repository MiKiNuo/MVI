using MiKiNuo.Mvi.Samples.Avalonia;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Samples.Avalonia.Tests;

public sealed class SampleProjectSmokeTests
{
    [Test]
    public async Task Sample_assembly_exposes_avalonia_app_type()
    {
        var assembly = typeof(App).Assembly;

        await Assert.That(assembly.GetName().Name).IsEqualTo("MiKiNuo.Mvi.Samples.Avalonia");
        await Assert.That(typeof(App).IsPublic).IsTrue();
        await Assert.That(typeof(MainWindow).IsPublic).IsTrue();
    }
}
