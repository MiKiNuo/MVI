namespace MiKiNuo.Mvi.Presentation.Events;

/// <summary>
/// 表示跨平台指针按钮。
/// </summary>
public enum MviPointerButton
{
    /// <summary>
    /// 未知或无按钮。
    /// </summary>
    None,

    /// <summary>
    /// 左键或主按钮。
    /// </summary>
    Left,

    /// <summary>
    /// 右键或次按钮。
    /// </summary>
    Right,

    /// <summary>
    /// 中键。
    /// </summary>
    Middle,

    /// <summary>
    /// 第一扩展按钮。
    /// </summary>
    XButton1,

    /// <summary>
    /// 第二扩展按钮。
    /// </summary>
    XButton2,

    /// <summary>
    /// 触摸输入。
    /// </summary>
    Touch,

    /// <summary>
    /// 笔输入。
    /// </summary>
    Pen
}
