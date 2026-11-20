# 연마 검 축소·흠집 개선 + 단조 주괴·망치 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 연마는 검을 줄이고 흠집을 긁힘/찍힘/패임으로 바꾸며, 단조는 달궈진 주괴를 두고 판정된 클릭마다 그 지점에 망치가 내리찍게 한다. 판정 규칙·난이도는 그대로.

**Architecture:** 로직에는 연마와 같은 방식으로 `ForgeSettings.spawnArea`(게임 공간 단위)만 추가한다. 이미지·애니메이션은 뷰와 씬이 맡는다. 이미지는 gpt-image(투명 PNG), 씬 구조는 1~5단계 방식을 따른다.

**Tech Stack:** Unity 6 uGUI, C#, UnityMCP(`execute_code`, 렌더링 검증), gpt-image 스킬(Node.js 26, Codex CLI, ChatGPT 로그인 완료).

**Spec:** `docs/superpowers/specs/2026-11-20-polish-forge-revision-design.md`

## Global Constraints

- 판정·점수·난이도·시간·원 개수·흠집 개수·흠집/원 반지름·문지르기 규칙은 바꾸지 않는다 (`docs/GameDesign.md` §4).
- `ForgeSettings.spawnArea` 기본값은 `Rect(2, 2, 12, 5)` (기존 동작과 동일). 같은 씨드에서 기본 영역의 원 위치가 예전 알고리즘과 모두 같아야 한다. `margin` 필드는 남기고 쓰지 않는다.
- 로직(`BlackSmith.Crafting`)은 이미지·씬 좌표를 모른다. 단위는 게임 공간(16×9, 원점 왼쪽 아래).
- 연마 `PlayArea`는 1280×720(80%)로 줄이고 1유닛 = 80px. 연마 흠집 시각·입력·이펙트는 뷰의 `playArea.rect.width / 16` 환산을 따른다.
- 이미지: `--background transparent`, 스타일 기준 `generated-images/heat_bar_frame.png` 또는 `Assets/Backgrounds/forge_interior_storybook.png`(검·망치·주괴). 흠집 3종은 무채색·외곽선 없음·표시 사이가 투명.
- 망치·주괴·검 등 장식 이미지는 `raycastTarget = false`. 입력은 `PlayArea`가 받는다.
- 코딩 규칙(CLAUDE.md): PascalCase 클래스·메서드, private camelCase, `[SerializeField]`, 네임스페이스 블록 스타일, 새 Singleton 금지.
- 씬 저장은 사용자가 요청할 때만. Game 씬은 additive로 열어 작업. `.meta` 삭제·재생성 금지.
- 검증: 스크립트 변경 뒤 컴파일 체크(`read_console clear` → `refresh_unity compile=request` → 대기 → 오류·경고 확인), 씬 변경 뒤 Missing Script·참조·콘솔 확인, 확인한 것과 못 한 것을 함께 보고.
- 이미지 생성은 `~/.codex/generated_images/`에서 `generated-images/`로 복사. PowerShell PATH에 `C:\Program Files\nodejs;C:\Users\ejy10\AppData\Local\Programs\OpenAI\Codex\bin`을 더한다.

## Review Focus

- 연마: 난이도 10 흠집 6개가 날 안에 있고(날 밖 원 0개) 겹친 판이 3% 이하, 최악 최소 거리 ≥ 1.5. 검 끝(뾰족한 부분)에 흠집이 걸치지 않는다.
- 연마: 흠집이 옅어질수록(알파 0.25) 날 위에서 완전히 사라지지 않고 위치를 알아볼 수 있다. 문지르는 중(주황)과 기본(회색)이 구분된다.
- 단조: 모든 난이도에서 모든 원의 중심이 `spawnArea` 안이고, 원(반지름 0.8)이 주괴 윗면 밖으로 나가지 않는다. 연속한 원 사이 간격 ≥ 2.4를 대부분 지킨다.
- 단조: 기본 `spawnArea`에서 예전 알고리즘과 씨드별 위치가 같다 (회귀).
- 단조: 놓친 Miss(`Clicked == false`)에는 망치가 나오지 않고, 눌러서 Miss에는 나온다. 빠른 연속 클릭(0.3초 간격)에서도 망치가 남아 있지 않다. `Hide`/`Show`/끝 뒤에 망치가 남지 않는다.
- 단조: 망치·주괴가 클릭을 가로채지 않는다(raycast 끔). `hammer`/`ingot` 필드를 비우면 예전처럼 동작한다.

