# v3 승급 퀘스트 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 대장장이 승급을 퀘스트(지정 레시피를 지정 품질 이상으로 제작)로 거치게 하고, 클리어 즉시 승급 팝업으로 알린다.

**Architecture:** 순수 C# `RankQuestRules`가 클리어 판정을 하고, ScriptableObject `RankQuestSettings`가 등급별 퀘스트 데이터를 갖는다. `RankController`가 개발용 직접 승급을 퀘스트 판정으로 바꾸고 `Promoted` 이벤트를 낸다. `RankHud`에 퀘스트 줄을, 새 `RankPromotionPopup`(자체 캔버스 22)에 승급 안내를 둔다. 퀘스트 상태는 세이브에 저장하지 않고 등급과 개수로 계산한다.

**Tech Stack:** Unity 6 (6000.3.27f2), C# 9, uGUI, Unity MCP(`execute_code`는 C# 6).

**Spec:** [2026-11-19-v3-rank-quest-design.md](../specs/2026-11-19-v3-rank-quest-design.md) (상위: [2026-11-19-v3-rank-skip-design.md](../specs/2026-11-19-v3-rank-skip-design.md))

## Global Constraints

- 네임스페이스 `Assets/Scripts/<Folder>` → `BlackSmith.<Folder>`, 블록 스타일, C# 9. 새 `.cs.meta`를 지우거나 다시 만들지 않는다.
- **미정 값은 임의로 채우지 않는다**: 등급별 퀘스트 레시피·최소 품질·설명, 혜택 문구, 팝업 비주얼, `승급 퀘스트 준비 중` 문구(임시 표기). 퀘스트 에셋 항목은 비워 두고(레시피 id 빈 문자열, 최소 품질 `None`) 필요 개수 -1도 그대로 둔다.
- 퀘스트가 비어 있는 등급은 승급하지 않는다. 튜토리얼 중(`PlayerData.IsTutorialPending`)에는 퀘스트가 없다. 알 수 없는 제작품(`CraftResult.IsUnknown`)은 해당 없다.
- 승급은 한 번에 한 단계. `SaveFile.CurrentVersion` 1 유지, `PlayerData` 필드 추가 없음.
- 팝업 캔버스 순서 22(결과 팝업 20, 튜토리얼 21 위). 글자 있는 버튼은 `ButtonLabelPress`를 붙이고 CLAUDE.md의 UI Buttons 구성을 따른다.
- 개인 설정 변경(URP 3 에셋, `ProjectSettings/UnityConnectSettings.asset`)은 커밋하지 않는다. 커밋 꼬리말: `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- 테스트 어셈블리가 없다. "테스트"는 `execute_code`로 돌리는 assert이고, 컴파일 확인은 CLAUDE.md 절차(`read_console clear` → `refresh_unity compile=request mode=force` → `wait_for_ready` → `read_console error,warning`)를 따른다. 씬 저장은 사용자가 요청할 때만 한다.

## Review Focus

- 퀘스트 항목이 비어(레시피 id 빈 문자열) 있는데 조건을 채운 경우: 승급하지 않고 HUD는 `승급 퀘스트 준비 중`. (Task 1, 3, 4)
- 알 수 없는 제작품이 우연히 최소 품질 이상이어도 클리어가 아니다. (Task 1)
- 조건 미달인데 퀘스트 레시피를 만든 경우 승급하지 않는다. (Task 1, 3)
- 승급 직후 다음 등급 조건도 이미 충족이면 다음 퀘스트가 시작되고 승급은 한 단계만이다. (Task 3)
- 팝업 중복·누락: 결과 팝업이 떠 있는 동안 승급하면 결과 팝업을 닫은 뒤 한 번만 뜬다. (Task 5)
- 스킵 장비를 켠 채 최상품 퀘스트: 클리어 불가가 정상(자동으로 끄지 않음). (Task 1 문서화)

---

## File Structure

- Create `Assets/Scripts/Rank/RankQuestSettings.cs`: 등급별 퀘스트 데이터(ScriptableObject).
- Create `Assets/Scripts/Rank/RankQuestRules.cs`: 순수 판정.
- Create `Assets/Resources/Rank/RankQuestSettings.asset`: 비어 있는 4개 항목.
- Modify `Assets/Scripts/Rank/BlacksmithRankSettings.cs`: 등급별 `benefitText`와 `GetBenefit`.
- Modify `Assets/Scripts/Rank/RankController.cs`: 직접 승급 제거, 퀘스트 판정, `Promoted`, `GetQuestStatus`.
- Modify `Assets/Scripts/UI/RankHud.cs`: 퀘스트 줄.
- Create `Assets/Scripts/UI/RankPromotionPopup.cs`: 승급 팝업.
- Modify `Assets/Scenes/Game.unity`: 팝업 오브젝트 추가, HUD 높이 확장.
- Modify docs: `docs/V3.md`, `docs/Roadmap.md`, `README.md`, `CLAUDE.md`.

---

### Task 1: 퀘스트 데이터와 판정 규칙

**Files:**
- Create: `Assets/Scripts/Rank/RankQuestSettings.cs`, `Assets/Scripts/Rank/RankQuestRules.cs`, `Assets/Resources/Rank/RankQuestSettings.asset`
- Modify: `Assets/Scripts/Rank/BlacksmithRankSettings.cs`

**Interfaces:**
- Produces:
  - `RankQuestSettings.ResourcePath = "Rank/RankQuestSettings"`, `static RankQuestSettings Load()`.
  - `RankQuestSettings.QuestEntry { BlacksmithRank targetRank; string description; string recipeId; ItemQuality minQuality; }`
  - `RankQuestSettings.TryGet(BlacksmithRank target, out QuestEntry entry)` — 항목이 있고 `recipeId`가 비어 있지 않고 `minQuality != None`일 때만 true.
  - `RankQuestRules(RankQuestSettings)`; `bool IsCleared(BlacksmithRank target, string craftedRecipeId, bool isUnknown, ItemQuality quality)`.
  - `BlacksmithRankSettings.GetBenefit(BlacksmithRank rank) : string` (없으면 빈 문자열).

- [ ] **Step 1: 실패하는 검증 작성.** `execute_code`로 아래를 한 번에 돌리도록 스크립트를 scratchpad에 작성한다(타입이 없으므로 컴파일 실패가 RED). 인메모리 `RankQuestSettings`(`ScriptableObject.CreateInstance`)에 `Skilled` 항목(`recipeId="iron_sword"`, `minQuality=ItemQuality.High`)을 넣고 단언:
  - `IsCleared(Skilled, "iron_sword", false, High) == true`, `Highest`도 true
  - `IsCleared(Skilled, "iron_sword", false, Medium) == false`
  - `IsCleared(Skilled, "other", false, Highest) == false`
  - `IsCleared(Skilled, "iron_sword", true, Highest) == false` (알 수 없는 제작품)
  - 항목 없는 `Artisan` → false, `recipeId` 빈 항목 → false, `minQuality=None` 항목 → false
  - `TryGet(Skilled)` true, `TryGet(Artisan)` false
  - 필드는 `SerializedObject`로 채운다(`private` 직렬화 필드 `entries`).
  Expected: 컴파일 단계에서 `RankQuestRules`를 찾을 수 없다는 오류로 실패.

- [ ] **Step 2: `RankQuestSettings` 구현.** `[CreateAssetMenu(menuName="BlackSmith/Rank Quest Settings")]` ScriptableObject, `[SerializeField] private List<QuestEntry> entries`(기본 4개: Novice, Skilled, Artisan, Grandmaster, `recipeId` 빈 문자열, `minQuality` None, description 빈 문자열), `TryGet`, `Load()`. 툴팁에 "비어 있으면 그 등급은 승급 불가(미정)"를 쓴다.

- [ ] **Step 3: `RankQuestRules` 구현.** `IsCleared`는 `isUnknown`이면 false, `TryGet` 실패면 false, `craftedRecipeId == entry.recipeId && quality >= entry.minQuality`(`ItemQuality` 정수 순서: Lowest < Low < Medium < High < Highest).

- [ ] **Step 4: `BlacksmithRankSettings`에 혜택 문구.** `RankEntry`에 `[TextArea] public string benefitText;`(기본 빈 문자열)와 `GetBenefit(BlacksmithRank)`를 추가한다. 기존 에셋은 필드가 비어 있는 채로 읽힌다(값은 사용자가 채움).

- [ ] **Step 5: 에셋 생성.** `Assets/Resources/Rank/RankQuestSettings.asset`을 만든다(`manage_scriptable_object` 또는 `execute_code`의 `AssetDatabase.CreateAsset`). 항목 4개 모두 비어 있다. 만들기 전 사용자에게 "빈 에셋 신규 생성"임을 알린다.

- [ ] **Step 6: 컴파일 확인 후 Step 1 검증 재실행.**
  Expected: 콘솔 error/warning 없음, 모든 단언 통과(`PASS` 출력).

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Rank Assets/Resources/Rank
git commit -m "feat : v3 승급 퀘스트 데이터와 판정 규칙"
```

