﻿﻿﻿namespace MiKiNuo.Mvi.Application.DI;

/// <summary>
/// 表示 MVI 服务解析器。
/// </summary>
public interface IMviResolver
{
    /// <summary>
    /// 解析服务。
    /// </summary>
    /// <typeparam name="TService">服务类型。</typeparam>
    /// <returns>服务实例。</returns>
    public TService Resolve<TService>()
        where TService : notnull;

    /// <summary>
    /// 按构造参数即时构造并返回服务实例。
    /// <para>
    /// 与 <see cref="Resolve{TService}"/> 的区别：本方法**不**走容器注册的生命周期缓存，
    /// 每次调用都按传入的 <paramref name="args"/> 通过匹配的公共构造函数重新创建实例。
    /// 适用于"按运行期参数（如用户名）实例化一次性子 ViewModel"的场景。
    /// </para>
    /// </summary>
    /// <typeparam name="TService">要实例化的服务类型。</typeparam>
    /// <param name="args">构造函数实参，按 <c>params object[]</c> 传入，
    /// 由实现在运行时按公共构造函数签名匹配；当前实现选择参数个数相等且类型可赋值的构造函数。</param>
    /// <returns>新构造的实例。</returns>
    public TService CreateWith<TService>(params object[] args)
        where TService : notnull;

    /// <summary>
    /// 解析指定类型的服务。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    /// <returns>服务实例。</returns>
    public object Resolve(Type serviceType);

    /// <summary>
    /// 创建作用域。
    /// </summary>
    /// <returns>服务作用域。</returns>
    public IMviScope CreateScope();
}
