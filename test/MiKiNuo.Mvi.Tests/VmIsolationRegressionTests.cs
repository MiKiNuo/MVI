using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;
using MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench;
using MiKiNuo.Mvi.Samples.Godot.Features.Lobby;
using TUnit.Assertions;
using TUnit.Core;
using AvaloniaDashboard = MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.DashboardViewModel;
using AvaloniaOutpatient = MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient.OutpatientWorkstationViewModel;
using AvaloniaArchitectureValidation = MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ArchitectureValidation.ArchitectureValidationViewModel;
using AvaloniaEventBinding = MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench.EventBindingWorkbenchViewModel;
using GodotLobby = MiKiNuo.Mvi.Samples.Godot.Features.Lobby.LobbyViewModel;
using GodotEventBinding = MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench.EventBindingWorkbenchViewModel;
using AvaloniaEventBindingPanelFactory = MiKiNuo.Mvi.Samples.Avalonia.Features.EventBindingWorkbench.IEventBindingPanelFactory;
using GodotEventBindingPanelFactory = MiKiNuo.Mvi.Samples.Godot.Features.EventBindingWorkbench.IEventBindingPanelFactory;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示"ViewModel 不持有其他 ViewModel 引用"架构约束的回归测试。
/// <para>
/// 重构后，5 个父 ViewModel（OutpatientWorkstation / Dashboard / Lobby / ArchitectureValidation / EventBindingWorkbench）
/// 不再以强类型属性形式持有任何子 ViewModel 引用；改为依赖工厂按需解析子 VM。本测试对每个父 VM 验证两件事：
/// </para>
/// <list type="number">
/// <item>父 VM 类型上不暴露任何以 <c>*ViewModel</c> 结尾的公共属性（除工厂注入字段外的对外契约）。</item>
/// <item>父 VM 暴露若干以 <c>CreateXxxViewModel</c> 命名的工厂方法，作为 View 端按需解析的入口。</item>
/// </list>
/// <para>
/// Avalonia 与 Godot 平台均覆盖；Godot 平台仅检查 <c>LobbyViewModel</c> 与 <c>EventBindingWorkbenchViewModel</c>。
/// </para>
/// </summary>
public sealed class VmIsolationRegressionTests
{
    private static readonly string[] AvaloniaOutpatientChildViewModelPropertyNames =
    {
        "QueueViewModel",
        "EditorViewModel",
        "ReminderViewModel",
        "PatientQueueViewModel",
        "ClinicalEditorViewModel",
        "ClinicalReminderViewModel",
    };

    private static readonly string[] AvaloniaDashboardChildViewModelPropertyNames =
    {
        "HeaderViewModel",
        "MenuViewModel",
        "DashboardMenuViewModel",
        "OutpatientWorkstationViewModel",
        "ArchitectureValidationViewModel",
        "EventBindingWorkbenchViewModel",
    };

    private static readonly string[] GodotLobbyChildViewModelPropertyNames =
    {
        "HeaderViewModel",
        "MenuViewModel",
        "LobbyMenuViewModel",
        "ActivityLogViewModel",
        "PlayerHeaderViewModel",
        "MissionBoardViewModel",
        "HeroRosterViewModel",
        "InventoryViewModel",
        "ForgeLabViewModel",
        "BattlePrepViewModel",
    };

    private static readonly string[] AvaloniaArchitectureValidationChildViewModelPropertyNames =
    {
        "PatientSearchViewModel",
        "AuditTimelineViewModel",
        "MiddlewareMetricViewModel",
        "ReuseMetricViewModel",
        "MediatorMetricViewModel",
        "EffectMetricViewModel",
    };

    private static readonly string[] AvaloniaEventBindingChildViewModelPropertyNames =
    {
        "SearchViewModel",
        "SelectionViewModel",
        "DetailViewModel",
        "EventBindingSearchViewModel",
        "EventBindingSelectionViewModel",
        "EventBindingDetailViewModel",
    };

    private static readonly string[] GodotEventBindingChildViewModelPropertyNames =
    {
        "SearchViewModel",
        "SelectionViewModel",
        "DetailViewModel",
        "EventBindingSearchViewModel",
        "EventBindingSelectionViewModel",
        "EventBindingDetailViewModel",
    };

    /// <summary>
    /// 验证 <c>OutpatientWorkstationViewModel</c>（Avalonia）不再暴露任何子 ViewModel 属性。
    /// </summary>
    [Test]
    public async Task AvaloniaOutpatientWorkstationViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(AvaloniaOutpatient);

