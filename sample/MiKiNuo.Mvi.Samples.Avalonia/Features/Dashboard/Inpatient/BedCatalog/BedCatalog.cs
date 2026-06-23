namespace MiKiNuo.Mvi.Samples.Avalonia.Features.Dashboard.Inpatient.BedCatalog;

/// <summary>
/// 表示仪表板演示用的床位目录（静态、不可变）。
/// 启动时一次性生成 60 张代表性床位，覆盖 4 个病区、4 种床位类型、4 种状态；
/// 供 BedOverview 卡片的 ComboBox 筛选后展示当前匹配数 + 前若干条床位摘要使用。
/// </summary>
/// <remarks>
/// 当前为 in-memory 演示数据；真实接入 HIS 时应由床位服务替换（参考 <c>IMviPatientRegistry</c> 的注入位置）。
/// 设计目标：UI 重设计期间能立刻在 Avalonia 样例中可视化 ComboBox 交互结果。
/// </remarks>
public static class BedCatalog
{
    private static readonly IReadOnlyList<BedRecord> _all = Build();

    /// <summary>获取全部床位记录（按 BedNo 升序）。</summary>
    public static IReadOnlyList<BedRecord> All => _all;

    /// <summary>获取床位目录总条数。</summary>
    public static int TotalCount => _all.Count;

    /// <summary>
    /// 按筛选维度返回匹配的床位集合。
    /// </summary>
    /// <param name="filter">筛选维度（<see cref="BedFilter.All"/> 返回全部）。</param>
    /// <returns>匹配筛选的床位集合（按 BedNo 升序）。</returns>
    public static IReadOnlyList<BedRecord> Query(BedFilter filter)
    {
        if (filter == BedFilter.All)
        {
            return _all;
        }

        BedStatus target = ToStatus(filter);
        List<BedRecord> result = new(_all.Count);
        foreach (BedRecord record in _all)
        {
            if (record.Status == target)
            {
                result.Add(record);
            }
        }

        return result;
    }

    /// <summary>
    /// 按床位类型 + 床位状态两个维度同时筛选。
    /// 任意一个集合为空表示「该维度不过滤」；两个集合同时非空时取交集。
    /// </summary>
    /// <param name="typeFilter">床位类型筛选集合（空集合 = 不限类型）。</param>
    /// <param name="statusFilter">床位状态筛选集合（空集合 = 不限状态）。</param>
    /// <returns>匹配筛选的床位集合（按 BedNo 升序）。</returns>
    public static IReadOnlyList<BedRecord> Query(IReadOnlySet<BedType> typeFilter, IReadOnlySet<BedStatus> statusFilter)
    {
        ArgumentNullException.ThrowIfNull(typeFilter);
        ArgumentNullException.ThrowIfNull(statusFilter);

        if (typeFilter.Count == 0 && statusFilter.Count == 0)
        {
            return _all;
        }

        List<BedRecord> result = new(_all.Count);
        foreach (BedRecord record in _all)
        {
            bool typeOk = typeFilter.Count == 0 || typeFilter.Contains(record.Type);
            bool statusOk = statusFilter.Count == 0 || statusFilter.Contains(record.Status);
            if (typeOk && statusOk)
            {
                result.Add(record);
            }
        }

        return result;
    }