## 파일 구조

- Modify: `Assets/Scripts/Crafting/ForgeMinigame.cs` (`ForgeSettings.spawnArea`, `PickPosition`)
- Modify: `Assets/Scripts/UI/ForgeMinigameView.cs` (`hammer`, `ingot`, 애니메이션)
- Create: `Assets/Sprites/Minigame/Polish/polish_flaw_pit.png`, `Assets/Sprites/Minigame/Forge/forge_ingot.png`, `forge_hammer.png` (+ `.meta`)
- Replace (같은 경로 덮어쓰기, GUID 유지): `Assets/Sprites/Minigame/Polish/polish_blade.png`, `polish_flaw_scratch.png`, `polish_flaw_dent.png`
- Modify: `Assets/Scenes/Game.unity` (`PolishPanel`, `ForgePanel`, `CraftingController`)
- Modify: `docs/GameDesign.md` §4 단조 행 문구, 설계 문서(결과 기록)

---

### Task 1: 연마 검 이미지 다시 생성 (끝까지 보이는 통통한 롱소드)

**Files:**
- Replace: `Assets/Sprites/Minigame/Polish/polish_blade.png`

**Interfaces:**
- Produces: 스프라이트 `polish_blade`(같은 경로, 같은 GUID)와 측정값(이미지 크기, 날 두께 px, 가드 오른쪽 끝 x, 날 직선 구간 끝 x, 끝 x). Task 3이 `Blade` 크기·위치와 `spawnArea`를 계산하는 데 쓴다.

- [ ] **Step 1: 생성.** 이전 결과(`generated-images/raw/polish_blade_v2.png`)를 `--edit-target`으로 두고 "같은 그림체·손잡이·은색 날을 유지하되, 오른쪽 끝에 짧게 모이는 뾰족한 날 끝이 이미지 안에 보이게 하고, 전체 비율이 약 3.5:1(날 두께가 이미지 높이의 약 85%)이 되게" 수정한다. 샌드박스 오류가 나면 출력의 경로에서 복사한다. Expected: 모서리 알파 0, 날 끝이 이미지 오른쪽 안쪽에 있음.
- [ ] **Step 2: 측정.** 불투명 영역으로 크롭하고, 날 두께(직선 구간 열), 가드 오른쪽 끝 x, 날 직선 구간 끝 x(두께가 줄기 시작하는 지점), 끝 x를 기록한다. 날 직선 구간 길이/두께 비가 약 2.5 이상이어야 한다 (모자라면 재생성).
- [ ] **Step 3: 사용자에게 결과 이미지 경로와 측정값을 보이고 승인받는다.** 불만이면 `--edit-target`으로 수정.
- [ ] **Step 4: 덮어쓰기.** `Assets/Sprites/Minigame/Polish/polish_blade.png`에 복사(`.meta` 유지), 임포트 확인(Sprite/Single, 알파, 압축 없음, 4096), 콘솔 확인.

### Task 2: 연마 흠집 이미지 3종 교체

**Files:**
- Replace: `Assets/Sprites/Minigame/Polish/polish_flaw_scratch.png`, `polish_flaw_dent.png`
- Create: `Assets/Sprites/Minigame/Polish/polish_flaw_pit.png`

**Interfaces:**
- Produces: 스프라이트 3개(Sprite/Single, PPU 100, maxTextureSize 512). `flawSprites` 순서: `polish_flaw_scratch`, `polish_flaw_dent`, `polish_flaw_pit`.

