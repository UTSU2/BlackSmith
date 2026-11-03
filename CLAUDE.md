# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

BlackSmith: a 2D crafting game (Unity `6000.3.27f2`, URP 2D, Input System, uGUI). Core loop: buy minerals → craft (heat → forge → heat-treat → polish) → grade determined by completion → sell. See `README.md` (Korean) for design and current status; most gameplay specs (crafting steps, grade names/counts, multipliers) are still **undecided/placeholder**. Only the title menu, a Game scene background, and the item data layer exist so far.

## Working in this repo

- No CLI build/lint/test setup. Development happens in the Unity Editor (open with Unity 6000.3.27f2; scenes `Assets/Scenes/TItle.unity` (build index 0) and `Game.unity` (index 1)). There are no test assemblies yet.
- The Unity editor is driven through the MCP for Unity package (`UnityMCP` tools). After editing scripts, run `refresh_unity` and check `read_console` for compile errors before using new types.
- Folder `Assets/Fantasy Wooden GUI  Free` has two spaces before "Free" — keep it exact in paths. Scene file is spelled `TItle.unity`.

## Item architecture (spans several files)

- Each item is one ScriptableObject asset (`ItemData` → `MineralItemData` / `CraftedItemData`). Minerals can be bought and sold; crafted items sell only (`CanBuy`, `BuyPrice` are virtual on `ItemData`).
- `ItemDatabase` (static, `RuntimeInitializeOnLoadMethod`) loads everything under `Assets/Resources/Items/` via `Resources.LoadAll` — no manual registry. Assets outside that folder are silently not loaded. Duplicate/empty IDs are logged and skipped.
- Create/edit items through `Tools > BlackSmith > Item Creator` (`Assets/Editor/ItemCreatorWindow.cs`): left pane lists items, right pane creates or edits. It writes fields via `SerializedObject` using the **private field names** (`id`, `displayName`, `icon`, `description`, `maxStack`, `defaultGrade`, `sellPrice`, `sellPriceMultipliers`, `buyPrice`) — renaming those fields in `ItemData`/`MineralItemData` requires updating the window. Item type can't change after creation (different classes); changing an ID renames the asset file.
- `ItemGrade` enum order is the index into each item's `sellPriceMultipliers`; `ItemData.OnValidate` pads/resizes to `ItemGradeInfo.Count` (new slots = 1.0). Sell price = `sellPrice × multiplier[grade]`, rounded.
