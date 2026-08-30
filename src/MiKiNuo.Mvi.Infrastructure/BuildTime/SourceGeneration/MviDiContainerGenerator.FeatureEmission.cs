using System.Collections.Generic;
using System.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 Feature 装配发射部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    internal static partial class Emission
    {
        private const string UiDispatcherTypeName =
            "global::MiKiNuo.Mvi.Application.MVI.Threading.IMviUiDispatcher";

        private const string MediatorTypeName =
            "global::MiKiNuo.Mvi.Application.MVI.Mediator.IMviMediator";

        /// <summary>
        /// 生成支持 Feature 装配的 DI 容器源码。
        /// </summary>
        /// <param name="assemblyName">目标程序集名称。</param>
        /// <param name="services">DI 服务信息集合。</param>
        /// <param name="features">Feature 装配模型集合。</param>
        /// <returns>生成的 C# 源码。</returns>
        public static string GenerateContainerSource(
            string assemblyName,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.MviFeatureInfo> features)
        {
            StringBuilder builder = new();
            string containerNamespace = string.IsNullOrEmpty(assemblyName) ? "GeneratedContainer" : assemblyName;

            EmitFileHeader(builder, containerNamespace, services);
            EmitFeatureFields(builder, features);
            EmitConstructor(builder, services, features);
            EmitResolveMethods(builder, services, features);
            EmitCreateScope(builder);
            EmitCreateWith(builder, services);
            EmitFeatureFactories(builder, features);
            EmitScopeClass(builder, services);

            return builder.ToString();
        }

        /// <summary>
        /// 发射 Feature 装配所需的字段与辅助方法（仅当存在 Feature 时）。
        /// </summary>
        private static void EmitFeatureFields(
            StringBuilder builder,
            IReadOnlyList<Models.MviFeatureInfo> features)
        {
            if (features.Count == 0)
            {
                return;
            }

            builder.AppendLine("    private readonly " + UiDispatcherTypeName + " _uiDispatcher;");
            builder.AppendLine("    private readonly " + MediatorTypeName + " _mediator;");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 获取跨 Feature 协调中介者，供组合根注册路由。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public " + MediatorTypeName + " Mediator => _mediator;");
            builder.AppendLine();
            builder.AppendLine("    private object GetSingleton(System.Type serviceType, System.Func<object> factory)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (_singletons.TryGetValue(serviceType, out object? existing))");
            builder.AppendLine("        {");
            builder.AppendLine("            return existing;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        object created = factory();");
            builder.AppendLine("        _singletons[serviceType] = created;");
            builder.AppendLine("        return created;");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        /// <summary>
        /// 发射支持 Feature 装配的构造函数。
        /// </summary>
        private static void EmitConstructor(
            StringBuilder builder,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.MviFeatureInfo> features)
        {
            if (features.Count == 0)
            {
                EmitConstructor(builder, services);
                return;
            }

            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 初始化由源生成器生成的泛型 DI 容器。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <param name=\"uiDispatcher\">UI 调度器，缺省时使用内联调度器。</param>");
            builder.AppendLine("    /// <param name=\"mediator\">中介者，缺省时由容器创建。</param>");
            builder.AppendLine("    public GeneratedMviContainer(");
            builder.AppendLine("        " + UiDispatcherTypeName + "? uiDispatcher = null,");
            builder.AppendLine("        " + MediatorTypeName + "? mediator = null)");
            builder.AppendLine("    {");
            builder.AppendLine("        _uiDispatcher = uiDispatcher");
            builder.AppendLine("            ?? global::MiKiNuo.Mvi.Application.MVI.Threading.MviInlineUiDispatcher.Instance;");
            builder.AppendLine("        _mediator = mediator ?? new global::MiKiNuo.Mvi.Application.MVI.Mediator.MviMediator();");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 获取服务描述集合。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public IReadOnlyList<MviServiceDescriptor> ServiceDescriptors => _descriptors;");
            builder.AppendLine();
            builder.AppendLine("    private static readonly IReadOnlyList<MviServiceDescriptor> _descriptors = new MviServiceDescriptor[]");
            builder.AppendLine("    {");

            foreach (Models.DiServiceInfo service in services)
            {
                builder.Append("        new(typeof(").Append(service.ServiceTypeName).Append("), typeof(")
                    .Append(service.ImplementationTypeName).Append("), ServiceLifetime.")
                    .Append(service.Lifetime.ToServiceLifetimeName()).AppendLine("),");
            }

            builder.AppendLine("    };");
            builder.AppendLine();
        }

        /// <summary>
        /// 发射支持 Feature 装配的 Resolve 方法组。
        /// </summary>
        private static void EmitResolveMethods(
            StringBuilder builder,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.MviFeatureInfo> features)
        {
            if (features.Count == 0)
            {
                EmitResolveMethods(builder, services);
                return;
            }

            EmitResolveGeneric(builder);
            EmitResolveByTypeWithFeatures(builder, services, features);
        }

        /// <summary>
        /// 发射带 Feature 分支的 Resolve(Type) 方法。
        /// </summary>
        private static void EmitResolveByTypeWithFeatures(
            StringBuilder builder,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.MviFeatureInfo> features)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 解析指定类型的服务。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <param name=\"serviceType\">服务类型。</param>");
            builder.AppendLine("    /// <returns>服务实例。</returns>");
            builder.AppendLine("    public object Resolve(Type serviceType)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (serviceType is null)");
            builder.AppendLine("        {");
            builder.AppendLine("            throw new ArgumentNullException(nameof(serviceType));");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        if (_singletons.TryGetValue(serviceType, out object? existing))");
            builder.AppendLine("        {");
            builder.AppendLine("            return existing;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        if (serviceType == typeof(" + UiDispatcherTypeName + "))");
            builder.AppendLine("        {");
            builder.AppendLine("            return _uiDispatcher;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        if (serviceType == typeof(" + MediatorTypeName + "))");
            builder.AppendLine("        {");
            builder.AppendLine("            return _mediator;");
            builder.AppendLine("        }");
            builder.AppendLine();

            foreach (Models.DiServiceInfo service in services)
            {
                builder.Append("        if (serviceType == typeof(").Append(service.ServiceTypeName)
                    .AppendLine("))");
                if (service.Lifetime == Models.GeneratedLifetime.Singleton)
                {
                    builder.Append("        {").AppendLine();
                    builder.Append("            object created = new ").Append(service.ImplementationTypeName).Append('(');
                    builder.Append(string.Join(", ", service.ConstructorArgumentExpressions));
                    builder.AppendLine(");");
                    builder.Append("            _singletons[serviceType] = created;").AppendLine();
                    builder.Append("            return created;").AppendLine();
                    builder.Append("        }").AppendLine();
                }
                else
                {
                    builder.Append("            return new ").Append(service.ImplementationTypeName).Append('(');
                    builder.Append(string.Join(", ", service.ConstructorArgumentExpressions));
                    builder.AppendLine(");");
                }
            }

            foreach (Models.MviFeatureInfo feature in features)
            {
                builder.Append("        if (serviceType == typeof(").Append(feature.StoreTypeName).AppendLine("))");
                builder.AppendLine("        {");
                builder.Append("            return GetSingleton(serviceType, Create").Append(feature.FeatureName).AppendLine("Store);");
                builder.AppendLine("        }");
                builder.AppendLine();
                builder.Append("        if (serviceType == typeof(").Append(feature.Reducer.TypeName).AppendLine("))");
                builder.AppendLine("        {");
                builder.Append("            return GetSingleton(serviceType, Create").Append(feature.FeatureName).AppendLine("Reducer);");
                builder.AppendLine("        }");
                builder.AppendLine();

                if (feature.EffectDispatcher is not null)
                {
                    builder.Append("        if (serviceType == typeof(").Append(feature.EffectDispatcher.TypeName).AppendLine("))");
                    builder.AppendLine("        {");
                    builder.Append("            return GetSingleton(serviceType, Create").Append(feature.FeatureName).AppendLine("EffectDispatcher);");
                    builder.AppendLine("        }");
                    builder.AppendLine();
                }

                if (feature.ViewModel is not null)
                {
                    builder.Append("        if (serviceType == typeof(").Append(feature.ViewModel.TypeName).AppendLine("))");
                    builder.AppendLine("        {");
                    builder.Append("            return GetSingleton(serviceType, Create").Append(feature.FeatureName).AppendLine("ViewModel);");
                    builder.AppendLine("        }");
                    builder.AppendLine();
                }
            }

            builder.AppendLine("        throw new InvalidOperationException($\"未注册服务：{serviceType.FullName}\");");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        /// <summary>
        /// 发射各 Feature 的工厂方法（Store / Reducer / EffectDispatcher / ViewModel）。
        /// </summary>
        private static void EmitFeatureFactories(
            StringBuilder builder,
            IReadOnlyList<Models.MviFeatureInfo> features)
        {
            foreach (Models.MviFeatureInfo feature in features)
            {
                string name = feature.FeatureName;
                string storeImplType = "global::MiKiNuo.Mvi.Application.MVI.Store.MviStore<"
                    + feature.StateTypeName + ", " + feature.IntentTypeName + ", " + feature.EffectTypeName + ">";
                string middlewareType = "global::MiKiNuo.Mvi.Application.MVI.Middleware.IMviMiddleware<"
                    + feature.StateTypeName + ", " + feature.IntentTypeName + ", " + feature.EffectTypeName + ">";

                builder.Append("    private object Create").Append(name).AppendLine("Reducer()");
                builder.AppendLine("    {");
                builder.Append("        return ").Append(feature.Reducer.NewExpression()).AppendLine(";");
                builder.AppendLine("    }");
                builder.AppendLine();

                if (feature.EffectDispatcher is not null)
                {
                    builder.Append("    private object Create").Append(name).AppendLine("EffectDispatcher()");
                    builder.AppendLine("    {");
                    builder.Append("        return ").Append(feature.EffectDispatcher.NewExpression()).AppendLine(";");
                    builder.AppendLine("    }");
                    builder.AppendLine();
                }

                builder.Append("    private object Create").Append(name).AppendLine("Store()");
                builder.AppendLine("    {");
                builder.Append("        return new ").Append(storeImplType).AppendLine("(");
                builder.Append("            ").Append(feature.StateTypeName).AppendLine(".Initial,");
                builder.Append("            this.Resolve<").Append(feature.Reducer.TypeName).AppendLine(">(),");

                if (feature.EffectDispatcher is not null)
                {
                    builder.Append("            this.Resolve<").Append(feature.EffectDispatcher.TypeName).AppendLine(">(),");
                }
                else if (feature.EffectTypeName.EndsWith("UnitEffect", System.StringComparison.Ordinal))
                {
                    builder.AppendLine("            global::MiKiNuo.Mvi.Application.MVI.Effect.NullEffectDispatcher.Instance,");
                }
                else
                {
                    builder.Append("            throw new System.InvalidOperationException(\"Feature ")
                        .Append(name).AppendLine(" 未发现匹配的 EffectDispatcher，请为 Effect 类型提供 MviEffectDispatcherBase 子类。\"),");
                }

                if (feature.Middlewares.Count == 0)
                {
                    builder.Append("            System.Array.Empty<").Append(middlewareType).AppendLine(">());");
                }
                else
                {
                    builder.Append("            new ").Append(middlewareType).AppendLine("[]");
                    builder.AppendLine("            {");
                    foreach (Models.FeatureComponentInfo middleware in feature.Middlewares)
                    {
                        builder.Append("                ").Append(middleware.NewExpression()).AppendLine(",");
                    }

                    builder.AppendLine("            });");
                }

                builder.AppendLine("    }");
                builder.AppendLine();

                if (feature.ViewModel is not null)
                {
                    builder.Append("    private object Create").Append(name).AppendLine("ViewModel()");
                    builder.AppendLine("    {");
                    builder.Append("        return ").Append(feature.ViewModel.NewExpression()).AppendLine(";");
                    builder.AppendLine("    }");
                    builder.AppendLine();
                }
            }
        }
    }
}
