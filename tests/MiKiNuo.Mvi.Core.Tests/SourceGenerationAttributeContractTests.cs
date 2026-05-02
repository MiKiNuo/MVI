using MiKiNuo.Mvi.Abstractions.Binding;
using MiKiNuo.Mvi.Abstractions.Generation;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Core.Tests;

public sealed class SourceGenerationAttributeContractTests
{
    [Test]
    public async Task Intent_attributes_expose_expected_contracts()
    {
        var unionUsage = GetAttributeUsage(typeof(MviIntentUnionAttribute));
        var kindUsage = GetAttributeUsage(typeof(MviIntentKindAttribute));
        var handlerUsage = GetAttributeUsage(typeof(MviIntentHandlerAttribute));
        var kind = new MviIntentKindAttribute(7);
        var handler = new MviIntentHandlerAttribute(typeof(SampleIntent));

        await Assert.That(unionUsage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(kindUsage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(handlerUsage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(kind.Kind).IsEqualTo(7);
        await Assert.That(handler.IntentType).IsEqualTo(typeof(SampleIntent));
    }

    [Test]
    public async Task ViewModel_binding_attributes_expose_expected_contracts()
    {
        var viewModelUsage = GetAttributeUsage(typeof(MviViewModelAttribute));
        var stateBinding = new MviStateBindingAttribute("UserName");
        var twoWayBinding = new MviTwoWayBindingAttribute("Password", typeof(PasswordChangedIntent));
        var command = new MviCommandAttribute(typeof(SubmitIntent))
        {
            CanExecuteStatePropertyName = "CanSubmit",
            ParameterType = typeof(string),
        };
        var argument = new MviCommandArgumentAttribute("userName", "UserName");

        await Assert.That(viewModelUsage.ValidOn).IsEqualTo(AttributeTargets.Class);
        await Assert.That(stateBinding.StatePropertyName).IsEqualTo("UserName");
        await Assert.That(twoWayBinding.StatePropertyName).IsEqualTo("Password");
        await Assert.That(twoWayBinding.IntentType).IsEqualTo(typeof(PasswordChangedIntent));
        await Assert.That(twoWayBinding.UpdateMode).IsEqualTo(MviTwoWayUpdateMode.StateFirst);
        await Assert.That(command.IntentType).IsEqualTo(typeof(SubmitIntent));
        await Assert.That(command.CanExecuteStatePropertyName).IsEqualTo("CanSubmit");
        await Assert.That(command.ParameterType).IsEqualTo(typeof(string));
        await Assert.That(argument.IntentParameterName).IsEqualTo("userName");
        await Assert.That(argument.ViewModelPropertyName).IsEqualTo("UserName");
    }

    [Test]
    public async Task Mediator_registry_and_middleware_attributes_expose_expected_contracts()
    {
        var mediator = new MviMediatorAttribute();
        var route = new MviRouteAttribute(typeof(SampleRequest), typeof(SampleResponse));
        var view = new MviViewRegistryAttribute(typeof(SampleView), typeof(SampleViewModel));
        var middleware = new MviMiddlewareAttribute(20);

        await Assert.That(mediator).IsNotNull();
        await Assert.That(route.RequestType).IsEqualTo(typeof(SampleRequest));
        await Assert.That(route.ResponseType).IsEqualTo(typeof(SampleResponse));
        await Assert.That(view.ViewType).IsEqualTo(typeof(SampleView));
        await Assert.That(view.ViewModelType).IsEqualTo(typeof(SampleViewModel));
        await Assert.That(middleware.Order).IsEqualTo(20);
    }

    private static AttributeUsageAttribute GetAttributeUsage(Type type)
    {
        return (AttributeUsageAttribute)Attribute.GetCustomAttribute(type, typeof(AttributeUsageAttribute))!;
    }

    private sealed class SampleIntent;

    private sealed class PasswordChangedIntent;

    private sealed class SubmitIntent;

    private sealed class SampleRequest;

    private sealed class SampleResponse;

    private sealed class SampleView;

    private sealed class SampleViewModel;
}
