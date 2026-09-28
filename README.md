# 블랙홀 키우기 (blackhole-client)

조준점을 움직여 자동 공격으로 천체를 부수고, 판마다 블랙홀 Level을 올려 성장도를 쌓고, 노드로 다음 판을 바꿔 이정표에 닿는 게임의 Unity 클라이언트.
"A Game About Feeding A Black Hole"을 참고 게임으로 분석해 규칙을 세웠습니다.

---

## 1. 시작

1. `Assets/Scenes/SampleScene.unity`
2. Play.

### 에디터 도구 및 콘솔

- **BlackHole > Node Tree**: 노드 목록 에셋(`Data/NodeCatalog.asset`)을 격자에서 고칩니다. 선 잇기·끊기, 게임과 같은 로더로 한 검사, 구매 미리보기를 제공합니다.
- 에디터와 개발 빌드에서만 뜨는 창들입니다. **`` ` ``(백쿼트) 키**로 한꺼번에 숨기고 보입니다.

## 2. 게임 흐름과 담당

`Assets/BlackHole/` 기준

### 5.1 담당 역할

| 담당 | 파일 | 하는 일 | 하지 않는 일 |
|---|---|---|---|
| **GameBootstrap** | `Unity/GameBootstrap.cs` | 씬의 에셋·화면으로 콘텐츠·노드 트리·시스템·화면 흐름을 한 번 조립한다 | 게임 진행 |
| **GameHost** | `Unity/GameHost.cs` | 매 프레임: 조준 입력 → 전투 진행 → 판이 끝난 프레임에 화면 흐름에 알림 → 전투 HUD 갱신 | 규칙 판정, 화면 전환 결정 |
| **ScreenFlow** | `Unity/Flow/ScreenFlow*.cs` | 버튼·시간 만료·이정표·콘솔 요청을 한 경로로 모아 전투 시작·종료를 요청하고, **성공 결과로** 다음 화면을 연다. 업그레이드 화면 값 계산(`RefreshUpgrade`) | 전투 순서, 규칙 계산 |
| **BattleOrchestrator** | `Unity/Battle/BattleOrchestrator.cs` | 시작·종료 순서의 책임자. 방장의 진행 상태(`PlayerState`)를 가진다. 진행 중인 요청이 있으면 새 요청을 무시한다 | 화면 전환 |
| **BattleSystem** | `Unity/Battle/BattleSystem.cs` | 한 판(`GameSession`)과 네 표현(적·스킬·사망 효과·블랙홀 화면)의 수명. 시작 2단계·정리 7단계를 확인하며 진행 | 스스로 시작·종료 |
| **SessionAssembler** | `Core/Session/SessionAssembler.cs` | 판을 만드는 유일한 입구. 산 노드와 성장도로 이 판의 수치를 **한 번** 모두 정한다 | 판 진행 |
| **GameSession** | `Core/Session/GameSession.cs` | 판의 단계·시간·시작 공급·종료·원자료·**결산** | 한 Step 안의 순서 |
| **World** | `Core/World/World.cs` | 한 Step의 처리 순서, 피해 입구(`DealDamage`), 생성·파괴 요청, 공급 처리 | 시간·종료 판정 |
| **EnemyRoster** | `Core/Enemies/EnemyRoster.cs` | 살아 있는 적 목록, 사망 확정(`RecordDeath`), 종류별 처치 수, 판의 Gold 합계 | 사망 효과 |
| **Hq** | `Core/Hq/Hq.cs` | 이 판의 블랙홀: 성장도, 판 EXP·Level, 목표 Level 도달, 이정표 | 진행 상태 변경 |
| **PlayerState** | `Core/Players/PlayerState.cs` | 판을 넘어 남는 것: Gold, 산 노드, 성장도 | 전투 중 변경(결산·구매만 바꾼다) |
| 화면(Screen·View) | `Unity/Screens/`, `Unity/Enemies/`, `Unity/Skills/`, `Unity/Battle/HqView.cs` | Core의 상태·기록을 읽어 그린다 | 규칙 계산 |

### 5.2 화면에서 판이 만들어지고 끝나기까지

```text
[업그레이드 화면]
  노드 클릭 ─▶ ScreenFlow.HandleNodeTreeNodeClicked ─▶ NodePurchase.TryPurchase (PlayerState에 산 노드 기록, Gold 차감)
                                                     ─▶ RefreshUpgrade (화면 다시 그림)
  Start battle ─▶ ScreenFlow.RequestStart
                    └▶ BattleOrchestrator.StartBattleAsync (seed를 새로 정함)
                         └▶ BattleSystem.StartAsync
                              1. SessionAssembler.CreateBattle(콘텐츠, 진행 상태, seed, 노드 트리)  ← 판의 수치가 모두 정해진다(5.5)
                              2. GameSession.Begin  ← 전투 시작 공급을 0초에 바로 처리
                    성공하면 ScreenFlow.GoToBattle

