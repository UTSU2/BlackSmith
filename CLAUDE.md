# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

- Engine: Unity 6 (`6000.3.27f2`, URP 2D, Input System, uGUI)
- Language: C#
- Genre: 2D Blacksmith Simulation
- Developer: Solo Developer

BlackSmith core loop: buy minerals → craft (heat → forge → heat-treat → polish) → quality determined by minigame score (grade is fixed by the recipe) → sell → use profit to buy better materials and craft higher-grade gear. Recipe acquisition (v2, implemented: exact-match crafting, unknown craft, recipe codex) — whether it counts as part of the main loop is **미정**. See `README.md` (Korean) for current status. Implemented: title menu (new game / load / settings / quit), shop, crafting (4 minigames, recipe matching, quality, finished-item grant), save/load (8 slots), sound (9 minigame SFX, 2 BGM tracks; placeholder files) and a settings screen with BGM/SFX volume. Content: 48 minerals, 17 crafted items (11 real-named gear + 5 temp + the unknown craft `999`), 49 recipes.

- Crafting is four fast, rhythm-game-style mouse minigames: heat (press/release to hold a bar at a target position), forge (click shrinking circles at the right timing), heat-treat (click when an oscillating marker hits a spot on a gauge), polish (click a flaw and drag to sand it off).
- Grades (materials and crafted gear share them): Common, Uncommon, Rare, Unique, Legendary. Every item (minerals and crafted gear) has exactly one grade, fixed in its item data; there is no other-grade version of the same item. Grade only affects price multipliers and display — it does not affect recipe matching, difficulty or quality. Implemented in `ItemGrade` (the old `Epic` was renamed `Unique`, same int value).
- Quality is separate from grade: each of the 4 minigames scores 0–10 in proportion to completion (100% = 10), total 0–40, floored: 0–9 최하품 / 10–19 하품 / 20–29 중품 / 30–39 상품 / 40 최상품. Price multipliers 0.7 / 1 / 1.2 / 1.5 / 2.
- Price: ingredient price is Common × (1, 1.2, 1.5, 1.8, 2) by grade, rounded. Crafted item price = sum of the recipe's listed ingredient buy prices × quality multiplier (grade does not change it directly). Implemented: `ItemGradeInfo.PriceMultiplier/ScalePrice`, `ItemData.GetBuyPrice/GetSellUnitPrice`, `CraftedItemData.GetSellUnitPrice`; rounding is half-up via `PriceRounding` (not `Mathf.RoundToInt`). The per-instance ingredient value is stored in the inventory entry (`ItemVariant`), since it depends on the grades actually used.
- Minigame difficulty is 10 levels (1–10), the same for all 4 minigames and never shown to the player. The difficulty is the recipe's base value as-is (no ingredient-grade bonus, no quality advantage; `CraftDifficulty.Calculate(baseDifficulty)` only clamps). Defined in `docs/GameDesign.md` §4–5; judging rules, per-level minigame values and the 20 s craft time (heat 5 / forge 8 / heat-treat 2 / polish 5) are fixed there (§4) and applied in code: each `*Settings` class holds the level-1 values and level-10 values, and `CraftDifficulty.Curve/CurveLerp` (exponent 2) interpolates. The values are also serialized on `CraftingController` in `Game.unity`, so changing class defaults alone does not change the game.
- Recipe matching (v2, implemented; `docs/V2.md` §3, `docs/GameDesign.md` §5): **exact match only** — ingredient kind + amount (inputs of the same item are summed; grade irrelevant). No match → the Common "unknown craft" (item ID `999`, icon **미정**, fixed sell price 50 via `CraftedItemData.fixedSellPrice`, difficulty 2, all 4 stages; from `UnknownCraftSettings`), with no codex entry and no proficiency; it is not a recipe. Two recipes with identical ingredients → the first registered wins (`RecipeDatabase` and Recipe Creator warn). All input ingredients are consumed when crafting starts (`CraftingSession.TryStart`). The crafting panel never reveals whether the input matches (same status text and start button for any non-empty input; only without the settings asset is a non-match unstartable). Discovery is by experimenting (no hints, no other unlock path in v2). The v1 closest-recipe scoring is gone. Content rule: every mineral must have at least one recipe using it, and adding an item means adding the recipes that use it; 48 minerals with 49 recipes (11 real-named gear + 38 placeholder recipes for 5 temp crafted items `temp_gear_*`). The 49 recipes are final for v2; ingredient-composition rebalancing is v3.
- MVP = **v1**; its completion criteria are fixed (`docs/MVP.md` §5): temporary minerals/crafted items complete, hit sounds, BGM, only very basic balancing. Hit effects, fine-grained balancing, proficiency and recipe acquisition are **v2** (gear enhancement was removed from v2). All v1 criteria are met (sound is in and very basic balancing is done); remaining work is replacing placeholder sound files and placeholder content, then v2. Status against the criteria is tracked in `docs/MVP.md` (현재 충족 현황).
- New game starts the tutorial (iron ore → iron sword; `docs/Tutorial.md`): `GameSession.StartNew` → `PlayerData.StartTutorial(startMoney)` with `startMoney` = sum of the tutorial recipe's ingredient buy prices from `TutorialConfig` (currently 100), then `MarkSaved()` so the grant is not an unsaved change. `TutorialController` calls `PlayerData.CompleteTutorial()` when the flow finishes (right after the sword is sold): the sword's sale proceeds are taken back and money is set to exactly 1000 (`TutorialRewardMoney`). With no `TutorialConfig` asset, the old behavior applies (no tutorial, 1000 granted immediately). `tutorialPending` is stored as "pending" so old saves (no field) count as completed and get no reward.
- Tutorial architecture: `TutorialConfig` + `TutorialStepData` assets in `Assets/Resources/Tutorial/` (popup texts are `[임시]` placeholders), `TutorialFlow` (pure C# step logic), `TutorialController` (scene object in Game) and `TutorialPopup`. Each step asset sets the popup position (`TutorialPopupPosition` Top/Bottom; steps 1 and 5 in the shop use Bottom, the rest and the completion message use Top). While active: `ShopPanel` filters the buy tab to iron ore and the sell tab to the iron sword (`IsBuyAllowed/IsSellAllowed`), `GameHUD.OnBook` (save/load) is locked, and the `SettingsPanel` title-return confirm shows a tutorial message. No skip; progress is not saved; leaving to Title restarts the tutorial on the next new game.
- v2 order (`docs/V2.md`): tutorial (done), proficiency (**implemented**; per recipe; gain = pre-bonus total score, none below 20, 60 for a perfect 40; 초급/중급/고급 Lv1–9 then Master; need 100 at Common 초급 Lv1, ×1.2 per level, ×1.5 at each stage change, +50 per grade on the Lv1 start; bonus 0.1 × level number (none at 초급 Lv1, so Lv2 0.2 … 고급 Lv9 2.7), Master 3.0 total, with the bonus the total is capped at 39 (40 only from a raw 40); a new craft result popup (item name/grade/quality, stage+level, gain, progress number+bar, bonus; closed with a confirm button; the tutorial craft also counts; in the tutorial a top-positioned proficiency-guidance step (`Step_03_Proficiency`, `[임시]` text) shows together with the result popup and the result popup's confirm advances to Step_04); details `docs/V2.md` §2; proficiency is checked in the recipe codex; the result popup's visual design and the tutorial proficiency text content are **미정** (layout rules and the guidance timing/position are fixed)), hit effects (**implemented**; forge: gold glow on each judged circle sized Perfect > Good > clicked Miss, none for a missed circle; polish: silver sparkle per cleared flaw; particles; current values are final, adjustable parts listed in `docs/V2.md` §4), recipe acquisition (**implemented**; exact match, unknown craft, recipe codex — see Recipe architecture; codex numbers are temporary, unknown-craft icon and codex/button visuals are **미정**), then fine balancing (planned). Gear enhancement is out of v2 (later adoption **미정**).
- Design docs live in `docs/` (`GameDesign.md`, `CoreLoop.md`, `MVP.md`, `Roadmap.md`). Items marked **미정** there (e.g. Common prices are decided per item when it is created, and items/crafted items/recipes will be authored all at once later) (including Roadmap schedules) must **not** be decided on your own — ask the user or leave them as placeholders.

## Architecture Rules

- 게임 데이터는 ScriptableObject 활용
- 런타임 상태와 정적 데이터 분리 (예: `ItemData`는 정적 데이터. 제작된 장비의 등급·품질 같은 개별 상태는 별도 런타임 데이터로 둔다)
- UI와 게임 로직 분리
- 기능별로 클래스 책임 분리

## Coding Rules

- 클래스와 메서드는 PascalCase
- private 필드는 camelCase
- 직렬화 필드는 [SerializeField] 사용
- 불필요한 Singleton 사용 금지 (기존 `static class`인 `ItemDatabase`, `GameSession`, `SaveSlotManager`는 그대로 두되, 새 코드에서 불필요한 Singleton/전역 상태를 늘리지 않는다)
- 네임스페이스 규칙은 아래 [Namespaces](#namespaces) 참고

## Workflow

- 기존 코드를 먼저 분석할 것
- 구현 전에 변경 계획을 설명할 것
- 요청하지 않은 시스템 수정 금지
- 새로운 기능은 작은 단위로 구현
- 변경한 파일과 테스트 방법 설명 (검증은 아래 [Unity MCP](#unity-mcp)의 절차를 따르고, 무엇을 확인했고 무엇을 확인하지 않았는지 함께 보고)
- 기획 문서에 **미정**으로 표시된 사항은 임의로 결정하지 않는다

## Issues

- 이슈 제목 맨 앞에 `[분류]` 말머리를 붙여, 제목만 보고 성격을 바로 알 수 있게 한다. 예: `[BUG] 저장 슬롯 로드 시 NullReference`
- 기본 말머리: `[BUG]` (버그), `[제안]` (기능/개선 제안), `[검증]` (확인·테스트가 필요한 항목). 이 외에도 내용에 맞는 짧은 말머리를 쓸 수 있다.
- 말머리는 하나만 쓰고, 본문에서 다시 설명하지 않아도 될 만큼 명확한 단어를 고른다.
- 기획 문서에 **미정**으로 표시된 사항은 이슈에서도 임의로 확정하지 않는다. 결정이 필요하면 그 사실을 이슈에 명시한다.
- `gh`가 PATH에 없을 수 있다. 이 경우 `C:\Program Files\GitHub CLI\gh.exe`를 전체 경로로 호출한다.

## Working in this repo

- No CLI build/lint/test setup. Development happens in the Unity Editor (open with Unity 6000.3.27f2; scenes `Assets/Scenes/TItle.unity` (build index 0) and `Game.unity` (index 1)). There are no test assemblies yet.
- The Unity editor is driven through the MCP for Unity package (`UnityMCP` tools). See [Unity MCP](#unity-mcp) for the compile/verify workflow.
- Folder `Assets/Fantasy Wooden GUI  Free` has two spaces before "Free" — keep it exact in paths. Scene file is spelled `TItle.unity`.

## Unity MCP

- **Always verify after any change**: whenever scripts, scenes, prefabs, or assets change, check the *affected* scenes/prefabs for errors before reporting done — Missing Script (null components), missing references, and console errors/warnings. Don't skip this because the change "looks harmless"; renames, moves, and namespace changes silently break references.
  - Loaded scene (live): `execute_code` over `GetRootGameObjects()` → `GetComponentsInChildren<Component>(true)`, count entries that are `null` (= Missing Script).
  - Scene/prefab not loaded: compare the `m_Script: {guid: ...}` values in the `.unity`/`.prefab` YAML against the `.meta` GUIDs of `Assets/` scripts and package scripts (`Library/PackageCache`), without opening the scene (opening changes the active scene). `fileID: 0` = already-missing script.
  - After namespace/class renames also check UnityEvent persistent calls: the `m_TargetAssemblyTypeName` stored in scenes (e.g. `GameHUD, Assembly-CSharp`) goes stale — refresh it via `SerializedObject` (`AssemblyQualifiedName` of the target).
  - Then read the console (`types=[error,warning]`). Report what was checked and what was *not* (e.g. "scene verified by file only").
- **Compile check** after editing scripts: `read_console action=clear` → `refresh_unity compile=request mode=force` → `refresh_unity wait_for_ready=true` (wait for the domain reload) → `read_console types=[error,warning]`. Don't use new types until this is clean.
- `[WebSocket] Unexpected receive error: WebSocket is not initialised` is a domain-reload side effect, not a compile error. Judge success by: no other errors, and `refresh_unity` back to `idle`. If still unsure, read `mcpforunity://editor/state` (`data.compilation.is_compiling`, `data.advice.ready_for_tools`).
- `execute_code` with `compiler=auto` uses CodeDom (C# 6; Roslyn is not installed): no local functions, `out var` or pattern matching. Use `Action`/`Func` lambdas, and fully qualify `UnityEngine.UI.*` and `UnityEngine.Object` (`Object` is ambiguous with `object`).
- Limits of verifying through MCP: `manage_camera screenshot` does not capture Screen Space - Overlay UI (Title's canvas; Game's `GameUICanvas` is Screen Space - Camera and is captured), and while the editor window is unfocused play mode barely advances frames (frame count stays ~2), so audio playback, `LoadScene` and other Update-driven behavior may not run. Test UI logic with `onClick.Invoke()` and report what could not be checked (looks, audible sound) instead of claiming it works.
- Never delete or recreate `.cs.meta` files (asset/scene references are by script GUID). Moving scripts: edit contents only, or move with the `.meta` (`manage_asset`).
- **Verify by change type**:
  - Scripts: compile check, then the Always-verify checks on `Title` and `Game`.
  - Item assets / `ItemDatabase`: no duplicate-ID or load warnings in the console.
  - UI / save: play mode through Title → load panel → slot → Game; **always exit play mode afterwards**.
- **State changes**: save scenes only when asked (check with `manage_scene`). To edit a second scene while the active one is unsaved, open it additively (the `More than one global light` warning is expected while both are loaded), then save and close it after the user agrees. Use `execute_code` for read-only inspection first; tell the user what any asset/scene-mutating code will change before running it.
- Before tool calls, check `mcpforunity://custom-tools` and `mcpforunity://instances` (call `set_active_instance` if more than one editor is connected). MCP paths are relative to `Assets/` with `/` separators.

## UI Buttons

- **글자(`Label`)가 있는 버튼은 모두 `ButtonLabelPress`(`Assets/Scripts/ButtonLabelPress.cs`)를 붙인다.** 눌림 스프라이트가 보이는 동안 글자를 함께 내려, 버튼 이미지와 글자 위치를 일치시킨다. 새 버튼도 예외 없이 같은 방식으로 만든다. (글자가 없는 닫기·화살표·아이콘 버튼은 해당 없음)
- 구성 (Title 씬 버튼과 동일):
  - `Button` 전환(Transition)은 `SpriteSwap`, `pressedSprite`에 눌림 스프라이트를 지정한다. (예: `TextBTN_Medium` / `TextBTN_Medium_Pressed`)
  - 글자는 이름이 정확히 `Label`인 자식 오브젝트(`Text`)로 두고 부모에 꽉 채운다(앵커 0,0 ~ 1,1).
  - **평소 위치**는 씬에 저장된 `Label`의 `anchoredPosition`이다. 평소에는 버튼 면이 위로 올라와 있으므로 `Label`을 **위로 +20**(`offsetMin`/`offsetMax`의 y = 20, Title 버튼 기준 높이 100~130) 올려 둔다. 버튼이 작으면 높이에 비례해 줄이고(예: 높이 90 → 16) 화면에서 확인한다.
  - **눌림 위치**는 `ButtonLabelPress.pressedPosition`이며 `(0, 0)`으로 둔다. `label` 필드는 비워 두면 `Label` 자식을 자동으로 찾는다.
- 눌림 여부는 포인터 이벤트가 아니라 `Button`의 현재 스프라이트(`image.overrideSprite == pressedSprite`)로 판단한다. 따라서 토글처럼 코드에서 `image.sprite`를 눌림 스프라이트로 바꿔 "선택됨"을 표시하면 글자도 함께 내려간다. (상점 구매/판매 탭이 이 방식)
- 새 버튼 스크립트를 짤 때 글자 위치를 직접 옮기는 코드를 따로 만들지 않는다. 버튼 연출은 `ButtonLabelPress`로만 처리한다.

## Namespaces

- Root is `BlackSmith`; `Assets/Scripts/<Folder>` → `BlackSmith.<Folder>`. Keep folder and namespace in sync, including for new folders (max 2 levels below root).

- Root files in `Assets/Scripts/` use `BlackSmith`; `Assets/Editor/` uses `BlackSmith.EditorTools`.
- One namespace per file, **block style** (`namespace X { ... }`). Unity 6 compiles C# 9, so file-scoped `namespace X;` (C# 10) does not compile.
- Editor code uses `BlackSmith.EditorTools`, not `BlackSmith.Editor` — the latter would make `Editor` resolve to the namespace instead of `UnityEditor.Editor` inside `BlackSmith.*`.
- Inside `BlackSmith.UI`, import `UnityEngine.UI` explicitly (`using UnityEngine.UI;`); use `global::UnityEngine.UI` if ambiguous.
- Third-party code (`Assets/vFavorites`, imported assets) is left untouched.
- When moving/renaming scripts, edit file contents only and never delete or recreate `.meta` files — asset/scene references are by script GUID. Saves (`JsonUtility`) store no type names, so namespaces don't affect save compatibility.

## Item architecture (spans several files)

- Each item is one ScriptableObject asset (`ItemData` → `MineralItemData` / `CraftedItemData`). Minerals can be bought and sold; crafted items sell only (`CanBuy` and `GetBuyPrice(grade)` are virtual on `ItemData`).
- `ItemDatabase` (static, `RuntimeInitializeOnLoadMethod`) loads everything under `Assets/Resources/Items/` via `Resources.LoadAll` — no manual registry. Assets outside that folder are silently not loaded. Duplicate/empty IDs are logged and skipped.
- Create/edit items through `Tools > BlackSmith > Item Creator` (`Assets/Editor/ItemCreatorWindow.cs`): left pane lists items, right pane creates or edits. It writes fields via `SerializedObject` using the **private field names** (`id`, `displayName`, `icon`, `description`, `maxStack`, `defaultGrade`, `sellPrice`, `buyPrice`) — renaming those fields in `ItemData`/`MineralItemData` requires updating the window. Item type can't change after creation (different classes); changing an ID renames the asset file.
- Future design note (not implemented): `CraftedItemData` is static data for a *kind* of item; per-crafted-instance state (quality) belongs in runtime data per Architecture Rules. Implemented as `ItemVariant` stored in `InventoryEntry`; the finished item is granted by `CraftingController.FinishCrafting` (see `docs/GameDesign.md` §5).
- `sellPrice` and `buyPrice` on items are **Common-grade** prices; other grades are derived by `ItemGradeInfo` (×1/1.2/1.5/1.8/2, half-up). There is no per-item multiplier array any more. Crafted items ignore `sellPrice`: their unit price is `ingredientValue × quality multiplier` (`CraftedItemData.GetSellUnitPrice`), unless `fixedSellPrice` > 0 (the unknown craft: 50 regardless of quality; set in Item Creator). `ItemGrade` values are stored as ints in assets — never reorder.
- Inventory entries are keyed by `(itemId, ItemVariant)`; `ItemVariant` = grade + quality (`ItemQuality`, `None` for minerals) + ingredient value. Old saves without the new fields load as `None`/0.

## Recipe architecture

- Each recipe is one ScriptableObject asset (`CraftRecipeData`: `id`, `result`, `ingredients`, `stages`, `baseDifficulty`, `codexNumber`). Every asset under `Assets/Resources/Recipes/` can be crafted; the player only sees recipes they have crafted (codex). Crafting starts by inserting ingredients, which `RecipeMatcher` matches exactly (or the unknown craft).
- `RecipeDatabase` (static, same pattern as `ItemDatabase`) loads `Resources/Recipes` via `Resources.LoadAll`. Empty/duplicate IDs and recipes without a result are logged and skipped.
- Create/edit recipes through `Tools > BlackSmith > Recipe Creator` (`Assets/Editor/RecipeCreatorWindow.cs`). It writes via `SerializedObject` using the **private field names** (`id`, `result`, `ingredients` (`item`, `count`), `stages`, `baseDifficulty`, `codexNumber`) — renaming those in `CraftRecipeData`/`CraftIngredient` requires updating the window. It warns when two recipes share the same ingredient set, about missing/duplicate codex numbers or numbers out of grade order, and when an edited id differs from the original (unlock and proficiency records are keyed by id). The list is shown in codex order (`No.n`).
- Difficulty (1–10): the base value is defined per recipe (`CraftRecipeData.baseDifficulty`, set in Recipe Creator; computed by `CraftDifficulty.Calculate` via `CraftScoring.CreateRequest`). Ingredient grade and amount do not affect difficulty.
- `CraftStage` values are stored as integers in assets: append new stages at the end, never reorder.
- Crafting design (stage minigames, score → quality, recipe matching, service/presenter split) is implemented in `Assets/Scripts/Crafting` (`CraftScoring`, `RecipeMatcher`, `CraftingSession`, `CraftingController`); there is no test assembly, so it was verified through the editor. Rules are in `docs/GameDesign.md` §4–5; per-minigame judging and per-level values are fixed in §4 and implemented.

## Codex architecture (recipe acquisition)

- Order: `CraftRecipeData.codexNumber` (1-based, lower result grade first, entered by hand in Recipe Creator); `RecipeCodex.GetOrdered()` sorts by number then id. The current numbers are **temporary** (grade, then id ordinal: Common 1–19 … Legendary 47–49, `iron_sword` = 1).
- Save: `PlayerData.unlockedRecipes` (recipe **ids**, so renumbering never moves an unlock) + `codexInitialized`. `GameSession.Set`/`StartNew`/`Current` call `EnsureCodexInitialized()`: an old save (field missing → false) gets its proficiency-record recipe ids as unlocks once; not an unsaved change. `UnlockRecipe` returns true only for a new unlock.
- Unlock: `CraftingController.GrantResult` unlocks the matched recipe at craft finish regardless of quality and records `CraftResult.NewlyUnlocked`; `CraftResultPopup.unlockLabel` shows `새 레시피 해금! (No.n)` (box 440 high; rows below moved down 50). For the unknown craft (`CraftResult.IsUnknown`, `CraftRequest.Recipe == null`) nothing is unlocked or gained and `priceLabel` shows the fixed price instead of the proficiency rows.
- UI: `CodexPanel` + 49 `CodexSlotView` cells in Game (`GameUICanvas/CodexPanel`, opened via `CodexTransition`, watched by `PanelInputBlocker`), 7×7 grid + detail (number, name, grade, ingredients × amount, proficiency via `ProficiencyRules`; no record → 초급 Lv.1 0/need), progress `해금 n / total`. Locked cells show number + `???`; unlocked cells without an icon show the item name's first letter. `GameHUD.OnCodex` (green-cover copy of the book icon, left of it) opens it, also during the tutorial. Visuals are placeholders (**미정**).

## Proficiency architecture

- Static data: `ProficiencySettings` (ScriptableObject, `Resources/Proficiency/ProficiencySettings.asset`, `Load()`): base requirement, per-grade add, level/stage multipliers, stage names, bonus per level, Master bonus, bonus cap 39, min score 20, perfect gain 60. With no asset, crafting runs without proficiency (warning logged).
- Pure C#: `ProficiencyRules` (`GetThresholds(grade)` with half-up rounding of an unrounded chain, `Evaluate(points, grade)` → `ProficiencyState`, `CalculateGain(rawScore)`, `ApplyBonus(rawScore, bonus)`). The level grade is the recipe result's `DefaultGrade`.
- Runtime state: `PlayerData.proficiencies` (`ProficiencyEntry`: recipe id + cumulative points; levels are derived, so overflow carries automatically). Old saves without the field load empty; `SaveFile.CurrentVersion` is still 1.
- Applied in `CraftingController.GrantResult`: bonus from the pre-craft level → quality, gain from the pre-bonus score; everything is recorded on `CraftResult` (`SetOutcome`/`SetProficiency`) before `CraftFinished`.
- `CraftResultPopup` (`BlackSmith.UI`, Game scene under `GameUICanvas`) listens to `CraftFinished`, closes with its confirm button and raises `Closed`. Layout rules are fixed in `docs/V2.md` §2 (icon top-left with its top aligned to the item name; name and grade/quality on two lines via `itemLabel`/`detailLabel`, centered; nothing overlaps or leaves the box; icon as far right as the text allows); the concrete sizes and the visual design are placeholders. The object must stay active in the scene (it subscribes in `Start` and hides itself). `TutorialController` advances `CraftFinished` steps immediately and `CraftResultClosed` steps (enum value 5) on `resultPopup.Closed`; with no `resultPopup` both advance at craft finish. `TutorialPopup` has its own canvas at order 21 so it stays above the result popup (20) when both are open.

## Hit effects architecture

- Game's `GameUICanvas` is **Screen Space - Camera** (`Main Camera`, plane distance 5) so particles can draw over UI. Sorting layers: `Default` (all world renderers: background sprites -10 to -1, forge fire particle 5) < `UI`. On `UI`: `GameUICanvas` order 0 < hit effects order 10 < `CraftResultPopup`/`TutorialPopup` (own `Canvas`, override sorting, order 20, + `GraphicRaycaster`). Keep world renderers on `Default` and UI-space renderers on `UI`; with the same layer a world renderer's order ≥ 0 would draw over panels. Title is still Overlay.
- Models only raise events: `ForgeMinigame.CircleJudged(Circle)` (with `Circle.Clicked`: false for a circle missed by timeout) and `PolishMinigame.FlawClearedAt(Flaw)`, after the existing `Judged`/`FlawCleared` used by `CraftingSfx`.
- `ForgeMinigameView` / `PolishMinigameView` subscribe in `Show`, unsubscribe and `Clear()` in `Hide`, and call `HitEffectSpawner.Play([prefab,] localPoint, scale)` (`BlackSmith.UI`, on each PlayArea). Forge picks a per-judgment prefab (`perfectEffect`/`goodEffect`/`missEffect` on the view); scale = actual circle/flaw diameter ÷ the view's `effectReferenceDiameter` (forge 160 = target circle, polish 180 = flaw), so the effect follows `targetRadius`/`flawRadius`.
- Forge effects are **donuts that must never cover the judged circle**: ring texture `SoftRing.png` (alpha 0 inside half the radius) at start size 2× the reference diameter, size curve never below 1, sparks spawned outside the circle edge. Keep those invariants when editing the prefabs.
- Effects are prefabs in `Assets/Effects` (`ForgeHitPerfect`/`ForgeHitGood`/`ForgeHitMiss` gold, `PolishSparkle` silver; materials `HitRing.mat`/`HitEffect.mat` = URP Particles/Unlit additive, textures `SoftRing.png`/`SoftGlow.png`): `scalingMode = Hierarchy` (sizes in canvas px), root `stopAction = Destroy`, renderer sorting layer `UI` order 10, no Graphic/Collider. Swap the art by editing or replacing the prefabs.
- MCP play-mode raycast checks need `Camera.main.Render()` first (graphics have depth -1 until rendered); that manual render logs harmless `RenderPass` size-mismatch errors.

## Audio & settings architecture

- Clips: `Assets/Audio/Sfx` (9 minigame SFX) and `Assets/Audio/Bgm` (`bgm_default`, `bgm_crafting`), all placeholders. SFX clips are referenced only through the `SfxClips` ScriptableObject (`Assets/Audio/SfxClips.asset`), so files can be swapped without code changes. Plan and decisions: `docs/SoundPlan.md`.
- `Assets/Scripts/Audio` (`BlackSmith.Audio`): `SoundSettings` (static; BGM/SFX volume 0–1 in `PlayerPrefs`, `Changed` event — subscribers must unsubscribe in `OnDisable`), `SfxPlayer` (one-shots and per-clip loops), `BgmPlayer` (two AudioSources crossfade; one per scene, no `DontDestroyOnLoad`, so music restarts on every scene load), `CraftingBgmSwitch`.
- Minigame models stay audio-free and only raise events (`ForgeMinigame.Judged/EmptyClicked`, `HeatTreatMinigame.Judged`, `PolishMinigame.FlawCleared`, `PolishMinigame.IsSanding`). `CraftingSfx` (`BlackSmith.Crafting`) is bound per stage by `CraftingController` (`Bind`/`Unbind`) and plays the clips; the loops (`heat_hold`, `sand_loop`) follow model state in `CraftingSfx.Update` and must stop on `Unbind`.
- BGM switches on `CraftingController.CraftStarted` / `CraftFinished`: `bgm_crafting` plays only from craft start to finish.
- `SettingsPanel` (`BlackSmith.UI`) exists as a scene object in both Title and Game and does not pause the game. Game's panel also has a return-to-Title button: with no unsaved changes it loads `titleSceneName` (`"TItle"`) directly, otherwise it shows a confirm popup. `PlayerData.HasUnsavedChanges` is set by money/inventory changes and cleared by `MarkSaved()` when `SaveSlotManager.Save` succeeds; it is not serialized.
