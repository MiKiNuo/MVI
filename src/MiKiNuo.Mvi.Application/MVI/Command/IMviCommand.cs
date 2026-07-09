using System.Windows.Input;

namespace MiKiNuo.Mvi.Application.MVI.Command;

/// <summary>
/// 表示 MVI 同步命令。
/// </summary>
/// <remarks>
/// 继承自 <see cref="ICommand"/> 以统一平台绑定入口:
/// <c>MviCommandBase</c> 已实现 <see cref="ICommand"/>,
/// 故 <see cref="IMviCommand"/> 直接扩展该接口。
/// <see cref="IMviAsyncCommand"/> 继承本接口以支持统一入口。
/// </remarks>
public interface IMviCommand : ICommand
{
}
