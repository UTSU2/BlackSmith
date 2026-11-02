# BlackSmith

망치를 직접 두드려 장비를 만드는 대장장이 제작 게임입니다.

## 게임 개요

BlackSmith는 **제작 중심**의 게임입니다. 플레이어는 대장간에서 직접 망치를 두드려 장비를 만듭니다.

- **상점**에서 광물을 구입할 수 있습니다.
- 제작한 **장비를 상점에 판매**할 수 있습니다.
- 장비는 **가열, 단조, 열처리, 연마** 네 단계를 거쳐 만들어집니다.
- 각 단계의 **완성도**에 따라 장비의 **등급**이 결정됩니다.

## 핵심 루프

```
광물 구매 (상점) → 제작 (가열 → 단조 → 열처리 → 연마) → 등급 결정 → 장비 판매 (상점)
```

## 제작 단계와 등급

| 순서 | 단계 | 진행 방식 / 조작 | 완성도 판정 |
|---|---|---|---|
| 1 | 가열 | 미정 | 미정 |
| 2 | 단조 | 미정 | 미정 |
| 3 | 열처리 | 미정 | 미정 |
| 4 | 연마 | 미정 | 미정 |

- 각 단계의 완성도가 장비 등급에 반영됩니다.
- 단계별 진행 방식, 완성도를 측정하는 방법, 등급의 이름과 개수, 등급을 정하는 계산 방식은 모두 **미정**입니다.

## 현재 구현 현황

### 구현됨

- **타이틀 씬 (`TItle`)**
  - 대장간 배경: 기본 도형 스프라이트를 조합해 만든 벽, 바닥, 천장 들보, 화로, 모루, 선반
  - 조명과 연출: 화로 위치의 주황색 Point Light 2D, 불꽃 파티클
  - 메뉴 버튼 3개 (우측 하단, Fantasy Wooden GUI 에셋 사용)
    - 새 게임: `Game` 씬으로 이동
    - 로드 게임: 버튼만 있고 기능은 아직 없음
    - 닫기: 게임 종료 (에디터에서는 플레이 모드 종료)
  - 버튼을 누르는 동안 글자도 함께 내려가는 눌림 연출
- **게임 씬 (`Game`)**
  - 대장간 배경
  - 우측 상단 상점 아이콘 버튼 (누르면 호출되는 함수만 있고 기능은 아직 없음)

### 구현 예정

- 로드 게임과 저장
- 상점 UI, 광물 구매, 장비 판매
- 제작 4단계 플레이 (가열, 단조, 열처리, 연마)
- 완성도 계산과 등급 판정

구현 예정 항목의 세부 사양은 아직 정해지지 않았습니다.

## 씬 구성

| 빌드 인덱스 | 씬 | 용도 |
|---|---|---|
| 0 | `Assets/Scenes/TItle.unity` | 타이틀 / 메인 메뉴 |
| 1 | `Assets/Scenes/Game.unity` | 게임 화면 |

