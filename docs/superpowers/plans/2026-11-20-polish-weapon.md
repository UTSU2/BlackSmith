# 연마 미니게임 개정 (무기 위의 흠집 + 숫돌) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 연마 미니게임에 검 이미지를 깔고, 흠집이 검 위에만 생기며, 누르는 동안 숫돌이 포인터를 따라다니게 한다. 문지르기 규칙·난이도는 그대로 둔다.

**Architecture:** 로직에는 흠집 생성 영역(`PolishSettings.spawnArea`, 게임 공간 단위)만 추가해 이미지를 모르게 둔다. 이미지·숫돌·흠집 변형은 뷰와 씬이 맡는다. 이미지는 gpt-image로 만들고 1~5단계 방식(투명 PNG, 임포트, 씬 구조 보정)을 따른다.

**Tech Stack:** Unity 6 uGUI, C#, UnityMCP(`execute_code`, 렌더링 검증), gpt-image 스킬(Node.js 26, Codex CLI, ChatGPT 로그인 완료).

**Spec:** `docs/superpowers/specs/2026-11-20-polish-weapon-design.md`

## Global Constraints

- 문지르기 규칙·난이도·시간 제한(5초)·흠집 개수(3→6)·흠집 반지름 0.9·완성도 계산은 바꾸지 않는다 (`docs/GameDesign.md` §4).
- `PolishSettings.spawnArea` 기본값은 `Rect(2, 2, 12, 5)` (기존 동작과 동일). 씬 값만 검에 맞춘다. `margin` 필드는 남기고 쓰지 않는다.
- 로직(`BlackSmith.Crafting`)은 이미지·씬 좌표를 모른다. 단위는 게임 공간(16×9, 1유닛 = 100px, 원점 왼쪽 아래).
- 이미지는 `--background transparent`, 스타일 기준 `generated-images/heat_bar_frame.png`를 `--reference`로 붙인다. 흠집 변형 3종은 흰색/무채색·외곽선 없음(틴트 사용), 숫돌·검은 만화풍 외곽선.
- 코딩 규칙(CLAUDE.md): PascalCase 클래스·메서드, private camelCase, 직렬화 `[SerializeField]`, 네임스페이스 블록 스타일, 새 Singleton 금지. 버튼 아님.
- 씬 저장은 사용자가 요청할 때만. Game 씬은 additive로 열어 작업하고 끝나면 상태를 보고한다. `.meta` 삭제·재생성 금지.
- 검증: 스크립트 변경 뒤 컴파일 체크(`read_console clear` → `refresh_unity compile=request` → 대기 → 오류·경고 확인), 씬 변경 뒤 Missing Script·참조·콘솔 확인, 확인한 것과 못 한 것을 함께 보고.
- 이미지 생성은 Codex 샌드박스 쓰기 제한으로 `~/.codex/generated_images/`에 저장되므로 거기서 `generated-images/`로 복사한다.

## Review Focus

- 난이도 10(흠집 6개)에서 모든 흠집 중심이 `spawnArea` 안이고, 흠집 원(반지름 0.9)이 검의 날 밖으로 나가지 않는다 (여러 씨드로 확인).
- 기본 `spawnArea`(미설정 새 설정)로 만들면 기존과 같은 범위(x 2~14, y 2~7)에서 생긴다 (회귀).
- `spawnArea`가 비정상(폭 또는 높이 ≤ 0)이면 중앙(8, 4.5) 한 점으로 대체되어 죽지 않는다.
- 숫돌이 문지르는 흠집을 가려 진행(알파 감소)이 안 보이지 않는다.
- 뗌·시간 초과·끝·`Hide` 뒤에 숫돌이 남아 있지 않다. 다음 판에서 흠집 스프라이트가 인덱스에 맞게 다시 적용된다.
- `flawSprites`가 비었거나 1종이어도 동작한다 (템플릿 스프라이트 그대로).

## 파일 구조

- Modify: `Assets/Scripts/Crafting/PolishMinigame.cs` (`PolishSettings.spawnArea`, `PickPosition`, 클래스 주석)
- Modify: `Assets/Scripts/UI/PolishMinigameView.cs` (`flawSprites`, `whetstone`)
- Create: `Assets/Sprites/Minigame/Polish/polish_blade.png`, `polish_flaw_scratch.png`, `polish_flaw_dent.png`, `polish_whetstone.png` (+ `.meta`)
- Modify: `Assets/Scenes/Game.unity` (`PolishPanel`, `CraftingController.polishSettings.spawnArea`)
- Modify: `docs/GameDesign.md` §4 연마 행 문구
- Create: `generated-images/polish-weapon-jobs.json` (작업용, 비추적)

