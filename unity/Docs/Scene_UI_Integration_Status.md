# CancerTrace Scene/UI Integration Status

## Phase B Completion — Two-Case Playable Loop (2026-10-05)

Phase B is implemented and verified. The playable path is:

`MainMenu -> CaseAnalysis -> CancerGalaxy -> CaseAnalysis -> ResultSummary -> CaseAnalysis (Case 2)`

### Formal scenes and Build Settings

| Build index | Scene | Status |
|---|---|---|
| 0 | `Assets/Scenes/MainMenu.unity` | Created; New Shift is active, Continue/Tutorial are visibly disabled |
| 1 | `Assets/Scenes/CaseAnalysis.unity` | Created; Case header, tools, evidence, fixed diagnosis choices and submit are active |
| 2 | `Assets/Scenes/CancerGalaxy.unity` | Created; 645 reference nodes, current Case node and five nearby references are rendered |
| 3 | `Assets/Scenes/ResultSummary.unity` | Created; result, AI review, optional ANOMALY label and Next Case are active |

`SampleScene.unity` remains on disk and has been removed from the formal Build Settings list. Tutorial and test scenes are not included.

### Runtime/UI integration

- `CancerTraceApp` creates one persistent, initialized `GameplayService` dependency graph and retains it across scene navigation.
- `MainMenuController` calls `GameplayService.StartNewShift()`; it does not create a Case or Runtime State itself.
- `CaseAnalysisController` consumes `PlayerCaseView` and `EvidenceBoardView`, calls the three formal tool APIs, renders the eight `CancerTypeOptionView` choices, and submits through `GameplayService.SubmitDiagnosis()`.
- `CancerGalaxyController` consumes the already-unlocked safe `CancerGalaxyView`. It never calls the paid action again on entry and never derives a diagnosis from distance.
- `ResultSummaryController` consumes `ResultView` only after submission and advances with `GameplayService.NextCase()`.
- `PlayerCaseView` now includes the safe `ShiftNumber` scalar so UI can show Shift without reading mutable Runtime State.
- Tool cost, enabled/used state, RP, Score, Case sequence, result correctness, and progression remain owned by Gameplay/Runtime code.

### UI behavior

- CaseAnalysis always shows exactly two Initial Clues before tools.
- Gene Scan success exposes exactly three formal clues and refreshes the Evidence Board.
- Cancer Galaxy success spends RP before scene navigation; Back returns to the same Case with tool/RP state intact.
- AI Assistant shows only Prediction, Confidence, and Top 3 Candidates.
- Evidence Board includes only unlocked Initial, Gene Scan, Galaxy, and AI evidence.
- Diagnosis is restricted to the eight formal `classId` / `labelZh` options; free submit remains available without buying tools.
- ResultSummary shows player/true diagnosis, correctness, score earned, Shift Score, RP, and AI review even when AI was not purchased.
- `AnomalyDisplayLabel` is rendered only when post-submit `ResultView.IsAnomaly` is true.

### Existing art used

- Backgrounds: `bg_mainmenu.png.png`, `SharedBackground.png`.
- Panels: case info, evidence board, notebook, and dialog panels.
- Buttons: Gene Scan, observe, hint, submit, back, and next normal-state sprites.
- Characters: detective idle and success sprites.
- Missing Phase B-specific art is handled with simple native Unity UI, especially Galaxy nodes, disabled button states, diagnosis cards, and result decoration.

### Font

- Every serialized formal-scene text component is `TMP_Text` / `TextMeshProUGUI`.
- All 45 serialized text components reference `Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset`.
- No legacy `UnityEngine.UI.Text`, missing font reference, or Windows system font was introduced.

### Verification results

