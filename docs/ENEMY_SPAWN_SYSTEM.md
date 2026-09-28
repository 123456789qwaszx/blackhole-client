# 적 생성 시스템

작성일: 2026-09-28 · 기준: `dev`

관련: [적 파괴·처치·결산 시스템](ENEMY_KILL_SYSTEM.md) · [블랙홀 성장 시스템](BLACKHOLE_GROWTH_SYSTEM.md) · 세부 규칙 [BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md) 3절

경로는 `Assets/BlackHole/` 기준이다.

## 1. 생성 요청은 어디서 오는가

적은 **정해진 통로로 들어온 생성 요청**으로만 만들어진다. 시간이 지난다고 저절로 생기지 않는다.

| 통로 | 처리하는 곳 | 요청 수 |
|---|---|---|
| 게임(판) 시작 | `GameSession.Begin`이 0초에 즉시 처리 | 콘텐츠의 시작 공급(소행성 8) + 시작 공급 노드 |
| 블랙홀 Level업 | `World.Step`의 정해진 순서(6에서 요청, 7에서 처리) | 성장 공급 노드의 수. 노드를 사기 전에는 0 |
| 소행성 파괴 시 | **현재 미구현.** 파괴가 다시 생성을 부르므로 끝나는 규칙을 먼저 정한다 | — |
| 개발용 적 명령 콘솔 | `World.RequestSpawn` → 다음 Step의 7 | 버튼마다 |

요청은 "종류 × 수"다. 공급 데이터는 계열 종류(소행성)로 요청하고, 실제로 어떤 종류로 나올지는 생성 단계가 정한다(3절).

## 2. 적 스탯 및 종류를 정하는 것

### 2.1 블랙홀 성장도

- 성장도는 판을 넘어 유지되는 블랙홀의 진행 단계다. **목표 Level에 닿은 판의 결산 때 +1** 오른다([블랙홀 성장 시스템](BLACKHOLE_GROWTH_SYSTEM.md)).
- 전체는 성장도 0~30이다. 소행성 단계는 0~10이다.
- 성장도마다 소행성의 색 비율이 달라진다(빨 → 주 → 노 → … → 보라). 성장도 8부터는 보라만 나온다.
- **성장도 10(이정표)에 도달하면 행성이 나오기 시작한다(기본 3%).**
- 색 비율은 판을 시작할 때의 성장도로 정해지고, 판 중에는 바뀌지 않는다.

### 2.2 노드 업그레이드

| 노드 | 효과 |
|---|---|
| 소행성·행성·별 질량 (각 8개) | 그 종류의 체력과 보상 Gold 증가(HP·Gold 계수). 색 비율은 바꾸지 않는다 |
| 소행성을 행성으로 (5개) | 행성 비율 증가: 처음 +52%, 이후 +5%씩. 성장도 기본값(3%)에 더한다 |
| 행성을 별로 (5개) | 별 비율 증가: 처음 +10%, 이후 +5%씩 |
| 황금 소행성 등장 (하나) | 소행성의 10%가 황금(돈 50배)으로 나온다 |
| 전기 소행성 등장 (하나) | 소행성의 10%가 전기 소행성으로 바뀐다 |
| 달·혜성·초신성 등장 (각 하나) | 행성의 10%가 달·혜성으로, 별의 10%가 초신성으로 바뀐다 |
| 소행성 공급 (3개) | 판 시작 공급 +4씩 |
| 소행성 성장 공급 (하나) | 블랙홀 Level업마다 소행성 +4 |

- 황금은 종류가 아니라 특성이다. 소행성만 황금이 된다. 전기 소행성은 황금이 되지 않으므로, 두 노드를 모두 사면 황금은 요청의 9%다.
- 변환·특수 확률은 %로 적고 100%에서 멈춘다.

## 3. 한 마리가 만들어지는 순서

`World.ProcessSpawnRequests`가 요청 한 마리마다 아래 순서로 처리한다.

```text
1 전체 동시 생존 상한(200)   가득이면 버린다. 버린 요청은 아래 몫을 쓰지 않는다   SpawnFilter
2 변환 사슬                 소행성 → 행성 → 별 (변환 비율만큼)                  World.KindOf
3 특수 종류                 전기 소행성·달·혜성·초신성 (생성 확률만큼)            World.KindOf
4 색                       이 판의 색 비율                                    QuotaPicker
5 황금                     황금 비율 (소행성만)                                QuotaPicker
6 위치                     HQ 둘레 출현 띠의 무작위 지점
7 수치                     (종류, 색, 황금)의 HP·속도·크기·Gold·EXP            EnemyStatTable
```

