using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia;
using Avalonia.Threading;

namespace MiKiNuo.Mvi.Platforms.Avalonia;

/// <summary>将生成的本地投影连接到 Avalonia 原生 UI 调度器。</summary>
public static class AvaloniaProjection
{
    /// <summary>以原生单向展示和控件输入连接生成属性，确保同值归一化也能纠正编辑值。</summary>
    /// <typeparam name="TProjection">生成的本地投影类型。</typeparam>
    /// <typeparam name="TValue">控件及投影属性的值类型。</typeparam>
    /// <param name="projection">该 View 的生成投影。</param>
    /// <param name="target">所属 View 的原生控件。</param>
    /// <param name="targetProperty">接受输入的原生属性。</param>
    /// <param name="input">直接选择一个生成的可编辑投影属性。</param>
    /// <returns>View 结束时应释放的本地原生输入连接。</returns>
    public static IDisposable BindInput<TProjection, TValue>(TProjection projection, AvaloniaObject target,
        AvaloniaProperty<TValue> targetProperty, Expression<Func<TProjection, TValue>> input)
        where TProjection : class, INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetProperty);
        ArgumentNullException.ThrowIfNull(input);
        Dispatcher.UIThread.VerifyAccess();
        if (input.Body is not MemberExpression { Member: PropertyInfo property } member
            || member.Expression != input.Parameters[0] || property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true)
        {
            throw new ArgumentException("输入必须直接选择生成投影的可编辑属性。", nameof(input));
        }

        Func<TProjection, TValue> read = property.GetMethod.CreateDelegate<Func<TProjection, TValue>>();
        Action<TProjection, TValue> write = property.SetMethod.CreateDelegate<Action<TProjection, TValue>>();
        bool writingTarget = false;
        void UpdateTarget()
        {
            writingTarget = true;
            try
            {
                target.SetCurrentValue(targetProperty, read(projection));
            }
            finally
            {
                writingTarget = false;
            }
        }

        EventHandler<AvaloniaPropertyChangedEventArgs> changed = (_, args) =>
        {
            if (args.Property == targetProperty && !writingTarget)
            {
                TValue value = (TValue)target.GetValue(targetProperty)!;
                write(projection, value);
            }
        };
        PropertyChangedEventHandler sourceChanged = (_, args) =>
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == property.Name)
            {
                UpdateTarget();
            }
        };
        UpdateTarget();
        target.PropertyChanged += changed;
        projection.PropertyChanged += sourceChanged;
        return new InputConnection(projection, target, sourceChanged, changed);
    }

    /// <summary>在 UI 线程创建该 View 的强类型投影；View 结束时应释放返回的连接。</summary>
    /// <typeparam name="TProjection">生成的投影类型。</typeparam>
    /// <param name="create">所属功能生成的投影工厂。</param>
    /// <param name="mode">等待展示的合并方式。</param>
    /// <returns>可作为 View 数据上下文的本地投影。</returns>
    public static TProjection Create<TProjection>(Func<Action<Action>, ProjectionMode, TProjection> create,
        ProjectionMode mode = ProjectionMode.Coalesce) where TProjection : IDisposable
    {
        ArgumentNullException.ThrowIfNull(create);
        Dispatcher.UIThread.VerifyAccess();
        return create(static action => Dispatcher.UIThread.Post(action), mode);
    }

    private sealed class InputConnection(INotifyPropertyChanged projection, AvaloniaObject target,
        PropertyChangedEventHandler sourceChanged, EventHandler<AvaloniaPropertyChangedEventArgs> changed) : IDisposable
    {
        private bool disposed;

        /// <summary>释放控件输入事件与原生展示绑定。</summary>
        public void Dispose()
        {
            Dispatcher.UIThread.VerifyAccess();
            if (!disposed)
            {
                disposed = true;
                target.PropertyChanged -= changed;
                projection.PropertyChanged -= sourceChanged;
            }
        }
    }
}
