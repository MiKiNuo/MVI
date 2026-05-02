using MiKiNuo.Mvi.Core.ViewModels;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class MviObservableObjectTests
{
    [Test]
    public async Task RaisePropertyChanged_raises_named_property()
    {
        var source = new TestObservableObject();
        var observedNames = new List<string>();
        source.PropertyChanged += (_, args) => observedNames.Add(args.PropertyName ?? string.Empty);

        source.Raise("UserName");

        await Assert.That(observedNames).IsEquivalentTo(new[] { "UserName" });
    }

    [Test]
    public async Task RaisePropertyChanged_without_subscribers_does_not_throw()
    {
        var source = new TestObservableObject();

        source.Raise("UserName");

        await Assert.That(source).IsNotNull();
    }

    private sealed class TestObservableObject : MviObservableObject
    {
        public void Raise(string propertyName)
        {
            RaisePropertyChanged(propertyName);
        }
    }
}
