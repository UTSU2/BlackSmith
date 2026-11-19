# 미니게임 이미지 교체 1단계 (가열) 설계

## 의도와 배경

- 미니게임 UI 요소는 현재 대부분 `WhiteSquare`/`WhiteCircle`/`WhiteRing` 같은 흰색 기본 도형에 코드가 `Image.color`로 색을 입힌 상태다. 이를 gpt-image 스킬로 만든 이미지로 모두 교체한다.
- 전체를 한 번에 바꾸지 않고 단계별로 진행한다. 이 문서는 **1단계(가열 미니게임)** 만 다룬다.
- 1단계는 이후 단계의 **스타일 기준을 확정**하는 역할도 한다. 이후 단계는 1단계 결과를 `--reference`로 붙여 일관성을 맞춘다.

## 전체 단계 (참고, 2단계 이후는 각각 별도 문서로 구체화)

1. 가열 + 스타일 기준 확정 (이 문서)
2. 단조 (원 UI)
3. 열처리 (게이지, 마커)
4. 연마 (흠집)
5. 힛 이펙트 텍스처 (`SoftRing`, `SoftGlow` 등)

## 목표 / 비목표

- 목표
  - `HeatPanel`의 5개 `Image`를 생성 이미지로 교체한다.
  - 영역 안/밖 색 구분, 목표 구간 높이 변화, 시간 바 줄어듦이 교체 후에도 지금처럼 동작한다.
  - 확정된 스타일이 2단계 이후의 기준이 된다.
- 비목표
  - `PlayArea` 배경(1600×900, 반투명 갈색)은 이번 단계에서 교체하지 않는다. 기존 `forge_interior` 배경과 겹치기 때문이다.
  - 코드 변경 없음 (`HeatMinigameView`의 색 필드·로직은 그대로).
  - 가열 난이도·판정 값 변경 없음.
- 안티 목표: 스프라이트 여백 때문에 보이는 모양과 판정 범위가 어긋나는 것.

## 현재 구성 (`Game.unity` / `HeatPanel`)

| 오브젝트 | 크기 | 코드가 하는 일 |
|---|---|---|
| `BarArea` | 160×700 | 고정. 바 영역 |
| `BarArea/TargetZone` | 폭 가득 × 가변 높이 | 난이도에 따라 `anchorMin/Max.y`로 높이가 바뀜. `zoneColor`로 틴트 |
| `BarArea/Indicator` | 폭 가득 × 18 | `anchor.y`로 위치 이동. 영역 안/밖에 따라 `indicatorInZoneColor`/`indicatorOutColor`로 틴트 |
| `TimeBar` | 700×24 | 고정. 틀 |
| `TimeBar/TimeFill` | 폭 가변 | `anchorMax.x`로 줄어듦 |

모든 `Image`의 `type`은 현재 Simple이고 스프라이트는 흰 사각형(`WhiteSquare`)이다.

## 제약

- 색 상태는 코드 틴트로 유지하므로 `TargetZone`, `Indicator`, `TimeFill`용 스프라이트는 **흰색/무채색 베이스**여야 한다.
- 늘어나거나 줄어드는 요소(`TargetZone`, `Indicator`, `TimeFill`)는 **9-slice** 로 임포트하고 `Image.type`을 `Sliced`로 바꾼다.
- 모든 이미지는 `--background transparent`(실제 알파)로 생성한다.
- 판정은 `RectTransform` 기준이므로 스프라이트는 **여백 없이 꽉 차게** 만든다.
- `Game.unity`는 이미 수정된 상태(미커밋)이므로, 씬 저장은 사용자가 요청할 때만 한다.
- CLAUDE.md 검증 절차(컴파일 필요 시 확인, Missing Script/참조/콘솔 확인)를 따른다.

## 접근 방식

- **A. 미니게임 순서대로**: 한 단계가 끝나면 한 미니게임을 플레이로 확인할 수 있으나, 미니게임 간 스타일이 어긋날 수 있다.
- **B. 요소 종류별로**: 스타일은 통일되나 단계 종료 시 완성된 미니게임이 없어 확인이 어렵다.
- **C. 스타일 기준부터**: 일관성에 좋으나 1단계 결과가 게임에 바로 적용되지 않는다.
- **선택: A + C 결합.** 가열을 먼저 만들면서 그 결과를 스타일 기준으로 확정한다. 단계 종료 시 플레이 확인이 가능하고, 2단계부터 기준이 생긴다.

## 작업 순서

1. **스타일 기준 확정**: 기존 `forge_interior`를 `--reference`로 붙여 `BarArea` 틀 1장을 먼저 생성하고, 사용자 확인으로 기준을 확정한다.
2. **나머지 생성**: 목표 구간, 표시기, 시간 바 틀, 시간 바 채움 4장을 확정된 기준 이미지를 `--reference`로 붙여 배치(병렬) 생성한다.
3. **임포트**: `Assets/Sprites/Minigame/Heat/`에 저장하고, 9-slice 테두리와 PPU를 설정한다.
4. **씬 연결**: `HeatPanel`의 5개 `Image`에 스프라이트를 연결하고 `Image.type`을 맞춘다.
5. **확인**: 플레이에서 다음을 본다.
   - 구간 높이가 바뀌어도 왜곡이 없는지
   - 영역 안/밖 틴트가 맞는지
   - 시간 바가 줄어들 때 끝이 깨지지 않는지
   - 표시기가 구간과 겹칠 때 잘 보이는지

