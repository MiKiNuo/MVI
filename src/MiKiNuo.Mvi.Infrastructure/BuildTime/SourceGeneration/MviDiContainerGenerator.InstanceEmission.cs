using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>发射具有独立对象图的 Feature 实例工厂。</summary>
public sealed partial class MviDiContainerGenerator
{
    internal static partial class Emission
    {
        /// <summary>发射实例工厂及复用服务工厂表的局部解析器。</summary>
        /// <param name="builder">代码缓冲。</param>
        /// <param name="features">Feature 定义。</param>
        /// <param name="services">普通服务定义。</param>
        private static void EmitInstanceFactories(StringBuilder builder, IReadOnlyList<Models.MviFeatureInfo> features, IReadOnlyList<Models.DiServiceInfo> services)
        {
            if (!features.Any(feature => feature.ViewModel is not null))
            {
                return;
            }

            foreach (Models.MviFeatureInfo feature in features)
            {
                if (feature.ViewModel is null)
                {
                    continue;
                }

                string handle = "global::MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<" + feature.ViewModel.TypeName + ">";
                string result = "global::System.Threading.Tasks.ValueTask<" + handle + ">";
                string endpoint = "global::MiKiNuo.Mvi.Application.MVI.Mediator.MviMediatorEndpoint";
                string method = feature.InstanceMethodName;
                string coreMethod = feature.InstanceCoreMethodName;
                builder.AppendLine("    /// <summary>以默认状态创建独立 Feature 实例，接管端点所有权。</summary>");
                builder.AppendLine("    /// <param name=\"endpoint\">该实例独占的中介端点。</param>");
                builder.AppendLine("    /// <returns>拥有独立状态和资源的实例。</returns>");
                builder.AppendLine("    public " + result + " " + method + "(" + endpoint + " endpoint)");
                builder.AppendLine("        => " + method + "(endpoint, " + feature.StateTypeName + ".Initial);");
                builder.AppendLine("    /// <summary>以指定状态创建独立 Feature 实例，失败时回收已创建资源。</summary>");
                builder.AppendLine("    /// <param name=\"endpoint\">该实例独占的中介端点。</param>");
                builder.AppendLine("    /// <param name=\"initialState\">不可变初始状态。</param>");
                builder.AppendLine("    /// <returns>拥有独立状态和资源的实例。</returns>");
                builder.AppendLine("    public async " + result + " " + method + "(" + endpoint + " endpoint, " + feature.StateTypeName + " initialState)");
                builder.AppendLine("    {");
                builder.AppendLine("        (" + handle + " instance, _) = await " + coreMethod + "(endpoint, initialState).ConfigureAwait(false);");
                builder.AppendLine("        return instance;");
                builder.AppendLine("    }");
                builder.AppendLine("    /// <summary>创建实例并返回实例服务表，供组合构建器完成接线。</summary>");
                builder.AppendLine("    /// <param name=\"endpoint\">该实例独占的中介端点。</param>");
                builder.AppendLine("    /// <param name=\"initialState\">不可变初始状态。</param>");
                builder.AppendLine("    /// <returns>实例与其构造期服务表。</returns>");
                builder.AppendLine("    internal async global::System.Threading.Tasks.ValueTask<(" + handle + " Instance, InstanceServices Services)> "
                    + coreMethod + "(" + endpoint + " endpoint, " + feature.StateTypeName + " initialState)");
                builder.AppendLine("    {");
                builder.AppendLine("        ArgumentNullException.ThrowIfNull(endpoint);");
                builder.AppendLine("        ArgumentNullException.ThrowIfNull(initialState);");
                builder.AppendLine("        InstanceServices services = new(this, endpoint);");
                builder.AppendLine("        try");
                builder.AppendLine("        {");
                EmitInstanceComponent(builder, feature.Reducer);
                if (feature.EffectDispatcher is not null)
                {
                    EmitInstanceComponent(builder, feature.EffectDispatcher);
                }

                foreach (Models.FeatureComponentInfo middleware in feature.Middlewares)
                {
                    EmitInstanceComponent(builder, middleware);
                }

                string store = "global::MiKiNuo.Mvi.Application.MVI.Store.MviStore<" + feature.StateTypeName + ", " + feature.IntentTypeName + ", " + feature.EffectTypeName + ">";
                string dispatcher = feature.EffectDispatcher is not null
                    ? "services.Resolve<" + feature.EffectDispatcher.TypeName + ">()"
                    : feature.EffectTypeName == "global::MiKiNuo.Mvi.Domain.MVI.Effect.UnitEffect"
                        ? "global::MiKiNuo.Mvi.Application.MVI.Effect.NullEffectDispatcher.Instance"
                        : "throw new InvalidOperationException(\"Feature 缺少副作用分发器。\")";
                string middlewareType = "global::MiKiNuo.Mvi.Application.MVI.Middleware.IMviMiddleware<" + feature.StateTypeName + ", " + feature.IntentTypeName + ", " + feature.EffectTypeName + ">";
                builder.AppendLine("            services.Factories.Add(typeof(" + feature.StoreTypeName + "), () => new " + store + "(");
                builder.AppendLine("                initialState, services.Resolve<" + feature.Reducer.TypeName + ">(), " + dispatcher + ",");
                builder.AppendLine("                new " + middlewareType + "[] { " + string.Join(", ", feature.Middlewares.Select(middleware => "services.Resolve<" + middleware.TypeName + ">()")) + " }));");
                EmitInstanceComponent(builder, feature.ViewModel);
                builder.AppendLine("            " + feature.ViewModel.TypeName + " vm = services.Resolve<" + feature.ViewModel.TypeName + ">();");
                builder.AppendLine("            " + handle + " instance = new(endpoint.InstanceId, vm, services.Resources);");
                builder.AppendLine("            _ = instance.Lifetime.Register(endpoint.Dispose);");
                builder.AppendLine("            return (instance, services);");
                builder.AppendLine("        }");
                builder.AppendLine("        catch (Exception failure)");
                builder.AppendLine("        {");
                builder.AppendLine("            try");
                builder.AppendLine("            {");
                builder.AppendLine("                await new global::MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance<object>(endpoint.InstanceId, services, services.Resources).DisposeAsync().ConfigureAwait(false);");
                builder.AppendLine("            }");
                builder.AppendLine("            catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }");
                builder.AppendLine("            throw;");
                builder.AppendLine("        }");
                builder.AppendLine("    }");
            }

            HashSet<string> ownedTypes = new(System.StringComparer.Ordinal)
            {
                MediatorTypeName,
                "global::MiKiNuo.Mvi.Application.MVI.Mediator.MviMediatorEndpoint",
                "global::MiKiNuo.Mvi.Application.DI.IMviScope",
                "global::MiKiNuo.Mvi.Application.DI.IMviResolver",
            };
            foreach (Models.MviFeatureInfo feature in features)
            {
                ownedTypes.Add(feature.StoreTypeName);
                ownedTypes.Add(feature.Reducer.TypeName);
                if (feature.ViewModel is not null) ownedTypes.Add(feature.ViewModel.TypeName);
                if (feature.EffectDispatcher is not null) ownedTypes.Add(feature.EffectDispatcher.TypeName);
                foreach (Models.FeatureComponentInfo middleware in feature.Middlewares) ownedTypes.Add(middleware.TypeName);
            }
            Dictionary<string, Models.DiServiceInfo> serviceMap = services.GroupBy(service => service.ServiceTypeName)
                .ToDictionary(group => group.Key, group => group.First(), System.StringComparer.Ordinal);
            builder.AppendLine("    private static readonly HashSet<Type> _instanceCapturingSingletons = new() {");
            foreach (Models.DiServiceInfo service in services)
            {
                if (service.Lifetime == Models.GeneratedLifetime.Singleton && CapturesInstance(service.ServiceTypeName, ownedTypes, serviceMap, new HashSet<string>(System.StringComparer.Ordinal)))
                    builder.AppendLine("        typeof(" + service.ServiceTypeName + "),");
            }
            builder.AppendLine("    };");
            builder.AppendLine("""
                internal sealed class InstanceServices : IMviServiceFactoryReceiver
                {
                    private readonly GeneratedMviContainer _root;
                    private readonly Dictionary<Type, object> _cache = new();
                    private readonly HashSet<Type> _constructing = new();
                    public Dictionary<Type, Func<object>> Factories { get; } = new();
                    public List<object> Resources { get; } = new();
                    public InstanceServices(GeneratedMviContainer root, global::MiKiNuo.Mvi.Application.MVI.Mediator.MviMediatorEndpoint endpoint)
                    {
                        _root = root;
                        _cache.Add(typeof(global::MiKiNuo.Mvi.Application.MVI.Mediator.IMviMediator), endpoint);
                        _cache.Add(typeof(global::MiKiNuo.Mvi.Application.MVI.Mediator.MviMediatorEndpoint), endpoint);
                        _cache.Add(typeof(global::MiKiNuo.Mvi.Application.MVI.Threading.IMviUiDispatcher), root._uiDispatcher);
                        Resources.Add(endpoint);
                    }
                    public TService Resolve<TService>() where TService : notnull => (TService)Resolve(typeof(TService));
                    private object Resolve(Type type)
                    {
                        if (_cache.TryGetValue(type, out object? cached)) return cached;
                        if (!_constructing.Add(type)) throw new InvalidOperationException($"Feature 构造依赖循环：{type}");
                        try
                        {
                            object created;
                            bool cache;
                            if (Factories.TryGetValue(type, out Func<object>? factory))
                            {
                                created = factory();
                                cache = true;
                            }
                            else if (_factories.TryGetValue(type, out (ServiceLifetime Lifetime, Func<IMviServiceFactoryReceiver, object> Factory) entry))
                            {
                                if (entry.Lifetime == ServiceLifetime.Singleton)
                                {
                                    if (_instanceCapturingSingletons.Contains(type)) throw new InvalidOperationException($"单例不能捕获 Feature 或作用域依赖：{type}");
                                    return _root.Resolve(type);
                                }
                                created = entry.Factory(this);
                                cache = entry.Lifetime == ServiceLifetime.Scoped;
                            }
                            else throw new InvalidOperationException($"实例内未注册服务：{type}");
                            if (cache) _cache.Add(type, created);
                            if (!Resources.Exists(resource => ReferenceEquals(resource, created))) Resources.Add(created);
                            return created;
                            }
                            finally { _constructing.Remove(type); }
                        }
                    }
                """);
        }

