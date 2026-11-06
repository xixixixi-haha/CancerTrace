# CancerTrace Unity 客户端

正式工程为 `CancerTraceClient/`，使用 Unity 2021.3.45f2c1。已有五个正式场景及完整教程、调查、病例结果和 Shift 汇总流程。`CancerTraceClient_backup/` 是历史副本，请勿作为正式工程。

从 MainMenu 运行；正式数据位于 Assets/StreamingAssets/GameData。已完成 Windows x64 / Mono 首次试构建，产物位于 `D:/Builds/CancerTrace/1.0.0-Windows-x64`。项目所有者已确认现有 EXE 的窗口模式运行验收通过，1920×1080 下显示正常；非 16:9 原生分辨率全屏显示不在已通过范围内，素材授权仍待核验。操作、存档和许可参见[根目录 README](../README.md)，设计记录见 [Docs/README.md](Docs/README.md)，历史规划不代表已实现功能。

Assets/Scripts/Debug 中保留的四个 Smoke 测试脚本均由 UNITY_EDITOR 条件编译保护，不进入 Player；临时显示诊断脚本已移除。RuntimeSaveSmokeTest 与 CoreGameplaySmokeTest 使用系统临时目录 CancerTraceSmokeTests。Play Mode Launcher 为每次运行提供独立 save/tutorial 路径，退出 Play Mode 清除路径覆盖；不会清理正式存档。
