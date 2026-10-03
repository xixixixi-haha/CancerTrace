# Shift 系统

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 4、9、14、18、20 节

## 已确认规则

- 一个Shift从冻结的162个独立Test Set病例中随机抽取10个Case。
- 同一Shift内不得出现重复Case。
- 不同Shift之间允许再次抽到以前出现过的Case。
- v1.0不维护跨Shift的全局病例去重池。
- 不人为保证类别均衡，也不人为保证Difficulty均衡。
- Shift开始时初始化`remainingRP = 200`和`currentScore = 0`。
- 整个Shift的10个Case共享200 RP；Shift中途不刷新、不补充。
- 病例依次从 Case 1 进行到 Case 10。
- 完成 10 个病例后进入 Shift Summary。
- 下一个Shift开始时`remainingRP`重置为200，`currentScore`重置为0。
- Shift Summary展示当前Shift的最终Score。
- Tutorial Case不属于正式Shift，也不影响正式统计。
- 本地存档保存当前Shift、当前Case、剩余RP、完成病例数量和当前Score。

## 待补充

- Shift 的创建、暂停、恢复和结束状态定义。
- 随机病例选择失败或数据不足时的异常处理方式。
- Shift Summary 完成后的导航流程。

本文档不得自行新增或修改 GDD 中的游戏规则。