- [ ] **Step 1: 프롬프트 작성(위임된 창작) 후 배치 생성.** 긁힘(깊고 굵은 줄 몇 개 + 가는 줄, 금속이 드러난 밝은 가장자리), 찍힘(가운데 패임 + 방사형 금), 패임 점(부식으로 파인 작은 점 무리). 모두 무채색, 외곽선 없음, 정사각 1:1, **표시 사이는 투명**(원판 채움 금지), 영역에 꽉 참, 어두운 표시와 밝은 가장자리로 은색 날 위에서 읽히게. 매니페스트로 3 job 배치.
- [ ] **Step 2: 크롭·알파 확인.** 정사각 크롭, 모서리 알파 0, 중심 영역에 불투명 비율이 40~75%인지(꽉 찬 원판이 아닌지) 확인한다.
- [ ] **Step 3: 사용자 확인 후 덮어쓰기/임포트.** 파일을 복사하고 임포터 설정(512, Sprite/Single)을 적용한다.

### Task 3: 연마 씬 반영

**Files:**
- Modify: `Assets/Scenes/Game.unity` (`PolishPanel`, `CraftingController`)

**Interfaces:**
- Consumes: Task 1 측정값, Task 2 스프라이트.
- Produces: `PlayArea` 1280×720, 새 `Blade` 배치, `flawSprites` 3종, `polishSettings.spawnArea`, 뷰의 `flawColor`/`activeFlawColor`, `Whetstone` 약 170×64.

- [ ] **Step 1: 변경 고지.** 무엇을 바꾸는지 사용자에게 알린 뒤 Game 씬을 additive로 연다 (이미 열려 있으면 그대로).
- [ ] **Step 2: PlayArea·Blade.** `PolishPanel/PlayArea.sizeDelta = (1280, 720)`. `Blade` 크기·위치는 1유닛 = 80px로 계산: 손잡이 끝 ≈ 게임 x 0.1, 가드 오른쪽 ≈ 2.5, 날 직선 끝 ≈ 14.7, 끝 ≈ 15.9가 되도록 스케일(이미지 가로 비율 유지)하고 세로 중심은 게임 y 4.5.
- [ ] **Step 3: 흠집 영역.** `polishSettings.spawnArea`를 날 직선 구간 안쪽으로 설정: x = [가드 오른쪽 + 0.9, 날 직선 끝 − 0.9], y = [4.5 − (날 두께/2 − 0.9), 4.5 + (날 두께/2 − 0.9)] (게임 단위). 기준 통계(Review Focus 1)를 만족하지 못하면 검 크기(Task 1 재생성)를 키우거나 영역을 조정한다.
- [ ] **Step 4: 스프라이트·색·숫돌.** `PolishMinigameView.flawSprites`를 새 3종으로 교체, `flawColor = (0.85,0.85,0.9,1)`, `activeFlawColor = (1,0.6,0.35,1)`(SerializedObject), `Whetstone.sizeDelta ≈ (170, 64)`.
- [ ] **Step 5: 통계 검증 (`execute_code`).** 난이도 1/5/10, 씨드 1000판씩: 날 밖으로 나간 원 수, 겹친 판 비율, 최악 최소 거리를 출력. Expected: 날 밖 0, 난이도 10 겹침 ≤ 3%, 최악 ≥ 1.5.
- [ ] **Step 6: 렌더링 확인.** Game 캔버스 카메라로 렌더링: 검 전체가 화면 안, 흠집 6개(3종), 숫돌, 옅어지는 흠집(알파 1/0.5/0.25), 기본/문지르는 중 색 구분. 확인하지 못한 것(실제 드래그·판정)은 보고한다.
- [ ] **Step 7: 정적 검증.** Missing Script 0, 뷰 참조(flawTemplate, playArea, timeFill, panelRoot, clearEffects, whetstone)와 `flawSprites` null 없음, 콘솔 오류 0.

### Task 4: 단조 원 생성 영역 (로직)

**Files:**
- Modify: `Assets/Scripts/Crafting/ForgeMinigame.cs`

**Interfaces:**
- Produces: `ForgeSettings.spawnArea` — `public Rect spawnArea = new Rect(2f, 2f, 12f, 5f);` (한글 툴팁). `ForgeMinigame.Begin`이 원 중심을 `spawnArea` 안(경계 포함)에 둔다. 폭 또는 높이 ≤ 0이면 `AreaSize * 0.5f`.

