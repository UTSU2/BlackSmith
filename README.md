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
- 레시피를 많이 알고 숙련도를 쌓으면 **대장장이 등급**이 오릅니다(승급 퀘스트). 등급이 오르면 일부 제작 단계를 건너뛰는 **스킵 장비**(자동 가열·서리 수조)와 판매 가격 보너스를 받습니다. 모든 단계를 건너뛰지는 않습니다.

기획 문서는 [docs/](docs/) 폴더에 있습니다. 확정되지 않은 사항은 문서에 **미정**으로 표시되어 있습니다.

## 현재 버전

- 최신 프리릴리스: **v3.0.0-beta.1** (v3 베타). 정식 릴리스는 v1.0 → v1.1 → v2.0입니다. ([Releases](https://github.com/UTSU2/BlackSmith/releases))
- v3는 같은 제작의 반복 피로를 줄이는 것이 목적입니다. 기능은 모두 구현됐고, 플레이 검증과 장비 38종의 이름·재료 최종 확정이 남아 있습니다. ([docs/V3.md](docs/V3.md))

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
- 미니게임 난이도는 1~10단계이고 레시피에 저장된 값 그대로 쓰며(실행 중 재료 등급에 따른 가중이나 품질 보정은 없음), 저장하는 값은 레시피 재료로 정합니다: `Σ(재료 개수 × 등급별 값)`을 반올림해 1~10으로 자르며 값은 일반 0.6 / 고급 1.1 / 희귀 1.5 / 유니크 1.7 / 전설 1.9입니다([GameDesign.md](docs/GameDesign.md) §4). 난이도는 4개 미니게임에 같이 적용하며 플레이어에게 표시하지 않습니다.
- 재료의 종류와 수량이 레시피와 정확히 같아야 그 장비가 만들어지고, 맞지 않으면 "알 수 없는 제작품"(판매가 50골드)이 나옵니다. 제작해 본 레시피는 레시피 도감(HUD의 초록 책 버튼)에 재료 구성·숙련도와 함께 기록됩니다. 세부 규칙은 [docs/GameDesign.md](docs/GameDesign.md)의 "레시피 매칭"과 [docs/V2.md](docs/V2.md) §3을 참고하세요.
- Common 가격은 전역 기준값이 없고, 아이템마다 생성할 때(Item Creator) 정합니다.
- 단계별 완성도 판정, 난이도 1~10단계별 수치, 1회 제작 시간(4개 합계 20초)은 확정이며 [docs/GameDesign.md](docs/GameDesign.md) §4에 있고 코드와 `Game` 씬 설정에 반영돼 있습니다. 기본 밸런스 조정은 마쳤고, 정밀 조정은 v2에서 합니다.

## 현재 구현 현황

### 구현됨

- **타이틀 씬 (`TItle`)**
  - 대장간 배경: 일러스트풍 배경 이미지와 모루 스프라이트 (`Assets/Backgrounds`, `Assets/Sprites`)
  - 조명과 연출: 화로 위치의 주황색 Point Light 2D, 불꽃 파티클
  - 메뉴 버튼 4개 (우측 하단, Fantasy Wooden GUI 에셋 사용)
    - 새 게임: `Game` 씬으로 이동해 튜토리얼(철 원석 구매 → 철검 제작 → 판매)로 시작합니다. 시작 지급금은 재료 구매에 필요한 금액(철 원석 1개 값)만이고, 철검을 판매하면 튜토리얼이 끝나며 1000원을 받습니다. 스킵은 없고, 튜토리얼 중에는 저장할 수 없으며 도중에 나가면 처음부터 다시 시작합니다.    - 로드 게임: 슬롯 8개(2페이지) 로드 패널에서 저장 파일을 불러와 `Game` 씬으로 이동
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
  - 레시피 도감 (우상단 초록 책 버튼): 7×7 격자, 재료 구성·숙련도 상세
  - 대장장이 등급 표시 (좌상단 명패): 등급과 승급 진행도, 진행 중인 승급 퀘스트. 글자 길이에 맞춰 명패가 늘어납니다
  - 스킵 장비 소품: 숙련에서 자동 가열, 명장에서 서리 수조가 대장간에 나타나고, 누르면 켜기/끄기·강화 팝업이 뜹니다
  - 우상단 버튼 4개(도감, 책, 상점, 톱니바퀴)의 패널은 타이틀처럼 위에서 내려오고 닫을 때 올라갑니다
  - 팝업(튜토리얼·제작 결과·승급·스킵 장비)과 도감, HUD 아이콘은 양피지·일러스트 스킨을 씁니다
  - 사운드: 가열·단조·열처리·연마 적중 효과음 9종, 기본 BGM과 제작 중 BGM(제작 시작부터 완료까지만 제작 BGM). 현재 파일을 변경 전까지 정식으로 씁니다

### v1(MVP) 상태

MVP 완료 기준(광물·제작품 임시 완성, 적중 사운드, BGM, 아주 기본적인 밸런스 조정)을 모두 충족해 v1은 완료입니다. ([docs/MVP.md](docs/MVP.md) §5) 아래는 완료 기준에는 들어 있지 않은 후속 작업입니다.

- 적중 사운드와 BGM 파일은 변경하기 전까지 정식으로 씁니다. 음색 교체는 v2 범위 밖입니다. ([docs/SoundPlan.md](docs/SoundPlan.md))
- 기본 밸런스 조정은 마쳤습니다. 제작 시간은 가열 5 / 단조 8 / 열처리 2 / 연마 5초입니다. 정밀 조정은 v2입니다.
- 콘텐츠 확충: 현재 광물 48종(등급 분포 18/12/9/6/3, 구매가 100 / 180 / 350 / 720 / 1200원, 되팔기는 절반), 실제 이름의 장비 11종과 그 레시피 11개(`Medieval_Weapons` 스프라이트 사용, 가격은 재료 구매가 합으로 결정), 2026-11-20에 추가한 장비 38종과 그 레시피 38개가 있습니다(임시 장비 `temp_gear_*`는 없앴습니다). 이 38종의 이름·재료 조합은 v3 기준으로 최종 확정했습니다(2026-11-23). 레시피 외 제작품으로 알 수 없는 제작품(999) 1종이 있습니다. 이 38종은 모든 광물이 쓰이도록 광물마다 하나씩 붙인 것이며, 재료를 바꾸면 난이도를 다시 계산해 입력합니다.

구현 예정 항목의 세부 사양은 아직 정해지지 않았습니다.

### v2 예정 (MVP 이후)

- 숙련도 시스템 — 구현됨: 레시피별 누적, 초급·중급·고급 Lv1~9와 Master, 품질 보너스, 제작 결과 팝업 ([docs/V2.md](docs/V2.md) §2. 팝업 배치 원칙은 확정, 디자인은 추후 수정 예정)
- 적중 이펙트 — 구현됨: 단조 판정 시 원 바깥으로 퍼지는 금색 도넛형 빛(Perfect > Good > 눌렀지만 Miss, 놓친 Miss는 없음), 연마 흠집 제거 시 흠집 크기의 은색 반짝임. 파티클이며 현재 수치로 확정, 프리팹에서 조정 가능 ([docs/V2.md](docs/V2.md) §4)
- 레시피 습득 — 구현됨: 정확 일치 매칭(재료 종류+수량), 맞지 않으면 알 수 없는 제작품(ID 999, 50골드 고정), 레시피 도감(7×7 격자, "해금 n / 49", 재료 구성·숙련도, 제작 결과 팝업의 해금 알림). 도감 번호는 변경 전까지 정식값, 알 수 없는 제작품 아이콘과 도감 비주얼은 추후 수정 예정(날짜 미정) ([docs/V2.md](docs/V2.md) §3)
- 정밀 밸런스 조정 — 구현됨: 상품 이상 완성품에 등급 배율(Common ×1, Uncommon ×1.1, Rare ×1.3, Unique ×1.7, Legendary ×2.5)을 곱하고, 광물 구매가를 100 / 180 / 350 / 720 / 1200원으로 올림(되팔기는 절반) ([docs/V2.md](docs/V2.md) §5)
- (장비 강화는 v2에서 제외했습니다. 이후 도입 여부는 미정입니다.)

### v3 베타 (v3.0.0-beta.1, 반복 제작 피로 완화)

- 대장장이 등급(견습 → 초급 → 숙련 → 장인 → 명장) — 구현됨: 도감 해금 수와 master 숙련도 수로 오르고, HUD 왼쪽 위에 표시됩니다. 필요 개수(초급 도감 10·master 5, 숙련 25·15, 장인 49·30, 명장 49·49)는 입력되어 있습니다.
- 스킵 장비 — 구현됨: 숙련에서 가열, 명장에서 열처리 스킵 장비를 받고, 대장간의 소품(가열 = 자동 가열, 열처리 = 서리 수조)을 눌러 켜기/끄기와 강화(비용 Lv1→2 20000 … Lv4→5 120000골드)를 합니다. 켜면 그 단계는 미니게임 없이 5~9점(레벨별)이 들어가므로 최상품(40)은 직접 해야 나옵니다.
- 승급 퀘스트 — 구현됨: 조건을 채우면 퀘스트(지정 레시피를 지정 품질 이상으로 제작)가 자동으로 시작되고, 클리어하면 승급 팝업과 함께 오릅니다. 퀘스트 내용(옥 장검 중품, 청옥 사브르 중품, 흑요 할버드 상품, 용심 루비 대검 최상품)과 혜택 문구는 입력되어 있습니다.
- 승급 혜택 — 구현됨: 초급부터 판매 가격 ×1.1, 장인부터 ×1.3 (상점 판매 단가에 곱함)
- 장비 38종 — 임시 장비를 없애고 레시피마다 고유한 완성품과 아이콘을 붙였고, 이름·재료 조합은 v3 기준으로 확정했습니다. 레시피 난이도는 재료 등급·개수로 정합니다.
- v3의 미정 항목은 모두 정해졌습니다(승급·스킵 장비 팝업 비주얼, 소품 배치, 등급 이름 겹침, 장비 38종 확정). 입력된 값과 확인 기록이 없는 항목은 [docs/V3.md](docs/V3.md) §4~6

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
| `Assets/Scripts/Crafting/` | 제작 로직: `CraftRecipeData`, `RecipeDatabase`, `RecipeMatcher`(정확 일치), `RecipeCodex`(도감 순서), `UnknownCraftSettings`(알 수 없는 제작), `CraftScoring`, `CraftDifficulty`, `CraftingSession`, `CraftingController`, 4개 미니게임(`HeatMinigame`, `ForgeMinigame`, `HeatTreatMinigame`, `PolishMinigame`), `CraftingSfx`(미니게임 판정 효과음 연결), `AnvilInteractable` |
| `Assets/Scripts/UI/` | 화면 표시: `ShopPanel`, `ShopItemView`, `ShopBuyPopup`, `CraftingPanel`, 미니게임 View 4종, `SaveLoadPanel`, `SaveSlotView`, `SaveSlotPager`, `SettingsPanel`, `TutorialPopup`(튜토리얼 안내 팝업), `MinigameGuidePopup`(튜토리얼 제작 중 미니게임 조작 안내), `CodexPanel`·`CodexSlotView`(레시피 도감), `PanelInputBlocker`, `DisplaySettings` 등 |
| `Assets/Scripts/Rank/` | 대장장이 등급과 스킵 장비: `BlacksmithRank`(등급), `BlacksmithRankSettings`(필요 개수·혜택·판매 배율), `RankController`, `RankQuest*`(승급 퀘스트), `SkipGear*`(스킵 장비 데이터·규칙·소품 `SkipGearProp`). 에셋은 `Assets/Resources/Rank/` |
| `Assets/Scripts/Proficiency/` | 숙련도: `ProficiencySettings`, `ProficiencyRules`(단계·보너스 계산) |
| `Assets/Scripts/Tutorial/` | 튜토리얼: `TutorialFlow`(순수 C# 단계 진행), `TutorialController`(이벤트 연결, 제한 질의), `TutorialConfig`/`TutorialStepData`/`TutorialCondition`(데이터). 에셋은 `Assets/Resources/Tutorial/` |
| `Assets/Scripts/ButtonLabelPress.cs` | 버튼이 눌림 이미지일 때 글자를 함께 내리는 연출 |
| `Assets/Scripts/Items/ItemData.cs` | 아이템 공통 데이터(ScriptableObject): ID, 이름, 아이콘, 설명, 최대 스택, 기본 등급(아이템당 하나), Common 기준 판매가 (등급 배율은 `ItemGradeInfo`) |
| `Assets/Scripts/Items/MineralItemData.cs` | 광물 아이템. 구매와 판매 가능 (구매가 포함) |
| `Assets/Scripts/Items/CraftedItemData.cs` | 제작품 아이템. 판매만 가능. 제작품 전용 정보는 규칙 확정 후 추가 예정 |
| `Assets/Scripts/Items/ItemType.cs`, `ItemGrade.cs` | 아이템 종류 enum, 등급 enum (Common, Uncommon, Rare, Unique, Legendary), `ItemQuality.cs` 품질 enum, `ItemVariant.cs` 등급·품질·재료 합산 가격 묶음 |
| `Assets/Scripts/Items/ItemDatabase.cs` | `Assets/Resources/Items/` 아래 모든 아이템을 게임 시작 시 자동으로 읽어 등록하는 데이터베이스 |
| `Assets/Editor/ItemCreatorWindow.cs` | 아이템 생성 툴. 메뉴 `Tools > BlackSmith > Item Creator` |
| `Assets/Editor/RecipeCreatorWindow.cs` | 레시피 생성 툴. 메뉴 `Tools > BlackSmith > Recipe Creator` |
| `Assets/Editor/MoneyCheatMenu.cs` | 테스트용 치트 메뉴(돈 지급, 도감 전체 해금). 메뉴 `Tools > BlackSmith > Cheat` |

## 아이템 데이터베이스

- 아이템 하나가 ScriptableObject 에셋 하나입니다. 종류는 광물과 제작품 두 가지입니다.
  - 광물: 구매와 판매 가능
  - 제작품: 판매만 가능
- 아이템은 `Assets/Resources/Items/` 아래에 두면 `ItemDatabase`가 게임 시작 시 자동으로 읽어 등록합니다. 별도로 목록을 관리할 필요가 없습니다.
- 아이템은 `Tools > BlackSmith > Item Creator` 창에서 만듭니다. ID는 영소문자, 숫자, 밑줄만 쓸 수 있고 다른 아이템과 겹칠 수 없습니다. 광물은 `Assets/Resources/Items/Minerals/`, 제작품은 `Assets/Resources/Items/Crafted/`에 저장됩니다.
- 코드에서는 `ItemDatabase.Get(id)`, `ItemDatabase.TryGet(id, out item)`, `ItemDatabase.GetByType(type)`, `ItemDatabase.All`로 접근합니다.
- 광물의 구매가·판매가는 Common 기준 가격이고 다른 등급은 배율(1.2 / 1.5 / 1.8 / 2)을 곱해 반올림합니다. Item Creator에서 Common 가격만 입력합니다.
- 제작품 판매가는 입력하지 않고, 제작할 때 `레시피에 적힌 재료 구매가 합 x 품질 배율`(상품 이상은 x 완성품 등급 배율)로 정해져 소지품 항목(`ItemVariant`)에 함께 저장됩니다.

## 사용 에셋

| 폴더 | 내용 |
|---|---|
| `Assets/Fantasy Wooden GUI  Free` | 나무 느낌의 UI 버튼과 패널 (타이틀·Game UI의 버튼과 패널에 사용) |
| `Assets/Audio` | 효과음 9종(`Sfx/`), BGM 2곡(`Bgm/`), 효과음 클립 모음 `SfxClips.asset` |
| `Assets/Fonts` | UI 글꼴 Maplestory(Bold/Light, 글자 크기 48 이상은 Bold)와 타이틀 제목 글자에 쓰는 폰트 `I AM A PLAYER.ttf` (아래 폰트 출처 참고) |
| `Assets/48 Free Minerals Pixel Art Icons Pack` | 광물 픽셀 아이콘 |
| `Assets/Medieval_Weapons` | 중세 무기 이미지와 데모 씬 |
| `Assets/Backgrounds` | 대장간 배경 이미지 |
| `Assets/Effects` | 단조·연마 적중 이펙트 프리팹과 텍스처 |
| `Assets/Sprites` | 흰색 도형 스프라이트와 이 프로젝트용으로 만든 이미지: 미니게임(`Minigame/`), 모루·불꽃, 스킵 장비 소품(자동 가열·서리 수조), 장비 아이콘 변형(`ItemIcons/`), UI 스킨(`UI/`) |

- 폴더 이름 `Fantasy Wooden GUI  Free`에는 "Free" 앞에 공백이 두 칸 있습니다.
- 이 프로젝트용 이미지의 상당수는 `gpt-image`(ChatGPT 구독)로 생성했습니다.
- 외부 에셋의 라이선스와 출처는 아래 `I AM A PLAYER` 폰트를 제외하고 이 문서에서 확인하지 않았습니다(Maplestory 글꼴 포함). 배포 전에 각 에셋의 라이선스를 확인해야 합니다.

### 폰트 출처

- **I AM A PLAYER** — 제작: greenegg (greenegg.co.kr), 2016. 눈누에서 무료로 배포 중인 폰트입니다. 영문·숫자 전용이라 한글 글자는 없으며, 타이틀 제목 `BlackSmith`에만 씁니다. (한글 UI는 Maplestory를 씁니다)
- 라이선스(원문 요지): 개인과 기업 모두 무료이고 수정과 재배포가 자유이며, 폰트 파일 자체를 유료로 판매하는 것만 금지입니다. 출처 표기는 권장입니다. ([라이선스 원문을 옮겨 둔 페이지](https://www.sandollcloud.com/free-font/15777/I-AM-A-PLAYER))
- 라이선스 조건은 바뀔 수 있으므로 정식 출시 전에 제작자 공식 사이트에서 최신 조건을 다시 확인해야 합니다.

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
│   ├── Rank/      대장장이 등급, 승급 퀘스트, 스킵 장비
│   ├── Proficiency/ 숙련도
│   ├── Tutorial/  튜토리얼
│   ├── Audio/     SoundSettings, SfxClips, SfxPlayer, BgmPlayer, CraftingBgmSwitch
│   ├── Items/     ItemData, MineralItemData, CraftedItemData, ItemDatabase 등
│   ├── Data/      PlayerData, GameSession, SaveSlotManager 등
│   ├── Shop/      ShopService
│   ├── Crafting/  레시피, 매칭, 점수, 미니게임
│   └── UI/        상점, 제작, 저장/불러오기, 설정 UI
├── Editor/        ItemCreatorWindow, RecipeCreatorWindow, MoneyCheatMenu
├── Resources/
│   ├── Items/     아이템 ScriptableObject 에셋 (Minerals/, Crafted/)
│   ├── Recipes/   레시피 ScriptableObject 에셋
│   ├── Rank/      등급·승급 퀘스트·스킵 장비 설정
│   └── Tutorial/, Proficiency/, UI/   튜토리얼·숙련도·글꼴 설정
├── Audio/         Sfx/ (효과음), Bgm/ (BGM), SfxClips.asset
├── Fonts/         Maplestory (UI 글꼴), I AM A PLAYER.ttf (타이틀 제목)
├── Backgrounds/   대장간 배경
├── Effects/       적중 이펙트
├── Prefabs/UI/    ShopItemView, CraftingIngredientView
├── UI/            상점 화살표 등 UI 스프라이트
├── Screenshots/   개발 중 캡처 이미지
├── Sprites/       도형 스프라이트, 미니게임·소품·UI 스킨 이미지 (Minigame/, ItemIcons/, UI/)
├── Fantasy Wooden GUI  Free/
├── 48 Free Minerals Pixel Art Icons Pack/
└── Medieval_Weapons/
```

## 실행 방법

1. Unity Hub에서 이 폴더를 프로젝트로 열고 Unity `6000.3.27f2`를 사용합니다.
2. `Assets/Scenes/TItle.unity`를 엽니다.
3. 플레이 모드로 실행합니다.