---

### Task 1: 흠집 생성 영역 (로직)

**Files:**
- Modify: `Assets/Scripts/Crafting/PolishMinigame.cs`

**Interfaces:**
- Produces: `PolishSettings.spawnArea` — `[SerializeField]` 아님, 기존 필드 스타일에 맞춰 `public Rect spawnArea = new Rect(2f, 2f, 12f, 5f);` (`[Tooltip]` 한글 설명). `PolishMinigame.Begin`이 흠집을 `spawnArea` 안(경계 포함)에 둔다.

- [ ] **Step 1: 실패 확인 스크립트.** `execute_code`로 `new PolishSettings { spawnArea = ... }` 컴파일이 되는지 시도해 **컴파일 오류**(필드 없음)를 본다. Expected: `CS1061` 또는 해당 필드 없음 오류.
- [ ] **Step 2: 필드 추가.** `PolishSettings`에 `public Rect spawnArea = new Rect(2f, 2f, 12f, 5f);`와 툴팁("흠집 중심이 놓일 수 있는 영역. 게임 공간 단위, 왼쪽 아래 기준. 기본값은 예전 동작(여백 2)과 같다.")을 추가. `margin`은 그대로 두고 툴팁에 "(미사용, 호환용)"을 덧붙인다.
- [ ] **Step 3: `PickPosition()` 수정.** `margin` 계산 대신 `spawnArea.xMin/xMax/yMin/yMax`를 쓴다. 폭 또는 높이가 0 이하이면 `AreaSize * 0.5f`를 반환. **(실행 중 판단, 설계 문서 §1)** 좁은 영역에서 30회 재시도 후 마지막 후보를 쓰면 6개가 크게 겹쳤으므로(10.3×1.6에서 500번 중 493번), 재시도를 400회(`PlacementAttempts`)로 늘리고 끝까지 못 찾으면 기존 흠집에서 가장 먼 후보를 쓴다. 첫 번째로 `flawRadius × 2.2` 이상 떨어진 후보를 쓰는 규칙과 난수 순서는 그대로.
- [ ] **Step 4: 클래스 주석 수정.** `PolishMinigame` 요약의 "사포질하듯"을 "숫돌로 갈듯이"로.
- [ ] **Step 5: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 6: 로직 검증 스크립트 (`execute_code`).** 아래를 모두 만족하는지 출력으로 확인한다.
  - 기본 설정 `new PolishSettings()`로 난이도 10, 씨드 0~199, `Begin(10)` → 모든 흠집 x∈[2,14], y∈[2,7].
  - `spawnArea = new Rect(2.65f, 3.4f, 10.7f, 2.2f)`로 같은 반복 → 모든 흠집 x∈[2.65,13.35], y∈[3.4,5.6], 흠집 개수 6.
  - `spawnArea = new Rect(0, 0, 0, 5)` → 모든 흠집 위치 == (8, 4.5).
  - 위 영역·난이도 10·씨드 1000개에서 원이 겹치는 판(최소 거리 < 1.8)이 2% 이하이고 최악 최소 거리 ≥ 1.5 (겹침 회피가 동작). 기본 영역에서는 예전 알고리즘(30회, 마지막 후보)과 씨드별 흠집 위치가 모두 같다(회귀).
  Expected: 4항목 모두 PASS 출력.
- [ ] **Step 7: 커밋.**

```bash
git add Assets/Scripts/Crafting/PolishMinigame.cs
git commit -m "feat : 연마 흠집 생성 영역(spawnArea) 설정 추가 (기본값은 기존 동작)"
```

### Task 2: 검 이미지

**Files:**
- Create: `Assets/Sprites/Minigame/Polish/polish_blade.png`

**Interfaces:**
- Produces: 스프라이트 `polish_blade` (Sprite/Single, PPU 100, 알파 투명, 압축 없음, maxTextureSize 4096)와 측정값: 이미지 크기, 날 직선 구간의 x 범위·y 범위(px, 이미지 왼쪽 아래 기준). Task 6이 `spawnArea`와 `Blade` 크기·위치를 계산하는 데 쓴다.

