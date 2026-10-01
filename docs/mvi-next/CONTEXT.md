# MiKiNuo.Mvi 下一版本

面向复杂业务 UI 与实时交互的 MVI 框架。业务由独立可复用的 Feature 组织，支持纯状态转换、异步业务流程与动态组合。

本词汇表适用于下一版本设计；当前代码的术语定义仍见仓库根 CONTEXT.md。

## Language

**Feature**:
可独立运行和复用的一块业务功能，具有自己的业务状态与交互流程，也可由完整的子 Feature 组成。
_Avoid_: 子 Reducer、页面类型

**Feature 实例**:
某一 Feature 的一次独立运行，拥有自己的状态、执行活动与生命周期；同类型实例之间互相独立。
_Avoid_: 业务对象 ID、全局单例

**State**:
一个 Feature 实例的不可变业务状态快照，是其状态规则与界面表达的依据；不同实例分别拥有自己的 State。
_Avoid_: ViewModel、外部业务实体

**纯状态转换**:
根据当前业务状态和输入计算新业务状态的规则，不执行 IO 或其他外部操作。
_Avoid_: 异步业务流程、副作用执行

**业务操作**:
属于某个 Feature 实例的一次业务流程，可以等待外部操作并提出业务状态变化。
_Avoid_: UI Command、EffectDispatcher

**操作运行状态**:
一次业务操作的执行状态，区分运行、结束、取消与异常；执行正常结束不代表业务结果成功。
_Avoid_: 业务状态、业务成功标志

**组合 Feature**:
由多个完整且独立的子 Feature 实例构成的 Feature，承担整体业务协调；子 Feature 能在其他宿主中复用。
_Avoid_: 子状态拼接、共享 Store

**Mediator**:
跨 Feature 业务通信的中介，按照明确的目标或通信范围传递请求和通知。
_Avoid_: 全局广播总线、直接调用兄弟功能

**View**:
Feature 的某种平台界面表达，展示状态并表达用户交互。
_Avoid_: Feature 实例、业务处理器