[전투 화면] 매 프레임 GameHost.Tick
  AimInput.Tick ─▶ GameSession.SetAimPoint (마우스 → 조준점)
  BattleSystem.Tick ─▶ GameSession.Advance ─▶ World.Step (5.3)
                   ─▶ 네 표현 Synchronize (적·스킬·사망 효과·블랙홀 화면)
  이번 프레임에 판이 끝났으면(시간 만료·이정표) ─▶ ScreenFlow.HandleBattleTimeExpired
  End battle 버튼·콘솔도 같은 ─▶ ScreenFlow.RequestEnd

ScreenFlow.RequestEnd
  └▶ BattleOrchestrator.EndBattleAsync
       └▶ BattleSystem.ShutdownAsync (단계마다 확인, 실패하면 멈추고 다시 부르면 처음부터)
            1. 종료 요청               2. 남은 적 정리(처치 아님, 보상 없음)
            3. 사망 처리 끝 확인        4. 원자료(BattleRawData) 보관
            5. 결산 GameSession.Settle  6. 표현 정리 확인      7. 완전 초기화
  원자료를 받으면 ScreenFlow.GoToSettlement

[결산 화면]  보여 주기만 한다 ─ Continue ─▶ ScreenFlow.GoToUpgrade
```

- 개발용 콘솔의 버튼은 같은 일을 하는 화면 버튼의 핸들을 그대로 부름(`Unity/Flow/ScreenFlow.Editor.cs`).

### 5.3 전투 한 Step (`World.Step`)

```text
1 적 이동
2 스킬 공격           참가자마다 Breaker·레이저 → World.DealDamage
3 파괴 요청 처리       쌓인 파괴 요청의 사망 확정
4 사망 효과           이번 Step에 피해로 죽은 효과 보유 적의 번개·폭발·처치 버프
5 EXP·Level 반영      Hq.RaiseLevels. 이정표 판이 목표 Level에 닿았으면 여기서 멈추고 판 종료
6 성장 효과           오른 Level마다 성장 공급 요청(성장 노드를 샀을 때)
7 공급 처리           쌓인 생성 요청을 적으로 만든다
8 종료 판정           (GameSession) 오른 Level마다 시간 연장 → 시간 만료 확인
```

### 5.4 적 생성과 처리

**생성 요청이 오는 곳 (공급 계기)**

| 계기 | 어디서 | 수 |
|---|---|---|
| 판 시작 | `GameSession.Begin`이 0초에 바로 처리 | 콘텐츠의 시작 공급(소행성 8) + 시작 공급 노드 |
| 블랙홀 판 Level업 | `World.Step` 6에서 요청, 7에서 처리 | 성장 공급 노드의 수(노드가 없으면 0) |
| 개발용 콘솔 | 적 명령 콘솔 → `World.RequestSpawn` | 버튼마다 |
| 적 파괴 뒤 생성 | **아직 없음.** 끝나는 규칙을 먼저 정해야 한다 | — |

시간이 지난다고 적이 생기지는 않습니다.

**한 마리가 만들어지는 순서** (`World.ProcessSpawnRequests`)

### 생성 커맨드는 오직 콘솔과만 이어져있는 상태.(게임 규칙과 무관)
따라서 규칙 확정 시 콘솔의 생성을 게임 내 실제 호출자쪽으로 연결.

**적이 죽는 곳과 그 순간 기록되는 것**

| 입구 | 코드 |
|---|---|
| 피해(스킬·사망 효과) | `World.DealDamage` → `EnemyRoster.DealDamage` (HP ≤ 0이면 사망) |
| 파괴 요청(개발용 콘솔) | `World.RequestDestroy` → Step 3 → `EnemyRoster.Destroy` |

두 입구 모두 `EnemyRoster.RecordDeath` 한 곳을 지납니다. 사망이 확정되는 순간 아래가 한 번에 일어납니다.

| 기록 | 어디에 |
|---|---|
| 살아 있는 적 목록에서 뺀다 | `EnemyRoster.Alive` |
| 종류별 처치 수 +1 | `EnemyRoster.Kills()` |
| 그 적의 Gold를 판의 합계에 | `EnemyRoster.EarnedGold` (= `World.EarnedGold`) |
| 그 색의 EXP를 블랙홀에 | `World` → `Hq.AddExp` |
| 사망 기록(번호·종류·위치) | `DeathRecord`. 화면이 읽으며, 다음 진행이 시작될 때 비운다 |
| 사망 효과 예약 | 피해로 죽은 효과 보유 적만 `DeathEffects`에 넣고, 같은 Step의 4에서 처리한다 |

- 이 판에 살아 있는 적만 피해를 받습니다. 다른 판의 적이나 정리된 적에 대한 요청은 아무것도 바꾸지 않습니다.
- 사망 효과를 가진 적은 사망 효과의 피해를 받지 않습니다. 그래서 연쇄가 반드시 끝납니다.
- 판 종료 때 남은 적을 치우는 **정리는 처치가 아닙니다.** Gold·EXP·처치 수·사망 효과가 없습니다.

### 5.5 스탯이 정해지고 바뀌는 흐름

**판 수치는 판 조립 때 한 번 정해지고, 판이 끝날 때까지 바뀌지 않습니다.**

```text
[휴식] 노드 구매 ─▶ PlayerState.OwnedNodes에 기록 (전투 중에는 살 수 없다)

