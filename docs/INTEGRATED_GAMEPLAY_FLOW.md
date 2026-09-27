# INTEGRATED GAMEPLAY FLOW — 통합된 한 루프

작성일: 2026-09-27
대상: `integration/dev-all` → `dev` (feature/스킬시스템 계통 + feature/처치보상 + feature/노드트리 문서)

이 문서는 통합 뒤 **실제 코드가 도는 순서**를 한 장에 적는다. 각 PLAN(UPGRADE_LINK_PLAN, SKILL_SYSTEM_PLAN, BATTLE_COMPOSITION_PLAN, NODE_TREE_SCREEN_PLAN)은 그때의 계획과 기록이라 고치지 않았다. 지금 구조는 여기를 본다.

---

## 1. 한 장 요약

```text
휴식(업그레이드 화면)
  │  노드 클릭 → NodePurchase.TryPurchase (Gold 차감, 산 노드 기록)
  │  Start battle → BattleOrchestrator.RequestStart
  ▼
판 조립 (SessionAssembler.CreateBattle, 한 번)
  │  산 노드 → UpgradeTable (참가자마다)
  │  UpgradeTable → Breaker 수치 (BreakerDefinition.Upgraded)
  │  UpgradeTable → 적 종류의 판 구성 (EnemyComposition.From) → 적 수치 표 (EnemyStatTable)
  │  판 구성의 공급 수 → 전투 시작 공급
  │  PlayerState.EnterBattle
  ▼
전투 (GameSession.Advance → World.Step, 프레임마다)
  │  1 적 이동 → 2 스킬 공격(Breaker, 레이저) → 3 파괴 요청 → 4 사망 효과(번개·폭발·처치 버프)
  │  → 7 공급(풀 여과·전체 상한 → 색 몫 → 황금 몫 → 위치 → 수치 표) → 종료 판정(시간)
  │  사망 확정 순간: World.EarnedGold += 그 적의 Gold (생성 때 정해진 값)
  ▼
종료·결산 (BattleSystem.ShutdownAsync, 오케스트레이터가 부름)
  │  종료 요청 → 남은 적 정리(처치 아님) → 사망 처리 끝 확인 → 원자료(BattleRawData)
  │  → 결산(GameSession.Settle: PlayerState.Gold += EarnedGold, 한 번) → 연출 정리 → 완전 초기화
  │  성공하면 BattleOrchestrator.BattleCompleted(원자료)
  ▼
결산 화면 (보여 주기만)
  │  Continue → 결산 대기 비움
  ▼
휴식(업그레이드 화면) — 결산한 Gold로 다음 노드를 사고, 다음 판 조립이 그것을 읽는다
```

---

## 2. 단계별: 무엇을 어디서 하는가

| 단계 | 하는 일 | 규칙 | 코드 |
|---|---|---|---|
| 휴식 | 노드 구매 | 드러남(시작 노드이거나 산 이웃이 있음), 한 번만, Gold ≥ 가격, 전투 중 불가 | `Core/Nodes/NodePurchase.cs`, `NodeGraph.cs`, `Unity/Flow/ScreenFlow.Upgrade.cs` |
| 판 조립 | 참가자별 업그레이드 표 | 산 노드의 업그레이드 합성: (기본 + Σ더하기) × (1 + Σ비율) × Π곱하기 | `NodePurchase.UpgradesFor`, `Core/Upgrades/UpgradeTable.cs` |
| 판 조립 | Breaker 수치 | 수치 이름 `breaker.*`, 한계는 Breaker가 건다 | `Core/Skills/BreakerDefinition.cs` (`Upgraded`, `BreakerUpgradeStats`) |
| 판 조립 | 적 판 구성 | 수치 이름 `enemy.<종류>.*`, 한계는 적 시스템이 건다. 참가자가 1명일 때만 보정 | `Core/Enemies/EnemyComposition.cs` (`From`, `EnemyUpgradeStats`), `EnemyStatTable.cs` |
| 판 조립 | 전투 시작 공급 | 콘텐츠 공급 + 판 구성의 더할 공급 수 | `SessionAssembler.StartSupplyOf` |
| 전투 | 한 Step | 순서는 GAME_RULES 13절 번호(5·6 HQ 성장은 없음) | `Core/World/World.cs` (`Step`) |
| 전투 | 피해·사망 | 이 판에 살아 있는 적만 받는다. 사망은 한 번, 즉시 판에서 빠진다 | `Core/Enemies/EnemyRoster.cs` (`Holds`, `RecordDeath`), `World.DealDamage` |
| 보상 | 판의 Gold | 사망 확정 순간 그 적의 Gold(생성 때 색·황금으로 정해짐)를 더한다. 정리된 적은 없다 | `EnemyRoster.RecordDeath`, `World.EarnedGold` |
| 종료 | 정리·원자료·결산 | 결산은 Gold를 더한 뒤에야 마친 것으로 기록. 다시 불러도 한 번 | `Unity/Battle/BattleSystem.cs` (`ShutdownAsync`), `Core/Session/GameSession.cs` (`Settle`) |
| 화면 | 전환 | 판 진행 → 전투 화면, 결산 대기 → 결산 화면, 판 없음 → 업그레이드 화면. 시작·정리 중에는 그대로. 결산 화면 중 새 판이 시작되면(개발용 콘솔) 결산 표시를 넘긴다 | `Unity/Flow/ScreenFlow*.cs`, `Unity/Battle/BattleOrchestrator.cs` (`BattleCompleted`) |

