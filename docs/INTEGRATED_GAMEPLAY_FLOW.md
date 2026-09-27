# INTEGRATED GAMEPLAY FLOW — 통합된 한 루프

작성일: 2026-09-27
대상: `feature/블랙홀성장` — 구현 기준 `e8a4e0e`, 통합 기준 `a05fe37` 이후 12개 커밋(BG-001~005, BL-001~006 및 공급 노드 정리). BL-007 문서 반영.

이 문서는 통합 뒤 **실제 코드가 도는 순서**를 한 장에 적는다. 각 PLAN(UPGRADE_LINK_PLAN, SKILL_SYSTEM_PLAN, BATTLE_COMPOSITION_PLAN, NODE_TREE_SCREEN_PLAN)은 그때의 계획과 기록이라 고치지 않았다. 지금 구조는 여기를 본다.

---

## 1. 한 장 요약

```text
휴식(업그레이드 화면)
  │  노드 클릭 → NodePurchase.TryPurchase (Gold 차감, 산 노드 기록)
  │  Start battle → ScreenFlow → BattleOrchestrator.StartBattleAsync
  ▼
판 조립 (SessionAssembler.CreateBattle, 한 번)
  │  방장의 산 노드 → UpgradeTable (판에 하나)
  │  UpgradeTable → Breaker 수치 (BreakerDefinition.Upgraded)
  │  UpgradeTable → 적 종류의 판 구성 (EnemyComposition.From) → 적 수치 표 (EnemyStatTable)
  │  콘텐츠 공급 + 판 구성의 공급 수 → 전투 시작 요청(샘플: 소행성 8)
  │  UpgradeTable → 블랙홀의 Level업마다 시간 (HqUpgradeStats.GrowthTimeFrom), 블랙홀(Hq)은 PlayerState.HqExp에서 시작. 시작 Level이 색 비율을 고정
  │  PlayerState.EnterBattle
  ▼
전투 (GameSession.Advance → World.Step, 프레임마다)
  │  1 적 이동 → 2 스킬 공격(Breaker, 레이저) → 3 파괴 요청 → 4 사망 효과(번개·폭발·처치 버프)
  │  → 5 블랙홀 Level 반영 → 이정표 도달이면 즉시 종료(6·7·시간 연장 없음)
  │  → 6 성장 효과(오른 Level마다 종류별 성장 공급 요청)
  │  → 7 공급(생성 여과: 전체 상한 → 변환 사슬 → 특수 종류 몫 → 색 몫 → 황금 몫 → 위치 → 수치 표) → 오른 Level마다 제한 시간 연장 → 종료 판정(시간)
  │  사망 확정 순간: World.EarnedGold += 그 적의 Gold, Hq.Exp += 그 적의 EXP (생성 때 정해진 값)
  ▼
종료·결산 (BattleSystem.ShutdownAsync, 오케스트레이터가 부름)
  │  종료 요청 → 남은 적 정리(처치 아님) → 사망 처리 끝 확인 → 원자료(BattleRawData)
  │  → 결산(GameSession.Settle: Gold += SettledGold, HqExp = 판의 누적 EXP, 한 번) → 연출 정리 → 완전 초기화
  │  성공하면 BattleOrchestrator가 원자료(BattleRawData) 반환
  ▼
결산 화면 (보여 주기만)
  │  Continue → 결산 대기 비움
  ▼
휴식(업그레이드 화면) — 결산 Gold로 노드 구매. 이어진 Level과 산 노드로 다음 판을 조립한다
```

---

## 2. 단계별: 무엇을 어디서 하는가