[판 조립] SessionAssembler.CreateBattle
  방장의 산 노드 ─▶ NodePurchase.UpgradesFor ─▶ 업그레이드 표 (UpgradeTable)
                    최종 값 = (기본값 + Σ더하기) × (1 + Σ비율) × Π곱하기

  업그레이드 표 ─┬▶ BreakerDefinition.Upgraded        ─▶ BattlePlayer (조준점 + Breaker·레이저)
                ├▶ EnemyComposition.From (종류마다)   ─▶ 질량 단계, 황금 비율·배율, 시작·성장 공급 수,
                │                                       변환 비율(성장도 기본값 + 노드), 특수 확률
                └▶ HqUpgradeStats.GrowthTimeFrom     ─▶ Level업마다 늘어나는 시간
  PlayerState.GrowthStage ─▶ Hq (이번 성장도의 Level 표·목표 Level)
                          ─▶ EnemyStatTable (성장도의 색 비율 + 판 구성 → (종류, 색, 황금)별 HP·속도·크기·Gold·EXP)
```

| 판에 묶이는 것 | 담는 것 | 쓰는 곳 |
|---|---|---|
| `BattlePlayer` | 참가자의 조준점, Breaker·레이저. 노드는 **Breaker 수치만** 바꾼다(레이저 노드는 아직 없음) | `World.Step` 2 |
| `EnemyComposition` | 종류마다의 판 구성(위 표) | 공급 수 계산, 종류·황금 선택 |
| `EnemyStatTable` | 이 판의 성장도, 종류별 색 비율, (종류, 색, 황금)별 수치 | 적을 만들 때 조회, 조종 콘솔 표시 |
| `Hq` | 이 판의 성장도, Level 표·목표 Level, Level업마다 시간 | Step 5, 시간 연장 |

- 노드 수치 이름(`breaker.*`, `enemy.<종류>.*`, `hq.growth-time`)과 샘플 노드는 [SKILL_TREE_PLAN 5절](docs/SKILL_TREE_PLAN.md), [BATTLE_COMPOSITION_PLAN 4절](docs/BATTLE_COMPOSITION_PLAN.md)에 있습니다.
- **판 중에 바뀌는 수치는 처치 버프 하나뿐입니다.** 달은 Breaker 공격 주기를 줄이고, 혜성은 Breaker를 확정 치명타로 만듭니다. 판 안의 모든 참가자가 받고, 다음 Step부터 적용되며, 다음 판으로 넘어가지 않습니다.
- 노드를 모두 산 조합도 판을 만들 수 있는지는 게임 시작 때 `UpgradeContentCheck`가 봅니다. 실패하면 게임이 시작하지 않습니다.

### 5.6 보상과 블랙홀 성장

**판 Level (판 안)**

- 매 판 Level 0, EXP 0에서 시작합니다. 적이 죽는 순간 그 색의 EXP가 듭니다(질량·황금 배율 없음).
- Step 5에서 이번 성장도의 Level 표로 Level이 오릅니다. 한 번에 여러 Level이 오를 수 있습니다.
- Level이 오를 때마다, **성장 노드를 샀다면** 그 판의 시간 연장(`growth-time`)과 적 추가 공급(`growth-supply`)이 옵니다. 추가 공급도 동시 생존 상한에 걸리면 버려집니다.
- 판이 끝나면 Level과 EXP는 버립니다.

**성장도 (판 밖)**

- `PlayerState.GrowthStage`에 있고, **결산 때만** 바뀝니다.
- 이 판이 목표 Level에 닿았으면 +1입니다. 목표를 훨씬 넘어도 +1이고, 마지막 성장도(30)에서는 그대로입니다.
- 성장도는 다음 판의 Level 표·목표 Level, 적의 색 비율, 다음 종류의 기본 비율을 정합니다. 예: 성장도 10부터 행성 기본 3%.

**이정표 (성장도 10·20·30)**

- 이정표 바로 앞 성장도(예: 9)의 판에서 목표 Level에 닿으면, **그 Step에서 판이 바로 끝납니다.** 남은 시간과 관계없고, 그 Step의 성장 효과도 없습니다.
- 결산은 번 Gold **대신** 고정 보상을 주고, 성장도가 이정표 성장도가 됩니다.
- 그 밖의 성장도에서는 목표에 닿아도 판이 계속됩니다.

**결산** (`GameSession.Settle`, 정리 5단계에서 한 번)

```text
결산 Gold = 이정표 판이면 이정표 보상, 아니면 이 판이 번 Gold(EnemyRoster.EarnedGold)
진행 상태 ─ Gold += 결산 Gold
          ─ 성장도 = Hq.NextStage (목표 Level에 닿았으면 +1)
