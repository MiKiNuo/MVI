using System.Collections.Generic;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的 Feature 装配数据模型部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    internal static partial class Models
    {
        /// <summary>
        /// 表示一个 MVI Feature 的装配模型。
        /// </summary>
        public sealed class MviFeatureInfo
        {
            /// <summary>
            /// 初始化 Feature 装配模型。
            /// </summary>
            /// <param name="featureName">Feature 名称（Reducer 名去掉 Reducer 后缀）。</param>
            /// <param name="stateTypeName">状态类型完整限定名。</param>
            /// <param name="intentTypeName">意图类型完整限定名。</param>
            /// <param name="effectTypeName">副作用类型完整限定名。</param>
            /// <param name="reducer">规约器组件。</param>
            /// <param name="effectDispatcher">副作用分发器组件；未发现时为 null。</param>
            /// <param name="viewModel">ViewModel 组件；未发现时为 null。</param>
            /// <param name="middlewares">中间件组件集合。</param>
            public MviFeatureInfo(
                string featureName,
                string stateTypeName,
                string intentTypeName,
                string effectTypeName,
                FeatureComponentInfo reducer,
                FeatureComponentInfo? effectDispatcher,
                FeatureComponentInfo? viewModel,
                IReadOnlyList<FeatureComponentInfo> middlewares)
            {
                FeatureName = featureName;
                StateTypeName = stateTypeName;
                IntentTypeName = intentTypeName;
                EffectTypeName = effectTypeName;
                Reducer = reducer;
                EffectDispatcher = effectDispatcher;
                ViewModel = viewModel;
                Middlewares = middlewares;
            }

            /// <summary>Feature 名称。</summary>
            public string FeatureName { get; }

            /// <summary>状态类型完整限定名。</summary>
            public string StateTypeName { get; }

            /// <summary>意图类型完整限定名。</summary>
            public string IntentTypeName { get; }

            /// <summary>副作用类型完整限定名。</summary>
            public string EffectTypeName { get; }

            /// <summary>规约器组件。</summary>
            public FeatureComponentInfo Reducer { get; }

            /// <summary>副作用分发器组件；未发现时为 null。</summary>
            public FeatureComponentInfo? EffectDispatcher { get; }

            /// <summary>ViewModel 组件；未发现时为 null。</summary>
            public FeatureComponentInfo? ViewModel { get; }

            /// <summary>中间件组件集合。</summary>
            public IReadOnlyList<FeatureComponentInfo> Middlewares { get; }

            /// <summary>获取 Store 接口完整限定名。</summary>
            public string StoreTypeName =>
                "global::MiKiNuo.Mvi.Application.MVI.Store.IMviStore<"
                + StateTypeName + ", " + IntentTypeName + ", " + EffectTypeName + ">";

            /// <summary>获取实例工厂公开方法名（实例工厂发射与组合发射共享此单一事实）。</summary>
            public string InstanceMethodName => "Create" + FeatureName + "InstanceAsync";

            /// <summary>获取实例工厂内部核心方法名（实例工厂发射与组合发射共享此单一事实）。</summary>
            public string InstanceCoreMethodName => "Create" + FeatureName + "InstanceCoreAsync";
        }

        /// <summary>
        /// 表示 Feature 内一个可装配组件（Reducer / EffectDispatcher / ViewModel / Middleware）。
        /// </summary>
        public sealed class FeatureComponentInfo
        {
            /// <summary>
            /// 初始化组件信息。
            /// </summary>
            /// <param name="typeName">组件类型完整限定名。</param>
            /// <param name="constructorParameterTypeNames">构造参数类型完整限定名集合；实参表达式由发射端按解析接收方渲染。</param>
            public FeatureComponentInfo(
                string typeName,
                IReadOnlyList<string> constructorParameterTypeNames)
            {
                TypeName = typeName;
                ConstructorParameterTypeNames = constructorParameterTypeNames;
            }

            /// <summary>组件类型完整限定名。</summary>
            public string TypeName { get; }

            /// <summary>构造参数类型完整限定名集合。</summary>
            public IReadOnlyList<string> ConstructorParameterTypeNames { get; }
        }
    }
}
