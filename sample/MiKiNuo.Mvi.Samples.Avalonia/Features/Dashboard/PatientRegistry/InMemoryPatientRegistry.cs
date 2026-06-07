using System.Collections.Concurrent;
using MiKiNuo.Mvi.Domain.DI;
using R3;

namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.PatientRegistry;

/// <summary>
/// 表示基于 R3 Subject 的 in-memory 患者注册表实现。
/// 线程安全：通过 <see cref="ConcurrentDictionary{TKey, TValue}"/> 存储，
/// Register 完成后通过 R3 <see cref="Subject{T}"/> 推送新快照给所有订阅者。
/// <para>
/// 行为契约：
/// (1) Register 写入 ConcurrentDictionary；相同 Id 重复注册视为"更新"（覆盖同 Id 的旧记录）。
/// (2) GetSnapshot 每次返回全新的 <see cref="PatientRegistrySnapshot"/>（不与内部状态共享引用）。
/// (3) Subscribe 把 onChange 桥接到内部 Subject；Dispose 取消订阅并释放 Subject。
/// </para>
/// </summary>
[DiService(ServiceLifetime.Singleton)]
public sealed class InMemoryPatientRegistry : IMviPatientRegistry, IDisposable
{
    private readonly ConcurrentDictionary<Guid, Patient> _patients = new();
    private readonly Subject<PatientRegistrySnapshot> _changes = new();
    private bool _disposed;

    /// <inheritdoc />
    public void Register(Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _patients[patient.Id] = patient;
        _changes.OnNext(GetSnapshot());
    }

    /// <inheritdoc />
    public PatientRegistrySnapshot GetSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Patient[] ordered = _patients.Values
            .OrderBy(static p => p.AdmittedAt)
            .ToArray();
        return new PatientRegistrySnapshot(ordered);
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Action<PatientRegistrySnapshot> onChange)
    {
        ArgumentNullException.ThrowIfNull(onChange);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _changes.Subscribe(onChange);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _changes.OnCompleted();
        _changes.Dispose();
    }
}
