using System.Collections.Generic;
using System.Text;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 Feature Store 工厂发射部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    /// <summary>
    /// 发射阶段的 Feature Store 工厂部分。
    /// </summary>
    internal static partial class Emission
    {
        /// <summary>
        /// 生成 Feature Store 工厂方法。
        /// </summary>
        private static void EmitFeatureStoreFactories(
            StringBuilder builder,
            IReadOnlyList<Models.FeatureStoreInfo> features)
        {
            foreach (Models.FeatureStoreInfo feature in features)
            {
                string storeType = "global::MiKiNuo.Mvi.Application.MVI.Store.IMviStore<"
                    + feature.StateTypeName + ", "
                    + feature.IntentTypeName + ", "
                    + feature.EffectTypeName + ">";

                builder.AppendLine("    /// <summary>");
                builder.Append("    /// 创建功能模块“").Append(feature.FeatureKey).AppendLine("”的 Store（由 [MviFeatureModule] 驱动生成）。");
                builder.AppendLine("    /// </summary>");
                builder.Append("    /// <returns>功能模块 Store 实例。</returns>");
                builder.AppendLine();
                builder.Append("    private ").Append(storeType).Append(" Create")
                    .Append(feature.GetSafeMethodKey()).AppendLine("FeatureStore()");
                builder.AppendLine("    {");
                builder.Append("        return new global::MiKiNuo.Mvi.Application.MVI.Store.MviStore<")
                    .Append(feature.StateTypeName).Append(", ")
                    .Append(feature.IntentTypeName).Append(", ")
                    .Append(feature.EffectTypeName).AppendLine(">(");
                builder.Append("            ").Append(feature.StateTypeName).AppendLine(".Initial,");
                builder.Append("            new ").Append(feature.HandlerTypeName).Append('(')
                    .Append(string.Join(", ", feature.HandlerConstructorArguments)).AppendLine("),");
                builder.Append("            new ").Append(feature.ReducerTypeName).Append('(')
                    .Append(string.Join(", ", feature.ReducerConstructorArguments)).AppendLine("),");
                builder.Append("            new ").Append(feature.DispatcherTypeName).Append('(')
                    .Append(string.Join(", ", feature.DispatcherConstructorArguments)).AppendLine("));");
                builder.AppendLine("    }");
                builder.AppendLine();
            }
        }
    }
}