---

### Task 2: 퀘스트 상태 계산

**Files:**
- Modify: `Assets/Scripts/Rank/RankQuestRules.cs`

**Interfaces:**
- Consumes: Task 1 `RankQuestRules`, `RankProgress`(기존).
- Produces:
  - `enum RankQuestState { None, Locked, Preparing, Active }` — `None` = 다음 등급 없음, `Locked` = 조건 미달(또는 필요 개수 미정), `Preparing` = 조건 충족인데 퀘스트가 비어 있음, `Active` = 퀘스트 진행 중.
  - `RankQuestState GetState(RankProgress progress)`.
  - `bool TryGetActive(RankProgress progress, out RankQuestSettings.QuestEntry entry)` — 상태가 `Active`일 때만 true.

- [ ] **Step 1: 실패하는 검증 작성.** 인메모리 설정으로: `HasNext=false` → `None`; `IsMet=false` → `Locked`; 필요 개수 -1(`IsDecided=false`) → `Locked`; 조건 충족 + 퀘스트 비어 있음 → `Preparing`; 조건 충족 + 퀘스트 있음 → `Active`이고 `TryGetActive` true. `RankProgress`는 생성자로 직접 만든다.
  Expected: `GetState` 없음으로 컴파일 실패.

- [ ] **Step 2: 구현.** `RankQuestRules`에 위 두 멤버를 추가한다(`TryGet(progress.Next, ...)` 사용).