- Unity version: 2021.3.45f2c1.
- Compilation: 0 C# errors, 0 C# warnings.
- Formal scene validation: four scenes open successfully; no Missing Script, missing controller, legacy Text, or missing formal TMP font reference.
- Batch UI integration smoke: passed with Unity return code 0.
- Automated Play Mode button/scene run: passed from MainMenu through Case 1 tools/Galaxy/Back/AI/Diagnosis/Result/Next into Case 2.
- RP observed in the formal tool path: `200 -> 190 -> 165 -> 125`.
- Correct-diagnosis integration path awards 100 Score; Case 2 retains RP 125 and the current Score, while all Case 2 tool flags reset.
- Case 2 receives a different Case ID, exactly two Initial Clues, and preserves the original `selectedCaseIds` order.
- Save/restore round trip preserves the Case ID, RP, tool state, Score, evidence unlocks, and `selectedCaseIds`; Play Mode used an isolated temporary save and removed it after the test.
- Existing Core Gameplay smoke test also passed the full 10-Case Shift, zero-RP submit, mid-Shift save recovery, ANOMALY pre-submit leak guard, and post-submit ANOMALY reveal.
- Formal JSON, Python, and the v1.0 GDD core rules were not modified.

### Remaining non-blocking assets

- Dedicated CaseAnalysis, CancerGalaxy, and ResultSummary backgrounds.
- Galaxy-specific node, legend, trail, scan, and tooltip artwork.
- Button hover/pressed/disabled variants.
- AI/Diagnosis/Result-specific panels, banners, icons, and decorative polish.

### Phase C backlog (not started)

- Implement and validate the formal Continue flow and save-selection/error UX.
- Implement Tutorial only when Phase C explicitly authorizes it.
- Add final visual refinement, responsive/layout polish, dedicated UI art, animation, and audio.
- Add Shift-complete presentation/summary navigation and final build QA when in scope.

### CaseAnalysis UI/interaction design confirmation (2026-10-05; design only)

The following confirmed CaseAnalysis visual revision is compatible with the frozen v1.0 rules and the existing `GameplayService` / safe view-model contract. This is a UI-layer follow-up; this confirmation makes no gameplay, scene, prefab, JSON, or code change.

- Use a three-column CaseAnalysis layout: left **调查工具**, centre **Evidence Board**, right **最终诊断**. Keep Shift, Case, Case ID, Remaining RP, and Score in the top header.
- Remove the separate large Initial Clues presentation panel. The two free Initial Clues remain unchanged and are shown directly in the Evidence Board at case start; this is only a presentation relocation.
- The left column contains only the three frozen paid tools: 扫描 / Gene Scan / 10 RP, 观察 / Cancer Galaxy / 25 RP, and 提示 / AI Assistant / 40 RP.
- The Evidence Board initially renders the two Initial Clues plus locked-state entries for Gene Scan, Galaxy evidence, and AI assistance. It progressively fills with the three Gene Scan clues, a Galaxy evidence card, and AI output after their respective successful first uses.
- Gene Scan remains a once-per-Case 10 RP action that unlocks exactly the formal three extra clues, for a maximum of `2 Initial + 3 Gene Scan` clues.
- Cancer Galaxy remains a once-per-Case 25 RP action. Its first successful use unlocks Galaxy evidence and opens `CancerGalaxy`; the board stores only a compact evidence card, not the full star map. Before unlock, the card reads “星图证据 / 待解锁”; after unlock, it reads “星图证据 / 已解锁 / 点击查看”.
- **Required UI-layer behavior:** an unlocked Galaxy evidence card must navigate directly to `CancerGalaxy` for repeat viewing, without calling `GameplayService.UseCancerGalaxy()` again. `UseCancerGalaxy()` correctly returns `AlreadyUsed` after the first purchase; `GetEvidenceBoard().GalaxyEvidence` already supplies the unlocked safe data, and `CancerGalaxyController` already renders from that evidence. No core Gameplay change is required.
- AI Assistant remains a once-per-Case 40 RP action. Its Evidence Board card may show only Prediction, Confidence, and Top 3 Candidates. It must not render `ai.correct`, true class, ANOMALY, an explanation, or the complete eight-class probability distribution.
- The diagnosis column retains the fixed eight `CancerType` choices. Players may select and submit without purchasing any tool; Submit stays free, including at 0 RP.
- Pre-submit rendering must continue to use only `PlayerCaseView`, `EvidenceBoardView`, and successful paid-tool views. These do not expose true class, ANOMALY, or `ai.correct`; locked evidence must not be derived from static data or result views.