## 스크립트

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/MainMenu.cs` | 타이틀 메뉴 버튼 동작 (새 게임, 로드 게임, 닫기) 과 버튼 글자 폰트 적용 |
| `Assets/Scripts/GameHUD.cs` | `Game` 씬 UI. 상점 버튼이 호출할 `OnShop()`만 있고 본문은 비어 있음 |
| `Assets/Scripts/ButtonLabelPress.cs` | 버튼이 눌림 이미지일 때 글자를 함께 내리는 연출 |
| `Assets/Scripts/Items/ItemData.cs` | 아이템 공통 데이터(ScriptableObject): ID, 이름, 아이콘, 설명, 최대 스택, 기본 등급, 판매가, 등급별 판매 배율 |
| `Assets/Scripts/Items/MineralItemData.cs` | 광물 아이템. 구매와 판매 가능 (구매가 포함) |
| `Assets/Scripts/Items/CraftedItemData.cs` | 제작품 아이템. 판매만 가능. 제작품 전용 정보는 규칙 확정 후 추가 예정 |
| `Assets/Scripts/Items/ItemType.cs`, `ItemGrade.cs` | 아이템 종류 enum, 등급 enum (등급 이름과 개수는 임시) |
| `Assets/Scripts/Items/ItemDatabase.cs` | `Assets/Resources/Items/` 아래 모든 아이템을 게임 시작 시 자동으로 읽어 등록하는 데이터베이스 |
| `Assets/Editor/ItemCreatorWindow.cs` | 아이템 생성 툴. 메뉴 `Tools > BlackSmith > Item Creator` |

## 아이템 데이터베이스

- 아이템 하나가 ScriptableObject 에셋 하나입니다. 종류는 광물과 제작품 두 가지입니다.
  - 광물: 구매와 판매 가능
  - 제작품: 판매만 가능
- 아이템은 `Assets/Resources/Items/` 아래에 두면 `ItemDatabase`가 게임 시작 시 자동으로 읽어 등록합니다. 별도로 목록을 관리할 필요가 없습니다.
- 아이템은 `Tools > BlackSmith > Item Creator` 창에서 만듭니다. ID는 영소문자, 숫자, 밑줄만 쓸 수 있고 다른 아이템과 겹칠 수 없습니다. 광물은 `Assets/Resources/Items/Minerals/`, 제작품은 `Assets/Resources/Items/Crafted/`에 저장됩니다.
- 코드에서는 `ItemDatabase.Get(id)`, `ItemDatabase.TryGet(id, out item)`, `ItemDatabase.GetByType(type)`, `ItemDatabase.All`로 접근합니다.
- 판매가는 `기본 판매가 x 등급별 배율`입니다. 등급 이름과 개수, 배율 값은 미정이라 임시로 5등급, 배율 1로 두었습니다.

## 사용 에셋

| 폴더 | 내용 |
|---|---|
| `Assets/Fantasy Wooden GUI  Free` | 나무 느낌의 UI 버튼과 패널 (현재 타이틀 버튼에 사용) |
| `Assets/48 Free Minerals Pixel Art Icons Pack` | 광물 픽셀 아이콘 |
| `Assets/Medieval_Weapons` | 중세 무기 이미지와 데모 씬 |
| `Assets/Sprites` | 배경 제작에 쓴 흰색 사각형 / 원 도형 스프라이트 |

- 폴더 이름 `Fantasy Wooden GUI  Free`에는 "Free" 앞에 공백이 두 칸 있습니다.
- 외부 에셋의 라이선스와 출처는 이 문서에서 확인하지 않았습니다. 배포 전에 각 에셋의 라이선스를 확인해야 합니다.

## 기술 정보

| 항목 | 내용 |
|---|---|
| Unity | `6000.3.27f2` |
| 렌더 파이프라인 | Universal Render Pipeline 2D (`com.unity.render-pipelines.universal` 17.3.0) |
| 입력 | Input System 1.20.1 (구 Input Manager는 사용하지 않음) |
| UI | uGUI 2.0.0, Canvas Scaler 기준 해상도 1920x1080 |
| 기본 해상도 | 1920x1080 |
| 개발 도구 | MCP for Unity (`com.coplaydev.unity-mcp`) |

## 폴더 구조

```
Assets/
├── Scenes/        TItle.unity, Game.unity
├── Scripts/       MainMenu.cs, GameHUD.cs, ButtonLabelPress.cs
│   └── Items/     ItemData, MineralItemData, CraftedItemData, ItemDatabase 등
├── Editor/        ItemCreatorWindow.cs (아이템 생성 툴)
├── Resources/
│   └── Items/     아이템 ScriptableObject 에셋 (Minerals/, Crafted/)
├── Sprites/       WhiteSquare.png, WhiteCircle.png
├── Fantasy Wooden GUI  Free/
├── 48 Free Minerals Pixel Art Icons Pack/
└── Medieval_Weapons/
```

## 실행 방법

1. Unity Hub에서 이 폴더를 프로젝트로 열고 Unity `6000.3.27f2`를 사용합니다.
2. `Assets/Scenes/TItle.unity`를 엽니다.
3. 플레이 모드로 실행합니다.