- [ ] **Step 3: 컴파일 + 검증 재실행.** Expected: error/warning 없음, `PASS`.

- [ ] **Step 4: Commit** (`feat : 승급 퀘스트 상태 계산`)

---

### Task 3: RankController 퀘스트 연결

**Files:**
- Modify: `Assets/Scripts/Rank/RankController.cs`

**Interfaces:**
- Consumes: Task 1·2 전체, `CraftingController.CraftFinished(CraftResult)`, `CraftResult.Recipe/IsUnknown/Quality`, `CraftRecipeData.Id`, `PlayerData.SetRank(BlacksmithRank) : bool`, `PlayerData.IsTutorialPending`.
- Produces:
  - `event Action<BlacksmithRank, BlacksmithRank> Promoted` (이전 등급, 새 등급).
  - `RankQuestState GetQuestState()`; `bool TryGetActiveQuest(out RankQuestSettings.QuestEntry entry)`.
  - `allowDirectPromotion` 필드 제거.

- [ ] **Step 1: 실패하는 검증 작성.** 플레이 모드에서 `RankController`를 찾아 임시 설정으로 시나리오를 돌린다(설정 에셋은 건드리지 않고, 리플렉션 대신 `Resources`의 `RankQuestSettings`/`BlacksmithRankSettings`를 **런타임 복제본**으로 바꿀 수 없으므로, 에셋 값을 `SerializedObject`로 임시 변경하고 종료 시 반드시 원복한다. 변경 전 값을 로그에 남긴다). 시나리오:
  - 견습, 초급 필요 개수를 0/0, 초급 퀘스트 `iron_sword`/`Low`로 임시 설정 → `GetQuestState() == Active`.
  - 합성 `CraftResult`(일반 레시피 `iron_sword`, 품질 Medium)로 `CraftFinished` 상당 호출 → 등급 유지(Apprentice).
  - 품질 High → `Novice`로 승급하고 `Promoted(Apprentice, Novice)`가 정확히 1번.
  - 숙련 조건도 0/0이어도 한 번의 제작으로 `Novice`까지만(한 단계).
  - 퀘스트 항목이 빈 상태에서 조건 충족 → 승급 없음, `Preparing`.
  - 알 수 없는 제작품(`IsUnknown`)·튜토리얼 대기 중 → 승급 없음.
  합성 결과를 만들기 어려우면 `CraftingController`의 `CraftFinished` 호출 대신 `RankController`에 `internal`/`public` `HandleCraftResult(CraftResult)`를 두고(이벤트 핸들러가 이를 호출) 그것을 직접 호출한다.
  Expected: 현재는 직접 승급이 켜져 있어 조건 충족만으로 승급되므로 단언 실패.

