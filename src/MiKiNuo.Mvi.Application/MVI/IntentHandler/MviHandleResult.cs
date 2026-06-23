using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Mutation;

namespace MiKiNuo.Mvi.Application.MVI.IntentHandler;

/// <summary>
/// 表示意图处理结果。
/// </summary>
/// <typeparam name="TMutation">变更类型。</typeparam>
/// <typeparam name="TEffect">副作用类型。</typeparam>
/// <param name="Mutations">产生的变更集合。</param>
/// <param name="Effects">产生的动作副作用集合。</param>
public sealed record MviHandleResult<TMutation, TEffect>(
    IReadOnlyList<TMutation> Mutations,
    IReadOnlyList<TEffect> Effects)
    where TMutation : IMviMutation
    where TEffect : IMviEffect;

/// <summary>
/// 表示意图处理结果工厂。
/// </summary>
public static class MviHandleResult
{
    /// <summary>
    /// 创建仅包含变更的处理结果。
    /// </summary>
    /// <typeparam name="TMutation">变更类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="mutations">变更集合。</param>
    /// <returns>处理结果。</returns>
    public static MviHandleResult<TMutation, TEffect> Mutations<TMutation, TEffect>(
        params TMutation[] mutations)
        where TMutation : IMviMutation
        where TEffect : IMviEffect
    {
        return new MviHandleResult<TMutation, TEffect>(mutations, Array.Empty<TEffect>());
    }

    /// <summary>
    /// 创建包含变更和副作用的处理结果。
    /// </summary>
    /// <typeparam name="TMutation">变更类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <param name="mutations">变更集合。</param>
    /// <param name="effects">副作用集合。</param>
    /// <returns>处理结果。</returns>
    public static MviHandleResult<TMutation, TEffect> MutationsAndEffects<TMutation, TEffect>(
        IReadOnlyList<TMutation> mutations,
        IReadOnlyList<TEffect> effects)
        where TMutation : IMviMutation
        where TEffect : IMviEffect
    {
        return new MviHandleResult<TMutation, TEffect>(mutations, effects);
    }

    /// <summary>
    /// 创建空的处理结果。
    /// </summary>
    /// <typeparam name="TMutation">变更类型。</typeparam>
    /// <typeparam name="TEffect">副作用类型。</typeparam>
    /// <returns>空处理结果。</returns>
    public static MviHandleResult<TMutation, TEffect> Empty<TMutation, TEffect>()
        where TMutation : IMviMutation
        where TEffect : IMviEffect
    {
        return new MviHandleResult<TMutation, TEffect>(
            Array.Empty<TMutation>(),
            Array.Empty<TEffect>());
    }
}
