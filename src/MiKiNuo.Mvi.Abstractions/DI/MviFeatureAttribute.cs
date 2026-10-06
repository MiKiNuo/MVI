namespace MiKiNuo.Mvi.Abstractions.DI;
/// <summary>标记组件业务 Handler；生成器自动装配唯一 Reducer、Store、ViewModel 和中间件。</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MviFeatureAttribute : Attribute { }
