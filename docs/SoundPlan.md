# 미니게임 효과음 계획

## Context
MVP(v1) 완료 기준에 "미니게임 적중 사운드"와 BGM이 들어 있다 (`docs/MVP.md:44-45`). 현재 프로젝트에는 오디오 코드와 에셋이 전혀 없다. 이펙트(시각)는 v2, 아트/사운드 방향은 기획서상 **미정**이다. 그래서 이 계획은 "몇 개를, 어떤 종류로, 어디에 붙이는가"만 정하고, 음색·스타일은 정하지 않는다 (임시 클립 사용).

## 효과음 목록

### A. v1 필수 (적중 사운드) — 9개
| 미니게임 | 클립 | 종류 | 재생 시점 (코드 위치) |
|---|---|---|---|
| 가열 | `heat_hold` | 루프 | 누르는 동안 (`HeatMinigame.IsHolding`, 뷰 `OnPointerDown/Up`). 피치·볼륨은 `Value`에 연동(선택) |
| 단조 | `forge_perfect` | 원샷 | `HandleClick`에서 Perfect 판정 (`ForgeMinigame.cs:358`) |
| 단조 | `forge_good` | 원샷 | Good 판정 |
| 단조 | `forge_miss` | 원샷 | 자동 Miss (`:322-327`) 및 빈 곳 클릭 |
| 열처리 | `quench_full` | 원샷 | 점수 1 (`HeatTreatMinigame.HandleClick:143`) |
| 열처리 | `quench_half` | 원샷 | 점수 0.5 |
| 열처리 | `quench_fail` | 원샷 | 점수 0 / 시간 초과 |
| 연마 | `sand_loop` | 루프 | 흠 안에서 드래그하며 실제 진행(`inside > 0`)될 때만 (`PolishMinigame.cs:180`) |
| 연마 | `flaw_cleared` | 원샷 | 흠 하나 완료 (`:185`) |

### B. 선택 (손맛 보강, 시간 남으면) — 최대 5개
- `stage_complete`: 각 단계 종료 (`CraftingController.OnStageCompleted:166`), 완성도로 성공/실패 분기하면 2개
- `craft_finished`: 제작 완료 (`FinishCrafting:176`), 품질별 변형은 v2 이후로 미룸
- `forge_ring_in`: 단조 원 생성 틱 (`SpawnTime`)
- `polish_miss_press`: 흠 밖을 눌렀을 때

### C. BGM — 2곡 (사용자 확정: 사운드 작업은 BGM까지만)
| 클립 | 종류 | 재생 구간 |
|---|---|---|
| `bgm_default` | 루프 | 기본 상태: 대장간 화면(제작 패널 열림 포함). Title 씬에서도 쓸지는 미정 → 같은 곡을 이어 쓰는 것을 기본안으로 함 |
| `bgm_crafting` | 루프 | 제작 미니게임 진행 중 (4단계 전체에서 같은 곡 유지, 단계마다 곡을 바꾸지 않음) |

전환 시점 (`CraftingController`):
- `bgm_default` → `bgm_crafting`: 제작 시작 (`OnStartRequested:78`, 첫 단계 `TryStartStage:108` 직전)
- `bgm_crafting` → `bgm_default`: `FinishCrafting:176` 또는 `AbortCrafting:205` 이후
- 전환은 짧은 크로스페이드(약 0.5~1초)로 처리. 길이는 임시값이며 조정 가능.
- 단계 스킵(`:100-104`)이나 단계 사이 전환에서는 BGM을 건드리지 않는다.

### D. 범위 밖
UI 버튼·상점 소리(문서에 언급 없음), 품질별 완성음, 이펙트(v2).

## 구현 방식 (요청 시 별도 승인 후)
- 모델에는 오디오 의존성을 넣지 않는다 (UI/로직 분리 규칙). 모델에 이벤트 추가: `ForgeMinigame.Judged(CircleState)`, `HeatTreatMinigame.Judged(float)`, `PolishMinigame.FlawCleared`. 가열은 뷰의 누름 상태, 연마 드래그 루프는 뷰가 "진행 중" 여부를 읽어 처리.
- `SfxClips` ScriptableObject(클립 필드 모음) + 재생기 컴포넌트 `SfxPlayer`(`BlackSmith.UI` 또는 새 `Audio` 폴더 → 네임스페이스 규칙상 `BlackSmith.Audio`). 새 Singleton은 만들지 않고 `CraftingController`가 참조를 뷰에 전달.
- 루프는 전용 AudioSource 1개씩, 원샷은 `PlayOneShot`. 판정 사운드끼리 겹쳐도 되게 풀 불필요.
- BGM은 `BgmPlayer`(AudioSource 2개로 크로스페이드)가 담당. `CraftingController`는 `BgmPlayer`를 직접 호출하지 않고 기존 흐름에서 `CraftStarted`/`CraftFinished`/중단 지점에 이벤트를 두어 연결한다(UI/로직 분리). Title↔Game 씬 이동 시 곡이 끊겨도 되는지는 구현 전에 확인.
- 클립은 `Assets/Audio/Sfx/`, `Assets/Audio/Bgm/`에 임시 파일로 배치. 음색은 미정이므로 교체 가능하게 SO로만 연결.

## 검증
- 컴파일 체크 후 Title/Game 씬 Missing Script·콘솔 확인 (Unity MCP는 현재 연결 실패 상태라 재연결 필요).
- 플레이 모드에서 4개 미니게임을 각각 돌려 판정별 소리, 루프 시작·정지(누름 해제·`Hide`·`Cancel` 시 반드시 정지)를 확인하고, 플레이 모드를 종료한다.

## E. 확정 사항 (설정 화면 / 연결 방식)
- 소리 크기: 슬라이더 2개(BGM / 효과음), AudioMixer 없이 각 재생기 볼륨에 곱함. `PlayerPrefs`에 저장, 기본값 BGM 0.7 / 효과음 1.0 (임시).
- BGM은 씬마다 처음부터 재생한다 (DontDestroyOnLoad 사용 안 함). Title과 Game 모두 `bgm_default`로 시작하고, 제작 시작부터 완료까지만 `bgm_crafting`.
- 설정 진입: Title은 로드 게임과 닫기 버튼 사이의 설정 버튼, Game은 우상단 톱니바퀴 버튼(상점·책 버튼 왼쪽). 설정 패널을 열어도 게임은 일시정지하지 않는다.
- 코드: `Assets/Scripts/Audio` (`SoundSettings`, `SfxClips`, `SfxPlayer`, `BgmPlayer`, `CraftingBgmSwitch`), `Crafting/CraftingSfx`, `UI/SettingsPanel`.

