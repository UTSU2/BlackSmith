# 미니게임 이미지 교체 3단계 (열처리) 설계

앞 단계: `2026-11-19-minigame-art-stage1-design.md`, `2026-11-20-minigame-art-stage2-design.md`. 스타일 기준은 `heat_bar_frame.png`(만화풍, 굵은 갈색 외곽선, 주황 나무 + 회색 쇠띠). 시간 바는 2단계에서 이미 교체했다. 작은 변경이므로 짧은 설계이며 "이어서 진행해줘"로 진행을 승인받았다.

## 범위

새로 생성 (3장, 모두 `--reference heat_bar_frame.png`, `--background transparent`):

| 이미지 | 대상 | 조건 |
|---|---|---|
| `heat_treat_gauge_frame` | `GaugeArea` 뒤의 틀 | 가로로 매우 긴 나무 틀, 속은 어두운 숯색. 9-slice로 늘림 |
| `heat_treat_zone` | `TargetZone` | 흰색/무채색의 둥근 직사각 띠. 폭이 난이도에 따라 변하므로 9-slice(사방) |
| `heat_treat_marker` | `Marker` | 흰색/무채색 구슬(원). 정사각, 원이 이미지에 꽉 참 |

제외: `PlayArea` 배경, 제목 글자, 시간 바(2단계 완료), 코드 변경.

## 제약 (`HeatTreatMinigameView`)

- 판정 크기: `game.MarkerHalfSize = marker.rect.width * 0.5 / gaugeArea.rect.width`. 따라서 **`Marker` 64×64와 `GaugeArea` 논리 크기 1200×120을 바꾸지 않는다.** 마커 스프라이트는 정사각이고 원이 꽉 차야 한다.
- 마커는 `gaugeArea` 앵커 x(0~1)로 이동하고 `Image.color`로 틴트된다(진행 중 주황, 판정 뒤 나쁨 빨강 ↔ 좋음 초록 보간). 구간은 `zoneColor`(초록 0.4)로 틴트. 그러므로 구간·마커는 흰색/무채색 베이스.
- `TargetZone`은 앵커 x가 `[c-h, c+h]`, y는 0~1 전체. 폭은 난이도에 따라 변하고 높이는 `GaugeArea`(120)에 고정.
- 틀은 시각용이므로 `GaugeArea`의 논리 영역과 분리한다: `GaugeArea`의 자체 Image를 비활성화하고, 자식 `Frame`(Sliced)을 맨 뒤에 두어 `GaugeArea`보다 사방으로 조금 크게 그린다. 속 영역이 `GaugeArea` rect와 일치하도록 크기를 맞춘다. `HeatTreatMinigameView` 참조는 그대로.
- 씬 저장은 사용자가 요청할 때만. Game 씬은 additive로 연다.

## 작업 순서

1. 프롬프트 작성(위임된 창작) → 3개 job 배치 생성 → `~/.codex/generated_images/`에서 복사 → 필요하면 투명 여백 크롭.
2. `Assets/Sprites/Minigame/HeatTreat/`에 임포트, 9-slice 값을 생성물에서 측정해 설정.
3. 씬 연결(`Frame` 신설, `TargetZone`/`Marker` 스프라이트).
4. 렌더링 확인: 구간 폭 3종, 마커 위치 0/0.5/1, 판정 뒤 색(빨강~초록), 틀 속 영역과 `GaugeArea` 일치, Missing Script·콘솔.

## 판단 (확인 필요)

- 틀 `Frame` 크기·9-slice 값은 생성물의 속 영역을 측정한 뒤 정한다.
- 구간 띠는 높이 120에 맞는 둥근 직사각(캡슐 아님)으로 한다.

## 결과 (2026-11-20)

- 생성: `heat_treat_gauge_frame` (2172×207 크롭), `heat_treat_zone` (1619×971), `heat_treat_marker` (1254×1254 정사각). 샌드박스 쓰기 제한으로 `~/.codex/generated_images/`에서 복사. 원본은 `generated-images/raw/`.
- 임포트 (`Assets/Sprites/Minigame/HeatTreat/`, Sprite/Single, 알파 투명, 압축 없음, maxTextureSize 4096):

| 스프라이트 | PPU | 9-slice (L,B,R,T) |
|---|---|---|
| heat_treat_gauge_frame | 154 | 106,61,94,68 (속 영역 x 106~2078, y 68~145를 측정) |
| heat_treat_zone | 550 | 110,110,110,110 |
| heat_treat_marker | 100 | 없음 |

- 씬 (`HeatTreatPanel/PlayArea/GaugeArea`): `GaugeArea`는 1200×120 논리 영역으로 유지하고 자체 Image만 비활성화. 맨 뒤 자식 `Frame`(Sliced) 신설, 크기 1329.2×203.8, 위치 (-3.6, 2.3) — 9-slice 캡·두께(좌 68.2, 우 61, 위 44.2, 아래 39.6유닛)를 더하면 속 영역이 정확히 1200×120. `TargetZone`은 Sliced, `Marker`는 64×64 Simple. `HeatTreatMinigameView` 참조·코드는 그대로이므로 판정 난이도(`MarkerHalfSize`)는 불변.
- 검증: Game 씬 Missing Script 0 (컴포넌트 2020개), 뷰 참조 5개 정상, 콘솔 오류 0(additive light 경고만). 편집 모드 렌더링: 구간 폭 2종(0.08/0.2), 마커 위치 0/0.1/0.7/1.0, 진행 중 주황·판정 뒤 초록·빨강 틴트. 틀 속 영역이 논리 영역과 일치함을 확인. 플레이 모드 입력·판정은 확인하지 않았다.
- 알려진 점: 마커가 0/1에 있으면 마커 절반이 게이지 밖으로 나가 틀 끝의 리벳 위에 겹친다 (기존에도 게이지 밖으로 나가는 동작).
