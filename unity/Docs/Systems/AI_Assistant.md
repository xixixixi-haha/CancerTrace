# AI Assistant

状态：v1.0规则已同步（实现细节待补充）  
规则来源：`../Game_Design_Document_v1.0.md` 第 2.2、10.3、15、19 节

## 已确认规则

- 统一定位为 AI Assistant（AI科研助手）。
- 每次使用消耗 40 RP。
- 每个病例最多使用一次。
- Tutorial中使用不消耗RP，且不影响正式统计。
- v1.0固定输出Prediction、Confidence和Top 3 Candidates。
- AI 预测不是 100% 准确，准确表现随病例难度变化。
- AI提供预测、置信度和候选类别，最终判断由玩家负责。
- CancerTrace不是医疗诊断系统；置信度不代表医学结论。

## 固定输出格式

1. `Prediction`：预测类别。
2. `Confidence`：预测置信度。
3. `Top 3 Candidates`：前三候选类别及各自概率。

AI Explanation属于未来可扩展功能，不属于当前v1.0必备功能。不得生成虚构解释或修改冻结模型数据。

## 提交前后规则

- 提交诊断前，只有支付40 RP并使用AI Assistant后，玩家才能查看Prediction、Confidence和Top 3 Candidates。
- AI Assistant每个Case最多使用一次。
- 提交诊断后，无论玩家此前是否使用AI Assistant，Result Summary都可展示AI Prediction、AI Confidence和AI Top 3。
- 结果页展示用于教学复盘，不视为免费使用AI Assistant，因为已无法影响当前Case的玩家决策。

## 待补充

- 概率显示精度。
- 如何避免把 AI 输出呈现为确定性医学结论。

本文档不得自行新增或修改 GDD 中的游戏规则。
