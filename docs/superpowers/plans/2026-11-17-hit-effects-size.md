# 적중 이펙트 크기 조정 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (or subagent-driven-development) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 단조 이펙트를 원 바깥으로 퍼지는 도넛형으로 바꿔 원을 가리지 않게 하고, 연마 이펙트를 흠집 크기에 맞춘다.

**Architecture:** 단조는 가운데가 뚫린 링 텍스처를 쓰고, 링의 안쪽 구멍이 판정 원(안쪽 원) 가장자리에서 시작해 바깥으로만 커지게 한다. 판정별 차이는 크기 배율이 아니라 **판정별 프리팹**(퍼지는 정도·불꽃 수·밝기)으로 낸다. 크기 배율은 "실제 원/흠집 지름 ÷ 프리팹 기준 지름"으로 정해, 난이도 설정으로 원 크기가 바뀌어도 구멍이 원에 맞는다.

**Tech Stack:** Unity 6, uGUI(Screen Space - Camera), ParticleSystem, URP Particles/Unlit, UnityMCP.

**Spec:** `docs/superpowers/specs/2026-11-17-hit-effects-design.md` + 사용자 요청(2026-11-17): "단조: 원 바깥으로 이펙트가 나오게, 원은 가려지지 않도록 도넛 형태. 연마: 원 크기 정도의 이펙트, 도넛이 아니어도 된다."

## 현재 상태 (조사 결과)

- 단조 PlayArea 1600×900, 미니게임 1단위 = 100px. 판정 원(`Target`, 안쪽 원) 반지름 `targetRadius` 0.8 → **지름 160px**. 판정 뒤 0.3초 동안 이 원이 판정 색으로 남는다.
- 지금 단조 빛: 꽉 찬 원형 빛(`SoftGlow`) 시작 크기 160 × (0.6→1.2) = 96→192px → **원 전체를 덮는다**. 불꽃 12개는 반지름 10 안쪽에서 시작해 원 위를 지나간다.
- 연마 흠집 반지름 `flawRadius` 0.9 → **지름 180px**. 지금 연마 빛 80×(0.8→1.1) ≈ 64~88px, 반짝 점은 150px/s로 흩어져 흠집보다 작고 산만하다.
- 판정별 크기는 지금 뷰가 프리팹 전체를 1 / 0.65 / 0.35배로 줄인다. 도넛에서는 전체를 줄이면 구멍이 원보다 작아져 원을 가리므로 이 방식은 쓸 수 없다.

## Global Constraints