- [ ] **Step 1: 실패 확인.** `execute_code`로 `new ForgeSettings().spawnArea`를 쓰는 코드가 컴파일 오류(필드 없음)임을 확인한다.
- [ ] **Step 2: 필드와 `PickPosition` 수정.** 필드 추가(`margin`은 남기고 툴팁에 "(미사용, 호환용)"). `PickPosition`은 `spawnArea.xMin/xMax/yMin/yMax`를 쓰고 나머지(바로 앞 원과 `targetRadius × 3` 간격, 최대 20회 재시도, 난수 순서)는 그대로.
- [ ] **Step 3: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 4: 로직 검증 (`execute_code`).** (a) 기본 설정, 난이도 1/10, 씨드 500개: 예전 알고리즘(margin 2, 20회)을 스크립트에 재현해 원 위치가 모두 같다. (b) `spawnArea = Rect(a,b,c,d)`: 모든 원 중심이 영역 안. (c) 비정상 영역은 중앙 (8,4.5). (d) 주괴용 후보 영역에서 연속 원 간격 ≥ 2.4인 비율을 출력.
- [ ] **Step 5: 커밋.**

```bash
git add Assets/Scripts/Crafting/ForgeMinigame.cs
git commit -m "feat : 단조 원 생성 영역(spawnArea) 설정 추가 (기본값은 기존 동작)"
```

### Task 5: 단조 주괴·망치 이미지

**Files:**
- Create: `Assets/Sprites/Minigame/Forge/forge_ingot.png`, `forge_hammer.png`

**Interfaces:**
- Produces: 스프라이트 2개 + 측정값: 주괴 윗면 사각형(이미지 px 기준 x/y 범위)과 이미지 크기, 망치 머리 타격면 중심 위치(이미지 px). Task 7이 `Ingot` 배치·`spawnArea`·`Hammer` 피벗을 계산하는 데 쓴다.

- [ ] **Step 1: 프롬프트 합의.** 주괴(가로로 놓인 사다리꼴 주괴, 달궈진 주황빛, 윗면이 넓고 평평하게 보이는 약간 비스듬한 정면 뷰, 만화풍, 투명 배경, 이미지에 꽉 참)와 망치(옆모습, 머리가 아래 왼쪽, 손잡이가 오른쪽 위로 비스듬, 만화풍 외곽선, 투명 배경)의 프롬프트를 사용자에게 보이고 확인받는다.
- [ ] **Step 2: 배치 생성** (2 job, 스타일 레퍼런스 `Assets/Backgrounds/forge_interior_storybook.png`). 샌드박스 경로에서 복사.
- [ ] **Step 3: 크롭·측정.** 불투명 영역으로 크롭. 주괴 윗면의 평평한 영역(원이 놓일 수 있는 사각형)과 망치 머리 타격면 중심을 이미지에서 측정해 기록. 윗면이 약 11:4.5 이상 넓지 않으면 재생성.
- [ ] **Step 4: 사용자 확인 후 임포트** (Sprite/Single, PPU 100, 알파, 압축 없음, 4096).

### Task 6: 단조 뷰 (망치·주괴 애니메이션)

**Files:**
- Modify: `Assets/Scripts/UI/ForgeMinigameView.cs`

**Interfaces:**
- Consumes: `ForgeMinigame.CircleJudged(Circle)`, `Circle.Clicked`, `Circle.Position`(변경 없음).
- Produces: `[SerializeField] private RectTransform hammer;`와 `[SerializeField] private RectTransform ingot;`(둘 다 선택). 판정된 클릭(`Clicked == true`)에 대해 `hammer`가 그 원 위치에서 약 0.2초 내리찍기 애니메이션(들어올린 각도 → 내리침 → 정지 → 사라짐)을 재생하고, `ingot`은 0.97배로 눌렸다 0.08초 만에 돌아온다. `Clicked == false`에는 재생하지 않는다. 새 클릭이 오면 처음부터 다시 재생. `Show`/`Hide`/`ClearCircles`에서 망치를 숨기고 주괴 스케일을 1로 복원한다.

