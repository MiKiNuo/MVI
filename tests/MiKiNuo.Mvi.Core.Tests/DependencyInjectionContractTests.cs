using MiKiNuo.Mvi.Abstractions.DependencyInjection;
using MiKiNuo.Mvi.Core.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class DependencyInjectionContractTests
{
    [Test]
    public async Task Service_lifetime_values_are_stable()
    {
        var values = new Dictionary<MviServiceLifetime, int>
        {
            [MviServiceLifetime.Transient] = (int)MviServiceLifetime.Transient,
            [MviServiceLifetime.Scoped] = (int)MviServiceLifetime.Scoped,
            [MviServiceLifetime.Singleton] = (int)MviServiceLifetime.Singleton,
        };

        await Assert.That(values[MviServiceLifetime.Transient]).IsEqualTo(0);
        await Assert.That(values[MviServiceLifetime.Scoped]).IsEqualTo(1);
        await Assert.That(values[MviServiceLifetime.Singleton]).IsEqualTo(2);
    }

    [Test]
    public async Task Service_attribute_exposes_service_type_and_lifetime()
    {
        var attribute = new MviServiceAttribute(typeof(ISampleService), MviServiceLifetime.Scoped);
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(MviServiceAttribute),
            typeof(AttributeUsageAttribute))!;

        await Assert.That(attribute.ServiceType).IsEqualTo(typeof(ISampleService));
        await Assert.That(attribute.Lifetime).IsEqualTo(MviServiceLifetime.Scoped);
        await Assert.That(usage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(usage.AllowMultiple).IsTrue();
        await Assert.That(usage.Inherited).IsFalse();
    }

    [Test]
    public async Task Service_container_creates_async_disposable_scope()
    {
        IMviServiceContainer container = new TestContainer();

        await using var scope = container.CreateScope();

        await Assert.That(scope).IsNotNull();
    }

    [Test]
    public async Task Service_scope_is_async_disposable_contract()
    {
        IMviServiceScope scope = new TestScope();

        await scope.DisposeAsync();

        await Assert.That(((TestScope)scope).Disposed).IsTrue();
    }

    private sealed class TestContainer : IMviServiceContainer
    {
        public IMviServiceScope CreateScope()
        {
            return new TestScope();
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }

    private sealed class TestScope : IMviServiceScope
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }

    [Test]
    public async Task Generated_disposal_disposes_known_async_and_sync_instances()
    {
        var asyncDisposable = new SampleAsyncDisposable();
        var disposable = new SampleDisposable();
        object?[] instances = [asyncDisposable, disposable, null, new object()];

        await MviGeneratedDisposal.DisposeAsync(instances);

        await Assert.That(asyncDisposable.Disposed).IsTrue();
        await Assert.That(disposable.Disposed).IsTrue();
    }

    private sealed class SampleAsyncDisposable : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }

    private sealed class SampleDisposable : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private interface ISampleService;
}