```

- 한 판에 한 번만 합니다. 다시 불러도 아무 일도 없습니다. Gold 더하기가 실패하면 결산하지 않은 상태로 남습니다.
- 원자료(`BattleRawData`)에 번 Gold·결산 Gold·처치 수·도달 Level·성장도 전과 후·이정표가 남습니다. 결산 화면이 이것을 보여 줍니다.
- 모은 Gold는 업그레이드 화면에서 노드를 사는 데만 씁니다.

## 6. 문서

| 순서 | 문서 | 읽는 이유 |
|---|---|---|
| 1 | [GAME_RULES_MVP](docs/GAME_RULES_MVP.md) | 게임 규칙의 기준입니다. 규칙이 충돌하면 이 문서가 이깁니다. 18절 "팀 공통 규칙"부터 보면 빠릅니다 |
| 2 | [BATTLE_COMPOSITION_PLAN](docs/BATTLE_COMPOSITION_PLAN.md) | 판에 어떤 적이 어떤 수치로 나오고 무엇을 주는가: 색·황금·변환·공급·Gold·EXP·성장도·이정표·결산, 샘플 콘텐츠를 고칠 곳 |
| 3 | [SKILL_TREE_PLAN](docs/SKILL_TREE_PLAN.md) | 스킬·사망 효과·처치 버프, 노드 그래프·업그레이드 합성·수치 이름, 업그레이드 화면과 노드 도구 |
| 필요할 때 | [BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES](docs/BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES.md) | 복잡도 제한과 성능 검증 방식. 8절에 지금 지키는 것과 비어 있는 것 |
| 필요할 때 | [REFERENCE_ANALYSIS](docs/REFERENCE_ANALYSIS.md) | 참고 게임 분석. 우리 명세가 아니라 근거 자료입니다 |

각 문서의 "남은 결정" 절이 아직 정하지 않은 것의 목록입니다. 지난 계획 문서와 티켓 기록은 git 기록에 있습니다.

## 7. 작업 방법 (현재 관례)

- `dev`에서 `feature/…`·`refactor/…` 브랜치를 따고, 끝나면 `dev`로 합칩니다.
- 커밋은 하나의 완결된 변경 단위로 나눕니다. 씬이나 Unity가 만든 파일은 그 변경을 일으킨 커밋에 같이 넣습니다.
- 커밋 메시지: `type: 무엇을 왜` (`feat`, `fix`, `refactor`, `docs`, `data`, `test`, `chore`, `style`, `perf`). 티켓이 있으면 끝에 `(GS-002)`처럼 붙입니다.
- `Assets/` 아래에 새 파일을 만들면 **`.meta`도 같이 커밋**합니다. `.meta`가 빠지면 다른 사람 쪽에서 GUID가 새로 생겨 참조가 끊깁니다.
- 규칙을 바꾸면 `Tests/EditMode/Contracts/`의 계약을 같이 고치고, 푸시 전에 3절의 명령을 돌립니다.
- `SampleScene.unity`와 `Prefabs/Screens/*`는 여러 사람이 동시에 고치면 충돌을 풀기 어렵습니다. 고치기 전에 말해 주세요.

## 8. 용어

코드와 문서가 이 뜻으로 씁니다.

| 용어 | 뜻 |
|---|---|
| 판 (Battle) | 전투 한 번. 기본 14초이고, Level이 오를 때 성장 노드가 그 판의 시간만 늘립니다 |
| 방장 (Host) | 진행 상태의 주인. 지금은 방장 한 명이 판의 유일한 참가자입니다 |
| 진행 상태 (PlayerState) | 판을 넘어 남는 것: Gold, 산 노드, 성장도. 결산과 노드 구매로만 바뀝니다 |
| 블랙홀 / HQ | 게임 공간의 원점 `(0,0)`이자 성장의 중심 |
| Level (판 Level) | 한 판 안에서 EXP로 오르는 블랙홀 Level. 매 판 0에서 시작하고 판이 끝나면 버립니다 |
| 성장도 (Growth Stage) | 판을 넘어 남는 블랙홀 진행 단계. 목표 Level에 닿은 판의 결산 때 +1 |
| 목표 Level | 이번 성장도에서 한 판 만에 닿으면 성장도가 오르는 Level |
| 이정표 (Milestone) | 성장도 10·20·30. 앞 성장도의 판이 목표 Level에 닿는 순간 끝나고, 처치 Gold **대신** 고정 보상을 받습니다 |
| 조준점 (Aim Point) | 마우스 위치. 공격할 곳만 정하고, 공격 시점은 스킬의 주기가 정합니다 |
| 스킬 | Breaker(주기마다 조준점 중심 원 안의 적 전부를 침), 관통 레이저(경계의 무작위 지점에서 조준점을 향해 예고한 뒤 직선 위 적 전부를 관통) |
| 사망 효과 | 적이 죽을 때 생기는 번개·폭발·처치 버프. 반드시 끝나는 규칙이 있습니다 |
| 공급 (Supply) | 적을 만드는 요청. 계기는 판 시작과 판 Level업(성장 효과)뿐입니다. 시간이 지난다고 저절로 생기지 않습니다 |
| 변환·특수 종류 | 공급된 한 마리의 종류를 바꾸는 비율·확률. 변환은 성장도의 기본값 + 노드, 특수 종류는 노드가 정합니다 |
| 색 등급 | 적의 HP·Gold·EXP 등급. 판을 시작할 때의 성장도가 비율을 정합니다 |
| 황금 | Gold 배율(소행성 50배)이 붙은 적. "황금 소행성 등장" 노드로 소행성의 10% |
| 노드 | 업그레이드 화면에서 Gold로 사는 칸. 산 노드들이 모여 판의 업그레이드 표가 됩니다 |
| 판 조립 | 판을 시작할 때 한 번, 산 노드와 성장도로 스킬·적·시간 수치를 정하는 일. 전투 중에는 바뀌지 않습니다 |
| 결산 | 판이 끝난 뒤 Gold와 성장도를 진행 상태에 한 번 반영하는 일 |
| 원자료 (BattleRawData) | 결산 화면이 보여 주는 판의 기록 |
| 정리 (Cleanup) | 판이 끝날 때 남은 적을 치우는 일. 처치가 아니므로 Gold·EXP를 주지 않습니다 |

## 개발 환경
**Unity 버전**: 6000.6.2f1