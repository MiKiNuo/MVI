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
    /// 验证含 [DiService] 的类触发生成器产出容器代码。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceContainerCodeAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            StubDefinitions + "\n" + ServiceSource);

        await Assert.That(result.GeneratedTrees.Length).IsGreaterThan(0);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("GeneratedMviContainer");
    }

    /// <summary>
    /// 验证生成的代码包含 Resolve 泛型方法。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceResolveMethodAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            StubDefinitions + "\n" + ServiceSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("public TService Resolve<TService>");
    }

    /// <summary>
    /// 验证生成的代码包含服务类型注册。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceServiceRegistrationAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            StubDefinitions + "\n" + ServiceSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("TestService");
        await Assert.That(generatedCode).Contains("ServiceLifetime.Singleton");
    }

    /// <summary>
    /// 验证生成的代码包含 CreateWith 方法。
    /// </summary>
    [Test]
    public async Task Generate_Should_ProduceCreateWithMethodAsync()
    {
        GeneratorDriverRunResult result = GeneratorTestHost.RunGenerator<MviDiContainerGenerator>(
            StubDefinitions + "\n" + ServiceSource);

        string generatedCode = result.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("CreateWith<TService>");
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
    /// 桩类型定义：模拟 DI 特性与生命周期枚举。
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
        """;

    /// <summary>
    /// 测试源代码：含 [DiService] 标记的服务类。
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
