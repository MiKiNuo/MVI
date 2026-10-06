namespace MiKiNuo.Mvi.Runtime.MVI.Store;
/// <summary>表示 Store 在途操作达到上限；调用方应重试或降低输入速率，而不是静默丢失。</summary>
public sealed class MviCapacityExceededException : InvalidOperationException
{
    /// <summary>创建容量错误。</summary>
    public MviCapacityExceededException() : base("Store 在途操作已达到上限。") { }
}
