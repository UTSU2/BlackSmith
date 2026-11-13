# BlackSmith

망치를 직접 두드려 장비를 만드는 대장장이 제작 게임입니다.

## 게임 개요

BlackSmith는 **제작 중심**의 게임입니다. 플레이어는 대장간에서 직접 망치를 두드려 장비를 만듭니다.

- **상점**에서 광물을 구입할 수 있습니다.
- 제작한 **장비를 상점에 판매**할 수 있습니다.
- 장비는 **가열, 단조, 열처리, 연마** 네 단계의 미니게임을 거쳐 만들어집니다.
- 각 단계의 **완성도**(단계당 최대 10점, 합계 최대 40점)에 따라 장비의 **품질**(최하품~최상품)이 결정됩니다. **등급**은 아이템마다 하나로 정해져 있고 제작 결과에는 영향을 주지 않습니다.
- 현실의 제작과 달리 **빠른 리듬게임처럼 속도감 있게** 진행합니다. 입력은 마우스입니다.
- 판매 수익으로 더 좋은 재료를 구입해 더 높은 등급의 장비를 만드는 것이 성장 구조입니다. (레시피 해금은 메인 루프에 포함하지 않습니다.)

기획 문서는 [docs/](docs/) 폴더에 있습니다. 확정되지 않은 사항은 문서에 **미정**으로 표시되어 있습니다.

## 핵심 루프

```
광물 구매 (상점) → 제작 (가열 → 단조 → 열처리 → 연마) → 품질 결정 → 장비 판매 (상점)
```

## 제작 단계, 등급, 품질

| 순서 | 단계 | 진행 방식 / 조작 | 완성도 판정 |
|---|---|---|---|
| 1 | 가열 | 마우스를 누르다 떼기를 반복해 바를 일정한 위치에 유지 | 목표 구간 안에 있던 시간 비율 |
| 2 | 단조 | 화면 여러 곳에 생기는 원이 줄어들 때, 안쪽 원과 맞는 타이밍에 클릭 (리듬게임 형식) | 타이밍 오차로 Perfect / Good / Miss 부분 점수 |
| 3 | 열처리 | 게이지 안에서 좌우로 왕복하는 오브젝트가 특정 위치에 도달할 때 클릭 | 마커가 목표 범위에 온전히 / 일부 / 전혀 들어오는지 |
| 4 | 연마 | 흠집을 클릭한 뒤 사포질하듯 드래그해서 갈아냄 | 흠집별로 지운 비율의 평균 |

- 각 단계를 100% 수행하면 10점이며 완성도에 비례합니다. 합계(최대 40점)를 버림한 값으로 품질이 정해집니다.

  | 점수 | 품질 | 판매가 배율 |
  |---|---|---|
  | 0 ~ 9 | 최하품 | 0.7 |
  | 10 ~ 19 | 하품 | 1.0 |
  | 20 ~ 29 | 중품 | 1.2 |
  | 30 ~ 39 | 상품 | 1.5 |
  | 40 | 최상품 | 2.0 |

- 등급은 Common, Uncommon, Rare, Unique, Legendary 5단계이며 재료와 완성품이 같은 이름을 씁니다. 한 아이템에는 등급이 하나뿐이며(광물도 마찬가지), 등급은 가격 배율과 표시에만 쓰이고 레시피 매칭·난이도·품질에는 영향을 주지 않습니다.
- 재료 가격은 Common을 1배로 Uncommon 1.2 / Rare 1.5 / Unique 1.8 / Legendary 2배이며 소수점은 반올림합니다. 완성품 판매가는 레시피에 적힌 재료의 구매가 합 × 품질 배율입니다.
- 미니게임 난이도는 1~10단계이고 레시피에 정의된 값 그대로 쓰며 재료 등급에 따른 가중이나 품질 보정은 없습니다. 난이도는 4개 미니게임에 같이 적용하며 플레이어에게 표시하지 않습니다.
- 재료를 넣으면 가장 가까운 레시피가 자동으로 선택됩니다(레시피 북은 보이지 않음). 세부 규칙은 [docs/GameDesign.md](docs/GameDesign.md)의 "레시피 매칭"을 참고하세요.
- Common 가격은 전역 기준값이 없고, 아이템마다 생성할 때(Item Creator) 정합니다.
- 단계별 완성도 판정, 난이도 1~10단계별 수치, 1회 제작 시간(4개 합계 20초)은 확정이며 [docs/GameDesign.md](docs/GameDesign.md) §4에 있고 코드와 `Game` 씬 설정에 반영돼 있습니다. 기본 밸런스 조정은 마쳤고, 정밀 조정은 v2에서 합니다.

