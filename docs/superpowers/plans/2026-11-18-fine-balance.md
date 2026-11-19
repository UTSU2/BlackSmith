# 정밀 밸런스 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 상품 이상 완성품에 등급 배율을 곱하고, Uncommon~Legendary 광물 가격을 올린다.

**Architecture:** 판매가는 팔 때 `CraftedItemData.GetSellUnitPrice(variant)`가 계산하므로 여기서 품질이 상품 이상이면 `ItemGradeInfo.CraftedGradeMultiplier(variant.Grade)`를 곱한다. 광물 가격은 공식을 유지하고 에셋의 Common 기준가(`buyPrice`, `sellPrice`)만 바꾼다.

**Tech Stack:** Unity 6 (C# 9), ScriptableObject, UnityMCP(`execute_code`, `read_console`, `refresh_unity`).

**Spec:** `docs/superpowers/specs/2026-11-18-fine-balance-design.md` (승인됨). 상위: `docs/V2.md` §5 "정밀 밸런스".

## Global Constraints

- 등급 배율: Common 1, Uncommon 1.1, Rare 1.3, Unique 1.7, Legendary 2.5. **상품(High)·최상품(Highest)에만** 곱한다.
- 판매가 계산은 decimal로 `ingredientValue × 품질 배율 × (등급 배율)` 후 `PriceRounding.Round` 한 번. `fixedSellPrice > 0`이면 그 값이 먼저다(999는 50 그대로).
- 광물 Common 기준가(Uncommon / Rare / Unique / Legendary): 구매 150 / 233 / 400 / 600, 되팔기 75 / 116 / 200 / 300 → 실제 구매 180 / 350 / 720 / 1200, 되팔기 90 / 174 / 360 / 600. Common 광물(100 / 50)은 그대로.
- 네임스페이스 block style, `ItemGradeInfo`는 `BlackSmith.Items`. 새 Singleton 금지. 에셋 필드는 `SerializedObject`로 private 이름(`buyPrice`, `sellPrice`)에 쓴다.
- 커밋·씬 저장은 사용자 요청 시에만. `Assets/Settings/*`, `Assets/vFavorites/*`, `ProjectSettings/UnityConnectSettings.asset`은 커밋하지 않는다. 커밋 메시지 끝 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 검증은 `execute_code`(CodeDom C# 6). 스크립트 변경 뒤 컴파일 체크(console clear → refresh force → wait → console).

## Review Focus

1. **경계 품질:** 중품(29점)은 배율 없음, 상품(30점)부터 배율 (Task 1 표 검증).
2. **알 수 없는 제작품:** 고정가가 등급 배율보다 먼저라 50 그대로 (Task 1).
3. **광물 외 아이템 불변:** 완성품 에셋·Common 광물의 가격 필드가 바뀌지 않는다 (Task 2 git diff).
4. **튜토리얼 지급금:** 철 원석(Common) 기준이라 100 그대로 (Task 2).
5. **상점 정렬·표시:** 판매 목록 가격이 같은 메서드를 거치므로 새 값으로 보인다 (Task 1 플레이 확인).

---

### Task 1: 완성품 등급 배율

**Files:**
- Modify: `Assets/Scripts/Items/ItemGrade.cs` (`ItemGradeInfo`)
- Modify: `Assets/Scripts/Items/CraftedItemData.cs` (`GetSellUnitPrice`, 클래스 주석)

**Interfaces:**
- Produces: `ItemGradeInfo.CraftedGradeMultiplier(ItemGrade grade) : decimal` (1, 1.1, 1.3, 1.7, 2.5); `ItemQualityInfo`는 그대로.

- [ ] **Step 1: 실패 확인 (`execute_code`).** 재료 합산 1000으로, 등급별 완성품 하나씩(`temp_gear_common` … `temp_gear_legendary`)에 `ItemVariant(grade, quality, 1000)`로 `GetSellUnitPrice`를 구해 기대표와 비교: Common 700/1000/1200/1500/2000, Uncommon 700/1000/1200/1650/2200, Rare 700/1000/1200/1950/2600, Unique 700/1000/1200/2550/3400, Legendary 700/1000/1200/3750/5000. Expected: 지금은 상품·최상품의 Uncommon 이상 8칸 불일치.
- [ ] **Step 2: 구현.** `CraftedGradeMultipliers` 배열과 메서드 추가. `GetSellUnitPrice`: 고정가 → 아니면 `value × 품질 배율 × (quality >= High ? 등급 배율 : 1)` 반올림.
- [ ] **Step 3: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 4: 재검증.** Step 1 표 25칸 일치, 반올림 확인(Rare 상품 재료 333 → 333×1.5×1.3 = 649.35 → 649, Uncommon 상품 재료 155 → 255.75 → 256), 999는 품질 5종 모두 50.
- [ ] **Step 5: 플레이 확인.** Game 씬 플레이 모드에서 Legendary 상품 완성품(`ItemVariant(Legendary, High, 1200)`)을 소지품에 넣고 `ShopService.GetSellUnitPrice`가 4500, 판매 목록 항목 가격 글자 `4,500 G`. 플레이 모드 종료.

### Task 2: 광물 가격

**Files:**
- Modify: `Assets/Resources/Items/Minerals/*.asset` 중 Uncommon 12 / Rare 9 / Unique 6 / Legendary 3 (총 30개, `buyPrice`·`sellPrice`만)

- [ ] **Step 1: 대상 확인 (`execute_code`, 읽기 전용).** 등급별 광물 수와 현재 기준가(130/65, 200/100, 300/150, 450/225)를 출력. 기준가가 등급 안에서 다른 광물이 있으면 멈추고 보고.
- [ ] **Step 2: 변경 (`execute_code`, 에셋 30개 — 실행 전 알림).** 등급별 새 기준가를 `SerializedObject`로 기록, `SaveAssets`.
- [ ] **Step 3: 검증.** 48종 모두: 구매가 100/180/350/720/1200, 되팔기 50/90/174/360/600 (`BuyPrice`, `GetSellUnitPrice(ItemVariant(grade))`). `TutorialConfig.CalculateStartMoney()` == 100. 레시피 재료 합산 가격의 등급별 범위 출력(보고용). `git diff --stat`이 광물 30개 × 2줄뿐.
- [ ] **Step 4: Missing Script(Title, Game) 0, 콘솔 오류·경고 없음.**

### Task 3: 문서

- [ ] **Step 1:** `docs/V2.md`(1장 5번, §5 표·"정밀 밸런스" 상태 → 구현 완료), `docs/GameDesign.md`(§5 가격 두 항목 "미구현" → "구현됨", 표), `CLAUDE.md`(가격 문장·v2 순서), `docs/Roadmap.md`, `README.md`(광물 가격·v2 목록)를 구현 완료로 갱신.
- [ ] **Step 2:** `grep -n "미구현" docs/V2.md docs/GameDesign.md`로 정밀 밸런스 관련 남은 표시 없음 확인.
- [ ] **Step 3: 커밋 (요청 시).** 코드·에셋 `feat : 정밀 밸런스 (상품 이상 등급 배율, 광물 가격 인상)`, 문서 `docs : 정밀 밸런스 설계·계획과 구현 반영`.
