# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

- Engine: Unity 6 (`6000.3.27f2`, URP 2D, Input System, uGUI)
- Language: C#
- Genre: 2D Blacksmith Simulation
- Developer: Solo Developer

BlackSmith core loop: buy minerals → craft (heat → forge → heat-treat → polish) → quality determined by minigame score (grade is fixed by the recipe) → sell → use profit to buy better materials and craft higher-grade gear. Recipe unlocking is **not** part of the main loop. See `README.md` (Korean) for current status. Implemented: title menu, shop, crafting (4 minigames, recipe matching, quality, finished-item grant), and save/load (8 slots). Content is still minimal: 1 mineral, 1 crafted item, 1 recipe.

- Crafting is four fast, rhythm-game-style mouse minigames: heat (press/release to hold a bar at a target position), forge (click shrinking circles at the right timing), heat-treat (click when an oscillating marker hits a spot on a gauge), polish (click a flaw and drag to sand it off).
- Grades (materials and crafted gear share them): Common, Uncommon, Rare, Unique, Legendary. Every item (minerals and crafted gear) has exactly one grade, fixed in its item data; there is no other-grade version of the same item. Grade only affects price multipliers and display — it does not affect recipe matching, difficulty or quality. Implemented in `ItemGrade` (the old `Epic` was renamed `Unique`, same int value).
- Quality is separate from grade: each of the 4 minigames scores 0–10 in proportion to completion (100% = 10), total 0–40, floored: 0–9 최하품 / 10–19 하품 / 20–29 중품 / 30–39 상품 / 40 최상품. Price multipliers 0.7 / 1 / 1.2 / 1.5 / 2.
- Price: ingredient price is Common × (1, 1.2, 1.5, 1.8, 2) by grade, rounded. Crafted item price = sum of the recipe's listed ingredient buy prices × quality multiplier (grade does not change it directly). Implemented: `ItemGradeInfo.PriceMultiplier/ScalePrice`, `ItemData.GetBuyPrice/GetSellUnitPrice`, `CraftedItemData.GetSellUnitPrice`; rounding is half-up via `PriceRounding` (not `Mathf.RoundToInt`). The per-instance ingredient value is stored in the inventory entry (`ItemVariant`), since it depends on the grades actually used.
- Minigame difficulty is 10 levels (1–10), the same for all 4 minigames and never shown to the player. The difficulty is the recipe's base value as-is (no ingredient-grade bonus, no quality advantage; `CraftDifficulty.Calculate(baseDifficulty)` only clamps). Defined in `docs/GameDesign.md` §4–5; judging rules, per-level minigame values and the 20 s craft time are fixed there (§4) and applied in code: each `*Settings` class holds the level-1 values and level-10 values, and `CraftDifficulty.Curve/CurveLerp` (exponent 2) interpolates. The values are also serialized on `CraftingController` in `Game.unity`, so changing class defaults alone does not change the game.
- Recipe matching (no recipe book shown to the player): matching looks at amounts only, not grades; recipes needing more than the input are excluded; per ingredient score 5 − |amount difference| (0 if the ingredient is not in the recipe or difference ≥ 5), highest total wins, ties go to the higher-priced recipe; all input ingredients are consumed when crafting starts (`CraftingSession.TryStart`). Implemented in `RecipeMatcher` / `CraftScoring`. Content rule: every mineral must have at least one recipe using it, and adding an item means adding the recipes that use it. The matching code is done; the content rule is currently satisfied by 48 placeholder minerals with 48 placeholder recipes (temp crafted items `temp_gear_*`).
- MVP = **v1**; its completion criteria are fixed (`docs/MVP.md` §5): temporary minerals/crafted items complete, hit sounds, BGM, only very basic balancing. Hit effects, fine-grained balancing, enhancement, proficiency and recipe unlocking are **v2**.
- Planned for v2 (after the core is done): gear enhancement (enhancement level affects price; formula **미정**) and a proficiency system.
- Design docs live in `docs/` (`GameDesign.md`, `CoreLoop.md`, `MVP.md`, `Roadmap.md`). Items marked **미정** there (e.g. Common prices are decided per item when it is created, and items/crafted items/recipes will be authored all at once later) (including Roadmap schedules) must **not** be decided on your own — ask the user or leave them as placeholders.

## Architecture Rules

- 게임 데이터는 ScriptableObject 활용
- 런타임 상태와 정적 데이터 분리 (예: `ItemData`는 정적 데이터. 제작된 장비의 등급·강화 수치 같은 개별 상태는 별도 런타임 데이터로 둔다)
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
- Never delete or recreate `.cs.meta` files (asset/scene references are by script GUID). Moving scripts: edit contents only, or move with the `.meta` (`manage_asset`).
- **Verify by change type**:
  - Scripts: compile check, then the Always-verify checks on `Title` and `Game`.
  - Item assets / `ItemDatabase`: no duplicate-ID or load warnings in the console.
  - UI / save: play mode through Title → load panel → slot → Game; **always exit play mode afterwards**.
