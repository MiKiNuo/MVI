using BenchmarkDotNet.Attributes;
using MiKiNuo.Mvi.Runtime.MVI.Store;
namespace MiKiNuo.Mvi.Benchmarks.Scenarios.Mvi.LoginReplica;
/// <summary>测量完整登录 Handler 路径和生成命令路径。</summary>
[MemoryDiagnoser]
public class LoginReplicaDispatchBenchmarks : IAsyncDisposable
{
    private MviStore<BenchLoginState, BenchLoginIntent> _store = null!;
    private BenchLoginViewModel _viewModel = null!;
    /// <summary>创建当前内核，不使用旧 Store 适配器。</summary>
    [GlobalSetup] public void Setup()
    { _store = new(BenchLoginState.Initial, new BenchLoginHandler(), new BenchLoginReducer()); _viewModel = new(_store); }
    /// <summary>直接派发输入快照。</summary>
    [Benchmark(Baseline = true)] public ValueTask DirectAsync() => _store.DispatchAsync(new("user", "benchmark"));
    /// <summary>测量属性输入、命令快照、Handler 和投影。</summary>
    [Benchmark] public ValueTask CommandAsync()
    { _viewModel.UserName = "user"; _viewModel.Password = "benchmark"; return _viewModel.SubmitCommand.ExecuteAsync(null); }
    /// <summary>等待所有资源真正退出。</summary>
    [GlobalCleanup] public Task CleanupAsync() => DisposeAsync().AsTask();

    /// <summary>释放基准拥有的对象，等待 Store 的在途工作退出。</summary>
    /// <returns>完整资源清理任务。</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>先解除模型订阅，再等待 Store 排空；支持尚未完成初始化时的清理。</summary>
    /// <returns>完整资源清理任务。</returns>
    protected virtual async ValueTask DisposeCoreAsync()
    {
        try
        {
            _viewModel?.Dispose();
        }
        finally
        {
            // 即使模型清理失败，也继续停止并回收 Store。
            if (_store is not null)
            {
                await _store.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
