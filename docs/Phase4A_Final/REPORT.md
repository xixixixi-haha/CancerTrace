# Phase 4A Final 交付报告

## 保护措施

开始：main，HEAD 3c36d77，暂存区为空；唯一原有未提交文件为指定 TMP 字体。未暂存/提交任何文件，未执行远端操作、reset、clean、checkout、stash 或删除历史备份。

最近提交内容：fde6020 为 MainMenu/ShiftSummary 菜单入口，c0ec269 为损坏存档与教程进度保护，3c36d77 为首次正式调查教程门禁。提交标题未使用 Fix-01/04/06 编号，内容已经提交；编号与提交的正式映射仍需项目记录确认。

## A. AutoSave

自动与手动保存通过统一结果记录 HasUnsavedProgress / LastSaveError；失败不回滚内存、不创建新 Session，后续失败保留状态，成功写入才清除。Gameplay 操作返回值仍表示操作成功，保存结果独立记录。菜单失败时显示“进度尚未保存”，在现有菜单中克隆按钮样式与 TMP 字体提供“重试保存”，正常时隐藏。Prefab 未改动。

去掉 File.Replace 不支持时直接覆盖有效存档的降级；不支持原子替换会明确失败。保留替换产生的 .bak，避免替换成功后的清理异常误报保存失败。SaveVersion、JSON 结构及迁移逻辑不变。

已通过独立路径测试：初次自动保存失败、连续重试失败、当前 Session 对象保留、恢复写入后清除错误、已有有效文件在失败时字节不变。原有 Runtime/Save 与完整 Core Gameplay smoke 均通过。菜单实际显示/点击尚未 Play Mode 验收。

## B. Debug / Smoke

四个 Debug 脚本整体受 UNITY_EDITOR 保护，正式代码无 Debug 类型依赖。Editor 与 Player 两种脚本编译通过；Player 元数据隔离作静态检查，未执行 Windows Build。

Runtime/Core 测试使用系统临时目录 CancerTraceSmokeTests。Play Mode Launcher 创建独立 GUID 子目录、通过仅 Editor 生效的 SessionState 覆盖正式/教程路径，离开 Play Mode 清除覆盖。Runner 在创建 Session、执行调查、清理存档之前检查路径必须位于该目录；教程完成种子只写独立测试进度。旧入口能力保留，Play Mode UI Runner 尚未实际执行。

## C. 文档与授权

更新根目录、unity、Scenes 和 Audio README；仅 BGM、不增加 SFX。扫描 Assets/Art 全部 102 个非 meta 素材，逐项列为 Pending Verification。未凭风格猜测 AI、原创或第三方；当前没有足够证据填写另外四类。每个素材均需要所有者补充来源/作者、授权、修改记录以及适用的生成/委托证明，完整文件列表见 ASSET_PROVENANCE.md。

OFL 全文保留。BGM 本地哈希核对记录见 verification.json；原始网页返回 403，保留既有 CC0 记录，发布前需人工复核。仓库未发现统一代码 LICENSE，需所有者决定许可证。历史日志与备份盘点见 HISTORY_REVIEW.md；没有删除任何历史文件或 PNG。

## D. Windows 发布配置

Company/Product 保持 DefaultCompany/CancerTraceClient；版本改为 1.0.0；仅 Standalone Identifier 改为 com.cancertrace.game；Standalone backend 显式 Mono。使用指定 Unity 的独立工程 API 生成并核验图标序列，再仅移入相关配置块，1024 至 16 像素均引用原侦探帽 PNG；原 PNG 未改变。小尺寸预览见 icon-size-review.png，16 像素细节有限但轮廓可识别，最终 Explorer/任务栏效果待 Build 后确认。

Windows x64 在独立工程中选择成功；正式工程的本机 Editor 目标选择尚未操作。正式工程打开后执行 CancerTrace → Release → Select Windows x64 (No Build)，并在 Build Settings 复核。它只选择目标、不构建。其他平台 Identifier、全屏配置、Canvas、正式 Scene 顺序未改变。

## 静态检查与兼容性

全部基线哈希一致（字体、冻结 JSON、五个正式 Scene、BGM、原始图标及 EditorBuildSettings），逐项见 verification.json。科学数据、RP/Score/Case 规则、ANOMALY、Tutorial 内容、ShiftSummary、BGM 和 PlayerPrefs 设置未改动。存档路径的 Company/Product 未变；旧存档恢复在 smoke 通过，教程进度迁移未改动但本轮未重新做 UI 验收。

完整 git diff --check 仅报告受保护字体原有两处尾随空格，未触碰；排除该字体后检查通过。所有新增 Unity 资产附 meta。无新增 Scene、美术或音频；小尺寸 PNG 仅为 docs 下的审查图。Temp/Phase4A 是本轮相关的忽略测试产物，不进入 Assets/Player。

