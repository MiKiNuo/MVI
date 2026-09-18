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
            return GenerateContainerSource(
                assemblyName,
                services,
                features,
                System.Array.Empty<Models.CompositionInfo>());
        }

        /// <summary>
        /// 生成支持 Feature 与组合装配的 DI 容器源码。
        /// </summary>
        /// <param name="assemblyName">目标程序集名称。</param>
        /// <param name="services">DI 服务信息集合。</param>
        /// <param name="features">Feature 装配模型集合。</param>
        /// <param name="compositions">组合装配模型集合。</param>
        /// <returns>生成的 C# 源码。</returns>
        public static string GenerateContainerSource(
            string assemblyName,
            IReadOnlyList<Models.DiServiceInfo> services,
            IReadOnlyList<Models.MviFeatureInfo> features,
            IReadOnlyList<Models.CompositionInfo> compositions)
        {
            StringBuilder builder = new();
            string containerNamespace = string.IsNullOrEmpty(assemblyName) ? "GeneratedContainer" : assemblyName;

            EmitFileHeader(builder, containerNamespace, services);
            EmitFeatureFields(builder, features);
            EmitConstructor(builder, services, features);
            EmitServiceFactoryTable(builder, services);
            EmitResolveMethods(builder, services, features);
            EmitCreateScope(builder);
            EmitCreateWith(builder, services);
            EmitInstanceFactories(builder, features, services);
            EmitCompositionBuilders(builder, compositions);
            EmitScopeClass(builder);
            EmitServiceFactoryReceiverInterface(builder);

            return builder.ToString();
        }

        /// <summary>
        /// 发射 Feature 装配所需的字段（仅当存在 Feature 时）。
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
            builder.AppendLine("    public GeneratedMviContainer(");
            builder.AppendLine("        " + UiDispatcherTypeName + "? uiDispatcher = null)");
            builder.AppendLine("    {");
            builder.AppendLine("        _uiDispatcher = uiDispatcher");
            builder.AppendLine("            ?? global::MiKiNuo.Mvi.Application.MVI.Threading.MviInlineUiDispatcher.Instance;");
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
                EmitResolveMethods(builder);
                return;
            }

            EmitResolveGeneric(builder);
            EmitResolveByTypeWithFeatures(builder);
        }

        /// <summary>
        /// 发射带 UI 调度器分支的 Resolve(Type) 方法。
        /// </summary>
        private static void EmitResolveByTypeWithFeatures(
            StringBuilder builder)
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

            builder.AppendLine("        if (_factories.TryGetValue(serviceType, out (ServiceLifetime Lifetime, Func<IMviServiceFactoryReceiver, object> Factory) entry))");
            builder.AppendLine("        {");
            builder.AppendLine("            object created = entry.Factory(this);");
            builder.AppendLine("            if (entry.Lifetime == ServiceLifetime.Singleton)");
            builder.AppendLine("            {");
            builder.AppendLine("                _singletons[serviceType] = created;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return created;");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        throw new InvalidOperationException($\"未注册服务：{serviceType.FullName}\");");
            builder.AppendLine("    }");
            builder.AppendLine();
        }
    }
}
