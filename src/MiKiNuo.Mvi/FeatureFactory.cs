using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace MiKiNuo.Mvi;

/// <summary>通过标准服务提供方为每个功能创建独立且由实例拥有的服务范围。</summary>
public static class FeatureFactory
{
    private static readonly AsyncLocal<Creation?> Current = new();

    /// <summary>创建独立功能与服务范围，按标准 DI 的公开构造函数选择规则解析依赖、默认值及 keyed 服务。</summary>
    /// <typeparam name="TFeature">通过构造函数注入服务的功能声明类型，不要求注册为 scoped 服务。</typeparam>
    /// <param name="services">拥有 singleton 服务的根服务提供方。</param>
    /// <returns>拥有本次范围的独立实例；创建失败先等已创建实例退出再释放范围。</returns>
    public static async ValueTask<TFeature> CreateAsync<TFeature>(IServiceProvider services) where TFeature : Feature
    {
        ArgumentNullException.ThrowIfNull(services);
        AsyncServiceScope scope = services.CreateAsyncScope();
        Creation creation = new();
        Creation? previous = Current.Value;
        Current.Value = creation;
        try
        {
            ConstructorInfo constructor = SelectConstructor(typeof(TFeature), scope.ServiceProvider);
            // 构造失败也必须持有真正目标；同型依赖、基类实参与构造体中的直接 new 都不归本次创建所有。
            TFeature feature = (TFeature)RuntimeHelpers.GetUninitializedObject(typeof(TFeature));
            creation.Target = feature;
            object?[] arguments = constructor.GetParameters().Select(parameter => ResolveParameter(parameter, scope.ServiceProvider)).ToArray();
            constructor.Invoke(feature, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
            feature.OwnScope(scope);
            return feature;
        }
        catch (Exception failure)
        {
            // 构造函数可能已经启动所属工作；失败回收也不能提前释放其依赖。
            Current.Value = previous;
            List<Exception> failures = [failure];
            if (creation.Instance is Feature created)
            {
                try
                {
                    ReleaseResult released = await created.Close().Ticket.Released.ConfigureAwait(false);
                    if (released.Exception is not null) failures.Add(released.Exception);
                }
                catch (Exception cleanupFailure)
                {
                    failures.Add(cleanupFailure);
                }
            }

            try
            {
                await scope.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception releaseFailure)
            {
                failures.Add(releaseFailure);
            }

            if (failures.Count > 1) throw new AggregateException(failures);
            throw;
        }
        finally
        {
            Current.Value = previous;
        }
    }

    internal static void Capture(Feature feature)
    {
        Creation? creation = Current.Value;
        if (creation is not null && ReferenceEquals(creation.Target, feature))
        {
            creation.Instance = feature;
        }
    }

    private static ConstructorInfo SelectConstructor(Type type, IServiceProvider services)
    {
        if (type.IsAbstract) throw new InvalidOperationException($"不能创建抽象功能类型 {type}。");
        ConstructorInfo[] constructors = type.GetConstructors();
        ConstructorInfo[] preferred = constructors.Where(constructor => constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute))).ToArray();
        if (preferred.Length > 1) throw new InvalidOperationException($"功能 {type} 有多个标记的构造函数。");
        IServiceProviderIsService? probe = services.GetService<IServiceProviderIsService>();
        if (preferred.Length == 1)
        {
            if (probe is not null && !preferred[0].GetParameters().All(parameter => CanResolve(parameter, probe)))
            {
                throw new InvalidOperationException($"功能 {type} 的标记构造函数存在无法解析的依赖。");
            }

            return preferred[0];
        }

        if (probe is not null)
        {
            ConstructorInfo[] candidates = constructors.Where(constructor => constructor.GetParameters().All(parameter => CanResolve(parameter, probe))).ToArray();
            if (candidates.Length != 0)
            {
                int length = candidates.Max(constructor => constructor.GetParameters().Length);
                constructors = candidates.Where(constructor => constructor.GetParameters().Length == length).ToArray();
            }
        }

        if (constructors.Length != 1) throw new InvalidOperationException($"功能 {type} 需要唯一可选的公开构造函数，当前候选数为 {constructors.Length}。");
        return constructors[0];
    }

    private static bool CanResolve(ParameterInfo parameter, IServiceProviderIsService services)
    {
        object? key = parameter.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
        bool available = key is null ? services.IsService(parameter.ParameterType)
            : services is IServiceProviderIsKeyedService keyed ? keyed.IsKeyedService(parameter.ParameterType, key)
            : throw new InvalidOperationException("服务提供方不支持 keyed 服务查询。");
        return available || parameter.HasDefaultValue;
    }

    private static object? ResolveParameter(ParameterInfo parameter, IServiceProvider services)
    {
        object? key = parameter.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
        object? value = key is null ? services.GetService(parameter.ParameterType)
            : services is IKeyedServiceProvider keyed ? keyed.GetKeyedService(parameter.ParameterType, key)
            : throw new InvalidOperationException("服务提供方不支持 keyed 服务解析。");
        if (value is not null) return value;
        if (parameter.HasDefaultValue)
        {
            object? fallback = parameter.DefaultValue;
            Type? nullable = Nullable.GetUnderlyingType(parameter.ParameterType);
            return nullable?.IsEnum == true && fallback is not null ? Enum.ToObject(nullable, fallback) : fallback;
        }

        throw new InvalidOperationException($"无法为功能 {parameter.Member.DeclaringType} 解析构造依赖 {parameter.ParameterType}。");
    }

    private sealed class Creation
    {
        internal Feature? Target { get; set; }
        internal Feature? Instance { get; set; }
    }
}
