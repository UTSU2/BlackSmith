# 미니게임 이미지 교체 2단계 (단조 + 시간 바 재사용) 설계

1단계: `2026-11-19-minigame-art-stage1-design.md` (스타일 기준 `heat_bar_frame.png`, 임포트·씬 구조 방식). 이 문서는 짧은 설계(작은 변경)이며 사용자가 제안 내용을 승인했다.

## 의도

- 미니게임 UI를 gpt-image 이미지로 단계별 교체한다. 2단계는 **단조**의 원 2장을 새로 만들고, 열처리·연마의 **시간 바는 1단계 이미지를 재사용**한다 (사용자 결정: "시간 바는 모두 재사용").

## 범위

- 새로 생성 (2장): `forge_target`(Target 원), `forge_shrinking`(Shrinking 링). 모두 `generated-images/heat_bar_frame.png`를 `--reference`로 붙이고 `--background transparent`.
- 재사용 (생성 없음): `HeatTreatPanel/PlayArea/TimeBar`, `PolishPanel/PlayArea/TimeBar`에 `heat_time_frame`(Sliced) + `FillArea`(offset 11,7/-11,-8) + `TimeFill`에 `heat_time_fill`(Sliced). 1단계 `HeatPanel`과 같은 구조.
- 제외: `PlayArea` 배경(3개 패널), 패널 제목 글자, 열처리·연마의 나머지 요소(3·4단계), 힛 이펙트(5단계). 코드 변경 없음.

## 제약

- `Target`/`Shrinking`은 코드가 `Image.color`로 틴트한다 (Target: 대기 흰색 0.35, Perfect 초록·Good 노랑·Miss 빨강 0.8 / Shrinking: 주황 0.9). 그러므로 **흰색/무채색 베이스**. 외곽선의 어두운 갈색은 틴트 후에도 남는다.
- `Target`은 정사각형에 원이 가득 차게(여백 없음), `Shrinking`은 **바깥 가장자리가 이미지 가장자리에 정확히 맞는 속이 빈 링** (코드가 링 바깥 가장자리를 판정 반경으로 쓴다). 둘 다 정사각형, 원의 중심이 이미지 중심.
- 크기는 코드가 `sizeDelta`로 정하므로 스프라이트는 Simple, 9-slice 없음. 원 크기 변화에 링 두께가 비례해 변하는 것은 허용.
- 단조 힛 이펙트 불변식(CLAUDE.md): 이펙트가 판정 원을 덮으면 안 된다. 이펙트 프리팹·`effectReferenceDiameter`(160)는 건드리지 않는다.
- 시간 바: `HeatTreat/PolishMinigameView.Draw`가 `TimeFill` offset을 매 프레임 0으로 덮어쓰므로 inset은 부모 `FillArea`에 둔다.
- 씬 저장은 사용자가 요청할 때만. Unity MCP로 Game 씬을 additive로 열어 작업한다.

## 작업 순서

1. 프롬프트 작성(위임된 창작): Target = 쇠 접시/달궈진 쇠 느낌의 둥근 판, Shrinking = 굵은 불씨 링. 스타일은 `heat_bar_frame`.
2. 2개 job 배치 생성 → `~/.codex/generated_images/`에서 복사 → 불투명 영역 기준 정사각 크롭.
3. `Assets/Sprites/Minigame/Forge/`에 임포트 (Sprite/Single, 알파 투명, 압축 없음, maxTextureSize 4096).
4. Game 씬: `CircleTemplate/Target`, `CircleTemplate/Shrinking` 스프라이트 연결(색은 코드가 정함), 열처리·연마 `TimeBar` 구조 적용.
5. 확인: 템플릿 복제본으로 Pending/Perfect/Good/Miss 틴트와 링이 원에 정확히 닿는 순간을 렌더링, 두 시간 바 렌더링, Missing Script·콘솔 확인.

## 판단 (확인 필요)

- 열처리·연마 시간 바도 이번 단계에서 함께 적용한다 (사용자의 "모두 재사용" 해석).
- 원 이미지 해상도는 생성 결과 기준(원본 유지, 모서리 정사각 크롭).

## 결과 (2026-11-20)

- 생성: `forge_target.png`, `forge_shrinking.png` (각 1254×1254, 정사각, 원이 이미지 가장자리까지 가득, 링은 가운데 실제 투명). 샌드박스 쓰기 제한으로 `~/.codex/generated_images/`에서 복사했고 크롭은 필요 없었다. 원본은 `generated-images/raw/`.
- 임포트: `Assets/Sprites/Minigame/Forge/` Sprite/Single, PPU 100, 9-slice 없음, maxTextureSize 4096, 압축 없음.
- 씬: `ForgePanel/PlayArea/CircleTemplate`의 `Target`/`Shrinking`에 스프라이트 연결(Simple). `HeatTreatPanel`·`PolishPanel`의 `TimeBar`에 `heat_time_frame`(Sliced, 흰색), `FillArea`(offset 11,7/-11,-8) 신설, `TimeFill`에 `heat_time_fill`(Sliced)을 1단계와 같은 구조로 적용. `HeatTreat/PolishMinigameView.timeFill` 참조는 그대로 `TimeFill`(부모 `FillArea`).
- 검증: Game 씬 Missing Script 0 (컴포넌트 2017개), `ForgeMinigameView` 참조 모두 정상, 콘솔 오류 0(additive light 경고만). 편집 모드에서 템플릿 복제본으로 링 3단계 크기(400/240/160), 대기·Perfect·Good·Miss 틴트, 두 시간 바(0.6)를 Game 캔버스 카메라로 렌더링해 확인. 플레이 모드·실제 판정·이펙트 겹침은 확인하지 않았다.
- 알려진 점: 링이 판정 직전(지름 160)에 도달하면 링 두께(약 1/7)만큼 `Target` 테두리를 덮는다. 기존 `WhiteRing`과 같은 방식이다.
