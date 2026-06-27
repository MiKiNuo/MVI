using MiKiNuo.Mvi.Domain.MVI.Effect;
using MiKiNuo.Mvi.Domain.MVI.Intent;
using MiKiNuo.Mvi.Domain.MVI.State;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Menu;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Outpatient;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.AuditTimeline;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.ReusableFeatures.PatientSearch;
using MiKiNuo.Mvi.Samples.Avalonia.Features.Login;
using MiKiNuo.Mvi.Samples.Shared.Features.Login;
using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 MVI 标记接口约束测试。
/// </summary>
public sealed class MviMarkerInterfaceTests
{
    /// <summary>
    /// 验证示例状态、意图和副作用均实现 MVI 标记接口。
    /// </summary>
    [Test]
    public async Task SampleMviTypes_Should_ImplementMarkerInterfacesAsync()
    {
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(LoginState))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(LoginIntent))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(LoginEffect))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(DashboardState))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(DashboardIntent))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(DashboardMenuState))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(DashboardMenuIntent))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(DashboardMenuEffect))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(OutpatientWorkstationState))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(OutpatientWorkstationIntent))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(PatientSearchState))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(PatientSearchIntent))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(PatientSearchEffect))).IsTrue();
        await Assert.That(typeof(IMviState).IsAssignableFrom(typeof(AuditTimelineState))).IsTrue();
        await Assert.That(typeof(IMviIntent).IsAssignableFrom(typeof(AuditTimelineIntent))).IsTrue();
        await Assert.That(typeof(IMviEffect).IsAssignableFrom(typeof(UnitEffect))).IsTrue();
    }
}
