# 레시피 습득 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 정확 일치 매칭, 알 수 없는 제작품(999), 레시피 도감(7×7 격자 + 상세, 진행도, 해금 알림)을 넣는다.

**Architecture:** 매칭은 `RecipeMatcher`가 정확 일치만 돌려주고, 일치가 없으면 `CraftingSession`이 `UnknownCraftSettings`(ScriptableObject)로 레시피 없는 `CraftRequest`를 만든다. `CraftRequest`가 결과 아이템·단계 목록을 직접 가지므로 `CraftingController`는 레시피 유무와 관계없이 같은 흐름으로 진행하고, 레시피가 있을 때만 숙련도·해금을 처리한다. 해금은 `PlayerData`에 레시피 id 목록으로 저장하고, 도감 순서는 레시피의 새 필드 `codexNumber`로 정한다.

**Tech Stack:** Unity 6 (6000.3.27f2), C# 9, uGUI, ScriptableObject, JsonUtility, UnityMCP(`execute_code`, `manage_scene`, `manage_asset`, `read_console`, `refresh_unity`).

**Spec:** `docs/superpowers/specs/2026-11-18-recipe-acquisition-design.md` (승인됨). 상위 문서 `docs/V2.md` §3.

## Global Constraints

- 네임스페이스는 폴더와 맞춘 block style. `Assets/Scripts/Crafting/` → `BlackSmith.Crafting`, `Data/` → `BlackSmith.Data`, `Items/` → `BlackSmith.Items`, `UI/` → `BlackSmith.UI`(`using UnityEngine.UI;` 명시), `Assets/Editor/` → `BlackSmith.EditorTools`.
- private 필드 camelCase, 직렬화는 `[SerializeField]`, 클래스·메서드 PascalCase. 새 Singleton/전역 상태 금지. 수치는 데이터(ScriptableObject)에 둔다.
- 정확 일치 기준은 **재료 종류 + 수량**(등급 무관). 투입 재료는 시작 시 모두 소모(기존 그대로).
- 알 수 없는 제작품: 아이템 ID `"999"`, 이름 `알 수 없는 제작품`, Common, 아이콘 없음(**[미정]**, 자리표시자로 비워 둠), **판매가 50 고정**, **난이도 2**, 단계 4개(Heat, Forge, HeatTreat, Polish). 숙련도·해금·도감 번호 없음. 레시피가 아니다(`Resources/Recipes`에 넣지 않는다).
- 도감 진행도 분모 = `RecipeDatabase.Count`(현재 49). 모르는 칸은 번호와 `???`만. 해금한 칸 상세 = 번호·이름·등급·재료와 수량·숙련도(단계·레벨, 진행 수치+막대, 보너스).
- 해금 저장 키는 레시피 `Id`(문자열). `codexNumber`는 순서·표시에만 쓴다.
- 결과 팝업 해금 줄 문구: `새 레시피 해금! (No.{codexNumber})`. 알 수 없는 제작품 판매가 줄: `판매가 {price} G`.
- 제작 패널 상태 문구는 매칭 여부를 드러내지 않는다: 비어 있으면 `재료를 넣어주세요`, 재료가 있으면 `제작할 수 있습니다`(레시피 여부 무관).
- `.cs.meta`는 삭제·재생성하지 않는다. 씬은 사용자가 요청할 때만 저장한다. 커밋은 사용자가 요청할 때만, 작업별 파일만 `git add`(`git add -A` 금지, `Assets/Settings/*`·`ProjectSettings/UnityConnectSettings.asset`·`Assets/vFavorites/*` 제외). `Assets/Resources/Tutorial/Step_03_Proficiency.asset`·`Step_04.asset`은 사용자 수정 중이므로 건드리지도 커밋하지도 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 테스트 어셈블리가 없다. 검증은 `execute_code`(CodeDom, C# 6: 지역 함수·`out var`·패턴 매칭 불가, `UnityEngine.Object`·`UnityEngine.UI.*` 완전 한정). 스크립트 변경 뒤 컴파일 체크: `read_console clear` → `refresh_unity compile=request mode=force wait_for_ready=true` → `read_console types=[error,warning]`(`WebSocket is not initialised` 무시). 씬 코드 실행 전 활성 씬이 `Game`인지 확인한다. 플레이 모드는 끝나면 반드시 종료한다. 사용자 저장 슬롯 파일은 건드리지 않는다.

## Review Focus

1. **제작 패널이 정답을 흘림:** 레시피 조합과 아닌 조합에서 상태 문구·시작 버튼 색이 같아야 한다 (Task 2 Step 5).
2. **옛 세이브 도감 채우기가 반복됨:** 한 번 채운 뒤 다시 저장·로드해도 목록이 그대로이고, 그 뒤 생긴 숙련도 기록이 해금으로 둔갑하지 않는다 (Task 4 Step 4).
3. **알 수 없는 제작품이 숙련도·해금을 건드림:** 999 제작 전후로 `proficiencies`·해금 목록이 같다 (Task 2 Step 6).
4. **알 수 없는 제작품 판매가가 품질을 따라감:** 품질 5종 모두 판매가 50, 상점 판매 목록에도 50 (Task 2 Step 6).
5. **도감 패널이 다른 패널·입력과 겹침:** 도감이 열려 있는 동안 모루·HUD 클릭이 막히고, 저장·로드 후 씬이 다시 로드되면 도감이 새 데이터로 보인다 (Task 5 Step 6).

---

### Task 1: 정확 일치 매칭

**Files:**
- Modify: `Assets/Scripts/Crafting/RecipeMatcher.cs` (전체 재작성, 시그니처 유지)
- Modify: `Assets/Scripts/Crafting/RecipeDatabase.cs` (`Reload`에 같은 구성 경고)

**Interfaces:**
- Produces: `RecipeMatcher.Match(IReadOnlyList<CraftInput> inputs) : CraftRecipeData`, `Match(inputs, recipes)` — 투입(아이템 id별 합산, 수량 0 이하 무시)과 `CraftScoring.SumRecipeIngredients(recipe)`(광물 `Id` 기준)가 종류·수량 모두 같은 첫 레시피, 없으면 null. `MaxIngredientScore`와 점수·가격 동률 로직은 삭제한다.

- [ ] **Step 1: `RecipeMatcher` 재작성.** 클래스 주석을 정확 일치 규칙과 `docs/V2.md` §3로 바꾼다.
- [ ] **Step 2: `RecipeDatabase.Reload`** 에서 재료 구성이 같은 레시피 쌍마다 `Debug.LogWarning($"[RecipeDatabase] 재료 구성이 같은 레시피: '{a}', '{b}'. 먼저 등록된 '{a}'가 쓰입니다.")`.
- [ ] **Step 3: 컴파일 체크.** Expected: 오류·경고 없음(현재 49종에 같은 구성은 없어야 한다. 경고가 나오면 멈추고 보고).
- [ ] **Step 4: 검증 (`execute_code`).** 모든 레시피에 대해: 정확한 재료 → 자기 자신; 첫 재료 수량 +1 → null; 첫 재료 수량 −1(1개면 그 재료 제거) → null; 레시피에 없는 광물 1개 추가 → null. 같은 재료를 두 입력으로 나눠 넣어도(예: 철 원석 1 + 1, `iron_axe`) 일치. 빈 입력 → null. Expected: 49 × 4 항목 모두 통과, 실패 0.
- [ ] **Step 5: 커밋 (요청 시).** `feat : 레시피 매칭을 정확 일치로 변경`

### Task 2: 알 수 없는 제작품

**Files:**
- Create: `Assets/Scripts/Crafting/UnknownCraftSettings.cs`, `Assets/Resources/Crafting/UnknownCraftSettings.asset`
- Create: `Assets/Resources/Items/Crafted/unknown_craft.asset` (Item Creator 방식으로 `SerializedObject` 기록)
- Modify: `Assets/Scripts/Items/CraftedItemData.cs`, `Assets/Editor/ItemCreatorWindow.cs`
- Modify: `Assets/Scripts/Crafting/CraftRequest.cs`, `CraftScoring.cs`, `CraftingSession.cs`, `CraftingController.cs`, `CraftResult.cs`
- Modify: `Assets/Scripts/UI/CraftingPanel.cs` (57행 세션 생성, 153-163행 상태), `Assets/Scripts/UI/CraftResultPopup.cs`
- Modify: `Assets/Scenes/Game.unity` (결과 팝업 `priceLabel`)

**Interfaces:**
- Produces:
  - `UnknownCraftSettings : ScriptableObject` — `const string ResourcePath = "Crafting/UnknownCraftSettings"`, `static UnknownCraftSettings Load()`, `CraftedItemData Result`, `IReadOnlyList<CraftStage> Stages`, `int BaseDifficulty`(기본값 2, `Range(1,10)`).
  - `CraftedItemData.FixedSellPrice : int`(직렬화 필드 `fixedSellPrice`, 0이면 기존 공식). `GetSellUnitPrice`는 `fixedSellPrice > 0`이면 그 값.
  - `CraftRequest(CraftRecipeData recipe, CraftedItemData product, IReadOnlyList<CraftStage> stages, int difficulty, int ingredientValue, IReadOnlyList<CraftInput> inputs)`; 새 속성 `Product`, `Stages`, `IsUnknown => Recipe == null`.
  - `CraftScoring.CreateUnknownRequest(UnknownCraftSettings settings, IReadOnlyList<CraftInput> inputs) : CraftRequest` — 레시피 null, 난이도 `CraftDifficulty.Calculate(settings.BaseDifficulty)`, 재료 합산 0.
  - `CraftingSession(PlayerData player, UnknownCraftSettings unknownCraft = null)`; `CanStart => !IsEmpty && (Recipe != null || (unknownCraft != null && unknownCraft.Result != null))`.
  - `CraftResult.IsUnknown => Request.IsUnknown`.

- [ ] **Step 1: 데이터 코드.** `UnknownCraftSettings`(로드 패턴은 `ProficiencySettings.Load`와 같게), `CraftedItemData.fixedSellPrice`(Tooltip: "0이면 재료가 합 × 품질 배율. 0보다 크면 품질과 관계없이 이 값"). Item Creator: 제작품일 때 `고정 판매가 (0 = 재료가 × 품질)` IntField, 로드·`ApplyFields`에서 `fixedSellPrice`를 읽고 쓴다(광물은 `FindProperty`가 null이므로 건너뜀). 제작품 안내 HelpBox에 고정가 설명 한 줄 추가.
- [ ] **Step 2: 제작 흐름 코드.** `CraftScoring.CreateRequest`는 새 생성자에 `recipe.Result`, `recipe.Stages`를 넘긴다. `CraftingSession.TryStart`: `Recipe`가 있으면 기존, 없고 설정이 있으면 `CreateUnknownRequest`, 둘 다 없으면 false. `CraftingController`: 필드 `recipe`를 `IReadOnlyList<CraftStage> stages`로 바꿔 `request.Stages`로 진행; `GrantResult`는 `request.Product` 사용, `request.IsUnknown`이면 숙련도 계산·`SetProficiency`를 건너뛰고(`bonus` 0, 품질은 원점수) 로그에 `'알 수 없는 제작'`. `CraftingPanel`은 `new CraftingSession(GameSession.Current, UnknownCraftSettings.Load())`; 설정이 null이면 `Debug.LogWarning("[Crafting] 알 수 없는 제작 설정이 없어 레시피와 일치하는 조합만 제작할 수 있습니다.")`. 상태 문구는 Global Constraints 그대로.
- [ ] **Step 3: 결과 팝업 코드.** `[SerializeField] private Text priceLabel;` — `result.IsUnknown`일 때만 켜고 `판매가 {result.Product.GetSellUnitPrice(variant)} G`(variant는 결과의 등급·품질, 재료가 0). 기존 숙련도·보너스 줄은 `ProficiencyAfter == null`이라 이미 숨겨진다.
- [ ] **Step 4: 컴파일 체크 후 에셋 생성 (`execute_code`, 에셋 변경).** `UnknownCraftSettings.asset`(Result=999, Stages 4개 순서대로, BaseDifficulty 2). `unknown_craft.asset`: `id "999"`, `displayName "알 수 없는 제작품"`, `description "[임시] 무엇을 만들려던 것인지 알 수 없는 물건."`, `defaultGrade Common`, `maxStack`은 기존 제작품과 같게, `fixedSellPrice 50`, `icon` null. Game 씬 `CraftResultPopup`에 `PriceLabel`(Text, `ProficiencyLabel`과 같은 위치·폰트 크기 복제) 추가·연결(씬 변경, 실행 전 사용자에게 알림). Expected: 콘솔에 ItemDatabase 중복·로드 경고 없음, `ItemDatabase.TryGet("999")` true.
- [ ] **Step 5: 패널 문구 검증 (플레이 모드).** `CraftingPanel`을 열고 세션에 철 원석 1개(레시피) → 상태 문구·시작 버튼 색 기록; 비우고 철 원석 3개(레시피 없음, 소지 수량은 테스트용으로 `AddItem`) → 기록. Expected: 두 경우 문구 `제작할 수 있습니다`, 색 `Color.white`로 같다.
- [ ] **Step 6: 제작 검증 (플레이 모드).** 테스트용 `PlayerData`를 `GameSession.Set`으로 넣고 숙련도·해금 목록을 기록. 레시피 없는 조합으로 `TryStart` → `request.IsUnknown`, `Stages.Count == 4`, `Difficulty == 2`. `CraftingController`의 시작·완료 경로를 리플렉션으로 호출해(단계 결과는 4개 모두 완성도 1) 완료 → 인벤토리에 `999` 1개, 결과 팝업 표시·`PriceLabel` `판매가 50 G`·숙련도 줄 숨김, `proficiencies`·해금 목록 변화 없음. `CraftedItemData(999).GetSellUnitPrice`가 품질 5종 모두 50, 상점 판매 목록 항목 가격 50. 플레이 모드 종료. Expected: 모두 통과.
- [ ] **Step 7: Missing Script(Title, Game) 0, 콘솔 오류·경고 없음.**
- [ ] **Step 8: 커밋 (요청 시, 씬 저장 후).** `feat : 알 수 없는 제작품 추가 (레시피 없는 조합, 판매가 50 고정)`

### Task 3: 도감 번호

**Files:**
- Modify: `Assets/Scripts/Crafting/CraftRecipeData.cs`, `Assets/Editor/RecipeCreatorWindow.cs`
- Create: `Assets/Scripts/Crafting/RecipeCodex.cs`
- Modify: `Assets/Resources/Recipes/*.asset` 49개 (초기 번호)

**Interfaces:**
- Produces:
  - `CraftRecipeData.CodexNumber : int` (직렬화 필드 `codexNumber`, Tooltip "도감 번호(1부터). 낮은 등급이 앞 번호. 해금 기록은 id로 저장되므로 번호를 바꿔도 해금은 유지된다.")
  - `static class RecipeCodex` (`BlackSmith.Crafting`): `List<CraftRecipeData> GetOrdered(IReadOnlyList<CraftRecipeData> recipes)` — `CodexNumber` 오름차순, 같으면 `Id` ordinal; `List<CraftRecipeData> GetOrdered()` — `RecipeDatabase.All` 기준.

- [ ] **Step 1: 필드와 `RecipeCodex` 작성.**
- [ ] **Step 2: Recipe Creator.** 폼에 `도감 번호` IntField; `CreateRecipe`/`UpdateRecipe`/`ApplyFields`에 `int codexNumber` 인자 추가(`codexNumber`에 기록), 편집 시 로드. `FindProblems`에 추가: 번호 ≤ 0 (`'{id}': 도감 번호가 없습니다`), 번호 중복, 번호순으로 정렬했을 때 앞 레시피의 결과 등급이 뒤 레시피보다 높은 경우(`도감 번호 순서가 등급 순서와 맞지 않습니다: No.{a} '{idA}'({gradeA}) > No.{b} '{idB}'({gradeB})`). 편집 중 id 입력이 원래 id와 다르면 HelpBox Warning: `레시피 id를 바꾸면 이 레시피의 도감 해금과 숙련도 기록이 끊깁니다.`
- [ ] **Step 3: 컴파일 체크.**
- [ ] **Step 4: 초기 번호 채우기 (`execute_code`, 에셋 49개 변경 — 실행 전 알림).** 결과 `DefaultGrade` 오름차순 → 같은 등급 안에서 `Id` ordinal 순으로 1부터 `SerializedObject`로 기록, `SaveAssets`. Expected 범위: Common 1–19, Uncommon 20–31, Rare 32–40, Unique 41–46, Legendary 47–49. `iron_sword`는 Common 안에서 글자순 첫째라 1번이어야 한다.
- [ ] **Step 5: 검증 (`execute_code`).** 번호 1–49 중복·빈 번호 없음, `RecipeCodex.GetOrdered()` 순서의 결과 등급이 감소하지 않음, Recipe Creator `FindProblems`(리플렉션) 결과에 번호 관련 경고 0. 임시로 한 에셋 번호를 다른 레시피와 같게 바꿔 경고가 뜨는지 확인 후 되돌림. Expected: 통과, 되돌린 뒤 git diff에 번호 외 변경 없음.
- [ ] **Step 6: 커밋 (요청 시).** `feat : 레시피 도감 번호 추가 (임시 번호 1~49, Recipe Creator 경고)`

### Task 4: 해금 저장과 결과 팝업 알림

**Files:**
- Modify: `Assets/Scripts/Data/PlayerData.cs`, `Assets/Scripts/Data/GameSession.cs`
- Modify: `Assets/Scripts/Crafting/CraftingController.cs` (`GrantResult`), `CraftResult.cs`
- Modify: `Assets/Scripts/UI/CraftResultPopup.cs`, `Assets/Scenes/Game.unity` (`UnlockLabel`, 상자 높이)

**Interfaces:**
- Consumes: `CraftResult.IsUnknown` (Task 2), `CraftRecipeData.CodexNumber` (Task 3).
- Produces:
  - `PlayerData`: 직렬화 `List<string> unlockedRecipes`, `bool codexInitialized`(필드 없는 옛 세이브는 false). `void EnsureCodexInitialized()` — false이면 `proficiencies`의 레시피 id를 중복 없이 추가하고 true로; 저장하지 않은 변경으로 보지 않는다. `bool IsRecipeUnlocked(string recipeId)`, `bool UnlockRecipe(string recipeId)`(새로 추가하면 true, `hasUnsavedChanges = true`).
  - `GameSession.StartNew`, `Set`, `Current`(새로 만들 때) 모두 `EnsureCodexInitialized()` 호출. 새 게임은 숙련도가 비어 있어 빈 목록 + true가 된다.
  - `CraftResult.NewlyUnlocked : bool`, `void SetUnlocked(bool newlyUnlocked)`. `GrantResult`는 레시피가 있을 때 지급 뒤 `player.UnlockRecipe(recipe.Id)` 결과를 기록(품질 무관). 로그에 `/ 도감 해금` 추가(새로 해금일 때).

- [ ] **Step 1: `PlayerData`·`GameSession` 작성.**
- [ ] **Step 2: `GrantResult`·`CraftResult` 작성.**
- [ ] **Step 3: 결과 팝업.** `[SerializeField] private Text unlockLabel;` — `NewlyUnlocked`일 때만 `새 레시피 해금! (No.{n})`. 씬: 상자 높이 390 → 440, `UnlockLabel`을 등급·품질 줄 아래(top 135)에 두고 숙련도·진행·보너스 줄과 `PriceLabel`을 50 아래로 내린다(135→185, 190→240, 240→290). 확인 버튼은 상자 하단 기준 그대로. 모든 글자·버튼 RectTransform이 서로 겹치지 않고 상자 안에 있는지 `execute_code`로 코너 비교(씬 변경, 실행 전 알림).
- [ ] **Step 4: 저장 검증 (`execute_code`, 메모리만).** (a) 숙련도 2개(`iron_sword`, `recipe_iron_axe`)를 가진 옛 형식 JSON(`unlockedRecipes`·`codexInitialized` 필드 없음) → `FromJson` → `EnsureCodexInitialized` → 해금 = 그 2개, `HasUnsavedChanges` false. (b) `ToJson` → `FromJson` → `EnsureCodexInitialized` 다시 → 여전히 2개. (c) 그 데이터에 `recipe_iron_mace` 숙련도만 직접 추가 → `EnsureCodexInitialized` → mace는 해금되지 않음. (d) `new PlayerData()` + `EnsureCodexInitialized` → 0개. (e) `UnlockRecipe` 같은 id 두 번 → true, false. Expected: 모두 통과.
- [ ] **Step 5: 플레이 검증.** 테스트용 데이터로 `iron_sword` 제작 경로 호출(Task 2 Step 6 방식) → `NewlyUnlocked` true, 팝업 `새 레시피 해금! (No.1)`; 같은 레시피 다시 → false, 줄 숨김; 최하품(완성도 0) 제작도 해금됨; 999 제작 → 해금 줄 숨김. 플레이 모드 종료.
- [ ] **Step 6: Missing Script 0, 콘솔 확인. 커밋 (요청 시, 씬 저장 후).** `feat : 레시피 해금 저장과 제작 결과 팝업 해금 알림`

### Task 5: 도감 패널

**Files:**
- Create: `Assets/Scripts/UI/CodexPanel.cs`, `Assets/Scripts/UI/CodexSlotView.cs`
- Modify: `Assets/Scripts/GameHUD.cs`, `Assets/Scenes/Game.unity` (`CodexPanel`, `CodexButton`, `PanelInputBlocker.watchedPanels`)

**Interfaces:**
- Consumes: `RecipeCodex.GetOrdered()`, `PlayerData.IsRecipeUnlocked`, `ProficiencyRules`/`ProficiencySettings.Load()`, `CraftScoring.SumRecipeIngredients`.
- Produces:
  - `CodexSlotView : MonoBehaviour` — 필드 `numberLabel`, `iconImage`, `unknownLabel`, `selectedFrame`, `button`; `void Bind(CraftRecipeData recipe, bool unlocked)`(모르는 칸: 번호 + `???`, 아이콘 숨김), `void SetSelected(bool)`, `event Action<CodexSlotView> Clicked`, `CraftRecipeData Recipe`.
  - `CodexPanel : MonoBehaviour` — 필드 `CodexSlotView[] slots`(씬에 49개), `progressLabel`(`해금 {n} / {total}`), `progressFill`(Filled Image), 상세 `detailRoot`, `detailIcon`, `detailNameLabel`(`No.{n} {이름}`), `detailGradeLabel`, `detailIngredientsLabel`(줄마다 `{광물 이름} × {수량}`), `detailProficiencyLabel`(`ProficiencyState.Label`), `detailProgressFill`, `detailProgressLabel`(`{Progress} / {Required}`, Master면 숨김), `detailBonusLabel`(`품질 보너스 +{bonus}`, 결과 팝업과 같은 서식), `detailUnknownLabel`(모르는 칸 선택 시 `???`), `closeButton`. `void Open()`, `void Close()`, `bool IsOpen`. 열 때마다 `GameSession.Current`로 다시 그린다. 첫 해금 칸을 기본 선택, 없으면 1번 칸(`???`).
  - 레시피 수가 슬롯 수보다 많으면 `Debug.LogWarning`하고 남는 레시피는 표시하지 않는다. 슬롯이 남으면 숨긴다.
  - `GameHUD.OnCodex()` — `codexPanel.Open()`. 튜토리얼 중에도 열린다.

- [ ] **Step 1: 스크립트 작성.** 폰트는 `CraftResultPopup.Awake`와 같은 OS 폰트 적용. 숙련도 기록이 없으면 0점 상태로 표시(spec 빌더 몫).
- [ ] **Step 2: 컴파일 체크.**
- [ ] **Step 3: 씬 구성 (`execute_code`, 씬 변경 — 실행 전 알림).** 먼저 `ShopPanel`·`BookButton` 구성을 읽기 전용으로 확인하고 같은 배경 스프라이트·닫기 버튼 방식을 따른다. `GameUICanvas` 아래 `CodexPanel`(1560×900, 중앙, 비활성으로 시작): 제목 `레시피 도감`, 진행도, 왼쪽 7×7 격자(칸 100×100, 간격 10, `GridLayoutGroup` 없이 좌표 고정 또는 사용, 빌더 선택), 오른쪽 상세(약 680×760). 칸 49개는 템플릿 하나를 복제. `CodexButton`은 `BookButton`을 복제해 오른쪽에 두고 onClick을 `GameHUD.OnCodex`로 바꾼다. 아이콘 스프라이트는 임시로 Book과 같고, 구분용으로 버튼 아래에 `도감` 캡션 Text를 둔다(버튼 자식 `Label`이 아니면 `ButtonLabelPress` 대상 아님. 원본에 `Label`이 있으면 `ButtonLabelPress` 규칙을 그대로 따른다). `PanelInputBlocker.watchedPanels`에 `CodexPanel` 추가. `GameHUD.codexPanel` 연결.
- [ ] **Step 4: 레이아웃 검증.** 패널 안 모든 RectTransform이 패널 경계 안, 칸끼리·상세 영역과 겹침 없음, 상세 글자 영역 서로 겹침 없음. 스크린샷(`manage_camera`, Game은 Camera 캔버스라 찍힘)으로 모양 확인.
- [ ] **Step 5: 동작 검증 (플레이 모드).** 테스트 데이터(해금 `iron_sword`·`recipe_iron_axe`, 숙련도 axe 150): `CodexButton.onClick.Invoke()` → 열림, `해금 2 / 49`, 1번 칸 아이콘·나머지 47칸 `???`, 철검 선택 상태. 철 도끼 칸 `Clicked` → 상세 `No.{n} 철 도끼`, 재료 `철 원석 × 2`, 숙련도 라벨·`{Progress} / {Required}`가 `ProficiencyRules.Evaluate(150, grade)`와 일치. 모르는 칸 선택 → `???`만. 닫기 → 비활성.
- [ ] **Step 6: 입력·재로드 검증.** 도감이 열린 상태에서 모루 화면 좌표·`ShopButton` 중심 레이캐스트(`Camera.main.Render()` 먼저) → 첫 결과가 `PanelInputBlocker` 또는 도감 패널. `SaveLoadPanel` 로드 경로로 씬이 다시 로드된 뒤 도감을 열면 새 데이터 기준. 튜토리얼 중(`IsTutorialPending` 데이터)에도 열림. 플레이 모드 종료.
- [ ] **Step 7: Missing Script(Title, Game) 0, 콘솔 확인. 커밋 (요청 시, 씬 저장 후).** `feat : 레시피 도감 패널 추가 (7x7 격자, 상세, 진행도)`

### Task 6: 문서

**Files:**
- Modify: `docs/V2.md` (§1 4번 상태, §3 결정·구현 현황, §5 표, §6), `docs/GameDesign.md`·`docs/CoreLoop.md`(매칭 규칙, "레시피 북은 보이지 않는다" → "해금한 레시피만 도감에 표시"), `docs/Tutorial.md`("정확 일치 도입 시 주의"를 구현됨으로, `Step_04` 문구는 사용자 몫), `docs/Roadmap.md`(1. 의존 관계 매칭 문장), `CLAUDE.md`(Recipe architecture·v2 문장·Item architecture의 고정가·새 "Codex" 설명), `README.md`.

- [ ] **Step 1: 문서 갱신.** 미정으로 남는 것: 알 수 없는 제작품 아이콘, 도감 패널·버튼 최종 비주얼, 튜토리얼 문구. 임시값: 도감 번호(등급순 → id 글자순), 패널 크기, 999 설명 문구.
- [ ] **Step 2: `grep`으로 "가장 가까운", "레시피 북은", "closest recipe" 잔존 확인.** Expected: 과거 기록(v1 설명·변경 이력)과 사용자 몫인 `Step_04.asset` 외에는 없음.
- [ ] **Step 3: 커밋 (요청 시).** `docs : 레시피 습득(정확 일치, 알 수 없는 제작품, 도감) 반영`
