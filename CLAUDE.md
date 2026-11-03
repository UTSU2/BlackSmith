# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

BlackSmith: a 2D crafting game (Unity `6000.3.27f2`, URP 2D, Input System, uGUI). Core loop: buy minerals → craft (heat → forge → heat-treat → polish) → grade determined by completion → sell. See `README.md` (Korean) for design and current status; most gameplay specs (crafting steps, grade names/counts, multipliers) are still **undecided/placeholder**. Only the title menu, a Game scene background, and the item data layer exist so far.

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

## Namespaces

- Root is `BlackSmith`; `Assets/Scripts/<Folder>` → `BlackSmith.<Folder>`. Keep folder and namespace in sync, including for new folders (max 2 levels below root).

  | Folder | Namespace |
  |---|---|
  | `Assets/Scripts/` (root files) | `BlackSmith` |
  | `Assets/Scripts/Items/` | `BlackSmith.Items` |
  | `Assets/Scripts/Data/` | `BlackSmith.Data` |
  | `Assets/Scripts/UI/` | `BlackSmith.UI` |
  | `Assets/Editor/` | `BlackSmith.EditorTools` |

- One namespace per file, **block style** (`namespace X { ... }`). Unity 6 compiles C# 9, so file-scoped `namespace X;` (C# 10) does not compile.
- Editor code uses `BlackSmith.EditorTools`, not `BlackSmith.Editor` — the latter would make `Editor` resolve to the namespace instead of `UnityEditor.Editor` inside `BlackSmith.*`.
- Inside `BlackSmith.UI`, import `UnityEngine.UI` explicitly (`using UnityEngine.UI;`); use `global::UnityEngine.UI` if ambiguous.
- Third-party code (`Assets/vFavorites`, imported assets) is left untouched.
- When moving/renaming scripts, edit file contents only and never delete or recreate `.meta` files — asset/scene references are by script GUID. Saves (`JsonUtility`) store no type names, so namespaces don't affect save compatibility.

## Item architecture (spans several files)

- Each item is one ScriptableObject asset (`ItemData` → `MineralItemData` / `CraftedItemData`). Minerals can be bought and sold; crafted items sell only (`CanBuy`, `BuyPrice` are virtual on `ItemData`).
- `ItemDatabase` (static, `RuntimeInitializeOnLoadMethod`) loads everything under `Assets/Resources/Items/` via `Resources.LoadAll` — no manual registry. Assets outside that folder are silently not loaded. Duplicate/empty IDs are logged and skipped.
- Create/edit items through `Tools > BlackSmith > Item Creator` (`Assets/Editor/ItemCreatorWindow.cs`): left pane lists items, right pane creates or edits. It writes fields via `SerializedObject` using the **private field names** (`id`, `displayName`, `icon`, `description`, `maxStack`, `defaultGrade`, `sellPrice`, `sellPriceMultipliers`, `buyPrice`) — renaming those fields in `ItemData`/`MineralItemData` requires updating the window. Item type can't change after creation (different classes); changing an ID renames the asset file.
- `ItemGrade` enum order is the index into each item's `sellPriceMultipliers`; `ItemData.OnValidate` pads/resizes to `ItemGradeInfo.Count` (new slots = 1.0). Sell price = `sellPrice × multiplier[grade]`, rounded.
