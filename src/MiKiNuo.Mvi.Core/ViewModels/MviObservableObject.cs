using System.ComponentModel;

namespace MiKiNuo.Mvi.Core.ViewModels;

/// <summary>
/// 表示支持属性变更通知的 MVI 可观察对象。
/// </summary>
public abstract class MviObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 触发属性变更通知。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
