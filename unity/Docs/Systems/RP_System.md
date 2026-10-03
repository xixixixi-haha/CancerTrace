# RP 科研点系统

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 9 节

## 已确认规则

- RP 表示单个 Shift 内可调用的科研资源，不是金币或奖励货币。
- 每个Shift初始`remainingRP = 200`。
- 整个Shift的10个Case共享这200 RP，中途不刷新、不补充。
- 进入下一个Shift时`remainingRP`重置为200。
- Gene Scan 消耗 10 RP。
- Cancer Galaxy 消耗 25 RP。
- AI Assistant 消耗 40 RP。
- 当`remainingRP < Tool Cost`时，对应按钮禁用、显示RP不足提示且不允许执行。
- `remainingRP`不得低于0，不允许欠费、借贷、自动补充或Emergency Grant。
- Submit Diagnosis始终免费，即使RP为0仍可提交。
- Tutorial不消耗RP。
- Gene Scan、Cancer Galaxy和AI Assistant在每个Case中均最多使用一次。
- 不存在每5个Case自动刷新、RP奖励、金币、商店或刷资源机制。

## 待补充

- 消耗操作的确认、取消和重复点击保护。
- RP 变更事件及显示刷新约定。

本文档不得自行新增或修改 GDD 中的游戏规则。
