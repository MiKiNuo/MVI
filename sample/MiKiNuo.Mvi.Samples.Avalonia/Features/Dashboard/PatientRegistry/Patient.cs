namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

/// <summary>
/// 表示已入院的患者记录。
/// 既是 <c>IMviPatientRegistry</c> 的存储单元，也是 <c>CardState</c> 在
/// <c>CardIntent.ApplyPatientAdmitted</c> 路径中跨卡片传递的载荷。
/// 设计为不可变 record：避免 Mediator 链路上任一节点对患者数据做意外突变。
/// </summary>
/// <param name="Id">患者唯一标识（GUID）。</param>
/// <param name="Name">患者姓名。</param>
/// <param name="Age">年龄（表单中可空，未填或解析失败时为 null）。</param>
/// <param name="Diagnosis">入院诊断。</param>
/// <param name="BedNo">目标床号。</param>
/// <param name="NurseNote">护士交接备注（过敏史、跌倒风险等，可空）。</param>
/// <param name="AdmittedAt">入院时间戳（UTC）。</param>
public sealed record Patient(
    Guid Id,
    string Name,
    int? Age,
    string Diagnosis,
    string BedNo,
    string? NurseNote,
    DateTimeOffset AdmittedAt);
