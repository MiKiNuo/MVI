using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Auth;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.V2Auth;

internal sealed class AuthFormFeedback<TState> : StackPanel where TState : notnull
{
    internal AuthFormFeedback(OperationCommand<AuthResult> command, Func<TState, string?> validation, Func<TState, AuthResult?> result)
    {
        Spacing = 12;
        SubmitButton = new Button { Content = "提交", Command = command };
        Loading = new ProgressBar { IsIndeterminate = true, Height = 4 };
        Error = new TextBlock { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
        Result = new TextBlock { TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
        Loading.Bind(IsVisibleProperty, SnapshotBinding(new FuncValueConverter<RuntimeSnapshot<TState>, bool>(
            snapshot => snapshot!.OperationStates.GetValueOrDefault("SubmitAsync")?.IsRunning == true)));
        Error.Bind(TextBlock.TextProperty, SnapshotBinding(new FuncValueConverter<RuntimeSnapshot<TState>, string>(snapshot =>
        {
            OperationState? operation = snapshot!.OperationStates.GetValueOrDefault("SubmitAsync");
            return operation?.LastResult switch
            {
                OperationResultKind.Rejected when operation.Reason == "ValidationFailed" => validation(snapshot.State) ?? "无法提交。",
                OperationResultKind.Rejected when operation.Reason == "AlreadyRunning" => "提交正在进行，请等待。",
                OperationResultKind.Rejected => "提交被拒绝。",
                OperationResultKind.Canceled => "操作已取消。",
                OperationResultKind.Faulted => "服务暂时不可用，请稍后重试。",
                OperationResultKind.Completed when result(snapshot.State) is { IsSuccess: false } failure => failure.ErrorMessage ?? "认证失败。",
                _ => string.Empty
            };
        })));
        Result.Bind(TextBlock.TextProperty, SnapshotBinding(new FuncValueConverter<RuntimeSnapshot<TState>, string>(snapshot =>
            snapshot!.OperationStates.GetValueOrDefault("SubmitAsync") is { IsRunning: false, LastResult: OperationResultKind.Completed }
                && result(snapshot.State) is { IsSuccess: true } success ? "成功：" + success.DisplayName : string.Empty)));
        Children.Add(SubmitButton);
        Children.Add(Loading);
        Children.Add(Error);
        Children.Add(Result);
    }

    internal Button SubmitButton { get; }
    internal ProgressBar Loading { get; }
    internal TextBlock Error { get; }
    internal TextBlock Result { get; }

    private static Binding SnapshotBinding(IValueConverter converter)
        => new("Snapshot") { Mode = BindingMode.OneWay, Converter = converter };
}