- [ ] **Step 2: 구현.**
  - `allowDirectPromotion` 필드와 툴팁 삭제. 클래스 요약 주석을 퀘스트 방식으로 수정.
  - `Start`에서 `RankQuestSettings.Load()`로 `questRules` 생성(없으면 경고 로그 + 승급 불가).
  - 제작 종료 처리: 튜토리얼 대기 중이면 승급하지 않는다. 아니면 `player.Rank` 기준으로 `GetProgress()`가 `IsMet`이고 `questRules.IsCleared(progress.Next, result.Recipe?.Id, result.IsUnknown, result.Quality)`이면 `player.SetRank(progress.Next)`로 **한 단계만** 올리고 `Promoted?.Invoke(이전, 새)`. 이후 `Changed`를 낸다. `OnRankChanged`(스킵 장비 동기화)는 기존 그대로.
  - `Start`/등급 로드 시점에는 승급 판정을 하지 않고 `Changed`만 낸다.

- [ ] **Step 3: 컴파일 + 검증 재실행.** 끝에 임시 변경한 에셋 값을 원복하고 `git status`로 두 설정 에셋 diff가 없음을 확인한다.
  Expected: 단언 모두 통과, 에셋 diff 없음, 플레이 모드 종료.

- [ ] **Step 4: Commit** (`feat : 승급을 퀘스트 클리어로 전환(개발용 직접 승급 제거)`)

---

### Task 4: HUD 퀘스트 줄

**Files:**
- Modify: `Assets/Scripts/UI/RankHud.cs`, `Assets/Scenes/Game.unity`(`RankHud` 높이)

**Interfaces:**
- Consumes: Task 3 `GetQuestState()`, `TryGetActiveQuest`.
- Produces: HUD 표시 규칙 — `Locked` = 기존 진행도 줄, `Active` = `승급 퀘스트: <설명>`(설명이 비어 있으면 `승급 퀘스트: <레시피 id> <품질 이름> 이상` 임시 표기), `Preparing` = `승급 퀘스트 준비 중`, `None` = `최고 등급`.

- [ ] **Step 1: `Refresh`의 line2를 위 규칙으로 바꾼다.** 품질 이름은 `ItemQualityInfo.DisplayName`. 레시피 이름이 필요하면 `RecipeDatabase`로 결과 아이템 이름을 쓰되 없으면 id.

- [ ] **Step 2: 컴파일 후 Game 씬 검증.** 플레이 모드에서 4가지 상태를 임시 에셋 값(Task 3과 같은 원복 규칙)으로 만들어 `label.text`를 읽어 단언. 한 줄이 늘어날 수 있으므로 `RankHud` 높이(현재 620×90)가 3줄에 충분한지 확인하고, 글자가 잘리면 높이를 늘린다. 카메라 RenderTexture 캡처(스크래치패드)로 겹침을 눈으로 본다. 확인하지 못한 것(에디터 포커스 없는 렌더 한계)은 보고에 쓴다.
  Expected: 4개 상태 문구 일치, 콘솔 error/warning 없음, Missing Script 0.

- [ ] **Step 3: Commit** (씬 변경이 있으면 사용자가 저장을 요청했을 때만 포함; 아니면 스크립트만 커밋하고 씬 변경 사실을 보고).

---

### Task 5: 승급 팝업

**Files:**
- Create: `Assets/Scripts/UI/RankPromotionPopup.cs`
- Modify: `Assets/Scenes/Game.unity`(`GameUICanvas` 아래 팝업 오브젝트, 자체 `Canvas` order 22 + `GraphicRaycaster`)

**Interfaces:**
- Consumes: `RankController.Promoted`, `CraftResultPopup.IsOpen`/`Closed`, `BlacksmithRankSettings.GetName/GetBenefit`.
- Produces: `RankPromotionPopup`(`[SerializeField]`: `RankController controller`, `CraftResultPopup resultPopup`, `Text titleLabel`, `Text benefitLabel`, `Button confirmButton`).