## 현재 구현 현황

### 구현됨

- **타이틀 씬 (`TItle`)**
  - 대장간 배경: 기본 도형 스프라이트를 조합해 만든 벽, 바닥, 천장 들보, 화로, 모루, 선반
  - 조명과 연출: 화로 위치의 주황색 Point Light 2D, 불꽃 파티클
  - 메뉴 버튼 4개 (우측 하단, Fantasy Wooden GUI 에셋 사용)
    - 새 게임: `Game` 씬으로 이동. 튜토리얼은 아직 없어 건너뛴 것으로 처리하고 시작 지급금 1000원을 받고 시작합니다. (튜토리얼이 생기면 완료·스킵 시 지급)
    - 로드 게임: 슬롯 8개(2페이지) 로드 패널에서 저장 파일을 불러와 `Game` 씬으로 이동
    - 설정: BGM / 효과음 소리 크기 슬라이더 패널 (값은 `PlayerPrefs`에 저장). 로드 게임과 같은 연출로 열립니다. 메뉴 버튼과 제목이 사라지고 패널이 위에서 내려옵니다
    - 닫기: 게임 종료 (에디터에서는 플레이 모드 종료)
  - 버튼을 누르는 동안 글자도 함께 내려가는 눌림 연출
  - 기본 BGM(`bgm_default`)이 재생됩니다. 씬마다 처음부터 재생합니다.
- **게임 씬 (`Game`)**
  - 대장간 배경
  - 상점 (우측 상단 상점 버튼): 구매/판매 탭, 광물 구매, 완성품 판매, 구매/판매 팝업. 목록은 가격 낮은 순(같으면 ID 순)으로 정렬
  - 제작: 모루를 클릭해 재료를 넣고 시작. 가열, 단조, 열처리, 연마 4개 미니게임, 레시피 자동 매칭, 점수 → 품질, 완성품 지급
  - 저장/불러오기 (우측 상단 책 버튼): 저장/불러오기 탭, 슬롯 8개(2페이지), 덮어쓰기 확인 팝업. 불러오면 `Game` 씬을 다시 로드
  - 저장 파일은 `persistentDataPath/saves/slot_N.json`에 슬롯별로 저장
  - 설정 (우상단 톱니바퀴 버튼): BGM / 효과음 슬라이더와 타이틀로 돌아가기. 저장 이후 금액·소지품이 바뀌었을 때만 확인 창이 뜹니다
  - 우상단 버튼 3개(상점, 책, 톱니바퀴)의 패널은 타이틀처럼 위에서 내려오고 닫을 때 올라갑니다
  - 사운드: 가열·단조·열처리·연마 적중 효과음 9종, 기본 BGM과 제작 중 BGM(제작 시작부터 완료까지만 제작 BGM). 파일은 임시입니다

### v1(MVP) 상태

MVP 완료 기준(광물·제작품 임시 완성, 적중 사운드, BGM, 아주 기본적인 밸런스 조정)을 모두 충족해 v1은 완료입니다. ([docs/MVP.md](docs/MVP.md) §5) 아래는 완료 기준에는 들어 있지 않은 후속 작업입니다.

- 적중 사운드와 BGM 파일은 임시이며 최종 음색은 미정입니다. ([docs/SoundPlan.md](docs/SoundPlan.md))
- 기본 밸런스 조정은 마쳤습니다. 제작 시간은 가열 5 / 단조 8 / 열처리 2 / 연마 5초입니다. 정밀 조정은 v2입니다.
- 콘텐츠 확충: 현재 광물 48종(등급 분포 18/12/9/6/3, 값은 확정), 실제 이름의 장비 11종과 그 레시피 11개(`Medieval_Weapons` 스프라이트 사용, 가격은 재료 구매가 합으로 결정), 임시 제작품 5종(`temp_gear_*`)과 임시 레시피 38개가 있습니다. 임시 레시피는 아직 실제 장비에 쓰이지 않은 광물을 위한 것이며, 장비를 추가하면서 차례로 교체합니다. 장비 이름·재료 조합·난이도는 초안입니다.

