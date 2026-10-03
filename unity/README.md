# CancerTrace Unity 开发区

`unity/` 是 CancerTrace 游戏客户端的开发区域。目前仅完成可提交到 GitHub 的目录骨架，尚未创建正式 Unity 工程，也没有游戏逻辑。

后续将在 Unity Hub 中正式创建项目，并由 Unity Editor 接管 `CancerTraceClient/` 下的 `Assets`、`Packages` 与 `ProjectSettings` 等目录。

客户端数据来源于项目根目录的 `results/game_data/`。本阶段不会自动复制这些 JSON；待正式开发时再按版本明确同步到 `Assets/StreamingAssets/GameData/`。

## 项目文档索引

游戏规则以 [`Docs/Game_Design_Document_v1.0.md`](Docs/Game_Design_Document_v1.0.md) 为最高优先级设计依据。

### 设计与系统

- [文档总览](Docs/README.md)
- [游戏设计文档 v1.0](Docs/Game_Design_Document_v1.0.md)
- [游戏核心循环](Docs/Systems/Core_Gameplay_Loop.md)
- [Shift 系统](Docs/Systems/Shift_System.md)
- [RP 科研点系统](Docs/Systems/RP_System.md)
- [Case 系统](Docs/Systems/Case_System.md)
- [Gene Scan](Docs/Systems/Gene_Scan.md)
- [Cancer Galaxy](Docs/Systems/Cancer_Galaxy.md)
- [AI Assistant](Docs/Systems/AI_Assistant.md)
- [Evidence Board](Docs/Systems/Evidence_Board.md)
- [Result Summary](Docs/Systems/Result_Summary.md)
- [Tutorial 教学流程](Docs/Systems/Tutorial_System.md)
- [本地存档系统](Docs/Systems/Save_System.md)
- [成就系统（可选扩展）](Docs/Systems/Achievement_System.md)

### 开发与美术规范

- [场景规划](Docs/Scene_Plan.md)
- [脚本架构](Docs/Script_Architecture.md)
- [Unity 开发规范](Docs/Unity_Development_Guide.md)
- [UI 与美术素材清单](Docs/UI_Asset_Checklist.md)
- [美术风格指南](Docs/Art_Style_Guide.md)
