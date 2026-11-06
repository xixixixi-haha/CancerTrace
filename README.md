# CancerTrace

CancerTrace 是以癌细胞转录组数据为线索的中文科研调查游戏。玩家结合初始线索、Gene Scan、Cancer Galaxy 与 AI Assistant 作出诊断，再查看病例结果和 Shift 汇总。

## 当前功能

- 首次正式调查前完成教程；教程与正式调查状态分离。
- 每个 Shift 包含 10 个病例，共享 RP；诊断后进入结果页并继续下一病例。
- 八类诊断选项、星图调查、证据展示、异常病例信息保护。
- 本地存档、继续调查、损坏存档保护、设置菜单与跨场景循环 BGM。只使用 BGM，不添加 SFX。

## 截图

现有界面参考图见 [unity/Docs/References](unity/Docs/References)。这些是设计参考，尚无经过 v1.0 验收的正式游戏截图；发布前在此补充实机截图。

## 运行

使用 Unity Hub 打开 `unity/CancerTraceClient`，选择 **Unity 2021.3.45f2c1**。从 `Assets/Scenes/MainMenu.unity` 进入 Play Mode。已完成 Windows x64 + Mono 首次试构建，输出目录为 `D:/Builds/CancerTrace/1.0.0-Windows-x64`。项目所有者已确认现有 EXE 通过窗口模式运行验收，1920×1080 下显示正常；该验收不代表非 16:9 原生分辨率全屏显示已通过，也不代表素材授权已完成。运行时保持 exe、同名 Data 目录和运行依赖在一起，启动 `CancerTraceClient.exe`。

Windows 目标：64 位 Windows 10/11、支持 DirectX 11 的显卡、键盘和鼠标。最低 CPU、内存与显存需求尚未实机测定，不作性能保证。

## 操作与流程

鼠标点击菜单与调查工具，选择诊断后提交；结果页继续下一个病例，完成 Shift 后查看汇总。教程入口提供引导，首次正式调查受教程完成状态保护。Esc 或右上角菜单按钮打开/关闭设置，调整 BGM 开关与音量，可返回主菜单或退出。保存失败时菜单显示“进度尚未保存”，点击“重试保存”保存当前内存进度；保存失败期间退出进程仍会丢失未写入进度。

## 存档

Windows 默认路径：`%USERPROFILE%/AppData/LocalLow/DefaultCompany/CancerTraceClient/`。
正式进度 `cancertrace_save_v1.json`，教程进度 `cancertrace_tutorial_progress_v1.json`。BGM 设置使用 Unity PlayerPrefs。保留 Company/Product 以维持原存档目录，勿手动覆盖存档。现有四个 Smoke 测试脚本仅在 Editor 编译，使用独立临时目录；临时显示诊断脚本已移除。

## 技术与目录

- Unity 2021.3、C#、UGUI、TextMesh Pro、Newtonsoft.Json；Windows x64 / Mono。
- `unity/CancerTraceClient/`：正式客户端；`unity/Docs/`：设计与开发记录。
- `data/`、`python/`：科学数据与 Python 分析；`results/game_data/`：冻结游戏数据，客户端副本位于 StreamingAssets/GameData。
- `docs/Phase4A_Final/`：本轮保护基线、验证与发布前验收报告。
- `unity/CancerTraceClient_backup/`：历史副本，不是运行入口。

## 许可与素材

仓库尚未选择统一代码许可证，不能据此宣称开源授权已完成。第三方字体、音乐及待核验美术见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。美术来源未确认前，不应作为已获授权的素材发布。