Compatibility finding: no core-rule or `GameplayService` change is required. The current CaseAnalysis controller/layout will need a later UI-only visual/controller revision because it still renders a separate Initial Clues panel and has no clickable Galaxy evidence-card navigation; that later revision must preserve the service-call boundary above.

### CaseAnalysis semi-static background integration (2026-10-05)

- `bg_caseanalysis.png` is the only full-screen CaseAnalysis background Image. It uses full-stretch anchors, zero offsets, preserved aspect ratio, and disabled raycast.
- Removed duplicate Unity-rendered paper/corkboard panels, title signs, card backings and decorations, diagnosis labels/icons/card fills, status-bar paper/icons, tool-button art/cost labels, and Submit art. All fixed presentation now comes from the frozen background.
- Five top status TMP overlays remain bound to the safe `PlayerCaseView`: Shift, Case, Case ID, Remaining RP, and Score. Each overlay renders its short label together with the live value; no sample value is hard-coded.
- The two Initial Clue cards contain dynamic TMP content only: Gene, HIGH/LOW with directional arrow, and Expression value from the safe view model. The three lower cards retain only the Chinese static placeholders “扫描证据 / 待解锁”, “星图证据 / 待解锁”, and “AI辅助 / 待解锁”; dynamic unlock replacement remains deferred.
- Gene Scan, Cancer Galaxy, and AI Assistant use transparent Button hit areas aligned over the three background buttons. Their existing Controller listeners and Gameplay calls are unchanged; Unity does not redraw the baked button art or cost text.
- Diagnosis uses eight transparent Button hit areas in the background's fixed 2-by-4 order (`lung`, `skin`, `cns_brain`, `bowel`, `esophagus_stomach`, `breast`, `bone`, `ovary_fallopian_tube`). A separate small `✓` TMP object supplies lightweight selection feedback without altering the background. Submit is a transparent Button hit area over the baked Submit control; its existing submit event is unchanged.
- Final 1920x1080 anchors: Tools `(.04,.14)-(.255,.57)`, Evidence Board `(.285,.18)-(.690,.78)`, and Diagnosis `(.710,.11)-(.985,.77)`. Initial clue text overlays are local `(.115,.515)-(.420,.700)` and `(.535,.515)-(.855,.700)` inside Evidence Board. The three lower evidence slots are local x ranges `.030-.315`, `.345-.655`, `.675-.975`, each with y `.045-.390`.
- Targeted Unity batch rebuild and single scene validation passed: scene opened, 19 formal-font TMP texts, 12 Buttons, all Controller references, no missing script/reference, no duplicate sprite layer, and 0 C# compilation errors. No full Two-Case smoke test was run.

### ResultSummary single-paper refactor (2026-10-05)

- ResultSummary now uses the existing large background paper as its sole visible information surface. The prior foreground dialog-paper layer is transparent and no longer renders a second paper.
- Removed the character decoration and the ANOMALY/red-X presentation from the scene and controller. The page contains only the result title/verdict, a left diagnosis and score column, a right AI teaching-review column, and the single Next button.
- The left column renders the player diagnosis, true diagnosis, earned score, current Shift score, and remaining RP. The right column renders only the safe post-submit AI review: Prediction, Confidence, and Top 3 Candidates.
- Added `CancerTrace/Phase B/Rebuild Result Summary UI` as a narrow editor action for rebuilding only this scene without touching the current CaseAnalysis layout.

## 后续正式开发规则：Scene 是视觉真值（2026-10-07 起）

项目已从自动搭建 Scene 骨架阶段进入正式 UI 定稿与增量开发阶段。以下规则覆盖后续日常开发方式；现有历史记录继续保留作为实现背景。

### 统一工作流