- 원(단조 `Target`)은 이펙트가 재생되는 동안 **어느 순간에도 가려지지 않는다**: 링 텍스처의 구멍 안은 알파 0, 링 입자의 크기는 시작 이후 줄어들지 않는다(크기 곡선 최솟값 ≥ 1), 불꽃은 원 가장자리에서 바깥으로만 나간다.
- 판정별 차이 유지: Perfect > Good > 눌렀지만 Miss (퍼지는 정도·불꽃). 놓친 Miss·빈 곳 클릭은 이펙트 없음. 색은 단조 금색 `(1, 0.82, 0.35)`, 연마 은색 `(0.85, 0.88, 0.95)` 그대로.
- 아래 수치는 모두 **임시값**(최종 아트 [미정]). 프리팹 Inspector에서 조정 가능해야 한다.
- 이펙트 정렬은 그대로: 레이어 `UI`, 순서 10, `scalingMode = Hierarchy`, 루트 `stopAction = Destroy`, Graphic·Collider 없음, 재생 0.5초 안팎.
- 미니게임 모델·판정·효과음은 바꾸지 않는다. Title 씬은 건드리지 않는다.
- 씬 저장·커밋은 요청 시에만. 커밋 대상에서 `Assets/Settings/*`, `ProjectSettings/UnityConnectSettings.asset`, `Assets/vFavorites/*` 제외.
- 검증은 `execute_code`(CodeDom C# 6) + 컴파일 체크 + 스크린샷. 씬 코드 실행 전 활성 씬이 Game인지 확인(아니면 Game을 연다).

## Review Focus

1. **판정 원이 가려지는가**: 링이 처음 나타나는 순간과 커지는 동안 모두 구멍이 원 지름 이상인지 (Task 1 Step 4 수치 검사, Task 2 스크린샷).
2. **원 크기가 바뀌는 설정**: `targetRadius`/`flawRadius`를 바꿔도 링 구멍·연마 빛이 새 크기를 따라가는지 (Task 2 Step 5: 반지름을 바꿔 배율 확인).
3. **연마 마지막 흠집**: 마지막 흠집을 지운 직후 단계가 끝나 화면이 닫혀도 이펙트가 남지 않는지 (기존 `Hide → Clear` 유지 확인).

---

### Task 1: 링 텍스처와 판정별 단조 프리팹, 연마 프리팹 조정

**Files:**
- Create: `Assets/Effects/Textures/SoftRing.png` (128×128, 흰색. 정규화 반지름 r(0=중심, 1=가장자리)에 대해 r < 0.5 이면 알파 0, 0.5 → 0.58 에서 0 → 1로 부드럽게 오르고, 0.58 → 1 에서 1 → 0으로 줄어든다. Clamp, 밉맵 없음, Alpha Is Transparency)
- Create: `Assets/Effects/HitRing.mat` (`HitEffect.mat`과 같은 설정, Base Map = `SoftRing`)
- Create: `Assets/Effects/ForgeHitPerfect.prefab`, `ForgeHitGood.prefab`, `ForgeHitMiss.prefab`
- Modify: `Assets/Effects/PolishSparkle.prefab`
- Delete: `Assets/Effects/ForgeHitGlow.prefab` (Task 2에서 참조를 바꾼 뒤. 다른 참조 없음을 확인하고 지운다)

**Interfaces:**
- Produces: 기준 지름 `ReferenceDiameter = 160`(단조), `180`(연마) — 프리팹은 이 원 크기에 맞춰 만든다. 뷰는 `실제 지름 / 기준 지름`을 배율로 넘긴다.

**단조 프리팹 공통** (기준 원 지름 D = 160px)
- 루트 = 링 1개: 재질 `HitRing`, 시작 크기 `2D = 320`(구멍 지름 = 320 × 0.5 = 160 = 원), 크기 곡선 1 → k (줄지 않음), 알파 a → 0, 속도 0.
- 자식 `Sparks`: 모양 Circle, 반지름 `D/2 = 80`, `radiusThickness = 0`(가장자리에서만 생성), 바깥 방향 속도, 크기 10~12, 알파 1 → 0.

| 프리팹 | 링 크기 곡선 k | 링 시작 알파 a | 링 수명 | 불꽃 수 | 불꽃 속도 | 불꽃 수명 |
|---|---|---|---|---|---|---|
| ForgeHitPerfect | 1.6 | 1.0 | 0.35 | 16 | 320 | 0.4 |
| ForgeHitGood | 1.35 | 0.85 | 0.3 | 10 | 230 | 0.35 |
| ForgeHitMiss | 1.15 | 0.6 | 0.25 | 5 | 140 | 0.3 |

(링 바깥 지름 = 320 × k → Perfect 512 / Good 432 / Miss 368px, 원 지름 160 기준. 모두 임시값)

**연마 프리팹** (기준 흠집 지름 D = 180px, 도넛 아님)
- 루트 = 원형 빛 1개: `SoftGlow`, 시작 크기 `D = 180`, 크기 곡선 0.9 → 1.15, 알파 1 → 0, 수명 0.35.
- 자식 `Sparkles`: 모양 Circle, 반지름 `D/2 = 90`, `radiusThickness = 1`(흠집 안 전체), 속도 40(거의 제자리), 크기 12, 크기 곡선 1 → 0, 개수 14, 수명 0.45.

- [ ] **Step 1: 링 텍스처·재질 생성** (`execute_code`).
- [ ] **Step 2: 단조 판정별 프리팹 3개 생성, 연마 프리팹 수정** (위 표 값, 공통 규칙은 Global Constraints).
- [ ] **Step 3: 컴파일·콘솔 확인.** Expected: 오류·경고 없음.
- [ ] **Step 4: 프리팹 검사** (`execute_code`). Expected 모두 true:
  - 단조 3개: 링 `startSize × 0.5 ≥ 160`(구멍 ≥ 원), 링 크기 곡선 최솟값 ≥ 1, 불꽃 shape 반지름 ≥ 80이고 `radiusThickness == 0`, 불꽃 시작 속도 > 0. 링 바깥 지름이 Perfect > Good > Miss. 텍스처 `SoftRing`에서 r < 0.5 픽셀 알파가 모두 0.
  - 연마: 빛 시작 크기 180, `Sparkles` shape 반지름 90.
  - 모두: 레이어 `UI` 순서 10, `Hierarchy`, 루트 `Destroy`, loop 없음, Graphic·Collider 없음, 시작 색이 금색/은색.

### Task 2: 뷰가 판정별 프리팹과 원 크기 배율을 쓰게 변경

**Files:**
- Modify: `Assets/Scripts/UI/HitEffectSpawner.cs`
- Modify: `Assets/Scripts/UI/ForgeMinigameView.cs` (적중 이펙트 필드, `OnCircleJudged`)
- Modify: `Assets/Scripts/UI/PolishMinigameView.cs` (`OnFlawCleared`)
- Modify: `Assets/Scenes/Game.unity` (단조 뷰에 프리팹 3개 연결)

**Interfaces:**
- Consumes: Task 1 프리팹, 기준 지름 160/180.
- Produces:
  - `HitEffectSpawner.Play(ParticleSystem effectPrefab, Vector2 localPosition, float scale)` — 지정 프리팹으로 띄운다. 기존 `Play(Vector2, float)`는 기본 `prefab`으로 이 메서드를 부른다. 프리팹이 null이거나 scale ≤ 0이면 아무것도 안 함.
  - `ForgeMinigameView`: `perfectEffectScale`/`goodEffectScale`/`missEffectScale` 필드를 지우고 `[SerializeField] ParticleSystem perfectEffect, goodEffect, missEffect`(Tooltip: 기준 지름 160px 원에 맞춘 도넛형 프리팹), `[SerializeField] float effectReferenceDiameter = 160f`를 둔다. 배율 = `targetRadius × 2 × (PlayArea 폭 / AreaSize.x) ÷ effectReferenceDiameter`.
  - `PolishMinigameView`: `[SerializeField] float effectReferenceDiameter = 180f`. 배율 = `flawRadius × 2 × (PlayArea 폭 / AreaSize.x) ÷ effectReferenceDiameter`.

- [ ] **Step 1: 동작 검사 코드를 먼저 실행해 실패를 확인** (플레이 모드, 기존 코드 기준): 단조 Perfect 판정 이펙트의 링 구멍 지름(`링 startSize × 0.5 × lossyScale / PlayArea lossyScale`)이 원 지름 160 이상인지 → 지금은 링이 없으므로 실패.
- [ ] **Step 2: `HitEffectSpawner`에 프리팹 지정 `Play` 추가.**
- [ ] **Step 3: 두 뷰 수정** (Interfaces 그대로). 놓친 Miss 무시, `Hide`/`Show`의 `Clear`는 그대로.
- [ ] **Step 4: 컴파일 체크 → 씬 연결** (단조 뷰 `perfectEffect`/`goodEffect`/`missEffect`에 Task 1 프리팹. 스포너 기본 `prefab`은 연마 쪽만 사용하므로 단조 스포너의 기본 프리팹은 `ForgeHitPerfect`로 둔다). 그 뒤 `ForgeHitGlow.prefab`을 참조하는 곳이 없음을 확인하고 삭제. Missing Script 0.
- [ ] **Step 5: 동작 검증** (플레이 모드). Expected 모두 통과:
  - 단조 Perfect/Good/눌렀지만 Miss → 각각 해당 프리팹 1개, 배율 1(기본 설정), 위치 원 중심 1px 이내. 놓친 Miss → 0개.
  - 각 이펙트의 링 구멍 지름(화면 기준) ≥ 원 지름, 불꽃 생성 반지름 ≥ 원 반지름.
  - 원 크기 변경 추종: 시험용 `ForgeSettings.targetRadius = 1.2`로 판 하나 → 배율 1.5, 구멍 ≥ 240px. 연마 `flawRadius = 0.6` → 배율 0.667.
  - 연마: 흠집 제거 → 1개, 빛 지름이 흠집 지름의 0.9~1.15배.
  - `Hide` 뒤 이펙트 0개. 끝나면 플레이 모드 종료.
- [ ] **Step 6: 스크린샷.** 단조 Perfect·Good·Miss 판정 직후(입자 0.05초, 0.2초 진행)와 연마 흠집 제거 직후를 찍어 원이 가려지지 않는지, 연마 빛이 흠집 크기인지 확인해 사용자에게 보여 준다.
- [ ] **Step 7: 정리 검증.** Game·Title Missing Script 0, 콘솔 오류·경고 없음(수동 `Camera.Render()`의 RenderPass 메시지는 제외하고 보고).

### Task 3: 문서

**Files:**
- Modify: `docs/V2.md` §4 (단조 도넛형·판정별 프리팹, 연마 흠집 크기, 현재 임시값)
- Modify: `CLAUDE.md` Hit effects architecture (판정별 프리팹 3개, 기준 지름 배율, `Play(prefab, …)`)
- Modify: `README.md` 적중 이펙트 줄 (필요 시)

- [ ] **Step 1: 문서 갱신.**
- [ ] **Step 2: 씬 저장·커밋은 사용자 요청 시.** 대상: `Assets/Effects`(새 텍스처·재질·프리팹, 수정·삭제 포함), 스크립트 3개, `Game.unity`, 문서, 이 계획.
