namespace MiKiNuo.Mvi.Tests.TestSupport;

/// <summary>
/// 表示源生成器行为测试共享的桩定义核。
/// 各行为测试只需追加场景专有桩，禁止再逐字复制 MVI 标记接口与 StatePath 运行时桩。
/// </summary>
internal static class GeneratorTestStubs
{
    /// <summary>
    /// MVI 状态标记接口桩。
    /// </summary>
    public const string StateContracts = """
        namespace MiKiNuo.Mvi.Domain.MVI.State
        {
            public interface IMviState { }
        }
        """;

    /// <summary>
    /// StatePath 运行时与切片特性桩（依赖 <see cref="StateContracts"/>）。
    /// </summary>
    public const string StatePathRuntime = """
        namespace MiKiNuo.Mvi.Domain.MVI.State
        {
            public readonly struct StatePath<TState, TValue> where TState : IMviState
            {
                public StatePath(string displayPath, System.Func<TState, TValue> getter)
                {
                    DisplayPath = displayPath;
                    Getter = getter;
                }

                public string DisplayPath { get; }

                public System.Func<TState, TValue> Getter { get; }

                public static StatePath<TState, TValue> Create(
                    string displayPath,
                    System.Func<TState, TValue> getter) => new(displayPath, getter);
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class MviStateSliceAttribute : System.Attribute
            {
                public MviStateSliceAttribute(System.Type stateType) { StateType = stateType; }

                public System.Type StateType { get; }
            }
        }
        """;

    /// <summary>
    /// 编译期 DI 容器运行时桩（特性、枚举、解析接口与服务描述符）。
    /// </summary>
    public const string DiContainerRuntime = """
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
    /// MVI Store 运行时桩（标记接口、Reducer/Handler/Dispatcher 契约、Store 与 Feature 特性）。
    /// </summary>
    public const string MviStoreRuntime = """
        namespace MiKiNuo.Mvi.Domain.MVI.State
        {
            public interface IMviState { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Intent
        {
            public interface IMviIntent { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Effect
        {
            public interface IMviEffect { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Feature
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class MviFeatureModuleAttribute : System.Attribute
            {
                public MviFeatureModuleAttribute(string featureKey) { FeatureKey = featureKey; }

                public string FeatureKey { get; }

                public string DisplayName { get; set; } = string.Empty;
            }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Business
        {
            public interface IMviBusinessResult { }
        }

        namespace MiKiNuo.Mvi.Domain.MVI.Reducer
        {
            public sealed class MviReduceResult<TState, TEffect>
            {
                public MviReduceResult(TState state) { State = state; }

                public TState State { get; }
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.Reducer
        {
            public interface IMviReducer<TState, TIntent, TEffect>
            {
                MiKiNuo.Mvi.Domain.MVI.Reducer.MviReduceResult<TState, TEffect> Reduce(
                    TState state,
                    TIntent intent,
                    MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult? result = null);
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.IntentHandler
        {
            public interface IMviIntentHandler<TState, TIntent, TEffect>
            {
                System.Threading.Tasks.ValueTask<MiKiNuo.Mvi.Domain.MVI.Business.IMviBusinessResult?> HandleAsync(
                    TState state,
                    TIntent intent,
                    System.Threading.CancellationToken cancellationToken = default);
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.Effect
        {
            public interface IMviEffectDispatcher<TEffect>
            {
                System.Threading.Tasks.ValueTask DispatchAsync(
                    TEffect effect,
                    System.Threading.CancellationToken cancellationToken = default);
            }
        }

        namespace MiKiNuo.Mvi.Application.MVI.Store
        {
            public interface IMviStore<TState, TIntent, TEffect> : System.IDisposable { }

            public sealed class MviStore<TState, TIntent, TEffect> : IMviStore<TState, TIntent, TEffect>
            {
                public MviStore(
                    TState initialState,
                    MiKiNuo.Mvi.Application.MVI.IntentHandler.IMviIntentHandler<TState, TIntent, TEffect> intentHandler,
                    MiKiNuo.Mvi.Application.MVI.Reducer.IMviReducer<TState, TIntent, TEffect> reducer,
                    MiKiNuo.Mvi.Application.MVI.Effect.IMviEffectDispatcher<TEffect> effectDispatcher)
                {
                }

                public void Dispose() { }
            }
        }
        """;
}
