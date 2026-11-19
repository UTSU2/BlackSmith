# 미니게임 이미지 교체 1단계 (가열) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Game.unity`의 `HeatPanel` 이미지 5개를 gpt-image로 만든 스프라이트로 교체하고, 그 결과를 이후 미니게임 단계의 스타일 기준으로 확정한다.

**Architecture:** 코드 변경 없음. gpt-image 브리지(`gpt_image.mjs`)로 투명 PNG를 생성해 `Assets/Sprites/Minigame/Heat/`에 임포트하고, 늘어나는 요소는 9-slice로 설정한 뒤 씬의 `Image`에 연결한다. 색 상태는 기존 `HeatMinigameView`의 코드 틴트를 그대로 쓴다.

**Tech Stack:** Unity 6 uGUI, UnityMCP(`execute_code`, `manage_asset`), gpt-image 스킬(Node.js 22+, Codex CLI, ChatGPT 로그인).

**Spec:** `docs/superpowers/specs/2026-11-19-minigame-art-stage1-design.md`

## Global Constraints

- 생성은 ChatGPT 구독 경로만 사용. Images API / `OPENAI_API_KEY` 금지.
- 모든 이미지 `--background transparent`(실제 알파). 체커보드·흰 배경으로 대체 금지.
- `TargetZone`, `Indicator`, `TimeFill`은 흰색/무채색 베이스 (코드 틴트가 색을 결정).
- 늘어나는 요소 `TargetZone`, `Indicator`, `TimeFill`은 9-slice + `Image.type = Sliced`.
- 스프라이트는 여백 없이 꽉 차게 (판정은 `RectTransform` 기준).
- `PlayArea` 배경은 이번 단계 제외. `HeatMinigameView` 등 코드 변경 없음.
- 저장 경로 `Assets/Sprites/Minigame/Heat/`. 씬 저장은 사용자가 요청할 때만.
- CLAUDE.md: 변경 후 Missing Script/참조/콘솔 확인, 확인한 것과 못 한 것을 함께 보고. `.meta`는 삭제·재생성 금지.
- 기준 크기: `BarArea` 160×700, `Indicator` 폭 가득×18, `TimeBar` 700×24 (`TimeFill`은 가득 채움).

## Review Focus

- 목표 구간이 최소/최대 높이일 때(난이도 1과 10) 9-slice가 늘어나도 무늬가 깨지지 않는다.
- `TimeFill`이 거의 0 폭까지 줄어도 끝 모양이 뒤집히거나 깨지지 않는다.
- `Indicator`가 `BarArea` 맨 위(1.0)·맨 아래(0.0)에 있을 때 프레임 밖으로 잘리지 않는다.
- 영역 안/밖 틴트(초록/주황)가 새 스프라이트에서도 구분된다 (스프라이트가 어두우면 틴트가 묻힘).
- `TargetZone` 반투명 틴트가 `BarArea` 틀 위에서 보인다 (렌더 순서: 틀 < 구간 < 표시기).

## 파일 구조

- Create: `Assets/Sprites/Minigame/Heat/heat_bar_frame.png`, `heat_target_zone.png`, `heat_indicator.png`, `heat_time_frame.png`, `heat_time_fill.png` (+ Unity가 만드는 `.meta`)
- Create: `generated-images/` (작업 폴더, 원본 PNG 보관. 커밋 여부는 사용자 판단)
- Modify: `Assets/Scenes/Game.unity` (`HeatPanel` 하위 `BarArea`, `TargetZone`, `Indicator`, `TimeBar`, `TimeFill`의 `Image`)
- Modify: `docs/superpowers/specs/2026-11-19-minigame-art-stage1-design.md` (확정한 스타일·9-slice 값 기록)

---

### Task 1: 생성 환경 확인과 스타일 기준(BarArea 틀) 확정

**Files:**
- Create: `generated-images/heat_bar_frame.png`

**Interfaces:**
- Produces: 확정된 `heat_bar_frame.png` (이후 모든 Task가 `--reference`로 붙이는 스타일 기준) 와 기준 프롬프트 문구.