- `MainMenu.unity`、`CaseAnalysis.unity`、`CancerGalaxy.unity`、`ResultSummary.unity` 当前保存内容是正式视觉真值。
- 后续默认在现有 Scene 中原地、局部、增量修改。人工保存的 RectTransform、Anchor、Pivot、Size、Font Size、Line Spacing、Alpha、Image/Button 尺寸与位置等参数优先于 Builder 中的旧参数。
- 禁止默认运行 Scene Builder、Rebuild Scene、全量重新生成 UI，或以旧 Builder 参数覆盖当前 Scene。不得因新增单个功能而重建整个 Scene。
- 小型视觉微调由用户在 Unity Editor 中完成；Codex主要负责数据绑定、Controller/View Model、状态显隐、按钮事件、Scene 跳转、Tutorial 状态机、Shift/Save/Runtime 接入、新功能对象、复杂局部布局与 Bug 修复。
- 中大型 UI 修改也必须限定在指定区域，不得顺带重建其他区域或整个 Scene。
- Builder 保留用于新 Scene 初始骨架、灾难恢复、Scene 损坏恢复，或用户明确要求的全量重构。新 Scene 骨架完成后，同样切换为 Scene 视觉真值。
- 如果确实必须运行现有 Scene Builder，Codex必须先停止修改，说明原因、目标 Scene、会覆盖的 GameObject，以及 RectTransform、Alpha、Font、Image 等参数影响，并等待用户明确确认。只有用户明确写出“允许重建Scene”后才能执行。

### 当前正式 Scene 状态

#### MainMenu

- 已有正式视觉基础。
- Continue 功能与 Tutorial 入口功能仍可增量开发。
- 不得为了新增功能整体重建 MainMenu。

#### CaseAnalysis

- Initial Clues、Gene Scan、Cancer Galaxy、AI Assistant、Diagnosis、Submit、Evidence Board 和 Galaxy 免费重复查看等核心功能基本完成。
- 正式背景为 `Assets/Art/Background/bg_caseanalysis.png`；当前 Scene 已包含大量人工与正式布局参数。
- 后续允许最终人工视觉微调；Tutorial 可在现有 Scene 上新增 Guide Layer / Overlay。
- 不允许整体 Rebuild。

#### CancerGalaxy

- 核心数据逻辑已存在。
- 标题、Current Case Node、Remaining RP、五条 nearbyReferences、字号、对齐与行距仍需单独收口。
- 后续必须基于当前 `CancerGalaxy.unity` 原地修改，不得全量重建。

#### ResultSummary

- 当前已使用 `ui_title_result_summary.png`、`ui_result_correct.png`、`ui_result_wrong.png`，并已在 Unity Editor 中完成人工视觉调整。
- 当前 `ResultSummary.unity` 布局是人工视觉真值。默认不得运行 ResultSummary Builder、重建 Scene，或覆盖人工 RectTransform、字号、透明度及素材尺寸/位置。
- 后续可增量修改 `ResultSummaryController`、数据绑定、ANOMALY 提示、Next 流程、ShiftSummary 接入和 Tutorial 复用，但必须保留现有 Scene 视觉参数。

### 尚未完成

1. CancerGalaxy UI 收口。
2. ShiftSummary：One Shift = 10 Cases；第 10 个 Case 完成后进入；当前尚未正式实现或定稿。
3. Tutorial：MainMenu 已有入口，但尚未正式实现；教程免费且不计入正式 Shift。后续优先复用 CaseAnalysis、CancerGalaxy、ResultSummary，并通过 Guide Layer / Overlay 教学，不为 Tutorial 重建正式 Scene。
4. 全局最终视觉 Polish：在功能与正式流程稳定后统一进行人工微调。

### 推荐后续顺序

1. 逐个确定现有正式 Scene 视觉。
2. 修复明显的中大型 UI 问题，小视觉问题由用户人工调整。
3. 完成 CancerGalaxy UI。
4. 完成 ShiftSummary。
5. 完成 Tutorial。
6. 执行完整正式流程测试。
7. 最终全局视觉 Polish 与 Build QA。