구현 예정 항목의 세부 사양은 아직 정해지지 않았습니다.

### v2 예정 (MVP 이후)

- 장비 강화: 판매가에 강화 수치가 반영됩니다. (가격 공식은 미정)
- 숙련도 시스템
- 적중 이펙트
- 정밀 밸런스 조정
- (레시피 해금은 도입 여부가 미정이며, 도입하더라도 메인 루프 밖의 기능입니다.)

## 씬 구성

| 빌드 인덱스 | 씬 | 용도 |
|---|---|---|
| 0 | `Assets/Scenes/TItle.unity` | 타이틀 / 메인 메뉴 |
| 1 | `Assets/Scenes/Game.unity` | 게임 화면 |

## 스크립트

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/MainMenu.cs` | 타이틀 메뉴 버튼 동작 (새 게임, 로드 게임, 설정, 닫기) 과 버튼 글자 폰트 적용 |
| `Assets/Scripts/GameHUD.cs` | `Game` 씬 HUD. 상점 버튼(`OnShop`), 책 버튼(`OnBook`, 저장/불러오기 패널 열기), 톱니바퀴 버튼(`OnSettings`, 설정 패널 열기)을 연결하고, 불러오기 성공 시 씬을 다시 로드 |
| `Assets/Scripts/Data/` | `PlayerData`(돈·소지품, 저장 이후 변경 여부, 튜토리얼 대기와 보상), `InventoryEntry`, `GameSession`(현재 세션, 새 게임 시작), `SaveSlotManager`(슬롯 8개 저장/불러오기), `SaveFile` |
| `Assets/Scripts/Audio/` | 사운드: `SoundSettings`(BGM / 효과음 크기, `PlayerPrefs`), `SfxClips`(효과음 클립 모음), `SfxPlayer`, `BgmPlayer`(크로스페이드), `CraftingBgmSwitch`(제작 시작·완료에 따른 BGM 전환) |
| `Assets/Scripts/Shop/ShopService.cs` | 구매/판매 규칙 |
| `Assets/Scripts/Crafting/` | 제작 로직: `CraftRecipeData`, `RecipeDatabase`, `RecipeMatcher`, `CraftScoring`, `CraftDifficulty`, `CraftingSession`, `CraftingController`, 4개 미니게임(`HeatMinigame`, `ForgeMinigame`, `HeatTreatMinigame`, `PolishMinigame`), `CraftingSfx`(미니게임 판정 효과음 연결), `AnvilInteractable` |
| `Assets/Scripts/UI/` | 화면 표시: `ShopPanel`, `ShopItemView`, `ShopBuyPopup`, `CraftingPanel`, 미니게임 View 4종, `SaveLoadPanel`, `SaveSlotView`, `SaveSlotPager`, `SettingsPanel` 등 |
| `Assets/Scripts/ButtonLabelPress.cs` | 버튼이 눌림 이미지일 때 글자를 함께 내리는 연출 |
| `Assets/Scripts/Items/ItemData.cs` | 아이템 공통 데이터(ScriptableObject): ID, 이름, 아이콘, 설명, 최대 스택, 기본 등급(아이템당 하나), Common 기준 판매가 (등급 배율은 `ItemGradeInfo`) |
| `Assets/Scripts/Items/MineralItemData.cs` | 광물 아이템. 구매와 판매 가능 (구매가 포함) |
| `Assets/Scripts/Items/CraftedItemData.cs` | 제작품 아이템. 판매만 가능. 제작품 전용 정보는 규칙 확정 후 추가 예정 |
| `Assets/Scripts/Items/ItemType.cs`, `ItemGrade.cs` | 아이템 종류 enum, 등급 enum (Common, Uncommon, Rare, Unique, Legendary), `ItemQuality.cs` 품질 enum, `ItemVariant.cs` 등급·품질·재료 합산 가격 묶음 |
| `Assets/Scripts/Items/ItemDatabase.cs` | `Assets/Resources/Items/` 아래 모든 아이템을 게임 시작 시 자동으로 읽어 등록하는 데이터베이스 |
| `Assets/Editor/ItemCreatorWindow.cs` | 아이템 생성 툴. 메뉴 `Tools > BlackSmith > Item Creator` |
| `Assets/Editor/RecipeCreatorWindow.cs` | 레시피 생성 툴. 메뉴 `Tools > BlackSmith > Recipe Creator` |
| `Assets/Editor/MoneyCheatMenu.cs` | 테스트용 돈 지급 메뉴 |

## 아이템 데이터베이스

- 아이템 하나가 ScriptableObject 에셋 하나입니다. 종류는 광물과 제작품 두 가지입니다.
  - 광물: 구매와 판매 가능
  - 제작품: 판매만 가능
- 아이템은 `Assets/Resources/Items/` 아래에 두면 `ItemDatabase`가 게임 시작 시 자동으로 읽어 등록합니다. 별도로 목록을 관리할 필요가 없습니다.
- 아이템은 `Tools > BlackSmith > Item Creator` 창에서 만듭니다. ID는 영소문자, 숫자, 밑줄만 쓸 수 있고 다른 아이템과 겹칠 수 없습니다. 광물은 `Assets/Resources/Items/Minerals/`, 제작품은 `Assets/Resources/Items/Crafted/`에 저장됩니다.
- 코드에서는 `ItemDatabase.Get(id)`, `ItemDatabase.TryGet(id, out item)`, `ItemDatabase.GetByType(type)`, `ItemDatabase.All`로 접근합니다.
- 광물의 구매가·판매가는 Common 기준 가격이고 다른 등급은 배율(1.2 / 1.5 / 1.8 / 2)을 곱해 반올림합니다. Item Creator에서 Common 가격만 입력합니다.
- 제작품 판매가는 입력하지 않고, 제작할 때 `레시피에 적힌 재료 구매가 합 x 품질 배율`로 정해져 소지품 항목(`ItemVariant`)에 함께 저장됩니다.

## 사용 에셋

| 폴더 | 내용 |
|---|---|
| `Assets/Fantasy Wooden GUI  Free` | 나무 느낌의 UI 버튼과 패널 (타이틀·Game UI의 버튼과 패널에 사용) |
| `Assets/Audio` | 임시 효과음 9종(`Sfx/`), BGM 2곡(`Bgm/`), 효과음 클립 모음 `SfxClips.asset` |
| `Assets/48 Free Minerals Pixel Art Icons Pack` | 광물 픽셀 아이콘 |
| `Assets/Medieval_Weapons` | 중세 무기 이미지와 데모 씬 |
| `Assets/Sprites` | 배경과 일부 UI(톱니바퀴 아이콘, 슬라이더) 제작에 쓴 흰색 사각형 / 원 / 링 도형 스프라이트 |

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
docs/              GameDesign.md, CoreLoop.md, MVP.md, Roadmap.md (기획 문서)
Assets/
├── Scenes/        TItle.unity, Game.unity
├── Scripts/       MainMenu.cs, GameHUD.cs, ButtonLabelPress.cs
│   ├── Audio/     SoundSettings, SfxClips, SfxPlayer, BgmPlayer, CraftingBgmSwitch
│   ├── Items/     ItemData, MineralItemData, CraftedItemData, ItemDatabase 등
│   ├── Data/      PlayerData, GameSession, SaveSlotManager 등
│   ├── Shop/      ShopService
│   ├── Crafting/  레시피, 매칭, 점수, 미니게임
│   └── UI/        상점, 제작, 저장/불러오기, 설정 UI
├── Editor/        ItemCreatorWindow, RecipeCreatorWindow, MoneyCheatMenu
├── Resources/
│   ├── Items/     아이템 ScriptableObject 에셋 (Minerals/, Crafted/)
│   └── Recipes/   레시피 ScriptableObject 에셋
├── Audio/         Sfx/ (효과음), Bgm/ (BGM), SfxClips.asset
├── Prefabs/UI/    ShopItemView, CraftingIngredientView
├── UI/            상점 화살표 등 UI 스프라이트
├── Screenshots/   개발 중 캡처 이미지
├── Sprites/       WhiteSquare.png, WhiteCircle.png, WhiteRing.png
├── Fantasy Wooden GUI  Free/
├── 48 Free Minerals Pixel Art Icons Pack/
└── Medieval_Weapons/
```

## 실행 방법

1. Unity Hub에서 이 폴더를 프로젝트로 열고 Unity `6000.3.27f2`를 사용합니다.
2. `Assets/Scenes/TItle.unity`를 엽니다.
3. 플레이 모드로 실행합니다.