| 단계 | 하는 일 | 규칙 | 코드 |
|---|---|---|---|
| 휴식 | 노드 구매 | 드러남(시작 노드이거나 산 이웃이 있음), 한 번만, Gold ≥ 가격, 전투 중 불가 | `Core/Nodes/NodePurchase.cs`, `NodeGraph.cs`, `Unity/Flow/ScreenFlow.Upgrade.cs` |
| 판 조립 | 판의 업그레이드 표(방장의 산 노드) | 산 노드의 업그레이드 합성: (기본 + Σ더하기) × (1 + Σ비율) × Π곱하기 | `NodePurchase.UpgradesFor`, `Core/Upgrades/UpgradeTable.cs` |
| 판 조립 | Breaker 수치 | 수치 이름 `breaker.*`, 한계는 Breaker가 건다 | `Core/Skills/BreakerDefinition.cs` (`Upgraded`, `BreakerUpgradeStats`) |
| 판 조립 | 적 판 구성 | 수치 이름 `enemy.<종류>.*`, 한계는 적 시스템이 건다. 변환 비율·특수 생성 확률도 여기서 정해진다. 색은 시작 Level로 정한다 | `Core/Enemies/EnemyComposition.cs` (`From`, `EnemyUpgradeStats`), `EnemyStatTable.cs` |
| 판 조립 | 전투 시작 공급 | 콘텐츠 공급 + 종류별 더할 공급 수. 실제 종류는 생성 시 변환·특수 몫으로 결정 | `SessionAssembler.StartSupplyOf` |
| 전투 | 한 Step | 순서는 GAME_RULES 13절 번호(1~8 모두) | `Core/World/World.cs` (`Step`) |
| 전투 | 블랙홀 성장 | 사망 순간 색 등급의 EXP(질량·황금과 무관). Step 5에서 Level·이정표 판정. 이정표에 닿지 않으면 6에서 오른 Level마다 성장 공급, 종료 판정 전에 Level마다 시간 연장. 성장 효과의 값은 산 성장 노드가 정하고 기본은 0(노드 전에는 Level만 오른다). 늘어난 시간은 그 판에만 있다 | `Core/Hq/Hq.cs`, `HqGrowthDefinition.cs`, `World.Step`, `GameSession.Advance`, `TimeLimitRule.Extend` |
| 전투 | 피해·사망 | 이 판에 살아 있는 적만 받는다. 사망은 한 번, 즉시 판에서 빠진다 | `Core/Enemies/EnemyRoster.cs` (`Holds`, `RecordDeath`), `World.DealDamage` |
| 보상 | 판의 Gold | 사망 확정 순간 그 적의 Gold(생성 때 색·황금으로 정해짐)를 더한다. 정리된 적은 없다 | `EnemyRoster.RecordDeath`, `World.EarnedGold` |
| 종료 | 정리·원자료·결산 | 처치 Gold 또는 이정표 보상(SettledGold)과 누적 EXP를 반영한 뒤 완료 기록. 다시 불러도 한 번 | `Unity/Battle/BattleSystem.cs` (`ShutdownAsync`), `Core/Session/GameSession.cs` (`Settle`) |
| 화면 | 전환 | ScreenFlow가 전투 시작 성공 후 `GoToBattle`, 정리·결산 결과 반환 후 `GoToSettlement` 호출. 시간 만료·이정표 도달과 개발용 콘솔도 같은 요청 경로를 쓰며 Continue는 업그레이드 화면을 연다 | `Unity/Flow/ScreenFlow*.cs`, `Unity/Battle/BattleOrchestrator.cs` |

---

## 3. 판 동안 고정되는 것과 바뀌는 것

| 고정(판 조립 때 한 번) | 판 동안 바뀜 | 판 동안 바뀌지 않는 진행 상태 |
|---|---|---|
| 판의 업그레이드 표 | 적(위치·HP·생존) | `PlayerState.Gold` |
| Breaker 수치(피해·주기·반지름·치명타 확률) | Breaker의 처치 버프(공격 주기 감소, 확정 치명타) | `PlayerState.OwnedNodes` |
| 적 판 구성(변환·특수 확률·질량 단계·황금 비율·황금 배율·더할 공급 수·성장 공급 수) | `World.EarnedGold`, 처치 수 | |
| 적 수치 표((종류, 색, 황금) → HP·크기·Gold·EXP), 시작 Level의 색 비율 | 생성·파괴 요청, 사망 효과 대기열 | `PlayerState.HqExp` |
| 블랙홀의 Level업마다 시간, Level 표 | 블랙홀의 EXP·Level·이번 판 이정표, 제한 시간(성장으로만 늘어난다) | |
| 전투 시작 공급, 난수 스트림(배치·종류·색·황금·레이저·치명타) | | |

