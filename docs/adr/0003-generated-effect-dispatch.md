# [MviEffect] 源生成器消除 EffectDispatcher 手写分派

统一 Effect 通道后，EffectDispatcher 成为唯一可能手写 if/switch 分派链的位置。与 Reducer 的 `[MviReduce]` 对称，新增 `[MviEffect(typeof(XxxEffect))]` 方法特性，由源生成器 emit `DispatchCoreAsync` 的 switch 分派，实现"全套框架零手写 switch"。配套诊断 MVI0012–MVI0015（非 partial、缺处理方法、重复标记、签名不符），规则与 MVI0004–MVI0007 对齐。
