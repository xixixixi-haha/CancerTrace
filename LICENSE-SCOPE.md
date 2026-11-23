# 授权范围 / License Scope

[MIT 许可证](LICENSE)仅适用于下方逐一列出的 CancerTrace 原创 Python 和 Unity C# 源文件。路径均相对于仓库根目录。后续新增文件须经明确列入方可纳入本授权。

## Python 源代码（9 个文件）

- `python/src/01_inspect_cancer_lineages.py`
- `python/src/02_prepare_classification_dataset.py`
- `python/src/03_split_train_test.py`
- `python/src/04_baseline_classification.py`
- `python/src/05_generate_cancer_galaxy.py`
- `python/src/06_evaluate_on_test_set.py`
- `python/src/07_1_rebalance_gene_clue_system.py`
- `python/src/07_build_gene_clue_system.py`
- `python/src/08_export_game_data.py`

## Unity C# 编辑器工具（14 个文件）

- `unity/CancerTraceClient/Assets/Editor/CaseAnalysisVisualBuilder.cs`
- `unity/CancerTraceClient/Assets/Editor/GameMenuBgmPrefabUpdater.cs`
- `unity/CancerTraceClient/Assets/Editor/GameMenuPrefabBuilder.cs`
- `unity/CancerTraceClient/Assets/Editor/MainMenuVisualBuilder.cs`
- `unity/CancerTraceClient/Assets/Editor/MainMenuVisualValidation.cs`
- `unity/CancerTraceClient/Assets/Editor/PhaseB5CameraValidation.cs`
- `unity/CancerTraceClient/Assets/Editor/PhaseBIntegrationSmokeTest.cs`
- `unity/CancerTraceClient/Assets/Editor/PhaseBPlayModeSmokeLauncher.cs`
- `unity/CancerTraceClient/Assets/Editor/PhaseBSceneBuilder.cs`
- `unity/CancerTraceClient/Assets/Editor/ResultSummaryUiValidation.cs`
- `unity/CancerTraceClient/Assets/Editor/ShiftSummarySceneRebuilder.cs`
- `unity/CancerTraceClient/Assets/Editor/SourceHanSansTmpFontSetup.cs`
- `unity/CancerTraceClient/Assets/Editor/TutorialGuideInstaller.cs`
- `unity/CancerTraceClient/Assets/Editor/WindowsReleaseSettings.cs`

## Unity C# 客户端脚本（35 个文件）

- `unity/CancerTraceClient/Assets/Scripts/Data/GameDataContext.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/GameDataLoader.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Models/GameDataModels.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Repositories/GameDataRepositories.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Save/SaveDataModels.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Save/SaveDataRepository.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Save/TutorialProgressDataV1.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Save/TutorialProgressRepository.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Validation/DataValidationResult.cs`
- `unity/CancerTraceClient/Assets/Scripts/Data/Validation/DataValidator.cs`
- `unity/CancerTraceClient/Assets/Scripts/Debug/CoreGameplaySmokeTest.cs`
- `unity/CancerTraceClient/Assets/Scripts/Debug/GameDataSmokeTest.cs`
- `unity/CancerTraceClient/Assets/Scripts/Debug/PhaseBPlayModeSmokeRunner.cs`
- `unity/CancerTraceClient/Assets/Scripts/Debug/RuntimeSaveSmokeTest.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/GameplayActionResult.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/GameplayService.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/GameplayViewFactory.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/GameplayViews.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/TutorialService.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Flow/TutorialViews.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Runtime/GameRuntimeService.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Runtime/GameRuntimeState.cs`
- `unity/CancerTraceClient/Assets/Scripts/Gameplay/Runtime/TutorialSessionState.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/CancerGalaxyController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/CancerTraceApp.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/CancerTraceUiFactory.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/CaseAnalysisController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/MainMenuController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/ResultSummaryController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Controllers/ShiftSummaryController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/GameMenu/GameMenuBootstrap.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/GameMenu/GameMenuController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/GameMenu/GameMenuPreferences.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Tutorial/TutorialGuideController.cs`
- `unity/CancerTraceClient/Assets/Scripts/UI/Tutorial/TutorialStepData.cs`

## 排除项

- 95 项通过 ChatGPT / OpenAI 图像生成功能生成的美术素材。
- 音乐、字体及其派生资源。
- 外部数据集及游戏数据，包括冻结的游戏数据与运行副本。
- Unity 引擎、运行时及第三方软件依赖。
- 第三方图片、插件及其他素材。
- 以上清单之外的所有文件，包括文档、Unity 场景、预制件、项目设置、元数据和构建产物。

第三方内容及其原有授权说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