전투 중에는 노드를 살 수 없고(`PurchaseResult.InBattle`), 개발용 진행 치트도 거부된다.

---

## 4. 업그레이드 수치 이름 (지금 노드가 쓰는 것)

| 이름 | 가져가는 시스템 | 기본값 | 한계 | 노드 |
|---|---|---|---|---|
| `breaker.damage` | Breaker | 콘텐츠의 피해 | > 0 | start, damage-1 (더하기 1) |
| `breaker.speed` | Breaker | 1 (주기 = 기본 주기 ÷ 공격 속도) [임시] | > 0 | speed-1·2 (비율 0.25) |
| `breaker.radius` | Breaker | 콘텐츠의 반지름 | > 0 | square-a·b (비율 0.1) |
| `breaker.crit-chance` | Breaker | 콘텐츠의 확률 | ≥ 0, 1에서 멈춤 | square-c·d (더하기 0.05) |
| `enemy.<종류>.upgrade` | 적(판 조립·공급) | 0% | 유한한 값 ≥ 0, 100%에서 멈춤. 변환 대상 필요 | asteroid-to-planet-1~5(+52, +5×4), planet-to-star-1~5(+10, +5×4) |
| `enemy.<특수 종류>.chance` | 적(판 조립·공급) | 0% | 유한한 값 ≥ 0, 100%에서 멈춤. 부모별 합 ≤ 100% | electric-asteroid-chance-1·moon-chance-1·comet-chance-1·supernova-chance-1(+10) |
| `enemy.<종류>.mass-level` | 적 | 0 | 정수(반올림), 질량 단계 표 안. HP·Gold 계수만 변경 | asteroid·planet·star-mass-1~8 |
| `enemy.<종류>.golden-ratio` | 적 | 0 | 0 ~ 1(1에서 멈춤), 황금이 되는 종류만 | golden-asteroid(더하기 0.0005), golden-digits-1~3(곱하기 10) |
| `enemy.<종류>.golden-multiplier` | 적 | 종류의 기본 배율 | ≥ 0 | golden-multiplier(더하기 4150 → 4200) [임시] |
| `enemy.<종류>.start-supply` | 적(판 조립) | 0 | 정수(반올림), 공급 합이 전체 상한 안 | asteroid-supply-1~3(행성·별 시작 공급 노드는 제거) |
| `enemy.<종류>.growth-supply` | 적(Level업) | 0 | 정수(반올림), ≥ 0. 요청한 종류에 변환·특수 몫을 적용하며 전체 상한에 닿으면 버린다 | asteroid-growth-supply(더하기 4) [임시] |
| `hq.growth-time` | 블랙홀 | 0 | ≥ 0 | growth-time(더하기 3초) [사용자] |

노드를 모두 산 경우에도 판을 조립할 수 있는지는 게임 시작 때 `UpgradeContentCheck`가 본다(Breaker 한계, 질량 단계 범위, 황금이 되는 종류, 공급 상한, 음수인 성장 시간, 출현 배치 없는 공급 수 노드, 변환 대상·특수 부모와 특수 확률 합). 실패하면 GameHost가 시작하지 않는다.
레이저를 보정하는 노드는 없어서 레이저 수치 이름은 두지 않았다.

---

## 5. 화면

