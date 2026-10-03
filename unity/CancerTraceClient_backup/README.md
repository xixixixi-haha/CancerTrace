# CancerTraceClient

这里是 CancerTrace 的 Unity 客户端主目录。目前仅提供目录骨架，尚未由 Unity Editor 生成完整项目文件。

当前目录已经具备 Unity 项目根目录的基本边界：`Assets/`、`Packages/` 和 `ProjectSettings/`。首次正式初始化时，应在 Unity Hub 中选择此目录或以此目录为目标创建项目，不要在其内部再嵌套一层同名工程。

## 推荐初始化配置

- **Unity 版本**：Unity 6.3 LTS 的最新补丁版（`6000.3.x`）。LTS 适合需要稳定环境、功能范围明确的学生项目。团队必须统一精确版本，创建工程后在本文件记录完整版本号。
- **项目模板**：2D Core。当前项目以 2D 中文界面和轻量动画为主，不需要预先引入复杂 3D 或多人游戏模板。
- **设计分辨率**：横屏 `1920 × 1080`，最低重点检查 `1280 × 720`；主要布局按 16:9 设计，同时避免把关键内容贴在屏幕边缘。
- **Canvas 模式**：主要 UI 使用 `Screen Space - Overlay`。`Canvas Scaler` 使用 `Scale With Screen Size`，Reference Resolution 设为 `1920 × 1080`，Match 初始建议为 `0.5`。只有确实需要相机后处理或世界空间效果的界面才单独使用其他 Canvas 模式。

Unity 6.3 LTS 官方说明：<https://unity.com/blog/unity-6-3-lts-is-now-available>

## 中文字体方案

- UI 文本统一使用 TextMeshPro，不使用旧版 `Text` 组件。
- 选择授权清晰、覆盖简体中文的字体，例如 Noto Sans CJK SC / 思源黑体；字体文件和许可证说明统一放入 `Assets/Fonts/`。
- 建立主中文 TMP Font Asset，并配置必要的 fallback，覆盖中文、英文、数字、标点和常用符号。
- 开发期可使用 Dynamic SDF 便于补字；发布前检查缺字、包体大小、字号清晰度和字体授权。
- 不要用图片代替普通中文文本，以便后续修改和适配。

后续主要开发内容包括：

- 主菜单
- 病例分析界面
- 证据板
- 基因扫描
- 结果与结算页面
- Cancer Galaxy 癌症星图页面

## 后续开发规范

- 正式创建工程后，应继续保留现有资源分类和脚本模块边界。
- 通过 Git 提交 Unity 生成的 `.meta` 文件；资源移动和重命名优先在 Unity Editor 内完成。
- 不提交 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/` 等本地生成目录。
- 场景、Prefab、脚本和图片遵循 `unity/Docs/Unity_Development_Guide.md` 的命名规范。
- 数据、配置和 UI 表现分离；不要把病例、类别、概率或规则硬编码进界面。
- 每次引入新包前先确认必要性，并提交由 Unity Package Manager 生成的版本文件。
- 小步提交、及时运行场景检查；不要把大量无关资源和功能混在同一次提交中。