    /// <summary>
    /// 按筛选维度统计匹配条数。
    /// </summary>
    /// <param name="filter">筛选维度。</param>
    /// <returns>匹配条数。</returns>
    public static int Count(BedFilter filter)
    {
        if (filter == BedFilter.All)
        {
            return _all.Count;
        }

        BedStatus target = ToStatus(filter);
        int count = 0;
        foreach (BedRecord record in _all)
        {
            if (record.Status == target)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 返回匹配筛选的前 N 条床位摘要（用于 BedOverview 卡片中部紧凑展示）。
    /// 当筛选结果 ≤ N 时按 BedNo 升序返回全部；否则取前 N 条。
    /// </summary>
    /// <param name="filter">筛选维度。</param>
    /// <param name="take">最大条数。</param>
    /// <returns>床位摘要集合。</returns>
    public static IReadOnlyList<BedRecord> Take(BedFilter filter, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        IReadOnlyList<BedRecord> matched = Query(filter);
        if (matched.Count <= take)
        {
            return matched;
        }

        BedRecord[] slice = new BedRecord[take];
        for (int i = 0; i < take; i++)
        {
            slice[i] = matched[i];
        }

        return slice;
    }

    private static BedStatus ToStatus(BedFilter filter)
    {
        return filter switch
        {
            BedFilter.Open => BedStatus.Open,
            BedFilter.Occupied => BedStatus.Occupied,
            BedFilter.Locked => BedStatus.Locked,
            BedFilter.Isolated => BedStatus.Isolated,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "未知的 BedFilter 值。"),
        };
    }

    /// <summary>
    /// 合并三种筛选维度：ComboBox 单选状态 + CheckBox 多选类型 + CheckBox 多选状态。
    /// 单选状态优先取对应 <see cref="BedStatus"/>，再与多选状态取交集；多选集合为空表示该维度不过滤。
    /// </summary>
    /// <param name="filter">ComboBox 单选筛选维度（<see cref="BedFilter.All"/> 不参与状态过滤）。</param>
    /// <param name="typeFilter">CheckBox 多选床位类型集合（空集合 = 不限类型）。</param>
    /// <param name="statusFilter">CheckBox 多选床位状态集合（空集合 = 不限状态）。</param>
    /// <returns>三个维度都满足的床位集合（按 BedNo 升序）。</returns>
    public static IReadOnlyList<BedRecord> QueryCombined(
        BedFilter filter,
        IReadOnlySet<BedType> typeFilter,
        IReadOnlySet<BedStatus> statusFilter)
    {
        ArgumentNullException.ThrowIfNull(typeFilter);
        ArgumentNullException.ThrowIfNull(statusFilter);

        BedStatus? filterStatus = filter == BedFilter.All ? null : ToStatus(filter);
        bool filterTypeEmpty = typeFilter.Count == 0;
        bool filterStatusEmpty = statusFilter.Count == 0;
        if (filterStatus is null && filterTypeEmpty && filterStatusEmpty)
        {
            return _all;
        }

        List<BedRecord> result = new(_all.Count);
        foreach (BedRecord record in _all)
        {
            if (MatchesCombined(record, filterStatus, typeFilter, statusFilter, filterTypeEmpty))
            {
                result.Add(record);
            }
        }

        return result;
    }

    /// <summary>
    /// 合并三种筛选维度后统计匹配条数。语义与 <see cref="QueryCombined"/> 一致。
    /// </summary>
    /// <param name="filter">ComboBox 单选筛选维度。</param>
    /// <param name="typeFilter">CheckBox 多选床位类型集合（空集合 = 不限类型）。</param>
    /// <param name="statusFilter">CheckBox 多选床位状态集合（空集合 = 不限状态）。</param>
    /// <returns>三个维度都满足的床位条数。</returns>
    public static int CountCombined(
        BedFilter filter,
        IReadOnlySet<BedType> typeFilter,
        IReadOnlySet<BedStatus> statusFilter)
    {
        ArgumentNullException.ThrowIfNull(typeFilter);
        ArgumentNullException.ThrowIfNull(statusFilter);

        BedStatus? filterStatus = filter == BedFilter.All ? null : ToStatus(filter);
        bool filterTypeEmpty = typeFilter.Count == 0;
        bool filterStatusEmpty = statusFilter.Count == 0;
        if (filterStatus is null && filterTypeEmpty && filterStatusEmpty)
        {
            return _all.Count;
        }

        int count = 0;
        foreach (BedRecord record in _all)
        {
            if (MatchesCombined(record, filterStatus, typeFilter, statusFilter, filterTypeEmpty))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 判断单条床位是否同时满足三个筛选维度。
    /// </summary>
    /// <param name="record">床位记录。</param>
    /// <param name="filterStatus">单选状态（null 表示不过滤）。</param>
    /// <param name="typeFilter">多选类型集合。</param>
    /// <param name="statusFilter">多选状态集合。</param>
    /// <param name="filterTypeEmpty">类型集合是否为空。</param>
    /// <returns>是否满足全部维度。</returns>
    private static bool MatchesCombined(
        BedRecord record,
        BedStatus? filterStatus,
        IReadOnlySet<BedType> typeFilter,
        IReadOnlySet<BedStatus> statusFilter,
        bool filterTypeEmpty)
    {
        bool typeOk = filterTypeEmpty || typeFilter.Contains(record.Type);
        bool statusOk = true;
        if (filterStatus is not null)
        {
            statusOk = record.Status == filterStatus.Value;
        }

        if (statusOk && statusFilter.Count > 0)
        {
            statusOk = statusFilter.Contains(record.Status);
        }

        return typeOk && statusOk;
    }

    private static IReadOnlyList<BedRecord> Build()
    {
        // 4 个病区 × 每病区 15 张床位 = 60 张；床位类型与状态按固定模式分布，确保 ComboBox 切换有可视差异。
        string[] wards = ["东病区", "西病区", "ICU", "急诊"];
        BedType[] typeCycle = [BedType.General, BedType.General, BedType.General, BedType.IntensiveCare, BedType.Isolation, BedType.Recovery];
        BedStatus[] statusCycle = [BedStatus.Open, BedStatus.Open, BedStatus.Occupied, BedStatus.Locked, BedStatus.Isolated];
        string[] wardAbbr = ["东", "西", "I", "急"];

        List<BedRecord> result = new(60);
        int globalIndex = 0;
        for (int wardIndex = 0; wardIndex < wards.Length; wardIndex++)
        {
            string ward = wards[wardIndex];
            string abbr = wardAbbr[wardIndex];
            for (int roomIndex = 1; roomIndex <= 15; roomIndex++)
            {
                BedType type = typeCycle[globalIndex % typeCycle.Length];
                BedStatus status = statusCycle[globalIndex % statusCycle.Length];
                string bedNo = $"{abbr}-{roomIndex:D2}-{(globalIndex % 2 == 0 ? "A" : "B")}";
                string? patient = status == BedStatus.Occupied ? GeneratePatientName(globalIndex) : null;
                string? doctor = status == BedStatus.Occupied ? GenerateDoctorName(globalIndex) : null;
                result.Add(new BedRecord(bedNo, ward, type, status, patient, doctor));
                globalIndex++;
            }
        }

        return result;
    }

    private static string GeneratePatientName(int index)
    {
        string[] surnames = ["张", "李", "王", "赵", "钱", "孙", "周", "吴", "郑", "冯"];
        string[] givens = ["明", "华", "建国", "秀英", "伟", "芳", "磊", "静", "勇", "丽"];
        string surname = surnames[index % surnames.Length];
        string given = givens[(index / surnames.Length) % givens.Length];
        return $"{surname}{given}";
    }

    private static string GenerateDoctorName(int index)
    {
        string[] surnames = ["陈", "林", "黄", "何", "高"];
        string[] titles = ["主任", "副主任", "主治"];
        return $"{surnames[index % surnames.Length]}{titles[(index / surnames.Length) % titles.Length]}医师";
    }
}