---

## 3. 판 동안 고정되는 것과 바뀌는 것

| 고정(판 조립 때 한 번) | 판 동안 바뀜 | 판 동안 바뀌지 않는 진행 상태 |
|---|---|---|
| 참가자별 업그레이드 표 | 적(위치·HP·생존) | `PlayerState.Gold` |
| Breaker 수치(피해·주기·반지름·치명타 확률) | Breaker의 처치 버프(공격 주기 감소, 확정 치명타) | `PlayerState.OwnedNodes` |
| 적 판 구성(질량 단계·황금 비율·황금 배율·더할 공급 수) | `World.EarnedGold`, 처치 수 | |
| 적 수치 표((종류, 색, 황금) → HP·크기·Gold) | 생성·파괴 요청, 사망 효과 대기열 | |
| 전투 시작 공급, 난수 스트림(배치·색·황금·레이저·치명타) | | |

전투 중에는 노드를 살 수 없고(`PurchaseResult.InBattle`), 개발용 진행 치트도 거부된다.

---

## 4. 업그레이드 수치 이름 (지금 노드가 쓰는 것)

| 이름 | 가져가는 시스템 | 기본값 | 한계 | 노드 |
|---|---|---|---|---|
| `breaker.damage` | Breaker | 콘텐츠의 피해 | > 0 | start, damage-1 (더하기 1) |
| `breaker.speed` | Breaker | 1 (주기 = 기본 주기 ÷ 공격 속도) [임시] | > 0 | speed-1·2 (비율 0.25) |
| `breaker.radius` | Breaker | 콘텐츠의 반지름 | > 0 | square-a·b (비율 0.1) |
| `breaker.crit-chance` | Breaker | 콘텐츠의 확률 | ≥ 0, 1에서 멈춤 | square-c·d (더하기 0.05) |
| `enemy.<종류>.mass-level` | 적 | 0 | 정수(반올림), 질량 단계 표 안 | asteroid·planet·star-mass-1~8 |
| `enemy.<종류>.golden-ratio` | 적 | 0 | 0 ~ 1(1에서 멈춤), 황금이 되는 종류만 | golden-asteroid(더하기 0.0005), golden-digits-1~3(곱하기 10) |
| `enemy.<종류>.golden-multiplier` | 적 | 종류의 기본 배율 | ≥ 0 | golden-multiplier(더하기 4150 → 4200) [임시] |
| `enemy.<종류>.start-supply` | 적(판 조립) | 0 | 정수(반올림), 공급 합이 전체 상한 안 | asteroid·planet·star-supply |

노드를 모두 산 경우에도 판을 조립할 수 있는지는 게임 시작 때 `UpgradeContentCheck`가 본다(Breaker 한계, 질량 단계 범위, 황금이 되는 종류, 공급 상한). 실패하면 GameHost가 시작하지 않는다.
레이저를 보정하는 노드는 없어서 레이저 수치 이름은 두지 않았다.

---

## 5. 화면