- [ ] **Step 1: 환경 확인.** `node --version`이 22 이상인지 확인. 아니면 gpt-image 스킬의 `platform-setup.md` 절차를 따르고 사용자에게 설치·로그인 허가를 구한다. Expected: `v22.x` 이상.
- [ ] **Step 2: 프롬프트 합의.** 사용자에게 BarArea 틀(세로 160×700 비율, 쇠/나무 프레임, 어두운 내부, 여백 없음)의 프롬프트 문구를 제시하고 확인받는다. 사용자가 준 문구는 그대로 쓴다.
- [ ] **Step 3: 생성.** `node C:/Users/ejy10/.claude/skills/gpt-image/scripts/gpt_image.mjs generate --prompt "<합의한 문구>" --reference "Assets/Backgrounds/forge_interior.png" --background transparent --out "generated-images/heat_bar_frame.png"`. Expected: PNG가 생성되고 알파 채널이 있음.
- [ ] **Step 4: 사용자 확인.** 결과를 채팅에 보여 주고 승인받는다. 불만이면 이전 출력을 `--edit-target`으로 붙여 수정 요청을 반영해 재생성한다. 승인될 때까지 반복.
- [ ] **Step 5: 기준 기록.** 확정한 프롬프트·스타일 설명(질감, 선 굵기, 색감)을 spec 문서의 "설계자의 판단" 아래 "확정된 스타일" 항목에 추가하고 커밋한다.

```bash
git add docs/superpowers/specs/2026-11-19-minigame-art-stage1-design.md
git commit -m "docs : 미니게임 이미지 1단계 스타일 기준 기록"
```

### Task 2: 나머지 4종 생성

**Files:**
- Create: `generated-images/heat_target_zone.png`, `heat_indicator.png`, `heat_time_frame.png`, `heat_time_fill.png`

**Interfaces:**
- Consumes: Task 1의 `heat_bar_frame.png` (모든 job에 `--reference`로 첫 번째 첨부).
- Produces: 4개 투명 PNG. `heat_target_zone`/`heat_indicator`/`heat_time_fill`은 흰색/무채색, 가로 또는 세로로 늘려도 되는 모양.

- [ ] **Step 1: 프롬프트 합의.** 4종 각각의 프롬프트(목표 구간: 세로로 늘어나는 연한 흰 광택 띠 / 표시기: 가로 막대, 양 끝 장식 / 시간 바 틀: 700×24 비율 가로 틀 / 시간 바 채움: 가로로 늘어나는 흰색 채움)를 사용자와 확인한다.
- [ ] **Step 2: 매니페스트 작성.** gpt-image 스킬의 `references/image-workflows.md` 멀티 이미지 절 형식으로 workspace 안에 manifest를 만들고, 4개 job 모두 `heat_bar_frame.png`를 첫 번째 reference로, `background: transparent`로 지정한다.
- [ ] **Step 3: 배치 실행.** `node .../gpt_image.mjs batch --manifest <manifest 경로>`. Expected: 4개 PNG 생성. 실패한 job은 보고하고 그 job만 다시 실행(API 경로로 우회 금지).
- [ ] **Step 4: 사용자 확인.** 4장을 채팅에 보여 주고 승인받는다. 불만인 장은 해당 이미지를 `--edit-target`으로 수정한다.
- [ ] **Step 5: 알파·여백 확인.** 각 PNG에 알파가 있는지, 가장자리에 불필요한 투명 여백이 없는지 `execute_code`나 이미지 열람으로 확인한다. 여백이 있으면 재생성하거나 잘라낸다.

### Task 3: 임포트와 9-slice 설정

**Files:**
- Create: `Assets/Sprites/Minigame/Heat/*.png` (5장)

**Interfaces:**
- Consumes: Task 1~2의 승인된 PNG 5장.
- Produces: 스프라이트 에셋 5개(`heat_bar_frame`, `heat_target_zone`, `heat_indicator`, `heat_time_frame`, `heat_time_fill`). Sprite Mode Single, 투명 알파 유지, 필터 Bilinear, 압축 없음. 늘어나는 3종(`target_zone`, `indicator`, `time_fill`)은 `spriteBorder`가 설정됨.