- **State changes**: save scenes only when asked (check with `manage_scene`). Use `execute_code` for read-only inspection first; tell the user what any asset/scene-mutating code will change before running it.
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

  | Folder | Namespace |
  |---|---|
  | `Assets/Scripts/` (root files) | `BlackSmith` |
  | `Assets/Scripts/Items/` | `BlackSmith.Items` |
  | `Assets/Scripts/Data/` | `BlackSmith.Data` |
  | `Assets/Scripts/UI/` | `BlackSmith.UI` |
  | `Assets/Scripts/Crafting/` | `BlackSmith.Crafting` |
  | `Assets/Editor/` | `BlackSmith.EditorTools` |

- One namespace per file, **block style** (`namespace X { ... }`). Unity 6 compiles C# 9, so file-scoped `namespace X;` (C# 10) does not compile.
- Editor code uses `BlackSmith.EditorTools`, not `BlackSmith.Editor` — the latter would make `Editor` resolve to the namespace instead of `UnityEditor.Editor` inside `BlackSmith.*`.
- Inside `BlackSmith.UI`, import `UnityEngine.UI` explicitly (`using UnityEngine.UI;`); use `global::UnityEngine.UI` if ambiguous.
- Third-party code (`Assets/vFavorites`, imported assets) is left untouched.
- When moving/renaming scripts, edit file contents only and never delete or recreate `.meta` files — asset/scene references are by script GUID. Saves (`JsonUtility`) store no type names, so namespaces don't affect save compatibility.

## Item architecture (spans several files)

- Each item is one ScriptableObject asset (`ItemData` → `MineralItemData` / `CraftedItemData`). Minerals can be bought and sold; crafted items sell only (`CanBuy` and `GetBuyPrice(grade)` are virtual on `ItemData`).
- `ItemDatabase` (static, `RuntimeInitializeOnLoadMethod`) loads everything under `Assets/Resources/Items/` via `Resources.LoadAll` — no manual registry. Assets outside that folder are silently not loaded. Duplicate/empty IDs are logged and skipped.
- Create/edit items through `Tools > BlackSmith > Item Creator` (`Assets/Editor/ItemCreatorWindow.cs`): left pane lists items, right pane creates or edits. It writes fields via `SerializedObject` using the **private field names** (`id`, `displayName`, `icon`, `description`, `maxStack`, `defaultGrade`, `sellPrice`, `buyPrice`) — renaming those fields in `ItemData`/`MineralItemData` requires updating the window. Item type can't change after creation (different classes); changing an ID renames the asset file.
- Future design note (not implemented): `CraftedItemData` is static data for a *kind* of item; per-crafted-instance state (quality, later enhancement level) belongs in runtime data per Architecture Rules. Implemented as `ItemVariant` stored in `InventoryEntry`; the finished item is granted by `CraftingController.FinishCrafting` (see `docs/GameDesign.md` §5).
- `sellPrice` and `buyPrice` on items are **Common-grade** prices; other grades are derived by `ItemGradeInfo` (×1/1.2/1.5/1.8/2, half-up). There is no per-item multiplier array any more. Crafted items ignore `sellPrice`: their unit price is `ingredientValue × quality multiplier` (`CraftedItemData.GetSellUnitPrice`). `ItemGrade` values are stored as ints in assets — never reorder.
- Inventory entries are keyed by `(itemId, ItemVariant)`; `ItemVariant` = grade + quality (`ItemQuality`, `None` for minerals) + ingredient value. Old saves without the new fields load as `None`/0.

## Recipe architecture

- Each recipe is one ScriptableObject asset (`CraftRecipeData`: `id`, `result`, `ingredients`, `stages`, `baseDifficulty`). There is no recipe unlocking and recipes are not shown to the player; every asset under `Assets/Resources/Recipes/` is open. Crafting starts by inserting ingredients, which `RecipeMatcher` matches to the closest recipe.
- `RecipeDatabase` (static, same pattern as `ItemDatabase`) loads `Resources/Recipes` via `Resources.LoadAll`. Empty/duplicate IDs and recipes without a result are logged and skipped.
- Create/edit recipes through `Tools > BlackSmith > Recipe Creator` (`Assets/Editor/RecipeCreatorWindow.cs`). It writes via `SerializedObject` using the **private field names** (`id`, `result`, `ingredients` (`item`, `count`), `stages`, `baseDifficulty`) — renaming those in `CraftRecipeData`/`CraftIngredient` requires updating the window. It warns when two recipes share the same ingredient set.
- Difficulty (1–10): the base value is defined per recipe (`CraftRecipeData.baseDifficulty`, set in Recipe Creator; computed by `CraftDifficulty.Calculate` via `CraftScoring.CreateRequest`). Ingredient grade and amount do not affect difficulty.
- `CraftStage` values are stored as integers in assets: append new stages at the end, never reorder.
- Crafting design (stage minigames, score → quality, recipe matching, service/presenter split) is implemented in `Assets/Scripts/Crafting` (`CraftScoring`, `RecipeMatcher`, `CraftingSession`, `CraftingController`); there is no test assembly, so it was verified through the editor. Rules are in `docs/GameDesign.md` §4–5; per-minigame judging and per-level values are fixed in §4 and implemented.
