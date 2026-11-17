# 적중 이펙트 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 단조 판정(금색 빛, 판정별 크기)과 연마 결함 제거(은색 반짝임)에 파티클 이펙트를 넣는다.

**Architecture:** 미니게임 모델은 "어느 원/결함이, 눌러서 판정됐는지"를 이벤트로 알리기만 한다. 각 미니게임 뷰가 그 이벤트를 받아 `HitEffectSpawner`로 파티클 프리팹을 자기 `playArea` 안 위치에 띄운다. 파티클이 UI 위에 보이도록 Game 씬 `GameUICanvas`를 Screen Space - Camera로 바꾸고, 결과·튜토리얼 팝업은 하위 캔버스로 이펙트보다 위에 둔다.

**Tech Stack:** Unity 6 (6000.3.27f2), C# 9, uGUI, ParticleSystem, URP 2D, UnityMCP(`execute_code`, `manage_scene`, `manage_camera`, `read_console`).

**Spec:** `docs/superpowers/specs/2026-11-17-hit-effects-design.md` (승인됨). 상위 계획 `docs/V2.md` §1 3번.

## Global Constraints

- 네임스페이스는 폴더와 맞춘 block style. 로직 `Assets/Scripts/Crafting/` → `BlackSmith.Crafting`, 뷰·이펙트 `Assets/Scripts/UI/` → `BlackSmith.UI`(`using UnityEngine.UI;` 명시).
- private 필드 camelCase, 직렬화는 `[SerializeField]`, 클래스·메서드 PascalCase. 새 Singleton/전역 상태 금지.
- 미니게임 모델(`ForgeMinigame`, `PolishMinigame`)은 시각·오디오를 모른다. 판정 규칙·점수·난이도·효과음은 바꾸지 않는다. 기존 이벤트 `Judged(CircleState)`, `FlawCleared()`와 `CraftingSfx`는 그대로 둔다.
- 대상은 단조·연마뿐. 가열·열처리, Title 씬은 건드리지 않는다.
- 단조: Perfect > Good > 눌렀지만 Miss 순으로 큰 **금색 계열** 빛. 누르지 못하고 놓친 Miss와 빈 곳 클릭은 이펙트 없음. 연마: 결함 제거마다 같은 크기의 **은색 계열** 반짝임.
- 이펙트 텍스처·모양·색 값·배율·지속 시간은 임시값이다(최종 아트 **[미정]**). 임시값: 금색 `(1, 0.82, 0.35, 1)`, 은색 `(0.85, 0.88, 0.95, 1)`, 단조 배율 Perfect 1 / Good 0.65 / 누른 Miss 0.35, 수명 0.3~0.5초.
- 그리기 순서: 배경 스프라이트(order -10 ~ -1) < `GameUICanvas`(order 0) < 이펙트(renderer sortingOrder 10) < `CraftResultPopup`·`TutorialPopup`(하위 캔버스 override sorting 20).
- `.cs.meta`는 삭제·재생성하지 않는다. 씬은 사용자가 요청할 때만 저장한다. 커밋은 사용자가 요청할 때만, 작업별 파일만 `git add`(`git add -A` 금지, `Assets/Settings/*`·`ProjectSettings/*`·`Assets/vFavorites/*` 제외). 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. 푸시·재빌드는 요청 시에만.
- 테스트 어셈블리가 없다. 검증은 `execute_code`(CodeDom, C# 6: 지역 함수·`out var`·패턴 매칭 불가, `UnityEngine.Object`·`UnityEngine.UI.*` 완전 한정)로 하고, 스크립트 변경 뒤 컴파일 체크: `read_console clear` → `refresh_unity compile=request mode=force wait_for_ready=true` → `read_console types=[error,warning]` (`WebSocket is not initialised`는 무시). 씬 코드 실행 전 활성 씬이 `Game`인지 확인한다.
- 후처리(Bloom 등)가 UI·이펙트 색을 바꾸는 상태면 임의로 끄지 말고 사용자에게 묻는다(배경 모양도 바뀌기 때문).

## Review Focus

1. **단계가 끝나 뷰가 숨겨질 때 남은 이펙트**: 마지막 원·결함 직후 `Hide`가 불려도 이펙트가 다음 단계 화면이나 결과 팝업 위에 남지 않는다 (Task 4: Hide 후 자식 이펙트 0개).
2. **이펙트가 입력을 막음**: 이펙트가 떠 있는 자리를 바로 다시 눌러도 다음 원 판정·결함 선택이 된다 (Task 3: 프리팹에 Graphic/Collider 없음, Task 4: 이펙트 위 클릭 판정).
3. **캔버스 전환 뒤 UI 위치·클릭**: 모든 UI가 전환 전과 같은 화면 위치이고 버튼·모루 클릭이 그대로 된다 (Task 2: 전후 화면 좌표 비교, 레이캐스트 확인).
4. **해상도·창 크기 변화**: 이펙트 크기가 UI와 같은 비율로 커지고 줄어든다 (Task 3: `scalingMode = Hierarchy`, Task 4: 다른 캔버스 배율에서 크기 비율 확인).
5. **결과 팝업·튜토리얼 팝업 클릭**: 하위 캔버스로 바꾼 뒤에도 확인 버튼이 눌린다 (Task 2: 하위 캔버스에 `GraphicRaycaster`, `onClick` 및 레이캐스트 확인).

---

### Task 1: 판정 정보 보강 (모델 이벤트)

**Files:**
- Modify: `Assets/Scripts/Crafting/ForgeMinigame.cs` (`Circle` 클래스 60-75행 부근, `Tick` 168-176행, `HandleClick` 194-215행)
- Modify: `Assets/Scripts/Crafting/PolishMinigame.cs` (이벤트 선언 62-65행 부근, `PointerMove` 199-205행)

**Interfaces:**
- Produces:
  - `ForgeMinigame.Circle.Clicked : bool { get; internal set; }` — 눌러서 판정됐으면 true, 시간이 지나 놓친 Miss면 false.
  - `ForgeMinigame.CircleJudged : event Action<ForgeMinigame.Circle>` — `Judged` 직후 같은 자리에서 판정된 원을 넘긴다.
  - `PolishMinigame.FlawClearedAt : event Action<PolishMinigame.Flaw>` — `FlawCleared` 직후 지워진 결함을 넘긴다(`activeFlaw`를 null로 만들기 전에 지역 변수로 잡아 둔다).

- [ ] **Step 1: 단조 — `Clicked` 속성과 `CircleJudged` 이벤트 추가.** `HandleClick`의 판정 지점에서 `Clicked = true`, `Tick`의 놓침 지점에서 `false`. 두 지점 모두 `Judged?.Invoke` 바로 뒤에 `CircleJudged?.Invoke(circle)`.
- [ ] **Step 2: 연마 — `FlawClearedAt` 이벤트 추가.** `FlawCleared?.Invoke()` 바로 뒤에 지워진 결함으로 호출.
- [ ] **Step 3: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 4: 모델 검증 (`execute_code`, 에디터 모드).**
  - 단조: `new ForgeMinigame(new ForgeSettings())`, `Begin(1)`, `CircleJudged`를 리스트에 모은다. 첫 원: `Tick`으로 `HitTime`까지 진행 후 그 `Position`을 `HandleClick` → `State == Perfect && Clicked`. 둘째 원: `HitTime - GoodWindow - 0.05` 시각에 클릭 → `Miss && Clicked`(원이 이미 나타났을 때). 나머지는 클릭 없이 끝까지 `Tick` → 모두 `Miss && !Clicked`. `CircleJudged` 호출 수 == `Judged` 호출 수 == 원 개수. 빈 곳(원과 먼 좌표) 클릭 → `CircleJudged` 증가 없음.
  - 연마: `new PolishMinigame(new PolishSettings())`, `Begin(1)`, 첫 결함 위치에 `PointerDown` 후 결함 중심을 지나가는 `PointerMove`를 지워질 때까지 반복 → `FlawClearedAt` 1회, 넘어온 객체가 그 결함(`ReferenceEquals`)이고 `IsCleared`. `FlawCleared` 호출 수와 같다.
  - Expected: 모든 항목 true.
- [ ] **Step 5: 커밋 (요청 시).** `git add Assets/Scripts/Crafting/ForgeMinigame.cs Assets/Scripts/Crafting/PolishMinigame.cs` / `feat : 단조·연마 판정 이벤트에 원·결함 정보 추가 (적중 이펙트용)`

### Task 2: Game 캔버스를 Screen Space - Camera로 전환

**Files:**
- Modify: `Assets/Scenes/Game.unity` (`GameUICanvas`, `GameUICanvas/CraftResultPopup`, `GameUICanvas/TutorialPopup`)

**Interfaces:**
- Produces: `GameUICanvas` = `RenderMode.ScreenSpaceCamera`, `worldCamera = Main Camera`, `planeDistance = 5`, sorting layer `Default` order 0. `CraftResultPopup`·`TutorialPopup`에 `Canvas`(overrideSorting, order 20) + `GraphicRaycaster`.

- [ ] **Step 1: 전환 전 기록 (`execute_code`, 읽기 전용).** `GameUICanvas` 아래 모든 `RectTransform`(비활성 포함)의 화면 좌표 4개 코너를 `RectTransformUtility.WorldToScreenPoint(null, corner)`로 기록해 경로→좌표 사전을 `EditorPrefs`나 스크래치 JSON 파일에 저장. 같은 코드에서 `Main Camera`의 `UniversalAdditionalCameraData.renderPostProcessing`과 `DefaultVolumeProfile`의 Bloom `active`/`intensity`를 출력.
- [ ] **Step 2: 후처리 판단.** `renderPostProcessing == true`이고 Bloom intensity > 0이면 멈추고 사용자에게 묻는다(Global Constraints). 아니면 계속.
- [ ] **Step 3: 전환 실행 (`execute_code`, 씬 변경 — 실행 전 사용자에게 변경 내용을 알린다).** `Undo.RecordObject` 후 Interfaces의 값을 적용. 두 팝업에 `Canvas`(`overrideSorting = true`, `sortingOrder = 20`)와 `GraphicRaycaster`를 추가. `MarkSceneDirty`.
- [ ] **Step 4: 전후 비교.** 같은 경로의 코너를 `WorldToScreenPoint(Main Camera, corner)`로 다시 구해 Step 1 값과 비교. Expected: 모든 RectTransform 오차 1px 이하. 다르면 경로와 차이를 출력하고 원인(CanvasScaler, planeDistance)을 고친다.
- [ ] **Step 5: 입력 확인 (플레이 모드).** `EventSystem.current.RaycastAll`로: (a) 패널이 닫힌 상태에서 모루 스프라이트의 화면 좌표 → 첫 결과가 `AnvilInteractable` 오브젝트, (b) `ShopButton` 중심 → 첫 결과가 그 버튼 계층, (c) `CraftResultPopup`을 켠 상태에서 `ConfirmButton` 중심 → 그 버튼 계층. `ShopButton`·`ConfirmButton`의 `onClick.Invoke()`로 패널 열림/팝업 닫힘. 끝나면 플레이 모드 종료. Expected: 모두 통과.
- [ ] **Step 6: 화면 확인 시도.** `manage_camera screenshot`으로 Game 화면을 찍어 UI가 보이는지 확인(Camera 방식은 찍힐 수 있다). 찍히지 않으면 보고.
- [ ] **Step 7: 정리 검증.** Game Missing Script 0, 콘솔 오류·경고 없음.
- [ ] **Step 8: 씬 저장·커밋 (요청 시).** `git add Assets/Scenes/Game.unity` / `feat : Game UI 캔버스를 Screen Space - Camera로 전환 (파티클 이펙트용)`. 저장 후 patience diff로 지워진 오브젝트가 없는지 확인.

### Task 3: 이펙트 프리팹과 `HitEffectSpawner`

**Files:**
- Create: `Assets/Scripts/UI/HitEffectSpawner.cs`
- Create: `Assets/Effects/Textures/SoftGlow.png` (64×64, 흰색 원형 그라디언트, 알파 중심 1 → 가장자리 0; 코드로 생성, Texture Type Default, Alpha Is Transparency)
- Create: `Assets/Effects/HitEffect.mat` (셰이더 `Universal Render Pipeline/Particles/Unlit`, Surface Transparent, Blend Additive, Base Map `SoftGlow`)
- Create: `Assets/Effects/ForgeHitGlow.prefab`, `Assets/Effects/PolishSparkle.prefab`

**Interfaces:**
- Consumes: 없음.
- Produces:
  - `BlackSmith.UI.HitEffectSpawner : MonoBehaviour` — `[SerializeField] ParticleSystem prefab`, `[SerializeField] RectTransform container`(비우면 자기 RectTransform).
  - `public void Play(Vector2 localPosition, float scale)` — `container` 아래에 프리팹을 만들어 `localPosition`, `localScale = Vector3.one * scale`로 두고 재생. `scale <= 0`이면 아무것도 안 함.
  - `public void Clear()` — 지금 떠 있는 이펙트를 모두 즉시 제거.
  - `public int ActiveCount { get; }` — 검증용, 살아 있는 이펙트 수.
- 프리팹 공통: 루트 `ParticleSystem`, `main.scalingMode = Hierarchy`, `simulationSpace = Local`, `playOnAwake = true`, `loop = false`, `stopAction = Destroy`, renderer `sortingLayer Default`, `sortingOrder 10`, 재질 `HitEffect.mat`. Graphic·Collider 컴포넌트 없음. 크기·속도 단위는 캔버스 px.
- `ForgeHitGlow` (금색 `(1,0.82,0.35,1)`): 루트 = 큰 빛 1개(burst 1, start size 160, lifetime 0.35, size over lifetime 0.6→1.2, alpha 1→0), 자식 = 불꽃 12개(burst 12, speed 300, size 12, lifetime 0.4, alpha 1→0, shape circle).
- `PolishSparkle` (은색 `(0.85,0.88,0.95,1)`): 루트 = 작은 빛 1개(start size 80, lifetime 0.3), 자식 = 반짝 점 10개(speed 150, size 14, lifetime 0.45, size over lifetime 1→0).

- [ ] **Step 1: `HitEffectSpawner.cs` 작성.** 만든 인스턴스를 리스트로 들고 `Clear`에서 `Destroy`, 이미 파괴된 항목은 `ActiveCount`/`Clear`에서 건너뛴다.
- [ ] **Step 2: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 3: 텍스처·재질·프리팹 생성 (`execute_code`).** 위 값으로 만든다. 셰이더 이름이 없으면(`Shader.Find` null) 프로젝트의 URP 패키지에서 파티클 Unlit 셰이더 이름을 찾아 쓰고 보고한다.
- [ ] **Step 4: 프리팹 검증 (`execute_code`).** 두 프리팹 모두: `scalingMode == Hierarchy`, `stopAction == Destroy`, `loop == false`, renderer `sortingOrder == 10`, 재질 셰이더 이름에 `Unlit` 포함, 계층에 `Graphic`·`Collider2D`·`Collider` 없음, 시작 색이 위 금색/은색. Expected: 모두 true.
- [ ] **Step 5: 커밋 (요청 시).** `git add Assets/Scripts/UI/HitEffectSpawner.cs Assets/Scripts/UI/HitEffectSpawner.cs.meta Assets/Effects` / `feat : 적중 이펙트 프리팹(단조 금색 빛, 연마 은색 반짝임)과 HitEffectSpawner 추가`

### Task 4: 뷰에 이펙트 연결

**Files:**
- Modify: `Assets/Scripts/UI/ForgeMinigameView.cs` (필드 18-33행, `Show`/`Hide` 52-68행)
- Modify: `Assets/Scripts/UI/PolishMinigameView.cs` (필드 17-31행, `Show`/`Hide` 46-63행)
- Modify: `Assets/Scenes/Game.unity` (`ForgePanel`·`PolishPanel`의 PlayArea에 `HitEffectSpawner` 추가·연결)

**Interfaces:**
- Consumes: Task 1의 `CircleJudged`, `Circle.Clicked`, `FlawClearedAt`. Task 3의 `HitEffectSpawner.Play/Clear/ActiveCount`, 프리팹.
- Produces:
  - `ForgeMinigameView`: `[SerializeField] HitEffectSpawner hitEffects`, `[SerializeField] float perfectEffectScale = 1f`, `goodEffectScale = 0.65f`, `missEffectScale = 0.35f`(Tooltip: 눌렀지만 Miss일 때. 놓친 Miss는 이펙트 없음).
  - `PolishMinigameView`: `[SerializeField] HitEffectSpawner clearEffects`.

- [ ] **Step 1: `ForgeMinigameView` 수정.** `Show`에서 `CircleJudged` 구독, `Hide`에서 해제 + `hitEffects.Clear()`(Show 시작에서도 Clear). 핸들러: `!circle.Clicked`면 무시, 판정별 배율 선택, 원의 미니게임 좌표를 `Draw`와 같은 식(`rect.xMin + x * scale`, `rect.yMin + y * scale`)으로 `playArea` 로컬로 바꿔 `Play`. `hitEffects`가 비어 있으면 조용히 넘어간다. 좌표 변환은 `Draw`와 핸들러가 함께 쓰는 private 메서드로 뺀다.
- [ ] **Step 2: `PolishMinigameView` 수정.** 같은 방식으로 `FlawClearedAt` 구독/해제, 결함 위치에 배율 1로 `Play`, `Hide`·`Show`에서 `Clear`.
- [ ] **Step 3: 컴파일 체크.** Expected: 오류·경고 없음.
- [ ] **Step 4: 씬 연결 (`execute_code`, 씬 변경 — 실행 전 알린다).** 각 뷰의 `playArea` 오브젝트에 `HitEffectSpawner`를 붙이고 `prefab`(단조 `ForgeHitGlow`, 연마 `PolishSparkle`), `container = playArea`를 설정, 뷰의 `hitEffects`/`clearEffects`에 연결. Missing Script 0 확인.
- [ ] **Step 5: 동작 검증 (플레이 모드, `execute_code`).**
  - 단조: 씬의 `ForgeMinigameView`에 `new ForgeMinigame(settings)`를 `Begin(1)` 후 `Show`. 모델을 직접 `Tick`/`HandleClick`해 Perfect·Good·누른 Miss·놓친 Miss를 하나씩 만든다 → 각 판정 직후 `hitEffects.ActiveCount` 증가량이 1,1,1,0. 만들어진 이펙트의 `localScale.x`가 1 / 0.65 / 0.35, `localPosition`이 원의 뷰 위치와 1px 이내.
  - 이펙트가 떠 있는 자리에서 `EventSystem.current.RaycastAll` → 첫 결과가 PlayArea(이펙트가 가로채지 않음).
  - `Hide()` 직후 `ActiveCount == 0`이고 PlayArea 아래 `ParticleSystem` 0개.
  - 연마: 같은 방식으로 결함 하나 제거 → `ActiveCount` 1, 위치가 결함 뷰 위치와 1px 이내. `Hide()` 뒤 0.
  - 해상도 비율: 이펙트의 `lossyScale.x / playArea.lossyScale.x`가 `localScale.x`와 같다(캔버스 배율을 따른다).
  - 끝나면 플레이 모드 종료. Expected: 모두 통과.
- [ ] **Step 6: 화면 확인 시도.** 플레이 모드에서 단조 이펙트를 띄운 직후 `manage_camera screenshot`. 보이면 첨부해 보고, 안 찍히면 보고.
- [ ] **Step 7: 정리 검증.** Title(파일 기준 GUID 비교)·Game Missing Script 0, 콘솔 오류·경고 없음.
- [ ] **Step 8: 씬 저장·커밋 (요청 시).** `git add Assets/Scripts/UI/ForgeMinigameView.cs Assets/Scripts/UI/PolishMinigameView.cs Assets/Scenes/Game.unity` / `feat : 단조 판정·연마 결함 제거 시 적중 이펙트 재생`

### Task 5: 문서

**Files:**
- Modify: `docs/V2.md` (§1 3번 상태, §4 표 적중 이펙트 행)
- Modify: `CLAUDE.md` (v2 order 줄, 새 "Hit effects architecture" 짧은 절: 모델 이벤트 → 뷰 → `HitEffectSpawner` → 프리팹, Game 캔버스가 Screen Space - Camera이고 팝업이 하위 캔버스 order 20, 이펙트 order 10; Unity MCP 절의 "Overlay UI는 스크린샷 불가"에 Game 씬은 이제 Camera 방식이라는 메모)
- Modify: `README.md` (v2 목록의 적중 이펙트 줄)

- [ ] **Step 1: 세 문서 갱신.** V2.md: "3. 적중 이펙트 — 구현 완료 (단조 금색 빛·연마 은색 반짝임, 이펙트 아트는 임시값/미정)", §4 표 미정 칸을 "이펙트 최종 아트"로. 범위 결정(단조·연마만, 판정별 크기, 놓친 Miss 없음, 색)을 §1 아래나 새 짧은 절에 적는다.
- [ ] **Step 2: 커밋 (요청 시).** `git add docs/V2.md CLAUDE.md README.md docs/superpowers/specs/2026-11-17-hit-effects-design.md docs/superpowers/plans/2026-11-17-hit-effects.md` / `docs : 적중 이펙트 구현 반영`

## 확인하지 못할 수 있는 것 (보고 대상)

- 실제 마우스 플레이에서의 손맛, 이펙트의 최종 모양(스크린샷이 안 찍히면).
- Windows 빌드에서의 표시.
