# CancerTrace v1.0

CancerTrace 是一款基于癌细胞系转录组数据的中文科研调查游戏。玩家扮演调查员，通过基因线索、癌症星图和病例证据，在八类选项中作出诊断。游戏用于科普和教学。

本项目仅用于科普与教学演示，不用于真实疾病诊断或临床医疗决策。

## 玩家下载与运行

从 [GitHub Release（course-final）](https://github.com/xixixixi-haha/CancerTrace/releases/tag/course-final) 的 **Assets** 下载 Windows PC 游戏 ZIP。

1. 将 ZIP 完整解压到一个文件夹。
2. 保持 `CancerTraceClient.exe`、`CancerTraceClient_Data/` 和同级运行依赖的位置不变。
3. 双击 `CancerTraceClient.exe` 启动游戏。

目标环境为 64 位 Windows 10/11、支持 DirectX 11 的显卡、键盘和鼠标。最低 CPU、内存和显存需求尚未实机测定。

## 核心玩法

- **完成教程。** 首次正式调查前需完成教程，教程进度与正式调查存档分开。
- **调查病例。** 每个 Shift 包含 10 个病例，共享 RP。结合初始线索、Gene Scan、Cancer Galaxy、AI Assistant 和证据板缩小诊断范围。异常病例的部分信息受保护。
- **提交诊断。** 在八类诊断选项中作出判断，查看病例结果，再继续下一个病例或查看 Shift 汇总。
- **管理进度和设置。** 按 Esc 或点击右上角菜单按钮，可调整 BGM、返回主菜单或退出。出现“进度尚未保存”时可点击“重试保存”。退出前请确认重试成功，以免丢失当前进度。

Windows 本地存档位于 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/CancerTraceClient/`。正式进度为 `cancertrace_save_v1.json`，教程进度为 `cancertrace_tutorial_progress_v1.json`，BGM 设置由 Unity PlayerPrefs 保存。

## 开发者入口

在 Unity Hub 中使用 **Unity 2021.3.45f2c1** 打开 `unity/CancerTraceClient/`，加载 `Assets/Scenes/MainMenu.unity` 并进入 Play Mode。环境和测试范围见 [Unity 客户端说明](unity/README.md)。

客户端使用 Unity、C#、UGUI、TextMesh Pro 和 Newtonsoft.Json，Windows 版使用 x64 / Mono。数据分析与导出使用 Python。

| 路径 | 用途 |
| --- | --- |
| `unity/CancerTraceClient/` | 正式 Unity 工程，包含 `Assets/`、`Packages/` 和 `ProjectSettings/`。 |
| `unity/Docs/` | 游戏设计、系统规格和客户端开发资料。 |
| `data/`、`python/` | 数据来源说明与分析、导出流程。 |
| `results/game_data/` | 冻结游戏数据，运行副本位于正式工程的 `Assets/StreamingAssets/GameData/`。 |
| `docs/` | 项目文档索引和 Phase 4A 历史验收记录。 |
| `unity/CancerTraceClient_backup/` | 历史工程副本。 |

## 数据来源与素材授权

游戏数据源自 **Broad Institute DepMap Public 26Q1**。版本证据、输入文件和引用见 [数据来源](data/README.md)，冻结数据范围见 [游戏数据说明](results/game_data/README.md)。

AI 生成图片、第三方字体、BGM 和软件依赖的说明统一见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。仓库尚未声明统一的代码许可证。