## 설계자의 판단 (확인 필요)

- `PlayArea` 배경은 이번 단계 제외.
- 틴트는 코드 유지, 코드 변경 없음.
- 저장 경로 `Assets/Sprites/Minigame/Heat/`는 임의 지정이므로 변경 가능.
- 생성 이미지의 크기·9-slice 테두리 픽셀 값은 생성 결과를 보고 구현 중에 정한다.

## 구현자에게 남기는 것

- 프롬프트 문구는 사용자와 확인하며 정한다 (스킬 규칙상 사용자가 준 프롬프트는 그대로 쓰고, 위임된 컨셉만 직접 작성한다).
- 확인 방법의 한계: 스크린샷은 Screen Space - Camera인 Game 캔버스에서는 캡처되지만, 포커스가 없으면 플레이 모드 프레임이 거의 진행되지 않는다. 확인하지 못한 항목은 그대로 보고한다.

## 확정된 스타일 (Task 1~3 결과)

- 기준 이미지: `Assets/Sprites/Minigame/Heat/heat_bar_frame.png` (레퍼런스: `Assets/Backgrounds/forge_interior_storybook.png`). 만화풍, 굵고 부드러운 어두운 갈색 외곽선, 주황 나무 결 + 회색 쇠띠 + 리벳, 단순한 평면 채색. 이후 단계 이미지는 이 파일을 `--reference`로 붙인다.
- 생성 시 Codex 샌드박스가 작업 폴더를 읽기 전용으로 잡아 PNG가 `~/.codex/generated_images/`에 저장된다. 그 파일을 `generated-images/`로 복사해 쓴다. 불투명 영역 기준으로 투명 여백을 잘랐고 원본은 `generated-images/raw/`에 있다 (둘 다 git 미추적).
- 임포트 값 (크기는 원본 그대로, maxTextureSize 4096, 압축 없음, Bilinear):

| 스프라이트 | 크기 | PPU | 9-slice (L,B,R,T) | 의도한 표시 높이/폭 |
|---|---|---|---|---|
| heat_bar_frame | 758×2073 | 100 | 없음 | 비율 0.366 (BarArea 160×700 = 0.229와 다름) |
| heat_target_zone | 343×2072 | 214 | 20,175,20,175 | 폭 160 |
| heat_indicator | 1672×94 | 523 | 47,0,47,0 | 높이 18 |
| heat_time_frame | 2141×203 | 846 | 230,0,230,0 | 높이 24 |
| heat_time_fill | 2140×95 | 594 | 47,0,47,0 | 높이 16 (틀 안쪽 inset 후) |

- 씬 연결(Task 4)에서 처리할 것: 틀 비율이 `BarArea`와 달라 프레임 전용 Image를 두고 속 영역 패딩 자식에 `TargetZone`/`Indicator`를 넣는다(`barArea` 참조를 그 자식으로 변경). `TimeFill`은 틀의 속 영역에 맞춰 anchor inset을 준다. 코드 변경은 없다.

## 씬 연결 결과 (Task 4~5)

- PPU 최종 조정: `heat_target_zone` 490 (표시 폭 70 기준), `heat_time_fill` 950 (표시 높이 10 기준). 나머지는 위 표 그대로.
- `HeatPanel` 구조 변경 (코드 변경 없음, `HeatMinigameView`는 그대로):
  - `BarArea`: 크기 256×700, `heat_bar_frame`(Simple), 색 흰색. 틀 이미지 전용.
  - `BarArea/Track` (신규): 70×543.7, 위치 (0, -29.9). 틀의 속 영역(원본 x 273~479, y 320~1930)에 맞춤. `TargetZone`/`Indicator`가 이 안으로 이동했고 `HeatMinigameView.barArea`도 `Track`을 가리킨다.
  - `TimeBar/FillArea` (신규): 앵커 0~1, offset (11,7)/(-11,-8). `TimeFill`이 이 안으로 이동. `HeatMinigameView.Draw`가 `TimeFill`의 offset을 매 프레임 0으로 덮어쓰므로 inset은 부모에 둔다.
  - `TargetZone`/`Indicator`/`TimeBar`/`TimeFill`: Sliced. `BarArea`: Simple.
- 검증: Game 씬 Missing Script 0 (컴포넌트 2015개), `HeatMinigameView` 참조 5개 정상, 콘솔 오류 0 (additive 로드 시 `More than one global light` 경고만). 화면은 편집 모드에서 Game 캔버스 카메라로 직접 렌더링해 구간 위치/표시기 안·밖/시간 바 길이 4가지 상태를 확인했다 (런타임 로직·입력·애니메이션은 확인하지 않음).