后续所有 Codex 任务默认遵守：**现有正式 Scene 原地增量开发；禁止完整重建；人工保存的视觉参数优先于旧 Builder 参数。**

The sections below are the retained Phase A audit baseline from 2026-10-04. They are historical; the Phase B completion record above is the current integration state.

## 1. Snapshot

- Audit date/phase: 2026-10-04, Phase A Read-only Integration Audit.
- Rules: CancerTrace v1.0 Rules Frozen.
- Unity version: 2021.3.45f2c1.
- Scope inspected: Scenes, Scripts, Art, Fonts, TextMesh Pro, Packages, and ProjectSettings only. No implementation, scene, build-setting, data, or asset changes were made by this audit.
- Git worktree was clean before this document was created. There were no pre-existing uncommitted files, scene changes, or Build Settings changes.

## 2. Confirmed Architecture

- `GameplayService` is the UI-facing flow facade. It returns `GameplayActionResult<T>` and safe view models rather than static JSON models.
- View models are defined in `Assets/Scripts/Gameplay/Flow/GameplayViews.cs`.
- `GameRuntimeService` owns mutable `GameRuntimeState`, tool state, selected diagnosis, shift progression, automatic saves, and restore.
- `SaveDataRepository` owns SaveData v1 serialization, validation, load, delete, and local-file persistence.
- The data layer uses repositories for cases, galaxy nodes, class profiles, and configuration. UI should not read formal JSON directly.

## 3. Current Scenes

| Scene | Path | Status | Notes |
|---|---|---|---|
| SampleScene | `Assets/Scenes/SampleScene.unity` | Empty | Default scene containing only Main Camera; it is not in `Scene_Plan.md`. |
| MainMenu | Planned only | Missing | No `.unity` file exists. |
| CaseAnalysis | Planned only | Missing | No `.unity` file exists. |
| CancerGalaxy | Planned only | Missing | No `.unity` file exists. |
| ResultSummary | Planned only | Missing | No `.unity` file exists. |
| Tutorial | Planned only | Missing | No `.unity` file exists. |

`Assets/Scenes/README.md` describes the five planned formal scenes and confirms that no formal Unity scene files have yet been created.

## 4. Build Settings

`ProjectSettings/EditorBuildSettings.asset` has one enabled scene, in order:

1. `Assets/Scenes/SampleScene.unity`

`SampleScene` is still present and is the only build scene. All five planned formal scenes are absent. No Build Settings modification was made.

## 5. Gameplay API for UI

All flow calls below are public `GameplayService` methods and return `GameplayActionResult<T>` unless otherwise noted.

| UI need | Actual API | Returned or effected view |
|---|---|---|
| Start Shift | `StartNewShift()` | First `PlayerCaseView` |
| Continue saved Shift | `RestoreSavedShift()` | Restored `PlayerCaseView`; uses runtime restore |
| Read active Case | `GetCurrentCase()` | `PlayerCaseView` |
| Diagnosis options | `GetDiagnosisOptions()` | Eight `CancerTypeOptionView` values |
| Select diagnosis | `SelectDiagnosis(string classId)` | Updated `PlayerCaseView` |
| Gene Scan | `UseGeneScan()` | `GeneScanView` |
| Cancer Galaxy | `UseCancerGalaxy()` | `CancerGalaxyView` |
| AI Assistant | `UseAiAssistant()` | `AiAssistantView` |
| Evidence Board | `GetEvidenceBoard()` | `EvidenceBoardView` |
| Submit diagnosis | `SubmitDiagnosis(string classId)` | `ResultView` |
| Read result | `GetResult()` | `ResultView`, only after submission |
| Next Case | `NextCase()` | Next `PlayerCaseView`, or `ShiftCompleted` status |
| Shift Summary | `GetShiftSummary()` | `ShiftSummaryView`, only after Case 10 |
| Next Shift | `StartNextShift()` | First `PlayerCaseView` of next completed-cycle shift |