- [ ] **Step 1: 복사.** PNG 5장을 `Assets/Sprites/Minigame/Heat/`로 복사(`manage_asset`)하고 임포트 완료를 기다린다.
- [ ] **Step 2: 임포터 설정.** `TextureImporter`로 `textureType = Sprite`, `spriteImportMode = Single`, `alphaIsTransparency = true`, `mipmapEnabled = false`, 압축 None을 적용한다.
- [ ] **Step 3: 9-slice 테두리.** 늘어나는 3종의 `spriteBorder`를 각 이미지의 장식 끝 부분을 보고 정한다 (픽셀 값은 생성 결과에 따름). 늘어나는 방향의 가운데는 단색에 가까운 영역만 남도록 한다.
- [ ] **Step 4: 확인.** 콘솔에 임포트 경고·오류가 없는지 `read_console types=[error,warning]`로 확인한다. Expected: 새 경고 없음.
- [ ] **Step 5: 커밋.**

```bash
git add Assets/Sprites/Minigame
git commit -m "feat : 가열 미니게임 이미지 5종 임포트 (gpt-image)"
```

### Task 4: 씬 연결

**Files:**
- Modify: `Assets/Scenes/Game.unity` (`GameUICanvas/.../HeatPanel`)

**Interfaces:**
- Consumes: Task 3의 스프라이트 5개.
- Produces: 새 스프라이트가 연결된 `HeatPanel`. `Image.type`: `BarArea`·`TimeBar`는 Simple, `TargetZone`·`Indicator`·`TimeFill`은 Sliced. 색(`Image.color`)은 코드가 덮어쓰므로 건드리지 않는다.

- [ ] **Step 1: 변경 내용 고지.** CLAUDE.md 규칙에 따라 mutating 코드를 실행하기 전에 무엇을 바꾸는지 사용자에게 알린다. `Game.unity`는 이미 수정 상태이므로 저장 시점은 사용자와 정한다.
- [ ] **Step 2: Game 씬 열기.** 활성 씬(`TItle`)이 저장되지 않았으면 사용자 동의를 받아 Game 씬을 additive로 열거나 활성 씬을 전환한다.
- [ ] **Step 3: 연결.** `execute_code`로 `HeatPanel` 하위 5개 오브젝트를 이름으로 찾아 `Image.sprite`를 지정하고 `Image.type`을 위 표대로 설정한다. (`TargetZone`/`Indicator`/`TimeFill`/`BarArea`/`TimeBar`가 모두 찾아졌는지 코드 안에서 null 확인)
- [ ] **Step 4: 정적 검증.** Missing Script와 null 참조가 없는지 `GetComponentsInChildren<Component>(true)`로 확인하고 콘솔을 읽는다. Expected: 에러 0.

### Task 5: 플레이 확인과 마무리

**Files:**
- Modify: `docs/superpowers/specs/2026-11-19-minigame-art-stage1-design.md` (확정 값 기록)

**Interfaces:**
- Consumes: Task 4의 연결된 씬.

- [ ] **Step 1: 플레이 진입.** 플레이 모드에서 Title → 새 게임/로드 → 모루 클릭으로 가열 미니게임을 연다 (필요하면 `HeatMinigameView.Show`를 `execute_code`로 직접 호출). 확인 후 **반드시 플레이 모드를 종료한다.**
- [ ] **Step 2: Review Focus 확인.** 아래를 스크린샷이나 값으로 확인한다.
  - 난이도 1과 10의 `TargetZone` 높이에서 9-slice가 깨지지 않는다.
  - `Indicator`가 0.0·1.0 위치에서 프레임 밖으로 잘리지 않는다.
  - 영역 안(초록)/밖(주황) 틴트가 구분된다.
  - `TimeFill`이 줄어들 때 끝이 깨지지 않는다.
  - `TargetZone`이 `BarArea` 틀 위에서 보인다 (렌더 순서).
- [ ] **Step 3: 한계 보고.** 포커스가 없으면 프레임이 거의 진행되지 않으므로, 실제 플레이 감각·애니메이션 부드러움처럼 확인하지 못한 항목을 그대로 보고한다.
- [ ] **Step 4: 문제 수정.** 틴트가 묻히거나 9-slice가 깨지면 해당 이미지를 `--edit-target`으로 수정해 Task 2~4의 해당 부분만 다시 한다.
- [ ] **Step 5: 기록과 커밋.** 확정한 `spriteBorder` 값과 PPU, 남은 문제를 spec 문서에 기록하고 커밋한다. 씬 저장과 씬 커밋은 사용자가 요청할 때만 한다.

```bash
git add docs/superpowers/specs/2026-11-19-minigame-art-stage1-design.md
git commit -m "docs : 가열 미니게임 이미지 1단계 확정 값 기록"
```
