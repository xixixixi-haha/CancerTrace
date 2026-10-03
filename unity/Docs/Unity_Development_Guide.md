# Unity 开发规范

本文档用于统一 CancerTrace Unity 客户端的资源命名、中文界面和 Git 协作方式。当前阶段只建立规范，不代表已经开始实现游戏功能。

## 1. 场景命名规范

- 场景文件使用英文 PascalCase，扩展名为 `.unity`。
- 名称应表达单一主要职责，不使用空格、中文文件名或 `Scene1` 等临时名称。
- 计划场景示例：`MainMenu.unity`、`CaseAnalysis.unity`、`CancerGalaxy.unity`、`ResultSummary.unity`、`Tutorial.unity`。
- 临时测试场景放在明确的开发目录并标注用途，合并前删除或改为正式名称。
- Build Settings 中的场景顺序和启用状态发生变化时，应在提交说明中注明。

## 2. Prefab 命名规范

- Prefab 使用“类型前缀 + PascalCase 名称”，例如：
  - `UI_CasePanel.prefab`
  - `UI_PrimaryButton.prefab`
  - `Char_Detective.prefab`
  - `Char_CatAssistant.prefab`
  - `Cell_DisplayCard.prefab`
  - `FX_GeneScan.prefab`
- 同类 Prefab 放入对应的 `Assets/Prefabs/` 子目录。
- Prefab 保持职责单一；通用组件优先复用，不复制出大量只有微小差异的版本。
- Variant 名称应说明差异，例如 `UI_PrimaryButton_Disabled.prefab`，避免 `New`、`Final`、`Final2`。

## 3. Script 命名规范

- C# 文件和其中的主要类型使用 PascalCase，且文件名必须与类型名一致，例如 `GameFlowController.cs`。
- 接口使用 `I` 前缀，例如 `IGameDataProvider`；枚举和 ScriptableObject 类型也使用清晰的 PascalCase 名称。
- 私有字段采用 `_camelCase`，公开属性和方法采用 PascalCase，局部变量采用 camelCase。
- 避免 `Manager` 包揽所有职责；按 `Core`、`Data`、`UI`、`Gameplay`、`Debug` 分层。
- 不在 UI 脚本中硬编码病例、概率、类别、分数或 RP 规则。
- Editor/Debug 辅助代码不得混入发布逻辑，后续应使用独立目录或程序集边界。

## 4. 图片资源命名规范

- 图片使用小写英文、下划线分隔和用途前缀，不使用空格或中文文件名。
- 推荐前缀：
  - `bg_*`：背景，例如 `bg_case_room.png`
  - `ui_btn_*`：按钮，例如 `ui_btn_primary.png`
  - `ui_panel_*`：面板，例如 `ui_panel_evidence.png`
  - `icon_*`：图标，例如 `icon_cancer_lung.png`
  - `char_*`：角色，例如 `char_detective_thinking.png`
  - `cell_*`：细胞，例如 `cell_reference_skin.png`
  - `fx_*`：效果，例如 `fx_scan_glow.png`
- 同一组件的状态以结尾区分：`_normal`、`_hover`、`_pressed`、`_disabled`。
- 导入前确认尺寸、透明通道、压缩需求、九宫格边界和来源授权。

## 5. 中文 UI 规范

- 默认语言为简体中文，术语在所有场景中保持一致。
- 普通 UI 文本统一使用 TextMeshPro 和项目中文字体资产，禁止随意混用系统字体。
- 标题、按钮、正文、注释建立固定字号层级；同级元素不得随意改变字号和字重。
- 按钮文案尽量短且使用明确动词，例如“开始调查”“提交诊断”。
- 面板应为中文文本预留足够宽度，避免强制压缩、遮挡和不自然换行。
- 使用全角中文标点；中英文、数字和单位混排时保持空格与格式一致。
- 重要信息不能只依赖颜色表达，还应配合图标、标签或文字。
- 每个目标分辨率检查字体清晰度、缺字、换行、溢出和按钮点击区域。
- 界面风格保持可爱、手账、侦探、温暖、轻松，避免冰冷科技风和过度 AI 化视觉。

## 6. Git 提交规范

- 一次提交只处理一个清晰主题，提交前检查场景、Prefab 和 Console 错误。
- 推荐提交前缀：
  - `feat(unity):` 新增客户端功能或资源
  - `fix(unity):` 修复 Unity 问题
  - `docs(unity):` 更新客户端文档
  - `chore(unity):` 工程设置、包或维护工作
- 提交信息使用简洁的祈使语句，例如：`docs(unity): add project initialization guide`。
- 资源文件必须与对应 `.meta` 一起提交；不要手工删除或重新生成已有 `.meta`。
- 不提交 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/` 和本地构建输出。
- 不提交无关格式化、个人编辑器设置或未经确认的大体积素材。
- 场景和 Prefab 容易产生合并冲突；开始编辑前先同步分支，避免多人同时修改同一文件。
- 正式升级 Unity Editor 或 Package 版本必须单独提交，并在提交说明中记录原因与验证结果。
