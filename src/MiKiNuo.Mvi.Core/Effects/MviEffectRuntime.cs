using System.Reflection;
using MiKiNuo.Mvi.Abstractions;

namespace MiKiNuo.Mvi.Core.Effects;

/// <summary>
/// 表示 MVI 副作用运行时。
/// </summary>
/// <typeparam name="TIntent">意图类型。</typeparam>
public sealed class MviEffectRuntime<TIntent>
    where TIntent : IMviIntent
{
    private readonly IIntentSink<TIntent> sink;
    private readonly IReadOnlyList<HandlerRegistration> handlers;

    /// <summary>
    /// 初始化 MVI 副作用运行时。
    /// </summary>
    /// <param name="sink">意图接收器。</param>
    /// <param name="handlers">显式注册的副作用处理器集合。</param>
    public MviEffectRuntime(
        IIntentSink<TIntent> sink,
        IEnumerable<object> handlers)
    {
        this.sink = sink;
        this.handlers = handlers.Select(CreateRegistration).ToArray();
    }

    /// <summary>
    /// 处理副作用。
    /// </summary>
    /// <param name="effect">副作用。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步任务。</returns>
    public ValueTask HandleAsync(IMviEffect effect, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var handler in handlers)
        {
            if (handler.EffectType.IsInstanceOfType(effect))
            {
                return handler.HandleAsync(effect, sink, cancellationToken);
            }
        }

        return default;
    }

    private static HandlerRegistration CreateRegistration(object handler)
    {
        Type? effectType = handler.GetType()
            .GetInterfaces()
            .Where(type => type.IsGenericType)
            .Where(type => type.GetGenericTypeDefinition() == typeof(IEffectHandler<,>))
            .Where(type => type.GetGenericArguments()[1] == typeof(TIntent))
            .Select(type => type.GetGenericArguments()[0])
            .FirstOrDefault();

        if (effectType is null)
        {
            throw new ArgumentException("副作用处理器类型与当前意图类型不匹配。", nameof(handler));
        }

        MethodInfo method = typeof(MviEffectRuntime<TIntent>)
            .GetMethod(nameof(InvokeHandlerAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(effectType);

        var invoker = (Func<object, IMviEffect, IIntentSink<TIntent>, CancellationToken, ValueTask>)Delegate.CreateDelegate(
            typeof(Func<object, IMviEffect, IIntentSink<TIntent>, CancellationToken, ValueTask>),
            method);

        return new HandlerRegistration(effectType, (effect, sink, cancellationToken) =>
            invoker(handler, effect, sink, cancellationToken));
    }

    private static ValueTask InvokeHandlerAsync<TEffect>(
        object handler,
        IMviEffect effect,
        IIntentSink<TIntent> sink,
        CancellationToken cancellationToken)
        where TEffect : IMviEffect
    {
        return ((IEffectHandler<TEffect, TIntent>)handler).HandleAsync(
            (TEffect)effect,
            sink,
            cancellationToken);
    }

    private sealed record HandlerRegistration(
        Type EffectType,
        Func<IMviEffect, IIntentSink<TIntent>, CancellationToken, ValueTask> HandleAsync);
}