## Unity 人工验收及剩余项

- 正式工程打开无编译错误；执行 Windows x64 选择菜单，核对 Mono、版本、图标、五 Scene 顺序及原全屏模式。
- 五 Scene 正常进入/退出；菜单失败提示、重试按钮的布局、颜色、字体缺字及成功后恢复；字体保持冻结，缺字时报告而不要自动生成字体。
- 通过独立测试路径运行 Play Mode Launcher，确认测试结束后普通 Play Mode 回到正式路径。
- 旧正式存档 Continue、教程门禁、教程重玩与损坏进度保护回归；不得用正式文件模拟失败。
- BGM 跨 Scene 循环、开关/音量及重启后的 PlayerPrefs 持久化。
- 完成全部美术授权、BGM 原站复核及代码 LICENSE 决策。

当前适合进入 Unity 人工验收；尚不能宣称已具备经过全部验收的 Windows Build 条件。完成上述目标选择与功能验收后可进行技术试构建；面向公开分发仍受素材/代码许可未确认阻塞。本轮未执行 Windows Build、Git Commit 或 Push。

## 修改文件及原因

| 文件 | 原因 |
|---|---|
| `docs/Phase4A_Final/HISTORY_REVIEW.md` | 本轮保护基线、验证证据与交付报告 |
| `docs/Phase4A_Final/TEST_RESULTS.md` | 本轮保护基线、验证证据与交付报告 |
| `docs/Phase4A_Final/baseline.json` | 本轮保护基线、验证证据与交付报告 |
| `docs/Phase4A_Final/icon-size-review.png` | 本轮保护基线、验证证据与交付报告 |
| `docs/Phase4A_Final/verification.json` | 本轮保护基线、验证证据与交付报告 |
| `unity/CancerTraceClient/Assets/Art/ASSET_PROVENANCE.md` | 记录全部 102 项美术素材的来源核验状态及 Unity 元数据 |
| `unity/CancerTraceClient/Assets/Art/ASSET_PROVENANCE.md.meta` | 记录全部 102 项美术素材的来源核验状态及 Unity 元数据 |
| `unity/CancerTraceClient/Assets/Audio/README.md` | 删除过时 SFX 规划，记录仅 BGM 决策 |
| `unity/CancerTraceClient/Assets/Editor/PhaseBPlayModeSmokeLauncher.cs` | Editor 测试专用路径覆盖与退出清理 |
| `unity/CancerTraceClient/Assets/Editor/WindowsReleaseSettings.cs` | 提供只选择 Windows x64、不构建的 Editor 命令 |
| `unity/CancerTraceClient/Assets/Editor/WindowsReleaseSettings.cs.meta` | 提供只选择 Windows x64、不构建的 Editor 命令 |
| `unity/CancerTraceClient/Assets/Scenes/README.md` | 记录五个正式场景顺序与教程复用方式 |
| `unity/CancerTraceClient/Assets/Scripts/Data/Save/SaveDataRepository.cs` | 原子替换不支持时安全失败，移除直接覆盖降级 |
| `unity/CancerTraceClient/Assets/Scripts/Debug/CoreGameplaySmokeTest.cs` | Editor 隔离、测试路径保护或保存失败重试覆盖 |
| `unity/CancerTraceClient/Assets/Scripts/Debug/GameDataSmokeTest.cs` | Editor 隔离、测试路径保护或保存失败重试覆盖 |
| `unity/CancerTraceClient/Assets/Scripts/Debug/PhaseBPlayModeSmokeRunner.cs` | Editor 隔离、测试路径保护或保存失败重试覆盖 |
| `unity/CancerTraceClient/Assets/Scripts/Debug/RuntimeSaveSmokeTest.cs` | Editor 隔离、测试路径保护或保存失败重试覆盖 |
| `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/GameplayService.cs` | 统一保存结果和失败状态，向菜单公开待保存状态 |
| `unity/CancerTraceClient/Assets/Scripts/Gameplay/Runtime/GameRuntimeService.cs` | 统一保存结果和失败状态，向菜单公开待保存状态 |
| `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/CancerTraceApp.cs` | Editor 测试专用路径覆盖与退出清理 |
| `unity/CancerTraceClient/Assets/Scripts/UI/GameMenu/GameMenuController.cs` | 复用现有菜单与字体，增加失败提示和当前 Session 重试 |
| `unity/CancerTraceClient/ProjectSettings/ProjectSettings.asset` | 1.0.0、Standalone Identifier、Mono 及侦探帽图标 |
| `docs/Phase4A_Final/REPORT.md` | 交付清单、限制及人工验收 |

| `README.md` | 更新实际功能、运行、操作、存档和许可状态 |
| `unity/README.md` | 替换骨架说明，记录正式工程与测试路径 |
| `THIRD_PARTY_NOTICES.md` | 保留已知许可，披露未知授权与待核验项 |
