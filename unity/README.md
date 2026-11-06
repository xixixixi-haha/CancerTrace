# CancerTrace Unity 客户端

## 开发环境

使用 Unity Hub 和 **Unity 2021.3.45f2c1** 打开正式工程 [CancerTraceClient/](CancerTraceClient/)。编辑器版本见 [ProjectVersion.txt](CancerTraceClient/ProjectSettings/ProjectVersion.txt)。`CancerTraceClient_backup/` 为历史副本。

加载 [MainMenu.unity](CancerTraceClient/Assets/Scenes/MainMenu.unity) 后进入 Play Mode。[场景说明](CancerTraceClient/Assets/Scenes/README.md) 记录五个正式场景，运行数据位于 [Assets/StreamingAssets/GameData/](CancerTraceClient/Assets/StreamingAssets/GameData/)。设计与系统文档见 [Unity 开发文档](Docs/README.md)。

## 已记录的验证范围

- Phase 4A 的 Runtime/Save 和 Core Gameplay smoke 测试在独立临时工程中通过。该工程未加载正式字体、场景或 UI。正式脚本分别以 Editor 和 Player 编译条件完成编译检查。Play Mode UI Runner 的完整实机流程未在该阶段执行。[历史测试记录](../docs/Phase4A_Final/TEST_RESULTS.md)
- 项目所有者已确认 Windows EXE 在 1920×1080 窗口模式下显示正常。非 16:9 原生分辨率全屏尚无对应的验收记录。

## 测试入口与存档隔离

`Assets/Scripts/Debug/` 中的四个 Smoke 脚本受 `UNITY_EDITOR` 条件编译保护。Runtime/Save 和 Core Gameplay 测试使用系统临时目录 `CancerTraceSmokeTests`。Play Mode Launcher 为每次测试单独覆盖正式进度和教程进度的路径，并在退出 Play Mode 时清除路径覆盖。