        /// <summary>沿已知构造图检查单例是否捕获实例依赖。</summary>
        /// <param name="type">当前服务类型。</param>
        /// <param name="owned">实例独占类型集合。</param>
        /// <param name="services">服务图。</param>
        /// <param name="visited">当前遍历访问集合。</param>
        /// <returns>存在实例或作用域依赖时为真。</returns>
        private static bool CapturesInstance(string type, HashSet<string> owned, Dictionary<string, Models.DiServiceInfo> services, HashSet<string> visited)
        {
            type = type.TrimEnd('?');
            if (owned.Contains(type)) return true;
            if (!visited.Add(type) || !services.TryGetValue(type, out Models.DiServiceInfo? service)) return false;
            return service.Lifetime == Models.GeneratedLifetime.Scoped
                || service.ConstructorParameterTypeNames.Any(dependency => CapturesInstance(dependency, owned, services, visited));
        }

        /// <summary>注册实例独占组件的延迟工厂，构造实参经实例服务表解析。</summary>
        /// <param name="builder">代码缓冲。</param>
        /// <param name="component">组件信息。</param>
        private static void EmitInstanceComponent(StringBuilder builder, Models.FeatureComponentInfo component)
        {
            string arguments = string.Join(", ", component.ConstructorParameterTypeNames
                .Select(parameterType => "services.Resolve<" + parameterType + ">()"));
            builder.AppendLine("            services.Factories.Add(typeof(" + component.TypeName + "), () => new "
                + component.TypeName + "(" + arguments + "));");
        }
    }
}
