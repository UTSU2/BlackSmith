# 미니게임 이미지 교체 4단계 (연마) 설계

앞 단계: stage1~3 문서(같은 폴더). 스타일 기준 `heat_bar_frame.png`. 시간 바는 2단계에서 교체 완료. 작은 변경이므로 짧은 설계이며 "다음 4단계 진행해줘"로 진행을 승인받았다.

## 범위

- 새로 생성 1장: `polish_flaw` (`PolishPanel/PlayArea/FlawTemplate`의 흠집).
- 제외: `PlayArea` 배경(회청색 판), 제목 글자, 코드 변경, 힛 이펙트(5단계).

## 제약 (`PolishMinigameView`)

- 흠집은 `FlawTemplate`(Image 하나)를 복제해 쓰며, 지름은 코드가 `flawRadius × 2 × scale`로 `sizeDelta`에 넣는다. 판정과 반짝임 이펙트(`effectReferenceDiameter` 180)도 이 지름 기준이므로 **원에 가까운 모양**이고 이미지에 꽉 차야 한다 (정사각, 여백 없음).
- 색은 코드가 틴트한다: 기본 (0.55,0.25,0.1), 문지르는 중 (0.9,0.45,0.15), 알파는 남은 양에 따라 0.25~1. 따라서 **흰색/무채색 베이스, 외곽선 없음**(외곽선이 있으면 틴트에 곱해져 검게 보임), 가장자리는 부드럽게 흐려지는 얼룩.
- 입력은 `PlayArea`가 받는다(흠집 `raycastTarget = false`는 코드가 설정). 9-slice 없음(Simple).
- 씬 저장은 사용자가 요청할 때만. Game 씬은 additive로 연다.

## 작업 순서

1. 프롬프트 작성 → 생성 → `~/.codex/generated_images/`에서 복사 → 필요하면 투명 여백 크롭.
2. `Assets/Sprites/Minigame/Polish/`에 임포트 (Sprite/Single, PPU 100, 압축 없음, 4096).
3. `FlawTemplate` 스프라이트 연결.
4. 렌더링 확인: 남은 양 1.0/0.5/0.1(알파)과 기본·활성 색, 여러 지름.

## 판단 (확인 필요)

- 흠집은 녹/그을음 얼룩으로 한다 (연마로 지워야 하는 대상).

## 결과 (2026-11-20)

- 생성: `polish_flaw.png` — 녹/그을음 얼룩, 흰색~중간 회색 무채색, 외곽선 없음, 가장자리가 부드럽게 투명. 원본 1254×1254에서 불투명 영역 기준으로 1213×1213 정사각 크롭(여백 약 2%). 샌드박스 쓰기 제한으로 `~/.codex/generated_images/`에서 복사. 원본은 `generated-images/raw/`.
- 임포트: `Assets/Sprites/Minigame/Polish/polish_flaw.png`, Sprite/Single, PPU 100, 9-slice 없음, 알파 투명, 압축 없음, maxTextureSize 4096.
- 씬: `PolishPanel/PlayArea/FlawTemplate`의 `Image`에 연결(Simple, 100×100, 비활성 유지). 코드 변경 없음.
- 검증: Game 씬 Missing Script 0, `PolishMinigameView` 참조 정상, 복제본 잔여 0, 콘솔 오류 0(additive light 경고만). 편집 모드 렌더링으로 기본 색(0.55,0.25,0.1)·활성 색(0.9,0.45,0.15), 남은 양 1.0/0.5/0.1에 따른 알파, 지름 180/120을 확인. 플레이 모드 문지르기·판정·반짝임 이펙트 겹침은 확인하지 않았다.
- 알려진 점: 기본 색에서 얼룩이 꽤 어둡다(회색 베이스 × 어두운 갈색). 가시성이 부족하면 `flawColor`를 조정하거나 스프라이트를 밝게 다시 생성한다 (이번에는 현 색 유지).