        foreach (string propertyName in AvaloniaOutpatientChildViewModelPropertyNames)
        {
            await Assert.That(viewModelType.GetProperty(propertyName)).IsNull();
        }
    }

    /// <summary>
    /// 验证 <c>OutpatientWorkstationViewModel</c>（Avalonia）暴露 3 个工厂方法。
    /// </summary>
    [Test]
    public async Task AvaloniaOutpatientWorkstationViewModel_ShouldExposeFactoryMethodsAsync()
    {
        Type viewModelType = typeof(AvaloniaOutpatient);

        await Assert.That(viewModelType.GetMethod("CreateQueueViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateEditorViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateReminderViewModel")).IsNotNull();
    }

    /// <summary>
    /// 验证 <c>DashboardViewModel</c>（Avalonia）不再暴露任何子 ViewModel 属性。
    /// </summary>
    [Test]
    public async Task AvaloniaDashboardViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(AvaloniaDashboard);

        foreach (string propertyName in AvaloniaDashboardChildViewModelPropertyNames)
        {
            await Assert.That(viewModelType.GetProperty(propertyName)).IsNull();
        }
    }

    /// <summary>
    /// 验证 <c>DashboardViewModel</c>（Avalonia）暴露 chrome / 页面工厂方法。
    /// </summary>
    [Test]
    public async Task AvaloniaDashboardViewModel_ShouldExposeFactoryMethodsAsync()
    {
        Type viewModelType = typeof(AvaloniaDashboard);

        await Assert.That(viewModelType.GetMethod("CreateHeaderViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateMenuViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateCurrentPageViewModel")).IsNotNull();
    }

    /// <summary>
    /// 验证 <c>ArchitectureValidationViewModel</c>（Avalonia）不再暴露任何子 ViewModel 属性。
    /// </summary>
    [Test]
    public async Task AvaloniaArchitectureValidationViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(AvaloniaArchitectureValidation);

        foreach (string propertyName in AvaloniaArchitectureValidationChildViewModelPropertyNames)
        {
            await Assert.That(viewModelType.GetProperty(propertyName)).IsNull();
        }
    }

    /// <summary>
    /// 验证 <c>EventBindingWorkbenchViewModel</c>（Avalonia）不再暴露任何子 ViewModel 属性。
    /// </summary>
    [Test]
    public async Task AvaloniaEventBindingWorkbenchViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(AvaloniaEventBinding);

        foreach (string propertyName in AvaloniaEventBindingChildViewModelPropertyNames)
        {
            await Assert.That(viewModelType.GetProperty(propertyName)).IsNull();
        }
    }

    /// <summary>
    /// 验证 <c>EventBindingWorkbenchViewModel</c>（Avalonia）暴露 3 个工厂方法。
    /// </summary>
    [Test]
    public async Task AvaloniaEventBindingWorkbenchViewModel_ShouldExposeFactoryMethodsAsync()
    {
        Type viewModelType = typeof(AvaloniaEventBinding);

        await Assert.That(viewModelType.GetMethod("CreateSearchViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateSelectionViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateDetailViewModel")).IsNotNull();
    }

    /// <summary>
    /// 验证 <c>LobbyViewModel</c>（Godot）不再暴露任何子 ViewModel 属性。
    /// </summary>
    [Test]
    public async Task GodotLobbyViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(GodotLobby);

        foreach (string propertyName in GodotLobbyChildViewModelPropertyNames)
        {
            await Assert.That(viewModelType.GetProperty(propertyName)).IsNull();
        }
    }

    /// <summary>
    /// 验证 <c>LobbyViewModel</c>（Godot）暴露 chrome / panel 工厂方法。
    /// </summary>
    [Test]
    public async Task GodotLobbyViewModel_ShouldExposeFactoryMethodsAsync()
    {
        Type viewModelType = typeof(GodotLobby);

        await Assert.That(viewModelType.GetMethod("CreateHeaderViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateMenuViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateActivityLogViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateCurrentPanelViewModel")).IsNotNull();
    }

    /// <summary>
    /// 验证 <c>EventBindingWorkbenchViewModel</c>（Godot）不再暴露任何子 ViewModel 属性。
    /// </summary>
    [Test]
    public async Task GodotEventBindingWorkbenchViewModel_ShouldNotExposeChildViewModelPropertiesAsync()
    {
        Type viewModelType = typeof(GodotEventBinding);

        foreach (string propertyName in GodotEventBindingChildViewModelPropertyNames)
        {
            await Assert.That(viewModelType.GetProperty(propertyName)).IsNull();
        }
    }

    /// <summary>
    /// 验证 <c>EventBindingWorkbenchViewModel</c>（Godot）暴露 3 个工厂方法。
    /// </summary>
    [Test]
    public async Task GodotEventBindingWorkbenchViewModel_ShouldExposeFactoryMethodsAsync()
    {
        Type viewModelType = typeof(GodotEventBinding);

        await Assert.That(viewModelType.GetMethod("CreateSearchViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateSelectionViewModel")).IsNotNull();
        await Assert.That(viewModelType.GetMethod("CreateDetailViewModel")).IsNotNull();
    }

    /// <summary>
    /// 验证所有工厂接口方法均显式声明 <c>public</c> 修饰符（满足 IDE0040 编码规范）。
    /// </summary>
    [Test]
    public async Task FactoryInterfaces_ShouldDeclarePublicAccessModifiersAsync()
    {
        Type[] factoryTypes =
        {
            typeof(IOutpatientSubPanelFactory),
            typeof(IDashboardChromeFactory),
            typeof(IDashboardPageFactory),
            typeof(IArchitectureValidationPanelFactory),
            typeof(AvaloniaEventBindingPanelFactory),
            typeof(ILobbyChromeFactory),
            typeof(ILobbyPanelFactory),
            typeof(GodotEventBindingPanelFactory),
        };

        foreach (Type factoryType in factoryTypes)
        {
            foreach (Type interfaceContract in factoryType.GetInterfaces())
            {
                foreach (System.Reflection.MethodInfo method in interfaceContract.GetMethods())
                {
                    await Assert.That(method.IsPublic).IsTrue();
                }
            }
        }
    }
}
