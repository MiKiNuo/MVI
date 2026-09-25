using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的组合装配发射部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    internal static partial class Emission
    {
        private const string CompositionScopeTypeName =
            "global::MiKiNuo.Mvi.Application.MVI.Mediator.MviCompositionScope";

        private const string MediatorEndpointTypeName =
            "global::MiKiNuo.Mvi.Application.MVI.Mediator.MviMediatorEndpoint";

        private const string FeatureInstanceTypeName =
            "global::MiKiNuo.Mvi.Application.MVI.Composition.MviFeatureInstance";

        /// <summary>
        /// 发射组合构建器方法（容器类内部，须位于实例工厂之后）。
        /// </summary>
        /// <param name="builder">代码缓冲。</param>
        /// <param name="compositions">组合装配模型集合。</param>
        public static void EmitCompositionBuilders(
            StringBuilder builder,
            IReadOnlyList<Models.CompositionInfo> compositions)
        {
            foreach (Models.CompositionInfo composition in compositions)
            {
                builder.AppendLine("    /// <summary>");
                builder.AppendLine("    /// 创建 " + composition.Name + " 组合：组合范围、成员实例与通信接线一次完成。");
                builder.AppendLine("    /// </summary>");
                builder.AppendLine("    /// <returns>接线完成的组合句柄。</returns>");
                builder.AppendLine("    public async global::System.Threading.Tasks.ValueTask<" + composition.TypeName + "> Create"
                    + composition.Name + "Async()");
                builder.AppendLine("    {");
                builder.AppendLine("        " + CompositionScopeTypeName + " scope = new();");
                builder.AppendLine("        global::System.Collections.Generic.List<" + FeatureInstanceTypeName + "> created = new();");
                builder.AppendLine("        try");
                builder.AppendLine("        {");

                foreach (Models.CompositionMemberInfo member in composition.Members)
                {
                    string variableName = member.VariableName;
                    builder.AppendLine("            " + MediatorEndpointTypeName + " " + variableName
                        + "Endpoint = scope.CreateEndpoint(global::System.Guid.NewGuid());");
                    builder.AppendLine("            (" + FeatureInstanceTypeName + "<" + member.Feature.ViewModel!.TypeName + "> "
                        + variableName + ", InstanceServices " + variableName + "Services) =");
                    builder.AppendLine("                await " + member.Feature.InstanceCoreMethodName + "("
                        + variableName + "Endpoint, " + member.Feature.StateTypeName + ".Initial).ConfigureAwait(false);");
                    builder.AppendLine("            created.Add(" + variableName + ");");
                }

                foreach (Models.CompositionRouteInfo route in composition.Routes)
                {
                    builder.AppendLine("            scope.Register<" + route.RequestTypeName + ", " + route.ResponseTypeName + ">("
                        + route.Provider.VariableName + "Endpoint.InstanceId,");
                    builder.AppendLine("                (request, token) => " + route.Provider.VariableName + "Services.Resolve<"
                        + route.Provider.Feature.EffectDispatcher!.TypeName + ">()." + route.HandlerMethodName + "(request, token));");
                    foreach (Models.CompositionMemberInfo consumer in route.Consumers)
                    {
                        builder.AppendLine("            scope.Bind<" + route.RequestTypeName + ">("
                            + consumer.VariableName + "Endpoint.InstanceId, " + route.Provider.VariableName + "Endpoint.InstanceId);");
                    }
                }

                foreach (Models.CompositionSubscriptionInfo subscription in composition.Subscriptions)
                {
                    builder.AppendLine("            scope.Subscribe<" + subscription.NotificationTypeName + ">("
                        + subscription.Subscriber.VariableName + "Endpoint.InstanceId,");
                    builder.AppendLine("                notification => " + subscription.Subscriber.VariableName + "Services.Resolve<"
                        + subscription.Subscriber.Feature.EffectDispatcher!.TypeName + ">()." + subscription.AcceptorMethodName + "(notification));");
                }

                builder.AppendLine("            return new " + composition.TypeName + "(scope, "
                    + string.Join(", ", composition.Members.Select(member => member.VariableName)) + ");");
                builder.AppendLine("        }");
                builder.AppendLine("        catch");
                builder.AppendLine("        {");
                builder.AppendLine("            for (int index = created.Count - 1; index >= 0; index--)");
                builder.AppendLine("            {");
                builder.AppendLine("                try { await created[index].DisposeAsync().ConfigureAwait(false); }");
                builder.AppendLine("                catch (global::System.Exception) { }");
                builder.AppendLine("            }");
                builder.AppendLine();
                builder.AppendLine("            scope.Dispose();");
                builder.AppendLine("            throw;");
                builder.AppendLine("        }");
                builder.AppendLine("    }");
                builder.AppendLine();
            }
        }

        /// <summary>
        /// 发射组合句柄源码（声明类命名空间内的 partial 合并部分）。
        /// </summary>
        /// <param name="compositions">组合装配模型集合。</param>
        /// <returns>生成的 C# 源码；无组合时为 null。</returns>
        public static string? GenerateCompositionHandlesSource(IReadOnlyList<Models.CompositionInfo> compositions)
        {
            if (compositions.Count == 0)
            {
                return null;
            }

            StringBuilder builder = new();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();

            foreach (IGrouping<string, Models.CompositionInfo> group in compositions.GroupBy(
                composition => composition.TypeName.Substring(0, composition.TypeName.Length - composition.Name.Length - 1)))
            {
                builder.Append("namespace ").Append(group.Key.Substring("global::".Length)).AppendLine();
                builder.AppendLine("{");
                foreach (Models.CompositionInfo composition in group)
                {
                    EmitCompositionHandle(builder, composition);
                }

                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private static void EmitCompositionHandle(StringBuilder builder, Models.CompositionInfo composition)
        {
            builder.AppendLine("/// <summary>");
            builder.AppendLine("/// 表示 " + composition.Name + " 组合的生成句柄：持有组合范围与各成员实例。");
            builder.AppendLine("/// </summary>");
            builder.AppendLine("public sealed partial class " + composition.Name + " : global::System.IAsyncDisposable");
            builder.AppendLine("{");
            builder.AppendLine("    private readonly " + CompositionScopeTypeName + " _scope;");
            builder.AppendLine();
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 初始化组合句柄，由生成的组合构建器调用。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <param name=\"scope\">组合范围。</param>");
            foreach (Models.CompositionMemberInfo member in composition.Members)
            {
                builder.AppendLine("    /// <param name=\"" + member.VariableName + "\">" + member.Feature.FeatureName + " 成员实例。</param>");
            }

            builder.AppendLine("    internal " + composition.Name + "(");
            builder.AppendLine("        " + CompositionScopeTypeName + " scope,");
            for (int index = 0; index < composition.Members.Count; index++)
            {
                Models.CompositionMemberInfo member = composition.Members[index];
                string suffix = index == composition.Members.Count - 1 ? ")" : ",";
                builder.AppendLine("        " + FeatureInstanceTypeName + "<" + member.Feature.ViewModel!.TypeName + "> "
                    + member.VariableName + suffix);
            }

            builder.AppendLine("    {");
            builder.AppendLine("        _scope = scope;");
            foreach (Models.CompositionMemberInfo member in composition.Members)
            {
                builder.AppendLine("        " + member.PropertyName + " = " + member.VariableName + ";");
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            foreach (Models.CompositionMemberInfo member in composition.Members)
            {
                builder.AppendLine("    /// <summary>");
                builder.AppendLine("    /// 获取 " + member.PropertyName + " 成员实例。");
                builder.AppendLine("    /// </summary>");
                builder.AppendLine("    public " + FeatureInstanceTypeName + "<" + member.Feature.ViewModel!.TypeName + "> "
                    + member.PropertyName + " { get; }");
                builder.AppendLine();
            }

            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 逆序释放全部成员实例并关闭组合范围。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    /// <returns>释放任务。</returns>");
            builder.AppendLine("    public async global::System.Threading.Tasks.ValueTask DisposeAsync()");
            builder.AppendLine("    {");
            for (int index = composition.Members.Count - 1; index >= 0; index--)
            {
                builder.AppendLine("        await " + composition.Members[index].PropertyName
                    + ".DisposeAsync().ConfigureAwait(false);");
            }

            builder.AppendLine("        _scope.Dispose();");
            builder.AppendLine("        global::System.GC.SuppressFinalize(this);");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            builder.AppendLine();
        }
    }
}