UI must handle `GameplayActionStatus`: `Success`, `InsufficientRp`, `AlreadyUsed`, `AlreadySubmitted`, `InvalidDiagnosis`, `ShiftCompleted`, and `InvalidState`.

## 6. Safe View Models

### Safe before submission

`PlayerCaseView` provides Case ID, cell-line name, Case index/count, remaining RP, current Score, exactly the initial clues, tool availability/cost/used state, temporary selected diagnosis, and submission/shift flags. It does not provide true class, difficulty, ANOMALY, `ai.correct`, or AI output.

After a successful paid tool call, the UI may use:

- `GeneScanView`: three additional clues and remaining RP.
- `CancerGalaxyView`: current Case coordinates, reference-node coordinates/classes, exactly five nearby references, and remaining RP. It has no recommended diagnosis.
- `AiAssistantView`: prediction, confidence, top candidates, and remaining RP. It has no `ai.correct` field.
- `EvidenceBoardView`: initial clues plus only tools already unlocked in this Case. It does not backfill unpaid AI results.

### Available only after submission

`ResultView` provides player diagnosis, true diagnosis and Chinese/English labels, correctness, score, RP, AI review, and the ANOMALY flag/display label. `GetResult()` refuses access before diagnosis submission.

`ShiftSummaryView` provides completed/correct counts, accuracy, Score, RP, and tool-use counts only after the completed 10-Case Shift.

Chinese labels are available in diagnosis, galaxy, AI, and result view models via `LabelZh` / `PredictionLabelZh` / `TrueDiagnosisLabelZh`; RP, Score, Gene Scan, Cancer Galaxy, AI Assistant, Result, and Shift Summary labels can therefore be assembled without reading static data from UI.

## 7. Existing UI Controllers

- `Assets/Scripts/UI/Controllers/` contains only its `.meta` file; no UI controller C# source exists.
- There are no reusable or partially implemented UI controllers in the inspected Scripts scope.
- No C# script in the inspected Scripts scope references `UnityEngine.UI.Text`, `Text`, `TMP_Text`, or `TextMeshPro`; there is consequently no UGUI Text temporary implementation in source.
- `Assets/Editor/SourceHanSansTmpFontSetup.cs` remains as a one-time editor menu utility that creates/verifies the formal TMP font asset. It is an Editor script, not a runtime UI controller; it refuses to overwrite an existing asset. Treat it as an unfinished/temporary setup artifact for later cleanup review, not as a Phase B dependency.

## 8. Font

| Item | Status |
|---|---|
| Formal TMP Font Asset | Present: `Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset` |
| Source font | Present: `Assets/Fonts/SourceHanSansSC-Regular.otf` |
| TMP Essential Resources | Present under `Assets/TextMesh Pro/Resources/` |
| Atlas Population Mode | Dynamic (`m_AtlasPopulationMode: 1`) |
| Multi Atlas Textures | Enabled (`m_IsMultiAtlasTexturesEnabled: 1`) |

The retained editor verification script explicitly tests Chinese, English, numerals, punctuation, Dynamic population, and Multi Atlas support. Formal Phase B UI should use `TMP_Text` with this formal font asset; do not introduce UGUI `Text` or Windows system fonts.

## 9. Existing Art Assets

| Directory | Existing assets / intended usable scope |
|---|---|
| `Assets/Art/Background/` | `SharedBackground.png`, `bg_mainmenu.png.png`; usable for MainMenu/shared presentation. No dedicated CaseAnalysis, Galaxy, Result, or Tutorial background is present. |
| `Assets/Art/UI/Buttons/` | Back, close, gene scan, hint, next, observe, and submit normal-state PNGs. No hover/pressed/disabled variants were found. |
| `Assets/Art/UI/Panels/` | Case info, dialog, evidence board, gene scan, initial clues, notebook, and sticky-note panels; usable in CaseAnalysis and modal presentation. |
| `Assets/Art/UI/TitleBanners/` | Case archive, evidence board, gene scan, initial clues, and research-points banners; usable in CaseAnalysis/Evidence Board. |
| `Assets/Art/Icons/Common/` | DNA, microscope, magnifier, detective, cell, question, star, sparkle, evidence decorations, and related common icons. |
| `Assets/Art/Icons/CancerTypes/` | All eight CancerType icons: lung, breast, skin, CNS/brain, bone, ovary/fallopian, esophagus/stomach, bowel. |
| `Assets/Art/Character/` | Detective idle/thinking/discovery/success/confused and cat assistant idle/hint/happy/surprised/sleep variants. |

