namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Cards;

/// <summary>
/// 表示 16 张仪表板卡片的静态注册表。
/// Reducer 和 ViewModel 通过 PageKey 查找对应的 CardDefinition。
/// 顺序：先 Inpatient（4），再 Lab（4），再 Pharmacy（4），再 Quality（4）。
/// 实际定义按业务域拆分到
/// <c>DashboardCardRegistry.Inpatient.cs</c> / <c>DashboardCardRegistry.Lab.cs</c> /
/// <c>DashboardCardRegistry.Pharmacy.cs</c> / <c>DashboardCardRegistry.Quality.cs</c>
/// 四个分部类文件中。
/// </summary>
public static partial class DashboardCardRegistry
{
    private static readonly IReadOnlyDictionary<PageKey, CardDefinition> DefinitionsByKey = BuildDefinitions();

    /// <summary>
    /// 获取所有卡片定义。
    /// </summary>
    public static IReadOnlyDictionary<PageKey, CardDefinition> All => DefinitionsByKey;

    /// <summary>
    /// 根据 PageKey 查找卡片定义。
    /// </summary>
    /// <param name="key">PageKey。</param>
    /// <returns>找到的 CardDefinition；若不存在则返回 null。</returns>
    public static CardDefinition? GetDefinition(PageKey key)
    {
        return DefinitionsByKey.TryGetValue(key, out CardDefinition? definition) ? definition : null;
    }

    /// <summary>
    /// 获取与给定 PageKey 同 SourceKey 组（住院床位 / 检验医嘱 / 处方 / 风险）的兄弟卡片 PageKey 集合。
    /// 同组卡片共享一个 4 卡片组合页面（如住院床位 = 床位总览 + 护理任务 + 病区风险 + 入院登记），
    /// 跨卡片副作用会通过此方法定位派发目标；不同 SourceKey 的卡片彼此隔离。
    /// </summary>
    /// <param name="key">源 PageKey。</param>
    /// <returns>同组的兄弟 PageKey 集合；找不到定义时返回空集合。</returns>
    public static IReadOnlyList<PageKey> GetSiblingKeys(PageKey key)
    {
        CardDefinition? source = GetDefinition(key);
        if (source is null)
        {
            return Array.Empty<PageKey>();
        }

        List<PageKey> siblings = new(DefinitionsByKey.Count);
        foreach (KeyValuePair<PageKey, CardDefinition> pair in DefinitionsByKey)
        {
            if (pair.Key == key)
            {
                continue;
            }

            if (string.Equals(pair.Value.SourceKey, source.SourceKey, StringComparison.Ordinal))
            {
                siblings.Add(pair.Key);
            }
        }

        return siblings;
    }

    private static IReadOnlyDictionary<PageKey, CardDefinition> BuildDefinitions()
    {
        Dictionary<PageKey, CardDefinition> dict = new();
        AddInpatientDefinitions(dict);
        AddLabDefinitions(dict);
        AddPharmacyDefinitions(dict);
        AddQualityDefinitions(dict);
        AddArchitectureValidationDefinitions(dict);
        return dict;
    }

    private static partial void AddInpatientDefinitions(Dictionary<PageKey, CardDefinition> dict);

    private static partial void AddLabDefinitions(Dictionary<PageKey, CardDefinition> dict);

    private static partial void AddPharmacyDefinitions(Dictionary<PageKey, CardDefinition> dict);

    private static partial void AddQualityDefinitions(Dictionary<PageKey, CardDefinition> dict);

    private static partial void AddArchitectureValidationDefinitions(Dictionary<PageKey, CardDefinition> dict);

    /// <summary>
    /// 构造通用 Form Card 验证器。
    /// 由各业务域分部的私有 Validator 构造方法复用。
    /// </summary>
    /// <param name="requiredKeys">提交必填字段 Key 集合。</param>
    /// <param name="contextName">业务上下文名称（用于日志前缀）。</param>
    /// <param name="incompleteStatusText">未填齐必填字段时的状态文本。</param>
    /// <param name="completeStatusText">已填齐必填字段时的状态文本。</param>
    /// <param name="logKeys">日志中需要展示的字段 (Key, DisplayName) 元组集合。</param>
    /// <returns>绑定到 FormValues 的验证函数。</returns>
    private static Func<IReadOnlyList<CardFormValueEntry>, (bool CanSubmit, string StatusText, string ActionLog)> BuildFormValidator(
        string[] requiredKeys,
        string contextName,
        string incompleteStatusText,
        string completeStatusText,
        (string Key, string DisplayName)[] logKeys)
    {
        return values =>
        {
            IReadOnlyDictionary<string, string> lookup = BuildLookup(values);
            bool canSubmit = requiredKeys.All(key => !string.IsNullOrWhiteSpace(lookup[key]));
            string statusText = canSubmit ? completeStatusText : incompleteStatusText;
            string logParts = logKeys.Length == 0
                ? string.Empty
                : logKeys.Select(entry => $"{entry.DisplayName}={lookup[entry.Key]}").Aggregate((left, right) => $"{left}，{right}");
            return (canSubmit, statusText, $"正在录入{contextName}：{logParts}。");
        };
    }

    /// <summary>
    /// 把 FormValues 数组转成以 Key 为索引的字典。
    /// 与原始实现一致：仅在尚未出现过的键上记录，保留"首个匹配"的语义。
    /// </summary>
    /// <param name="values">Form 字段值集合。</param>
    /// <returns>Key → Value 的查找字典。</returns>
    private static IReadOnlyDictionary<string, string> BuildLookup(IReadOnlyList<CardFormValueEntry> values)
    {
        Dictionary<string, string> lookup = new(StringComparer.Ordinal);
        foreach (CardFormValueEntry entry in values)
        {
            lookup.TryAdd(entry.Key, entry.Value);
        }

        return lookup;
    }
}
