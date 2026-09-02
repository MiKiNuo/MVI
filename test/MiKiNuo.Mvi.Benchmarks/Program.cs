using BenchmarkDotNet.Running;

namespace MiKiNuo.Mvi.Benchmarks;

/// <summary>
/// 表示基准测试程序入口。
/// <para>
/// 默认全量运行全部基准；支持 BenchmarkDotNet 标准参数
/// （如 <c>-f</c> 过滤、<c>--list flat</c> 列出基准清单、<c>-j short</c> 缩短迭代）。
/// </para>
/// </summary>
public static class Program
{
    /// <summary>
    /// 程序入口，转发命令行参数到 BenchmarkDotNet。
    /// </summary>
    /// <param name="args">BenchmarkDotNet 命令行参数。</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
