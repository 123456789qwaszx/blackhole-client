# 블랙홀 성장 시스템

작성일: 2026-09-28 · 기준: `dev`

관련: [적 생성 시스템](ENEMY_SPAWN_SYSTEM.md) · [적 파괴·처치·결산 시스템](ENEMY_KILL_SYSTEM.md) · 세부 규칙 [GAME_RULES_MVP](GAME_RULES_MVP.md) 3.2·11절, [BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md) 3.7절

경로는 `Assets/BlackHole/` 기준이다.

## 1. 목적

적을 처치해 **한 판 만에** 도달하는 블랙홀의 Level을 높이고, 목표 Level에 도달한 성과를 결산 때 **영구 성장도**로 바꾼다.
성장도는 다음 판 적의 구성과 Level별로 필요한 EXP를 정한다.

## 2. 성장 시스템에 쓰는 값

### 2.1 성장도

- 판을 넘어 유지되는 진행 단계다. **결산에서만 바뀐다.**
- `PlayerState.GrowthStage`에 있다. 전투 단위를 넘어서는 영구 진행 상태다(앱을 끄면 아직 저장되지 않는다).
- 목표 Level에 도달한 판의 결산 때 +1 오른다. 목표를 훨씬 넘겨도 +1씩만 오른다. 마지막 성장도(30)에서는 그대로다.
- 다음 판의 다음 것들을 정한다.
  - 판 Level EXP 표와 목표 Level
  - 적의 색 비율
  - 다음 종류의 기본 출현 비율(성장도 10부터 행성 3%)
- 새 진행은 성장도 0이다.

### 2.2 Level

- 매 판 Level 0, EXP 0에서 시작한다. 즉 현재 전투에서만 유지된다.
- 적을 처치하면(사망이 확정되는 순간) 그 적의 EXP만큼 오른다.
- EXP를 100% 채우면 Level업한다. 한 번에 여러 Level이 오를 수 있다. 표 끝에서는 EXP만 쌓인다.
- 성장 노드를 샀다면 Level업마다 적 추가 공급과 시간 연장이 온다.

즉, 목표 Level에 도달하지 못하면 성장도는 변화가 없다. 그 판의 Level과 EXP는 다음 판에 이월되지 않는다.

### 2.3 목표 Level

- 성장도마다 정해진 Level이다. 이번 판에서 여기에 닿으면 결산 때 성장도가 오른다.
- 판 중에 목표에 닿아도 판은 계속된다(이정표 판 제외). 성장도는 결산 때 오르므로, 그 판의 색과 Level 표는 끝까지 같다.

## 3. 흐름

```text
판 시작   성장도 s → 판 Level 표, 목표 Level, 색 비율, 기본 변환 비율      SessionAssembler.CreateBattle
          Level 0, EXP 0                                               new Hq(…, s)
판 중     적 사망 → EXP                                                 Hq.AddExp
          Step 5: EXP로 Level 상승                                      Hq.RaiseLevels
          Step 6·시간 연장: 오른 Level마다 성장 효과(성장 노드를 샀을 때)  World.Step, GameSession.Advance
          이정표 판이 목표 Level에 닿으면 이 Step에서 판 종료
결산      목표 Level에 닿았으면 성장도 s + 1                              GameSession.Settle
          EXP·Level은 버린다
```

## 4. 이정표 (특별 규칙)

- 이정표는 성장도 10, 20, 30을 말한다.
- 이정표 바로 앞 성장도(예: 9)의 판에서 목표 Level에 닿으면, **그 순간 판이 끝난다.**

**예외 규칙**
- 남은 시간과 관계없이 끝나고, 그 Step의 성장 효과는 버린다.
- 그 밖의 성장도에서는 목표에 닿아도 판이 계속 진행된다.
- 결산은 번 Gold 대신 고정 보상을 주고, 성장도가 이정표 성장도(예: 10)가 된다.
- 성장도는 줄지 않으므로 같은 이정표를 다시 받지 않는다.

## 5. 개발용 예외

업그레이드 콘솔의 `Stage -1 / +1`은 결산을 거치지 않고 성장도를 바꾼다(`ProgressCheats.SetGrowthStage`). 이정표를 시험할 때 성장도 9로 옮기는 데 쓴다. 지난 이정표의 보상은 주지 않는다.

## 6. 쓰는 곳과 읽는 곳

| | 값 | 코드 |
|---|---|---|
| 쓰는 곳 | 판 EXP | `World.DealDamage`·파괴 요청 처리 → `Hq.AddExp` |
| | 판 Level | `Hq.RaiseLevels` (Step 5) |
| | 성장도 | `GameSession.Settle` → `PlayerState.KeepGrowthStage(Hq.NextStage)` |
| 읽는 곳 | 성장도 → 판 Level 표·목표 | `SessionAssembler.CreateBattle` → `Hq` 생성자 → `HqGrowthDefinition.StageAt` |
| | 성장도 → 색 비율 | `EnemyStatTable` → `EnemyDefinition.TierRatiosAt` |
| | 성장도 → 기본 변환 비율 | `EnemyComposition.From` → `EnemyDefinition.BaseUpgradeAt` |
| | 목표 도달·이정표 | `Hq.ReachedGoal`, `Hq.Milestone` → `World.Step`, `GameSession.Advance`·`SettledGold` |
| | 화면 | 업그레이드 화면(성장도·목표 Level), 전투 HUD(`Lv 2 / 3`), 결산 화면(`Stage 2 -> 3`, 이정표 n / 전체) |

## 7. 데이터

`Data/HqGrowthSetup.asset` — 값은 모두 [임시]다.

| 칸 | 지금 값 |
|---|---|
| `stages` (성장도 0~30) | 성장도마다 판 Level 10개의 누적 EXP와 목표 Level 3. 마지막 성장도 30은 목표 없음 |
| `milestones` | 성장도 10 → 30,000, 20 → 200,000,000, 30 → 1,000,000,000 Gold |

Level 표를 만든 방식(성장도별 평균 적 EXP × 1, 3, 6, 10, …)은 [BATTLE_COMPOSITION_PLAN 4절](BATTLE_COMPOSITION_PLAN.md)에 있다. 계약은 `Tests/EditMode/Contracts/GrowthContracts.cs`(`Hq.*`, `Stage.*`, `Milestone.*`)에 있다.
