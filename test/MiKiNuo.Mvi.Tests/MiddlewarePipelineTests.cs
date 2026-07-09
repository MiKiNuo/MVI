using MiKiNuo.Mvi.Application.MVI.Diagnostics;
using MiKiNuo.Mvi.Application.MVI.Middleware;
using MiKiNuo.Mvi.Application.MVI.Store;
using MiKiNuo.Mvi.Domain.Common.Errors;
using MiKiNuo.Mvi.Domain.Common.Results;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI 中间件管道测试。
/// </summary>
public sealed class MiddlewarePipelineTests
{
    /// <summary>
    /// 验证中间件可以记录 Intent、校验结果和性能耗时。
    /// </summary>
    [Test]
    public async Task MiddlewarePipeline_Should_RecordIntentValidationAndPerformanceAsync()
    {
        using MviMemoryDiagnosticSink diagnosticSink = new();
        IReadOnlyList<IMviMiddleware<PatientSearchState, PatientSearchIntent, PatientSearchEffect>> middlewares =
        [
            new MviValidationMiddleware<PatientSearchState, PatientSearchIntent, PatientSearchEffect>(
                static (state, intent) => intent is PatientSearchIntent.SearchPatient && string.IsNullOrWhiteSpace(state.QueryText)
                    ? OperationResult.Failure(new DomainError("PatientSearch.QueryEmpty", "患者检索关键字不能为空"))
                    : OperationResult.Success(),
                diagnosticSink,
                "患者检索测试 MVI"),
            new MviLoggingMiddleware<PatientSearchState, PatientSearchIntent, PatientSearchEffect>(diagnosticSink, "患者检索测试 MVI"),
            new MviPerformanceMiddleware<PatientSearchState, PatientSearchIntent, PatientSearchEffect>(diagnosticSink, "患者检索测试 MVI")
        ];

        using MviStore<PatientSearchState, PatientSearchIntent, PatientSearchEffect> store = new(
            PatientSearchState.CreateInitial("住院床位"),
            new PatientSearchIntentHandler(),
            new PatientSearchReducer(),
            new NoopEffectDispatcher<PatientSearchEffect>(),
            middlewares);

        await store.DispatchAsync(new PatientSearchIntent.SearchPatient());
        await store.DispatchAsync(new PatientSearchIntent.ChangeQueryText("张三"));
        await store.DispatchAsync(new PatientSearchIntent.SearchPatient());

        await Assert.That(diagnosticSink.Entries.Count > 0).IsTrue();
        await Assert.That(diagnosticSink.Entries.Any(static entry => entry.Stage == "Validation")).IsTrue();
        await Assert.That(diagnosticSink.Entries.Any(static entry => entry.Stage == "Middleware")).IsTrue();
        await Assert.That(store.CurrentState.CanSelectPatient).IsTrue();
    }
}
