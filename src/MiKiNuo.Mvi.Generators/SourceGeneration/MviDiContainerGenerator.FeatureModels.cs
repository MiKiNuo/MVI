using System.Collections.Generic;
namespace MiKiNuo.Mvi.Generators.SourceGeneration;
/// <summary>组件装配的编译期模型。</summary>
public sealed partial class MviDiContainerGenerator
{
    internal static partial class Models
    {
        internal sealed class MviFeatureInfo(string featureName, string stateTypeName, string intentTypeName,
            FeatureComponentInfo reducer, FeatureComponentInfo handler, FeatureComponentInfo? viewModel,
            IReadOnlyList<FeatureComponentInfo> middlewares)
        {
            public string FeatureName { get; } = featureName;
            public string StateTypeName { get; } = stateTypeName;
            public string IntentTypeName { get; } = intentTypeName;
            public FeatureComponentInfo Reducer { get; } = reducer;
            public FeatureComponentInfo Handler { get; } = handler;
            public FeatureComponentInfo? ViewModel { get; } = viewModel;
            public IReadOnlyList<FeatureComponentInfo> Middlewares { get; } = middlewares;
            public string StoreTypeName => "global::MiKiNuo.Mvi.Runtime.MVI.Store.IMviStore<" + StateTypeName + ", " + IntentTypeName + ">";
            public string InstanceMethodName => "Create" + FeatureName + "InstanceAsync";
            public string InstanceCoreMethodName => "Create" + FeatureName + "InstanceCoreAsync";
        }
        internal sealed class FeatureComponentInfo(string typeName, IReadOnlyList<string> constructorParameterTypeNames)
        {
            public string TypeName { get; } = typeName;
            public IReadOnlyList<string> ConstructorParameterTypeNames { get; } = constructorParameterTypeNames;
        }
    }
}
