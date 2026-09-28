# 적 파괴·처치 집계·결산 시스템

작성일: 2026-09-28 · 기준: `dev`

관련: [적 생성 시스템](ENEMY_SPAWN_SYSTEM.md) · [블랙홀 성장 시스템](BLACKHOLE_GROWTH_SYSTEM.md) · 세부 규칙 [BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md) 3.6·3.8절

경로는 `Assets/BlackHole/` 기준이다.

## 1. 적 파괴 요청 처리

| 입구 | 흐름 |
|---|---|
| 공격 스킬(Breaker·레이저)·사망 효과 | `World.DealDamage(enemy, damage)` → 적의 HP가 0 이하가 되면 사망 |
| 파괴 요청(개발용 적 명령 콘솔) | `World.RequestDestroy` → 다음 Step의 3에서 사망 |

- 두 입구 모두 `EnemyRoster.RecordDeath` 한 곳을 지난다. 그래서 어떤 이유로 죽어도 처리가 같다.
- 이 판에 살아 있는 적만 피해를 받는다. 다른 판의 적이나 이미 정리된 적에 대한 요청은 아무것도 바꾸지 않는다.
- 한 적은 한 번만 죽는다. 같은 Step에 여러 공격이 겹쳐도 사망·보상은 한 번이다.

## 2. 적 사망이 확정되는 순간 기록하는 것

| 기록 | 어디에 | 언제 반영 |
|---|---|---|
| 살아 있는 적 목록에서 제거 | `EnemyRoster.Alive` | 즉시. 이후 공격·이동 대상이 아니다 |
| 종류별 처치 수 증가 | `EnemyRoster.Kills()` | 즉시 |
| 그 적의 Gold를 판의 합계에 더함 | `EnemyRoster.EarnedGold` (= `World.EarnedGold`) | 즉시. 흡수 연출을 기다리지 않는다 |
| 그 적의 EXP를 블랙홀에 더함 | `World.Hq.Exp` (`Hq.AddExp`) | 즉시. Level은 Step 5에서 오른다 |
| 사망 기록(순번·종류·위치) | `DeathRecord` 목록 | 화면이 읽어 그린다. 다음 진행이 시작될 때 비운다 |
| 사망 효과 예약 | `DeathEffects` 대기열 | 같은 Step의 4에서 실행 |

**사망 효과 예약**
- 사망 효과를 가진 적이 **피해로** 죽으면, 그 효과·죽은 위치·마지막 피해의 출처를 대기열에 넣는다.
- 예약된 효과는 이번 Step의 사망 효과 단계(Step 4)에서 사망 순서대로 실행한다.
- 파괴 요청으로 죽은 적은 효과를 내지 않는다.
- 사망 효과를 가진 적은 사망 효과의 피해를 받지 않는다. 그래서 효과가 효과를 부르지 않고, 연쇄는 반드시 끝난다.
- 효과로 죽은 적도 같은 Step의 사망·처치 수·보상에 든다.

## 3. 처치 결과는 전투 중 어떻게 쓰이는가

- **EXP**는 블랙홀 판 Level에 쓰인다.
- **Gold**는 이 판이 번 Gold로 집계된다. 전투 중에는 진행 상태(`PlayerState.Gold`)를 바꾸지 않는다.

### 3.1 블랙홀 Level이 오르면

- 블랙홀 Level은 판을 시작할 때 0으로 초기화된다. 성장도에 따른 Level EXP 표를 쓴다.
- Level이 오를 때마다, **성장 노드를 샀다면**:
  - 적 추가 공급(`enemy.<종류>.growth-supply`). 동시 생존 상한에 걸리면 버려진다.
  - 그 판의 시간 연장(`hq.growth-time`).
- 성장 노드를 사기 전에는 Level만 오른다.

### 3.2 예외 — 이정표

- 이정표는 성장도 10·20·30이다.
- 성장도 9(이정표 바로 앞)인 판에서 블랙홀 Level이 목표 Level에 닿으면, **즉시 그 판을 끝내고 결산을 진행한다.**
- 남은 시간과 관계없고, 그 Step의 성장 효과(추가 공급·시간 연장)는 버린다.

## 4. 판 종료와 결산

`BattleSystem.ShutdownAsync`가 단계마다 확인하며 진행한다. 한 단계라도 실패하면 멈추고, 다시 부르면 처음부터 확인한다.

```text
1 종료 요청
2 남은 적 정리           처치가 아니다: Gold·EXP·처치 수·사망 효과가 없다
3 사망 처리 끝 확인
4 원자료(BattleRawData) 보관
5 결산 GameSession.Settle
6 표현 정리 확인
7 완전 초기화
```

**결산** (한 판에 한 번)

```text
결산 Gold = 이정표로 끝난 판이면 이정표의 고정 보상, 아니면 이 판이 번 Gold
진행 상태 ─ Gold += 결산 Gold
          ─ 성장도 = 이 판이 목표 Level에 닿았으면 +1
```

- 다시 불러도 아무 일도 없다. Gold를 더하다 실패하면(넘침) 결산하지 않은 상태로 남는다.
- 원자료에 번 Gold·결산 Gold·종류별 처치 수·도달 Level·성장도 전과 후·이정표가 남는다. 결산 화면이 이것을 보여 준다.
- 시간 종료·End battle·이정표 도달 모두 같은 정리·결산 절차를 쓴다.

## 5. 획득한 Gold의 사용처

- 업그레이드 화면에서 노드를 사는 데 쓴다.
- 예외: 이정표 판(성장도 9 → 10 등)은 집계된 Gold가 아니라 **정해진 큰 금액**을 받는다(성장도 10 → 30,000, 20 → 2억, 30 → 10억 [임시]).

## 6. 코드 위치

| 자리 | 파일 |
|---|---|
| 피해·파괴 입구 | `Core/World/World.cs` (`DealDamage`, `RequestDestroy`) |
| 사망 확정·처치 수·Gold | `Core/Enemies/EnemyRoster.cs` (`RecordDeath`, `Kills`, `EarnedGold`) |
| 사망 효과 | `Core/DeathEffects/DeathEffects.cs` |
| EXP·Level | `Core/Hq/Hq.cs` (`AddExp`, `RaiseLevels`) |
| 정리·원자료·결산 | `Unity/Battle/BattleSystem.cs` (`ShutdownAsync`), `Core/Session/GameSession.cs` (`ClearRemainingEnemies`, `CreateRawData`, `Settle`), `Core/Session/BattleRawData.cs` |
| 결산 화면 | `Unity/Flow/ScreenFlow.Settlement.cs`, `Unity/Screens/SettlementScreen.cs` |