## 10. Missing Assets

### Blocking

None for a functional Phase B two-Case playable loop. Existing data, Gameplay API, TMP font, common UI assets, and basic button/panel assets are sufficient to build the loop with standard Unity UI components.

### Non-blocking

- Dedicated backgrounds for CaseAnalysis, CancerGalaxy, ResultSummary, and Tutorial.
- Cancer Galaxy-specific star-map background, node sprites/visual treatment, legend, star trail/line/scan decorations, and case-node visual.
- Button hover, pressed, and disabled-state artwork.
- AI Assistant-specific panel/banner, diagnosis selection/confirmation/result popup artwork, RP/Score/usage panel, Tutorial arrows/highlight/stickers/overlay.
- Result/Shift Summary-specific backgrounds and presentation art.
- Detective failure artwork requested by the checklist (the available detective set has confused rather than a file explicitly named failure).
- Dedicated cell illustration/card asset for the case presentation.

## 11. Known Integration Risks

- There are no formal scenes, UI controllers, or scene composition yet; Phase B begins from an empty scene/UI layer rather than extending a partial integration.
- Build Settings still opens `SampleScene`; switching formal flow into the build is required later, but is intentionally outside Phase A.
- UI composition must establish and retain a single initialized `GameplayService` / runtime-data dependency graph across scene navigation. `GameplayService` is not a Unity `MonoBehaviour` and has constructor dependencies.
- UI must only use the safe view models and must not expose `ResultView` before submit; specifically, avoid direct static-case or repository reads that could leak true class, ANOMALY, or unpaid AI data.
- Current art provides normal button states only; disabled/interaction feedback should use native Unity UI styling until dedicated assets exist.
- Cancer Galaxy has the required frozen data contract (645 reference nodes, current Case coordinates, five precomputed nearby references, and CancerType mappings) through `CancerGalaxyView`, but has no dedicated visual node/legend/background assets.

## 12. Phase B Recommended Scope

Implement only the two-Case playable loop:

`MainMenu` -> New Shift -> `CaseAnalysis` -> Initial Clues -> Gene Scan -> `CancerGalaxy` -> Back -> AI Assistant -> Diagnosis -> Submit -> `ResultSummary` -> Next Case -> repeat one additional Case.

Do not include Tutorial refinement, final visual reproduction, complex animation, or a final build in that phase. A MainMenu Continue action should first call `SaveDataRepository.HasSave()` to decide availability, then call `GameplayService.RestoreSavedShift()` when selected; it should surface the returned failure rather than attempting to repair a bad save.

## 13. Files Expected to Change in Phase B

Suggested scope only; none of these were changed in Phase A:

- Create: `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/CaseAnalysis.unity`, `Assets/Scenes/CancerGalaxy.unity`, `Assets/Scenes/ResultSummary.unity`.
- Defer creation/refinement of `Assets/Scenes/Tutorial.unity` unless a minimal navigation placeholder is explicitly needed later.
- Create supporting UI controller scripts beneath `Assets/Scripts/UI/Controllers/` (for example bootstrap/navigation, MainMenu, CaseAnalysis, CancerGalaxy, and ResultSummary controllers), plus narrowly scoped UI binding/support scripts as needed.
- Modify later, after explicit Phase B authorization: `ProjectSettings/EditorBuildSettings.asset` to replace/remove the sample-only build list; scene files and their UI bindings.
- Reuse rather than modify: `GameplayService`, view models, Runtime/Save/Data Layer, frozen JSON, TMP font asset, and existing art assets.
