using System.ComponentModel;
using System.Runtime.CompilerServices;
using MiKiNuo.Mvi.Abstractions.MVI.Intent;
using MiKiNuo.Mvi.Abstractions.MVI.State;
using MiKiNuo.Mvi.Binding.Command;
using MiKiNuo.Mvi.Binding.Disposables;
using MiKiNuo.Mvi.Binding.EventBinding;
using MiKiNuo.Mvi.Binding.Threading;
using MiKiNuo.Mvi.Runtime.MVI.Store;
using R3;
namespace MiKiNuo.Mvi.Binding.ViewModel;
/// <summary>
/// UI 绑定模型：只保存本地输入与只读状态投影；业务必须通过 Intent 进入 Store。
/// 不从基类构造函数调用虚方法，投影的全部写入和通知在 UI 调度器上执行。
/// </summary>
/// <typeparam name="TState">业务状态。</typeparam>
/// <typeparam name="TIntent">用户意图。</typeparam>
public abstract class MviViewModelBase<TState, TIntent> : MviComponent, INotifyPropertyChanged
    where TState : IMviState where TIntent : IMviIntent
{
    private readonly MviDisposableBag _resources = new();
    private readonly List<MviCommandBase> _commands = [];
    private readonly List<string?> _notifications = [];
    private int _disposed;
    private bool _initialized;
    private bool _applying;
    private TState _state;
    /// <summary>只初始化依赖；生成的派生构造函数在自身初始化后调用 InitializeBindings。</summary>
    /// <param name="store">借用的 Store，不由 ViewModel 释放。</param>
    /// <param name="uiDispatcher">平台 UI 调度器。</param>
    protected MviViewModelBase(IMviStore<TState, TIntent> store, IMviUiDispatcher? uiDispatcher = null)
    { Store = store ?? throw new ArgumentNullException(nameof(store)); _state = store.CurrentState; UiDispatcher = uiDispatcher ?? MviInlineUiDispatcher.Instance; }
    /// <summary>获取只读业务状态快照；直接使用 State.X 进行 UI 绑定。</summary>
    public TState State => _state;
    /// <summary>获取借用的 Store；业务代码不应在 ViewModel 内处理它。</summary>
    protected IMviStore<TState, TIntent> Store { get; }
    /// <summary>获取 UI 调度器。</summary>
    public IMviUiDispatcher UiDispatcher { get; }
    /// <summary>获取当前是否在应用状态，生成的输入 setter 用来抑制回声。</summary>
    protected bool IsApplyingState => _applying;
    /// <summary>属性变更通知；一轮投影先完成所有写入，再发送通知。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>启动生成式命令和投影；仅在构造或 UI 线程初始化期间调用一次。</summary>
    protected void InitializeBindings()
    {
        if (_initialized) throw new InvalidOperationException("绑定已初始化。");
        _initialized = true;
        try
        {
            InitializeGeneratedCommands();
            _resources.Add(Store.States.Subscribe(this, static (state, model) => model.QueueProjection(state)));
        }
        catch (Exception failure)
        {
            try { Dispose(); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
            throw;
        }
    }
    /// <summary>由生成器安装命令。</summary>
    protected virtual void InitializeGeneratedCommands() { }
    /// <summary>登记生成命令的通知和所有权。</summary>
    /// <typeparam name="TCommand">命令类型。</typeparam>
    /// <param name="command">新建命令。</param>
    /// <returns>原命令。</returns>
    protected TCommand OwnCommand<TCommand>(TCommand command) where TCommand : MviCommandBase
    { _commands.Add(command); _resources.Add(command); return command; }
    private void QueueProjection(TState state) => UiDispatcher.Post(() =>
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        _applying = true;
        try
        {
            _state = state;
            _notifications.Add(nameof(State));
            ApplyStateCore(state);
        }
        finally
        {
            _applying = false;
            string?[] pending = _notifications.ToArray(); _notifications.Clear();
            foreach (string? name in pending) RaisePropertyChanged(name);
            foreach (MviCommandBase command in _commands) command.NotifyCanExecuteChanged();
        }
    });
    /// <summary>只投影明确声明的状态属性；本地输入不受业务回显覆盖。</summary>
    /// <param name="state">完整状态快照。</param>
    protected virtual void ApplyStateCore(TState state) { }
    /// <summary>更新 UI 线程拥有的属性缓冲，生成 setter 调用此方法。</summary>
    /// <typeparam name="TValue">属性类型。</typeparam>
    /// <param name="field">缓冲引用。</param>
    /// <param name="value">新值。</param>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>是否发生变化。</returns>
    protected bool SetProperty<TValue>(ref TValue field, TValue value, [CallerMemberName] string? propertyName = null)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (EqualityComparer<TValue>.Default.Equals(field, value)) return false;
        field = value;
        if (_applying) { if (!_notifications.Contains(propertyName)) _notifications.Add(propertyName); }
        else
        {
            RaisePropertyChanged(propertyName);
            foreach (MviCommandBase command in _commands) command.NotifyCanExecuteChanged();
        }
        return true;
    }
    private void RaisePropertyChanged(string? name)
    { if (Volatile.Read(ref _disposed) == 0) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
    /// <summary>生成的命令从唯一的意图入口进入运行时。</summary>
    /// <param name="intent">快照意图。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>完整业务任务。</returns>
    protected ValueTask DispatchAsync(TIntent intent, CancellationToken cancellationToken = default)
        => DispatchIntentAsync(intent, cancellationToken);
    /// <summary>校验跨平台入口的意图类型并转入 Store。</summary>
    /// <param name="intent">平台输入。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>处理任务。</returns>
    protected override ValueTask DispatchCoreAsync(IMviIntent intent, CancellationToken cancellationToken)
        => intent is TIntent typed ? Store.DispatchAsync(typed, cancellationToken)
            : throw new ArgumentException("意图不属于当前组件。", nameof(intent));
    /// <summary>派生模型拥有的额外资源清理钩子。</summary>
    protected virtual void OnDispose() { }
    /// <summary>使排队的投影失效并清理所有订阅；不销毁借用的 Store。</summary>
    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        List<Exception> errors = [];
        try { _resources.Dispose(); } catch (Exception exception) { errors.Add(exception); }
        try { OnDispose(); } catch (Exception exception) { errors.Add(exception); }
        // 不在非 UI 线程清空投影列表；生命周期标志使已排队的 UI 回调失效。
        PropertyChanged = null;
        base.Dispose();
        GC.SuppressFinalize(this);
        if (errors.Count != 0) throw new AggregateException("ViewModel 清理失败。", errors);
    }
}