- [ ] **Step 1: 실패 확인.** 리플렉션으로 `hammer`/`ingot` 필드가 없음을 확인한다. Expected: null.
- [ ] **Step 2: 필드·초기화.** 필드와 툴팁(한글) 추가. `Awake`에서 `hammer`/`ingot`의 `Image.raycastTarget = false`, 망치 비활성. 비어 있으면 모든 코드가 건너뛴다.
- [ ] **Step 3: 애니메이션.** `OnCircleJudged`에서 `circle.Clicked`일 때 시작 시각과 위치(`ToPlayAreaPoint(circle.Position) - playArea.rect.center`)를 기록하고 `hammer`를 활성화(`SetAsLastSibling`로 원 위). `Update`에서 경과 시간으로 각도(예: +35° → 0° 0.05초, 유지 0.06초, 사라짐 0.09초)와 알파를 계산해 적용하고 끝나면 비활성. 주괴 스케일도 같은 방식으로 복원.
- [ ] **Step 4: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 5: 동작 검증 (`execute_code`, 편집 모드).** 뷰의 `Show`와 `OnCircleJudged`를 호출해 (a) `Clicked=true`면 망치 활성, (b) `Clicked=false`면 비활성 유지, (c) `Hide` 뒤 비활성·주괴 스케일 1, (d) 필드가 비었을 때 예외 없음을 출력으로 확인한다 (`Update`는 편집 모드에서 안 돌므로 리플렉션으로 호출하고, 내부 `Destroy` 호출이 필요한 경로는 피한다).
- [ ] **Step 6: 커밋.**

```bash
git add Assets/Scripts/UI/ForgeMinigameView.cs
git commit -m "feat : 단조 뷰에 망치 내리찍기와 주괴 반응 추가"
```

### Task 7: 단조 씬 반영과 확인

**Files:**
- Modify: `Assets/Scenes/Game.unity` (`ForgePanel`, `CraftingController`)
- Modify: `docs/GameDesign.md` (§4 단조 행), 설계 문서(결과 기록)

**Interfaces:**
- Consumes: Task 5 스프라이트·측정값, Task 4 `spawnArea`, Task 6 `hammer`/`ingot` 필드.

- [ ] **Step 1: 변경 고지 후 연결.** `ForgePanel/PlayArea`에 `Ingot`(맨 뒤 sibling 0, 가운데, `raycastTarget=false`)과 `Hammer`(비활성, 피벗을 머리 타격면 중심으로, `raycastTarget=false`)를 추가하고 `ForgeMinigameView.ingot`/`hammer`에 연결.
- [ ] **Step 2: 영역 설정.** `forgeSettings.spawnArea`를 주괴 윗면 안쪽으로 설정: 윗면 사각형 − 목표 반지름 0.8(사방), 게임 단위. 연속 원 간격(Task 4 Step 4(d))이 충분한지 확인.
- [ ] **Step 3: 통계 검증.** 난이도 1/5/10, 씨드 500판씩: 모든 원 중심이 `spawnArea` 안이고 원(반지름 0.8)이 주괴 윗면 안, 연속 원 간격 ≥ 2.4 비율 출력.
- [ ] **Step 4: 렌더링 확인.** 편집 모드에서 주괴 위 원 4가지 상태(대기/Perfect/Good/Miss), 줄어드는 링, 망치가 내리찍는 순간(몇 가지 각도)과 힛 이펙트가 겹친 모습을 렌더링. 망치가 링·원을 가려 읽기 어렵다면 크기를 조정한다.
- [ ] **Step 5: 정적 검증.** Missing Script 0, `ForgeMinigameView` 참조(`circleTemplate`, `playArea`, `panelRoot`, `hitEffects`, 이펙트 3종, `hammer`, `ingot`) 정상, 복제본 잔여 0, 콘솔 오류 0.
- [ ] **Step 6: 문서.** `docs/GameDesign.md` §4 단조 행의 "화면 여러 곳에" → "달궈진 주괴 위 여러 곳에"(규칙·수치 불변). 설계 문서에 결과(측정값, 영역 값, 통계, 확인하지 못한 것)를 기록.
- [ ] **Step 7: 커밋** (스프라이트·문서만; `Game.unity`는 사용자가 저장을 요청한 뒤).

```bash
git add Assets/Sprites/Minigame docs
git commit -m "feat : 단조 주괴·망치 연출과 연마 검·흠집 개선 적용 (gpt-image)"
```
