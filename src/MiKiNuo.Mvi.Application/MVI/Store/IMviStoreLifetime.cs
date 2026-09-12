namespace MiKiNuo.Mvi.Application.MVI.Store;

/// <summary>供实例所有者在等待子级释放前停止存储准入。</summary>
internal interface IMviStoreLifetime
{
    /// <summary>拒绝新派发并取消在途操作，不提前释放资源。</summary>
    public void Stop();
}
