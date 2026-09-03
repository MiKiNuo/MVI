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
    /// 验证作用域拥有独立缓存、在作用域内解析构造依赖并释放缓存实例；
    /// 服务经静态工厂字典路由（含生命周期判定），工厂内构造依赖经接收器解析。
    /// </summary>
    [Test]
    public async Task Generate_Should_EmitOwnedScopedLifetimeAsync()
    {
        (GeneratorDriverRunResult runResult, bool emitSuccess) =
            GeneratorTestHost.RunGeneratorAndCompile<MviDiContainerGenerator>(
                ScopedServiceSource + "\n" + StubDefinitions);
        string generatedCode = runResult.GeneratedTrees.Single().GetText().ToString();

        await Assert.That(emitSuccess).IsTrue();
        await Assert.That(generatedCode).Contains("CreateServiceFactories");
        await Assert.That(generatedCode).Contains(
            "internal static readonly Dictionary<Type, (ServiceLifetime Lifetime, Func<IMviServiceFactoryReceiver, object> Factory)> _factories");
        await Assert.That(generatedCode).Contains(
            "static receiver => new global::TestApp.ScopedConsumer(receiver.Resolve<global::TestApp.ScopedDependency>())");
        await Assert.That(generatedCode).Contains("private readonly Dictionary<Type, object> _scoped = new();");
        await Assert.That(generatedCode).Contains("_scoped[serviceType] = created;");
        await Assert.That(generatedCode).Contains("if (instance is IDisposable disposable)");
        await Assert.That(generatedCode).Contains("return _container.Resolve(serviceType);");
    }

    /// <summary>
    /// 桩类型定义：模拟 DI 特性、生命周期枚举与应用层 DI 接口。
    /// </summary>
    private const string StubDefinitions = TestSupport.GeneratorTestStubs.DiContainerRuntime;

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

    /// <summary>
    /// 测试源代码：含作用域依赖链。
    /// </summary>
    private const string ScopedServiceSource = """
        using MiKiNuo.Mvi.Domain.DI;

        namespace TestApp
        {
            [DiService(ServiceLifetime.Singleton)]
            public sealed class SingletonDependency
            {
            }

            [DiService(ServiceLifetime.Scoped)]
            public sealed class ScopedDependency : System.IDisposable
            {
                public void Dispose() { }
            }

            [DiService(ServiceLifetime.Scoped)]
            public sealed class ScopedConsumer
            {
                public ScopedConsumer(ScopedDependency dependency) { }
            }
        }
        """;
}