- [ ] **Step 1: 프롬프트 합의.** 스펙 §2 조건(가로 방향 넓고 곧은 날, 밝은 회색 강철, 손잡이 왼쪽·끝 오른쪽, 만화풍, 투명 배경, 이미지 가장자리까지 꽉 참)으로 프롬프트를 사용자에게 보이고 확인받는다. 사용자가 준 문구는 그대로 쓴다.
- [ ] **Step 2: 생성.** `node C:/Users/ejy10/.claude/skills/gpt-image/scripts/gpt_image.mjs generate --prompt "<문구>" --reference "generated-images/heat_bar_frame.png" --background transparent --out "generated-images/polish_blade.png"`. PowerShell PATH에 `C:\Program Files\nodejs;C:\Users\ejy10\AppData\Local\Programs\OpenAI\Codex\bin`을 더한다. 샌드박스 쓰기 오류가 나면 출력에 적힌 경로에서 복사한다. Expected: 투명 PNG(모서리 알파 0).
- [ ] **Step 3: 측정.** 불투명 영역 bbox로 크롭하고, 날 직선 구간(폭이 일정한 구간)의 x 범위와 두께(y 범위)를 열 단위 알파 스캔으로 측정해 기록한다. 날 두께가 이미지 높이의 대부분이 되도록 날이 넓어야 한다 (좁으면 재생성).
- [ ] **Step 4: 사용자 확인.** 결과를 보여주고 승인받는다. 불만이면 이전 출력을 `--edit-target`으로 수정해 재생성.
- [ ] **Step 5: 임포트.** `Assets/Sprites/Minigame/Polish/`로 복사, 임포터 설정, 콘솔 확인. Expected: 새 경고 없음.

### Task 3: 흠집 변형 2종

**Files:**
- Create: `Assets/Sprites/Minigame/Polish/polish_flaw_scratch.png`, `polish_flaw_dent.png`

**Interfaces:**
- Produces: 스프라이트 2개 (Sprite/Single, PPU 100). 기존 `polish_flaw`(녹)와 함께 `flawSprites` 3종 (순서: `polish_flaw`, `polish_flaw_scratch`, `polish_flaw_dent`).

- [ ] **Step 1: 프롬프트 작성(위임된 창작) 후 배치 생성.** 긁힌 자국 무리 / 찍히고 깨진 자국. 둘 다 흰색~회색 무채색, 외곽선 없음, 원에 가까운 형태로 이미지에 꽉 참, 가장자리 부드럽게 투명. 매니페스트로 2 job 배치 실행하고 샌드박스 경로에서 복사.
- [ ] **Step 2: 정사각 크롭·알파 확인.** 불투명 영역 정사각 크롭, 모서리 알파 0 확인.
- [ ] **Step 3: 사용자 확인 후 임포트.** 기존 `polish_flaw`와 같은 임포터 설정.

### Task 4: 숫돌 이미지

**Files:**
- Create: `Assets/Sprites/Minigame/Polish/polish_whetstone.png`

**Interfaces:**
- Produces: 스프라이트 `polish_whetstone` (Sprite/Single, PPU 100). 표시 크기는 Task 6에서 약 240×90으로 둔다. 접촉면(아래 변)이 이미지 아래 가장자리에 있도록 여백 없이 만든다 (pivot은 씬에서 아래 중앙).

- [ ] **Step 1: 프롬프트 합의 후 생성.** 가로로 긴 직육면체 회색 숫돌(젖은 느낌), 약간 비스듬한 정면 뷰, 만화풍 갈색 외곽선, 틴트 없음, 투명 배경, 여백 없음.
- [ ] **Step 2: 크롭·확인·임포트.** 불투명 영역 크롭, 사용자 확인, 임포트.

### Task 5: 뷰 변경 (흠집 변형 + 숫돌)

**Files:**
- Modify: `Assets/Scripts/UI/PolishMinigameView.cs`

**Interfaces:**
- Consumes: Task 1의 로직(변경 없음, 뷰는 `game.ActiveFlaw`, `game.IsFinished`, `PointerEventData`만 사용).
- Produces: `[SerializeField] private Sprite[] flawSprites;` (인덱스 `i % Length`, 비었으면 템플릿 스프라이트 유지)와 `[SerializeField] private RectTransform whetstone;` (누르는 동안 표시·포인터 추종, `Hide`/끝/뗌에서 숨김, 비워 두면 숫돌 없이 동작).

- [ ] **Step 1: 실패 확인.** `execute_code`로 `GetType().GetField("flawSprites", ...)`가 null임을 확인한다. Expected: null.
- [ ] **Step 2: 흠집 스프라이트 적용.** `Update()`의 흠집 그리기 루프에서 `flawSprites`가 비어 있지 않으면 `view.sprite = flawSprites[i % flawSprites.Length]`를 적용. 인덱스 기반이라 프레임마다 값이 같고 로직 난수에 영향이 없다.
- [ ] **Step 3: 숫돌 추종.** `OnPointerDown`/`OnDrag`에서 `TryToGamePosition`과 같은 좌표 변환으로 `PlayArea` 로컬 좌표를 구해 `whetstone.anchoredPosition`에 넣고 `SetActive(true)`. `OnPointerUp`, `Hide`, `Show`(초기), `game.IsFinished`일 때 `SetActive(false)`. `whetstone`이 null이면 모두 건너뛴다. `raycastTarget`은 코드에서 `false`로 설정(복제 시와 같은 방식).
- [ ] **Step 4: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 5: 커밋.**