| 화면 | 프리팹 | 보이는 것 | 알리는 것 |
|---|---|---|---|
| 업그레이드 | `Assets/BlackHole/Prefabs/Screens/UpgradeScreen.prefab` | Gold, 산 노드 수 / 전체, 노드 트리(격자 칸 배치, 선은 `NodeGraph.Links`, 네 상태) | 노드 ID 클릭, Start battle |
| 전투 | `BattleScreen.prefab` (배경 없음, 위쪽 띠) | 남은 시간, 이 판이 번 Gold, Pause/Resume | Pause, End battle |
| 결산 | `SettlementScreen.prefab` | 끝난 사유, 시간, 처치 수(종류별), 번 Gold, 결산 뒤 Gold | Continue |

- 씬(`SampleScene`)의 `UI > UI Canvas > RootLayer`에 세 화면이 있고 GameHost의 Root Layer·Panel Layer·Registered Views에 연결돼 있다. 자식 이름은 각 화면의 `Refs`와 같다(`UIRoot<TRefs>`).
- 연결을 비우면 코드로 만든 임시 화면(`PlaceholderScreens`)을 쓴다. 개발용 대체일 뿐 정상 실행 경로가 아니다.
- 화면은 규칙을 계산하지 않는다. 가격·구매 가능 여부는 `NodePurchase`, 번 Gold는 `World.EarnedGold`, 결산은 `GameSession.Settle`이 가진다.
- 적·스킬·사망 효과 표현(EnemyView, SkillView, DeathEffectView)은 Core 기록을 읽어 그린다. 결산 화면으로 넘어가기 전에 셋 다 비었는지 확인한다(정리 6단계).
- 개발용 콘솔(` 키)은 그대로 있다. 루프의 어떤 단계도 콘솔 없이 된다.

---

## 6. [임시]와 남은 것

- 공격 속도 → 주기 변환(주기 = 기본 주기 ÷ 공격 속도)은 [임시]다. 원작의 공격 속도 표기와 맞는지 미확인.
- 황금 배율 노드: 옛 구조의 "정하기 4200"을 범용 표에서 "더하기 4150"(기본 50 + 4150)으로 옮겼다. 노드 모양(곱하기인지, 몇 단계인지)은 BATTLE_COMPOSITION_PLAN의 미정 그대로다.
- 적 종류 노드 35개의 격자 칸은 Breaker 노드 왼쪽에 계열별로 둔 [임시] 배치다. `asteroid-mass-1`은 선행 노드가 없던 노드라 두 번째 시작 노드가 됐다.
- 참가자가 둘 이상이면 적 판 구성은 보정 없이 기본값이고 결산은 아무도 받지 않는다(보상·공유 대상 귀속은 F06 미정).
- 판이 끝난 사유는 시간 종료 하나다. End battle 버튼도 같은 사유로 끝낸다(결산 화면은 "Battle over"로만 적는다).
- HQ EXP·성장 이정표(Step 5·6), 저장, 노드 표시 이름·아이콘, 한글 글꼴은 없다(화면 글자는 영문).

---

## 7. 통합 검증 기록 (2026-09-27)

- Core 계약: `tests/CoreSmoke` 93개 통과. Unity EditMode(같은 계약 목록)도 batchmode로 통과.
- Unity Play: 저장소에 두지 않은 일회용 PlayMode 검사로 SampleScene을 실제 화면 버튼과 가상 마우스(적이 가장 많이 모인 곳을 조준)로 돌렸다.
  - 첫 진입: 업그레이드 화면, Gold 0, 0 / 43 nodes, 시작 노드 둘이 보임.
  - 첫 판(30초, 시간 종료): 처치 13(소행성 8, 전기 소행성 2, 달 2, 초신성 1), 연쇄 번개·폭발·달 버프 발생, 번 Gold 33. 판 동안 진행 상태 Gold는 0 그대로.
    일시정지 중 시간이 흐르지 않음. 결산 화면: +33, 총 33. 적·스킬·사망 효과 표현이 모두 비어 있음.
  - Continue → 업그레이드 화면에서 노드 클릭으로 `start`(Breaker 피해 +1)와 `asteroid-mass-1`(소행성 질량 +1)을 삼(33 → 13).
  - 두 번째 판: Breaker 피해 2 → 3, 소행성 색 비율 [1, 0, …] → [0.6, 0.4, …](주황 소행성이 섞여 나옴). End battle(8초)로 끝내 +8, 총 21.
