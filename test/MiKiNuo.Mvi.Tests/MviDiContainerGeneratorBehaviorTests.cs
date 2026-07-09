using Microsoft.CodeAnalysis;
using MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>MviDiContainerGenerator</c> 源生成器的行为测试。
/// 使用 <c>CSharpGeneratorDriver</c> 驱动生成器并验证生成产物。
/// </summary>
public sealed class MviDiContainerGeneratorBehaviorTests
{
    /// <summary>
    /// 验证含 [DiService] 的类
    /// 触发生成器产出可编译的容器代码。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableContainerCodeAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                ServiceSource + "\n" + StubDefinitions);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的 Resolve 泛型方法可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableResolveMethodAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                ServiceSource + "\n" + StubDefinitions);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的服务类型注册可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableServiceRegistrationAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                ServiceSource + "\n" + StubDefinitions);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证生成的 CreateWith 方法可成功编译。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCompilableCreateWithMethodAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                ServiceSource + "\n" + StubDefinitions);

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    /// <summary>
    /// 验证无 [DiService] 的编译不触发生成器。
    /// </summary>
    [Test]
    public async Task Generate_Should_NotProduceCode_ForCompilationWithoutDiServiceAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            StubDefinitions + "\n" + PlainSource);

        await Assert.That(result.GeneratedTrees.Length).IsEqualTo(0);
    }

    /// <summary>
    /// 桩类型定义：模拟 DI 特性、生命周期枚举与应用层 DI 接口。
    /// </summary>
    private const string StubDefinitions = """
        namespace MiKiNuo.Mvi.Domain.DI
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
            public sealed class DiServiceAttribute : System.Attribute
            {
                public DiServiceAttribute(ServiceLifetime lifetime) { Lifetime = lifetime; }
                public ServiceLifetime Lifetime { get; }
                public System.Type? ServiceType { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
            public sealed class DiConstructorAttribute : System.Attribute { }

            public enum ServiceLifetime { Singleton = 0, Scoped = 1, Transient = 2 }
        }

        namespace MiKiNuo.Mvi.Application.DI
        {
            public interface IMviResolver
            {
                TService Resolve<TService>() where TService : notnull;
                TService CreateWith<TService>(params object[] args) where TService : notnull;
                object Resolve(System.Type serviceType);
                IMviScope CreateScope();
            }

            public interface IMviServiceGraph
            {
                System.Collections.Generic.IReadOnlyList<MviServiceDescriptor> ServiceDescriptors { get; }
            }

            public interface IMviScope : System.IDisposable
            {
                TService Resolve<TService>() where TService : notnull;
                object Resolve(System.Type serviceType);
            }

            public sealed class MviServiceDescriptor
            {
                public MviServiceDescriptor(
                    System.Type serviceType,
                    System.Type implementationType,
                    MiKiNuo.Mvi.Domain.DI.ServiceLifetime lifetime)
                {
                    ServiceType = serviceType;
                    ImplementationType = implementationType;
                    Lifetime = lifetime;
                }

                public System.Type ServiceType { get; }
                public System.Type ImplementationType { get; }
                public MiKiNuo.Mvi.Domain.DI.ServiceLifetime Lifetime { get; }
            }
        }
        """;

    /// <summary>
    /// 测试源代码：含 [DiService] 标记的服务类。
    /// 放在桩定义之前拼接,确保 using 语句位于文件顶部。
    /// </summary>
    private const string ServiceSource = """
        using MiKiNuo.Mvi.Domain.DI;

        namespace TestApp
        {
            [DiService(ServiceLifetime.Singleton)]
            public sealed class TestService
            {
            }
        }
        """;

    /// <summary>
    /// 测试源代码：无 [DiService] 标记的普通类。
    /// </summary>
    private const string PlainSource = """
        namespace TestApp
        {
            public sealed class PlainClass
            {
            }
        }
        """;
}