```bash
git add Assets/Scripts/UI/PolishMinigameView.cs
git commit -m "feat : 연마 뷰에 흠집 변형 스프라이트와 숫돌 추종 추가"
```

### Task 6: 씬 연결과 확인

**Files:**
- Modify: `Assets/Scenes/Game.unity` (`PolishPanel`, `CraftingController`)
- Modify: `docs/GameDesign.md` (§4 연마 행)
- Modify: `docs/superpowers/specs/2026-11-20-polish-weapon-design.md` (결과 기록)

**Interfaces:**
- Consumes: Task 2~4 스프라이트와 측정값, Task 1 `spawnArea`, Task 5 `flawSprites`/`whetstone`.

- [ ] **Step 1: 변경 고지.** CLAUDE.md 규칙에 따라 mutating 코드를 실행하기 전에 무엇을 바꾸는지 사용자에게 알린다. Game 씬은 additive로 연다 (이미 열려 있으면 그대로).
- [ ] **Step 2: 배경·검.** `PolishPanel/PlayArea`의 `Image` 색을 (0.08,0.05,0.04,0.55)로. 자식 `Blade`(`Image`, `polish_blade`, Simple, `raycastTarget=false`, 맨 뒤 sibling 0, 가운데)를 추가. 크기·위치는 Task 2 측정값으로 날 직선 구간이 게임 공간에서 가운데 (8, 4.5) 중심, 두께 약 4.0유닛, 날 직선 구간 약 12.5유닛이 되게 계산한다.
- [ ] **Step 3: spawnArea.** `CraftingController`의 `polishSettings.spawnArea`를 `SerializedObject`로 설정: x 범위 = 날 직선 구간 − 흠집 지름 1.8, y 범위 = 날 두께 − 1.8 (목표 `Rect(2.65, 3.4, 10.7, 2.2)`, 실제는 측정값). 영역이 이보다 좁아지면 6개 겹침이 급증하므로(12.5×3.6 날은 15%) 모자라면 Task 2를 재생성한다.
- [ ] **Step 4: 흠집 스프라이트·숫돌.** `PolishMinigameView.flawSprites`에 3종을 순서대로 연결. `PlayArea`에 `Whetstone`(`Image`, `polish_whetstone`, 약 240×90, pivot 아래 중앙, 비활성, `raycastTarget=false`)을 `FlawTemplate`/흠집 위에 오도록 추가하고 `whetstone` 필드에 연결.
- [ ] **Step 5: 정적 검증.** Missing Script 0, `PolishMinigameView` 참조(flawTemplate, playArea, timeFill, panelRoot, clearEffects, whetstone) null 아님, 콘솔 오류 0.
- [ ] **Step 6: 로직 재검증 (Review Focus 1).** 씬의 실제 `spawnArea`로 난이도 10, 씨드 0~199를 돌려 흠집 중심이 영역 안이고, 흠집 원이 검 날(Task 2 측정 범위)을 벗어나지 않음을 확인.
- [ ] **Step 7: 렌더링 확인.** 편집 모드에서 Game 캔버스 카메라로 렌더링: (a) 검 위 흠집 6개(3종 변형 혼합), (b) 숫돌이 흠집 위에 있을 때 가림 정도와 진행이 보이는지, (c) 지워지는 중 알파 변화. 가림이 심하면 숫돌 크기·pivot을 조정한다. 확인하지 못한 항목(실제 입력, 드래그 감각)은 그대로 보고한다.
- [ ] **Step 8: 문서.** `docs/GameDesign.md` §4 연마 행의 "사포질하듯"을 "숫돌로 갈듯이"로 (규칙·수치 불변). 설계 문서에 결과(측정값, `spawnArea` 값, 검증, 알려진 점)를 기록.
- [ ] **Step 9: 커밋.** 스프라이트·문서를 커밋한다 (`Game.unity`는 사용자가 저장을 요청한 뒤에 따로).

```bash
git add Assets/Sprites/Minigame/Polish docs
git commit -m "feat : 연마 미니게임에 검 이미지·흠집 변형·숫돌 적용 (gpt-image)"
```
