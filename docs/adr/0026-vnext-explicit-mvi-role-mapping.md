# v2 明确保留 MVI 单向闭环，生成器隐藏角色样板

2026-10-01 设计澄清。State、Feature、View 是业务代码组织方式；运行语义明确为 Intent 进入 Model，纯 Reducer 产出新 RuntimeSnapshot 与 Effect 描述，Store 原子提交，EffectRunner 在门外执行，结果 Intent 重新进入统一入口。RuntimeSnapshot 同时包含业务状态、操作运行状态与版本，View 根据这个统一快照投影。

异步 Operation 方法对应副作用执行角色，UpdateAsync 对应内部强类型状态转换 Intent，不能直接修改 Store；Mediator 消息在目标入口映射为 Intent，不能绕过目标验证与 Reduce。生成器减少手写 Intent/Effect/Dispatcher 类型，保持其逻辑职责；组合仍遵循 ADR 0025，不实现跨 View 观察者订阅。

本澄清修复原方案未显示 MVI 角色及数据流的问题，具体类型结构、组件依赖与时序见 [架构设计](../mvi-next/ARCHITECTURE.md)。Reducer、Store、Effect 为本框架实现形式，并非所有原始 MVI 定义都要求使用这些类型名。