- [ ] **Step 1: 구현 규칙.**
  - `CraftResultPopup`과 같은 구독 패턴: 오브젝트는 씬에서 활성으로 두고 `Start`에서 구독 후 숨긴다. `Awake`에서 OS 폰트 적용.
  - `Promoted(from, to)` 수신 시 `resultPopup`이 열려 있으면(`IsOpen`) 대기 상태로 저장하고 `resultPopup.Closed`에서 표시, 열려 있지 않으면 즉시 표시. 대기 중 승급이 또 와도 가장 최근 하나만 표시되도록 하지 않고 순서대로 큐에 쌓아 하나씩 보인다(최대 4개).
  - 표시: `대장장이 등급: 초급 → 숙련`(이름은 `BlacksmithRankSettings.GetName`), 혜택 문구가 비어 있으면 `benefitLabel` 오브젝트를 숨긴다. 확인 버튼으로 닫으면 큐의 다음 것을 보인다.
  - `confirmButton`은 `ButtonLabelPress` 구성(CLAUDE.md UI Buttons)을 따른다. 비주얼(스프라이트·색)은 임시이며 **[미정]** 주석을 단다.
  - 튜토리얼 자막(21)과 겹쳐도 22가 위. 제작 패널 입력 차단은 별도로 하지 않는다(확인 버튼 배경 클릭 차단용 전체 화면 반투명 이미지는 `raycastTarget`으로 둔다).

- [ ] **Step 2: 씬 구성.** 기존 `CraftResultPopup` 구조를 참고해 `GameUICanvas`에 `RankPromotionPopup`을 만든다(Button은 `TextBTN_Medium` / `TextBTN_Medium_Pressed` SpriteSwap, `Label` 자식 +20 규칙). 씬 저장은 하지 않는다.

- [ ] **Step 3: 검증(플레이 모드).**
  - 결과 팝업이 닫힌 상태에서 `Promoted`를 발생시키면 팝업이 즉시 활성, 텍스트가 `대장장이 등급: 견습 → 초급`.
  - 결과 팝업을 연 상태에서 발생시키면 승급 팝업은 아직 비활성이고, 결과 팝업 확인 후 활성.
  - 혜택 문구 비어 있음 → 줄 숨김, 값 있음 → 표시.
  - 연속 2번 발생 → 확인 2번에 걸쳐 차례로 표시.
  - 콘솔 error/warning 없음, Missing Script 0, 플레이 모드 종료.
  Expected: 모두 일치. 실제 렌더 모습과 Title→Game 흐름은 포커스 한계로 확인 못 할 수 있음을 보고한다.

- [ ] **Step 4: Commit** (`feat : 승급 팝업(결과 팝업을 닫은 뒤 표시)`; 씬은 저장 요청이 있을 때만).

---

### Task 6: 문서와 마무리

**Files:**
- Modify: `docs/V3.md`, `docs/Roadmap.md`, `README.md`, `CLAUDE.md`, `docs/superpowers/specs/2026-11-19-v3-rank-quest-design.md`(상태를 "승인됨, 구현됨"으로)

- [ ] **Step 1:** `docs/V3.md`: 출하 순서 3번을 구현됨으로, §2 승급 흐름을 현재 규칙으로, 퀘스트 구현 파일 목록과 미정 목록(퀘스트 내용·혜택 문구·팝업 비주얼) 갱신. `Roadmap.md` 8번 줄에서 승급 퀘스트를 구현됨(내용 미정)으로. `README.md` 상태 반영. `CLAUDE.md` "Rank & skip gear architecture"에서 `allowDirectPromotion` 문장을 퀘스트 설명(`RankQuestSettings`, `RankQuestRules`, `Promoted`, `RankPromotionPopup` order 22, 비어 있으면 승급 불가)으로 교체.
- [ ] **Step 2:** 컴파일 확인, 두 씬(Title, Game) Missing Script·콘솔 확인 후 결과 보고(확인한 것과 못 한 것 분리).
- [ ] **Step 3: Commit** (`docs : v3 승급 퀘스트 구현 반영`). 개인 설정 파일은 제외.

---

## Self-Review

- 스펙 커버리지: 클리어 규칙(T1·T3), 자동 시작/HUD(T2·T4), 한 단계 승급·튜토리얼·알 수 없는 제작품(T3), 팝업과 표시 시점(T5), 직접 승급 제거(T3), 혜택 문구(T1·T5), 문서(T6). 미정 값은 빈 에셋으로 남김.
- 타입 일관성: `RankQuestSettings.QuestEntry`, `RankQuestState`, `Promoted(from, to)`, `GetQuestState/TryGetActiveQuest` 이름을 전 태스크가 동일하게 쓴다.
- 스킵 장비와 최상품 퀘스트는 자동 처리하지 않는다(스펙대로 문구 안내는 사용자 몫).