- 변환과 특수 선택은 수를 늘리지 않는다. 요청 한 마리의 종류를 바꿀 뿐이다.
- 비율은 개체마다 굴리지 않고 **몫을 쌓아** 판 전체에서 거의 정확히 지킨다(`Core/Common/QuotaPicker.cs`).
- 색과 황금 여부는 생성 때 정해지고, 그 적이 살아 있는 동안 바뀌지 않는다.

## 4. "블랙홀 성장도, 노드 업그레이드"를 쓰고 읽는 곳

### 4.1 쓰는 곳

**블랙홀 성장도**
- 전투 중 EXP가 100% 차면 판 Level이 오른다(`Hq.RaiseLevels`, Step 5).
- 결산 때 이 판이 목표 Level에 닿았으면 성장도 +1을 `PlayerState.GrowthStage`에 반영한다(`GameSession.Settle`).

**노드 업그레이드**
- 업그레이드 화면에서 노드를 사면 `PlayerState.OwnedNodes`에 기록한다(`NodePurchase.TryPurchase`). 기록은 즉시다.
- 전투에는 **다음 판 조립 때** 반영된다. 전투 중에는 살 수 없다.

### 4.2 읽는 곳 — `SessionAssembler.CreateBattle()`

업그레이드 화면에서 전투에 들어갈 때 한 번 부른다.

- 블랙홀 성장도(`PlayerState.GrowthStage`)와 산 노드(`PlayerState.OwnedNodes`)를 읽는다.
- 산 노드는 노드 트리(`NodeCatalog`)에서 수치를 찾아 업그레이드 표(`UpgradeTable`)로 모은다(`NodePurchase.UpgradesFor`).
- 콘텐츠(`GameContent`)의 적 종류·성장도 표에 업그레이드 표와 성장도를 적용한다.
- 출현 가능한 적의 종류와 종류별 비율, 적의 크기·체력·획득 Gold를 정하고 판(`GameSession`)에 묶는다. **판이 끝날 때까지 바뀌지 않는다.**

### 4.3 판에 묶이는 것

| 객체 | 담는 것 | 쓰이는 곳 |
|---|---|---|
| `GameSession` | 이번 판의 상태·시간·시작 공급·`World` | 전투 시작, Step 진행, 종료·결산 |
| `BattlePlayer` | 참가자의 조준점과 실행할 `BreakerSkill`·`LaserSkill`. 구매 노드는 Breaker 수치(피해·공격 속도·반지름·치명타)에 적용된다 | `World.Step` 2에서 스킬 실행 |
| `EnemyComposition` | 종류별 질량 단계, 황금 비율·배율, 시작·성장 공급 수, 다음 종류로의 변환 비율(성장도 기본값 + 노드), 특수 적 출현 비율 | `EnemyStatTable`에 담겨 공급 수 계산과 종류·황금 선택에 쓰인다 |
| `EnemyStatTable` | 이 판의 성장도, 종류별 색 비율, (종류, 색, 황금 여부)별 HP·속도·크기·Gold·EXP | `World`가 적을 만들 때 조회 |
| `Hq` | 이 판의 성장도, 판 Level 표·목표 Level, Level업마다 늘어나는 시간 | Step 5, 시간 연장 |

## 5. 코드 위치

| 자리 | 파일 |
|---|---|
| 시작 공급 | `Core/Session/GameSession.cs` (`Begin`), `Core/Session/SessionAssembler.cs` (`StartSupplyOf`) |
| Level업 공급 | `Core/World/World.cs` (`Step`, `RequestGrowthSupply`) |
| 생성 처리 | `Core/World/World.cs` (`ProcessSpawnRequests`, `KindOf`), `Core/Enemies/SpawnFilter.cs`, `Core/Common/QuotaPicker.cs` |
| 판 구성 | `Core/Enemies/EnemyComposition.cs` (`From`), `Core/Enemies/EnemyStatTable.cs` |
| 종류 데이터 | `Data/Enemies/*.asset`, `Data/EnemySupplySetup.asset`, `Data/NodeCatalog.asset` |
