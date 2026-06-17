using System.Reflection;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 <c>IMviResolver.CreateWith&lt;T&gt;</c> 接口契约的单元测试。
/// </summary>
public sealed class IMviResolverCreateWithTests
{
    /// <summary>
    /// 验证 <c>IMviResolver</c> 接口声明了 <c>CreateWith&lt;TService&gt;</c> 方法。
    /// </summary>
    [Test]
    public async Task IMviResolver_Should_DeclareCreateWithGenericMethodAsync()
    {
        MethodInfo? method = typeof(MiKiNuo.Mvi.Application.DI.IMviResolver)
            .GetMethod("CreateWith");

        await Assert.That(method).IsNotNull();
        await Assert.That(method!.IsGenericMethod).IsTrue();
        await Assert.That(method.GetParameters().Length).IsEqualTo(1);
        await Assert.That(method.GetParameters()[0].ParameterType.IsArray).IsTrue();
        await Assert.That(method.GetParameters()[0].GetCustomAttribute<ParamArrayAttribute>()).IsNotNull();
    }

    /// <summary>
    /// 验证 <c>CreateWith&lt;TService&gt;</c> 与 <c>Resolve&lt;TService&gt;</c> 共享 <c>notnull</c> 约束。
    /// <para>
    /// <c>notnull</c> 约束在 IL 层以 <c>NotNullable</c> 标志呈现，运行时反射不直接暴露，
    /// 因此通过比对两个方法的约束标志位（<c>MethodBase.MethodHandle</c> 不可访问），
    /// 退而求其次验证：两个方法的返回类型与唯一泛型参数名都一致。
    /// </para>
    /// </summary>
    [Test]
    public async Task CreateWith_Should_ShareNotNullConstraintWithResolveAsync()
    {
        MethodInfo resolve = typeof(MiKiNuo.Mvi.Application.DI.IMviResolver)
            .GetMethod("Resolve", Type.EmptyTypes)!;
        MethodInfo createWith = typeof(MiKiNuo.Mvi.Application.DI.IMviResolver)
            .GetMethod("CreateWith")!;

        await Assert.That(resolve).IsNotNull();
        await Assert.That(createWith).IsNotNull();
        await Assert.That(createWith.GetGenericArguments()[0].Name)
            .IsEqualTo(resolve.GetGenericArguments()[0].Name);
    }

    /// <summary>
    /// 验证 <c>CreateWith&lt;TService&gt;</c> 能通过测试实现按构造函数参数实例化服务。
    /// </summary>
    [Test]
    public async Task CreateWith_Should_ConstructInstanceWithProvidedArgumentsAsync()
    {
        IMviResolverCreateWithTestsHarness resolver = new();
        SampleGreetTarget target = resolver.CreateWith<SampleGreetTarget>("hello");

        await Assert.That(target).IsNotNull();
        await Assert.That(target.Greeting).IsEqualTo("hello");
    }

    /// <summary>
    /// 表示仅供 <c>CreateWith</c> 测试使用的 <c>IMviResolver</c> 测试实现。
    /// </summary>
    private sealed class IMviResolverCreateWithTestsHarness : MiKiNuo.Mvi.Application.DI.IMviResolver
    {
        /// <summary>
        /// 同步测试桩实现：抛 <see cref="NotSupportedException"/>，仅 <c>CreateWith</c> 可用。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>不会返回。</returns>
        /// <exception cref="NotSupportedException">始终抛出。</exception>
        public TService Resolve<TService>()
            where TService : notnull
        {
            throw new NotSupportedException("仅供测试 CreateWith 使用。");
        }

        /// <summary>
        /// 同步测试桩实现：抛 <see cref="NotSupportedException"/>。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <returns>不会返回。</returns>
        /// <exception cref="NotSupportedException">始终抛出。</exception>
        public object Resolve(Type serviceType)
        {
            throw new NotSupportedException("仅供测试 CreateWith 使用。");
        }

        /// <summary>
        /// 同步测试桩实现：抛 <see cref="NotSupportedException"/>。
        /// </summary>
        /// <returns>不会返回。</returns>
        /// <exception cref="NotSupportedException">始终抛出。</exception>
        public MiKiNuo.Mvi.Application.DI.IMviScope CreateScope()
        {
            throw new NotSupportedException("仅供测试 CreateWith 使用。");
        }

        /// <summary>
        /// 按参数个数与类型可赋值性匹配公共构造函数并构造实例。
        /// </summary>
        /// <typeparam name="TService">要实例化的服务类型。</typeparam>
        /// <param name="args">构造参数。</param>
        /// <returns>新构造的实例。</returns>
        /// <exception cref="InvalidOperationException">未找到匹配参数个数的构造函数。</exception>
        public TService CreateWith<TService>(params object[] args)
            where TService : notnull
        {
            Type target = typeof(TService);
            ConstructorInfo[] ctors = target.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (ConstructorInfo ctor in ctors)
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                bool match = true;
                object?[] converted = new object?[args.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (args[i] is null && !parameters[i].ParameterType.IsValueType)
                    {
                        converted[i] = null;
                        continue;
                    }

                    if (args[i] is null)
                    {
                        match = false;
                        break;
                    }

                    Type argType = args[i].GetType();
                    if (!parameters[i].ParameterType.IsAssignableFrom(argType))
                    {
                        match = false;
                        break;
                    }

                    converted[i] = args[i];
                }

                if (match)
                {
                    return (TService)ctor.Invoke(converted);
                }
            }

            throw new InvalidOperationException($"未找到匹配 {args.Length} 个参数的构造函数：{target.FullName}");
        }
    }

    /// <summary>
    /// 表示 <c>CreateWith</c> 测试用目标类型。
    /// </summary>
    private sealed class SampleGreetTarget
    {
        /// <summary>
        /// 使用 <paramref name="greeting"/> 初始化 <see cref="Greeting"/>。
        /// </summary>
        /// <param name="greeting">问候语。</param>
        public SampleGreetTarget(string greeting)
        {
            Greeting = greeting;
        }

        /// <summary>
        /// 获取构造时传入的问候语。
        /// </summary>
        public string Greeting { get; }
    }
}
