namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Mediator;

/// <summary>
/// 表示仅供 CreateWith 测试用的目标 ViewModel。
/// </summary>
public sealed class SampleGreetingViewModel
{
    /// <summary>
    /// 使用指定问候语初始化实例。
    /// </summary>
    /// <param name="greeting">问候语。</param>
    public SampleGreetingViewModel(string greeting)
    {
        Greeting = greeting;
    }

    /// <summary>
    /// 获取构造时传入的问候语。
    /// </summary>
    public string Greeting { get; }
}