| 화면 | 프리팹 | 보이는 것 | 알리는 것 |
|---|---|---|---|
| 업그레이드 | `Assets/BlackHole/Prefabs/Screens/UpgradeScreen.prefab` | Gold, 산 노드 수 / 전체, 블랙홀 Lv·%·이정표 n / 전체, 노드 트리(격자 칸 배치, 선은 `NodeGraph.Links`, 네 상태) | 노드 ID 클릭, Start battle |
| 전투 | `BattleScreen.prefab` (배경 없음, 위쪽 띠) | 남은 시간, 이 판이 번 Gold, 블랙홀 Lv·다음 Level까지 %, Pause/Resume | Pause, End battle |
| 결산 | `SettlementScreen.prefab` | 진행 시간, 블랙홀 도달 Level, 처치 수(종류별), 처치 Gold, 실제 결산 Gold, 결산 뒤 Gold. 이정표 판은 Milestone reached 표시 | Continue |

- 씬(`SampleScene`)의 `UI > UI Canvas > RootLayer`에 세 화면이 있고 GameBootstrap의 Root Layer·Panel Layer·Registered Views에 연결돼 있다. 자식 이름은 각 화면의 `Refs`와 같다(`UIRoot<TRefs>`).
- 세 화면과 UI Layer 연결이 없으면 GameBootstrap이 오류를 알리고 실행을 중단한다.
- 트리 보기(`NodeTreeView`)는 업그레이드 화면을 호스트(`IUIPageOwner`)로 두는 페이지(`UIPage`)다. 화면과 함께 Registered Views에 등록하고, `ScreenFlow`가 업그레이드 화면을 연 뒤 `SwitchPage`로 열어 트리를 짓고 노드 클릭을 연결한다. `UpgradeScreen`은 어떤 페이지가 올라오는지 모른다. 업그레이드 화면이 닫히면 페이지도 닫히고 연결이 풀린다.
- 화면은 규칙을 계산하지 않는다. 가격·구매 가능 여부는 `NodePurchase`, 번 Gold는 `World.EarnedGold`, 결산은 `GameSession.Settle`이 가진다.
- `BattleSystem.Tick`은 시간 만료 또는 이정표 도달로 판이 끝난 프레임에만 `true`를 한 번 반환한다. `GameHost`는 그때 `ScreenFlow.HandleBattleTimeExpired()`를 직접 호출한다. `ScreenFlow`는 버튼·시간 만료·이정표 도달·개발 콘솔의 요청을 한곳으로 모으고, 오케스트레이터의 성공 결과를 받아 화면을 전환한다. 업그레이드 화면의 Gold·소유 수·Level·%·이정표 진행도·노드별 상태도 `ScreenFlow.RefreshUpgrade`가 계산해 넘긴다. 화면 진입·구매·개발용 업그레이드 콘솔이 부르고, 업그레이드 화면이 열려 있을 때만 그린다(콘솔은 결산 화면 위에서도 누를 수 있다). 개발용 콘솔은 `ScreenFlow.Editor`의 콘솔 핸들만 받고, 그 핸들은 같은 일을 하는 화면 버튼의 핸들로 간다. 전투 HUD의 남은 시간과 번 Gold는 전투 Step 뒤에 갱신한다. 화면 전환을 위한 프레임별 상태 확인은 없다.
- 적·스킬·사망 효과·블랙홀 표현(EnemyView, SkillView, DeathEffectView, HqView)은 Core 기록을 읽어 그린다. 결산 화면으로 넘어가기 전에 넷 다 비었는지 확인한다(정리 6단계).
- 블랙홀 그림(HqView)은 원점의 원이다. Level마다 커질 뿐 출현 띠·공전·Breaker와 무관하다 [임시 크기].
- 개발용 콘솔(` 키)은 그대로 있다. 루프의 어떤 단계도 콘솔 없이 된다.

---

## 6. [임시]와 남은 것

- 공격 속도 → 주기 변환(주기 = 기본 주기 ÷ 공격 속도)은 [임시]다. 원작의 공격 속도 표기와 맞는지 미확인.
- 황금 배율 노드: 옛 구조의 "정하기 4200"을 범용 표에서 "더하기 4150"(기본 50 + 4150)으로 옮겼다. 노드 모양(곱하기인지, 몇 단계인지)은 BATTLE_COMPOSITION_PLAN의 미정 그대로다.
- 적 종류 노드의 격자 칸은 Breaker 노드 왼쪽에 계열별로 둔 [임시] 배치다. 해금 노드는 변환·확률 노드로 바꿨고 행성·별 시작 공급 노드는 제거했다. `asteroid-mass-1`은 선행 노드가 없던 노드라 두 번째 시작 노드가 됐다.
- 나오는 종류는 소행성 → 행성 → 별 변환과 특수 생성 확률이 정한다. 단계·적 풀·종류별 동시 최대 수는 없다. 시작 요청은 소행성 8이며, 변환·확률 노드의 가격·배치는 [임시]다.
- 진행 상태는 방장의 것 하나다(사용자, 2026-09-27). 방장이 노드를 사며, 스탯(업그레이드 표)과 처치 버프는 판 안의 모든 참가자가 함께 받는다. 결산 Gold와 블랙홀 누적 EXP는 그 진행 상태로 간다. 지금 판 안의 참가자는 방장 한 명이다.
- 시간 만료·End battle·이정표 도달은 같은 정리·결산 절차를 쓴다. 이정표 판은 "Milestone reached", 나머지는 "Battle over"다.
- 성장 샘플: 기본 제한 시간 14초 [사용자], 성장 노드 구매 시 Level업마다 +3초 [사용자]·소행성 요청 +4 [임시]. Level 1~30을 표현하며 `levelExp`는 Level 2~30의 누적 임계값 **29개**(10~560,000)다. 새 진행만 Level 1이다. 색 등급 EXP는 별도 칸이며 샘플은 해당 색의 기본 Gold와 같은 값이다.
- 이정표 샘플은 Level 10·20·30, 보상 30,000·200,000,000·1,000,000,000 Gold [임시]다. 처치 Gold에 합산하지 않는다.
- 영구 저장, 기본 시간 노드, 성장 공급 확률 노드, 파괴 뒤 같은 종류 생성, 특수 종류별 최대 개수, 흡수 연출, 이정표 전용 화면·에필로그는 후속이다. 노드 표시 이름·아이콘·한글 글꼴도 없다.
- 전투·업그레이드 화면의 성장 진행은 Lv·% 텍스트다. 결산 화면에는 이정표 도달 여부·보상만 보이며, 이정표 n / 전체는 업그레이드 화면에 보인다.

---

## 7. 통합 검증 기록 (2026-09-27, 성장 브랜치 이전 기록)

아래는 당시 통합 실행 기록을 보존한 것이다. 30초·초기 특수 종류·질량에 따른 색 변화는 현재 규칙이 아니며, 아래 통과 수와 Unity Play 결과는 `e8a4e0e` 검증 결과가 아니다. 현재 문서 대조 범위는 BLACKHOLE_LEVEL_PLAN 12절을 본다.

- Core 계약: `tests/CoreSmoke` 93개 통과. Unity EditMode(같은 계약 목록)도 batchmode로 통과.
- Unity Play: 저장소에 두지 않은 일회용 PlayMode 검사로 SampleScene을 실제 화면 버튼과 가상 마우스(적이 가장 많이 모인 곳을 조준)로 돌렸다.
  - 첫 진입: 업그레이드 화면, Gold 0, 0 / 43 nodes, 시작 노드 둘이 보임.
  - 첫 판(30초, 시간 종료): 처치 13(소행성 8, 전기 소행성 2, 달 2, 초신성 1), 연쇄 번개·폭발·달 버프 발생, 번 Gold 33. 판 동안 진행 상태 Gold는 0 그대로.
    일시정지 중 시간이 흐르지 않음. 결산 화면: +33, 총 33. 적·스킬·사망 효과 표현이 모두 비어 있음.
  - Continue → 업그레이드 화면에서 노드 클릭으로 `start`(Breaker 피해 +1)와 `asteroid-mass-1`(소행성 질량 +1)을 삼(33 → 13).
  - 두 번째 판: Breaker 피해 2 → 3, 소행성 색 비율 [1, 0, …] → [0.6, 0.4, …](주황 소행성이 섞여 나옴). End battle(8초)로 끝내 +8, 총 21.


## 8. 콘텐츠를 고칠 곳 (BL-007)

경로는 `Assets/BlackHole/` 기준이다. 표의 값은 원작 확정 수치가 아니라 현재 샘플이다.

| 바꿀 것 | 저작 에셋·칸 | 실행에서 읽는 곳 |
|---|---|---|
| Level 임계값 | `Data/HqGrowthSetup.asset`의 `levelExp` | `HqGrowthDefinition`: 줄 0 = Level 2의 **누적** EXP. 29개 임계값으로 Level 1~30 표현 |
| 이정표 Level·보상 | 같은 에셋의 `milestones` (`level`, `reward`) | `Hq.RaiseLevels` → `GameSession.SettledGold` |
| 색마다 HP·크기·Gold·EXP | `Data/Enemies/*.asset`의 `tiers` | `EnemyDefinition.StatsAt`. EXP는 `exp` 그대로, 질량·황금 배율 없음 |
| Level별 색 비율 | 종류 에셋의 `levelColors` (`fromLevel`, `tierRatios`) | `TierRatiosAt(시작 Level)`. 시작 Level 이하 마지막 줄, 첫 줄보다 낮으면 첫 줄 |
| 질량 단계 | 종류 에셋의 `massLevels` (`healthMultiplier`, `goldMultiplier`) | 질량 노드가 줄 번호 선택. HP·Gold만 보정, 색·크기·EXP는 그대로 |
| 변환 대상·특수 부모 | 종류 에셋의 `upgradesTo`, `specialOf` 참조 | `EnemyStatTable`이 연결, `World.KindOf`가 몫 선택 |
| 변환·확률·성장 효과 | `Data/NodeCatalog.asset`의 `Upgrades` (`Stat`, `Operation`, `Value`) | `UpgradeTable` → `EnemyComposition.From`·`HqUpgradeStats.GrowthTimeFrom` |
| 시작 공급·전체 상한·출현 띠 | `Data/EnemySupplySetup.asset` | `SessionAssembler.StartSupplyOf` → `SpawnFilter` → 배치 |

- 변환·특수 확률은 **퍼센트 단위**다. `Value: 10`은 10%다. 황금 비율은 0~1 단위이며 `0.0005`는 0.05%다.
- `tierRatios`는 가중치 목록이다. 색 등급 수와 길이가 같고, 각 값 ≥ 0·합 > 0이어야 한다. 합이 반드시 1일 필요는 없다.
- Level 임계값은 양수·엄격 증가, `fromLevel`은 1 이상·엄격 증가다. 이정표는 Level 2~최대 Level 안에서 엄격 증가하며 보상은 0 이상이다.
- 변환 대상·특수 부모는 종류 목록에 있어야 한다. 변환 사슬은 순환할 수 없고, 특수 종류의 부모는 특수 종류일 수 없다. 같은 부모의 특수 확률 합은 100% 이하다.
- 노드로 변환 비율을 올리려면 변환 대상이 필요하고, 특수 확률을 올리려면 특수 부모가 필요하다. 기본값은 모두 0이다.
- 샘플은 소행성 요청을 행성·별로 변환하고, 최종 종류가 소행성이면 전기 소행성, 행성이면 달·혜성, 별이면 초신성으로 교체한다. 시작 공급과 성장 공급 모두 이 경로를 사용한다. Core의 직접 종류 요청까지 금지하는 해금 장치는 없으므로, 새 공급 데이터를 작성할 때도 이 구성을 지켜야 한다.
- 콘텐츠 정의·참조 오류는 `ContentLoader`, 산 노드와 콘텐츠 조합 오류는 `UpgradeContentCheck`가 검사한다. 오류가 있으면 게임 시작을 막는다.

`BattleRawData.EarnedGold`는 처치 Gold 기록이고 `SettledGold`는 실제 지급액이다. 이정표 판에서 둘을 더하지 않는다. `Exp`는 이번 판 획득량이 아니라 **이전 판까지 포함한 누적 EXP**다.
