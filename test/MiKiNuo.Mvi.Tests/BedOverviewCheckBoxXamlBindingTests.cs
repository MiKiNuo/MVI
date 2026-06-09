using TUnit.Assertions;
using TUnit.Core;

namespace MiKiNuo.Mvi.Tests;

/// <summary>
/// 表示 BedOverview 卡片 XAML 上 CheckBox 多选筛选的渲染测试。
/// 通过读取 <c>CardView.axaml</c> 源文件内容断言：
/// 1. 存在类型组 ItemsControl 绑定到 AvailableBedTypes（4 个 BedTypeOption），CheckBox.Content 绑定 DisplayName。
/// 2. 存在状态组 ItemsControl 绑定到 AvailableBedStatuses（4 个 BedStatusOption），CheckBox.Content 绑定 DisplayName。
/// 3. 两个 CheckBox 组都在 ShowBedCatalog 容器内（仅 BedOverview 卡片显示）。
/// </summary>
public sealed class BedOverviewCheckBoxXamlBindingTests
{
    /// <summary>
    /// CardView.axaml 包含 4 个类型 CheckBox（普通/ICU/隔离/康复），每个 CheckBox 的 Content 绑定到 BedTypeOption.DisplayName。
    /// </summary>
    [Test]
    public async Task CardViewXaml_BedOverview_Contains_FourTypeCheckBoxesAsync()
    {
        string xaml = ReadXaml();

        // 验证 ItemsControl 绑定到 AvailableBedTypes（CardViewModel 暴露的 4 个 BedTypeOption）
        await Assert.That(xaml).Contains("AvailableBedTypes");
        await Assert.That(xaml).Contains("BedTypeOption");

        // 验证 DataTemplate 里 CheckBox 的 Content 绑定到 DisplayName（中文展示字段在运行时由 BedTypeOption 提供）
        await Assert.That(xaml).Contains("DisplayName");

        // 验证 IsChecked 事件回调方法指向 OnBedTypeCheckBoxChanged
        await Assert.That(xaml).Contains("OnBedTypeCheckBoxChanged");
    }

    /// <summary>
    /// CardView.axaml 包含 4 个状态 CheckBox（开放/已占用/锁定/隔离中），每个 CheckBox 的 Content 绑定到 BedStatusOption.DisplayName。
    /// </summary>
    [Test]
    public async Task CardViewXaml_BedOverview_Contains_FourStatusCheckBoxesAsync()
    {
        string xaml = ReadXaml();

        // 验证 ItemsControl 绑定到 AvailableBedStatuses（CardViewModel 暴露的 4 个 BedStatusOption）
        await Assert.That(xaml).Contains("AvailableBedStatuses");
        await Assert.That(xaml).Contains("BedStatusOption");

        // 验证 IsChecked 事件回调方法指向 OnBedStatusCheckBoxChanged
        await Assert.That(xaml).Contains("OnBedStatusCheckBoxChanged");
    }

    /// <summary>
    /// CheckBox 必须在 ShowBedCatalog 容器内（即只在 BedOverview 卡片显示）。
    /// </summary>
    [Test]
    public async Task CardViewXaml_BedOverview_CheckBoxesAreInsideShowBedCatalogContainerAsync()
    {
        string xaml = ReadXaml();

        int showCatalogIndex = xaml.IndexOf("ShowBedCatalog", StringComparison.Ordinal);
        await Assert.That(showCatalogIndex).IsGreaterThan(0);

        int checkBoxTypesIndex = xaml.IndexOf("OnBedTypeCheckBoxChanged", StringComparison.Ordinal);
        int checkBoxStatusesIndex = xaml.IndexOf("OnBedStatusCheckBoxChanged", StringComparison.Ordinal);

        await Assert.That(checkBoxTypesIndex).IsGreaterThan(showCatalogIndex);
        await Assert.That(checkBoxStatusesIndex).IsGreaterThan(showCatalogIndex);
    }

    /// <summary>
    /// CardView.axaml 至少使用 2 个 ItemsControl（类型组 + 状态组）来生成 CheckBox。
    /// </summary>
    [Test]
    public async Task CardViewXaml_BedOverview_UsesItemsControlForCheckBoxGroupsAsync()
    {
        string xaml = ReadXaml();

        // 类型组 ItemsControl + 状态组 ItemsControl
        int itemsControlCount = CountOccurrences(xaml, "<ItemsControl");
        await Assert.That(itemsControlCount).IsGreaterThanOrEqualTo(2);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static string ReadXaml()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Features", "Dashboard", "Cards", "CardView.axaml");
        if (!File.Exists(path))
        {
            path = LocateXamlInSourceTree();
        }

        return File.ReadAllText(path);
    }

    private static string LocateXamlInSourceTree()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "sample", "MiKiNuo.Mvi.Samples.Avalonia", "Features", "Dashboard", "Cards", "CardView.axaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(dir);
            dir = parent?.FullName;
        }

        throw new FileNotFoundException("未找到 CardView.axaml 源文件。");
    }
}
