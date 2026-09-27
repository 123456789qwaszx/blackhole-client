# ENEMY UNLOCK PLAN — 단계·풀을 지우고, 나오는 적 종류를 노드가 정하게 하기

작성일: 2026-09-27

브랜치: `integration/dev-all`

기준 문서: [BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md) 2.1·3·4.7 · [INTEGRATED_GAMEPLAY_FLOW](INTEGRATED_GAMEPLAY_FLOW.md) · [REFERENCE_ANALYSIS](REFERENCE_ANALYSIS.md) 3.3 · [SKILL_TREE_PLAN](SKILL_TREE_PLAN.md) 4.3

| 표기 | 뜻 |
|---|---|
| 원작 | 원작 관찰(BATTLE_COMPOSITION_PLAN 2.1의 사용자 관찰, REFERENCE_ANALYSIS) |
| 사용자 | 사용자의 요구와 결정 |
| 제안 | 이 PLAN이 정한 것. 사용자가 바꾸면 따른다 |
| 임시 | 확인 전까지 쓰는 값 |
| 미정 | 결정 필요 |

## 1. 목표

1. **단계(진행도)와 적 풀을 지운다** [사용자]. 지금 단계는 풀(나올 수 있는 종류)만 고르고, 바꾸는 곳은 개발용 콘솔 버튼뿐이다. 개발용이 아닌 빌드는 늘 1단계다.
2. **나오는 적 종류를 산 노드가 정한다** [사용자]. 원작에서 나오는 종류는 판 사이의 구매(질량 진행)가 정한다 [원작].
3. 판 구성의 출처를 업그레이드 표 하나로 모은다. 질량 단계·황금·시작 공급처럼 해금도 판 조립 때 표에서 한 번 계산하고, 판 동안 바뀌지 않는다.

## 2. 범위

| 한다 | 하지 않는다 |
|---|---|
| 종류 해금을 업그레이드 수치로(`enemy.<종류>.unlock`) | 블랙홀 성장(판 안의 레벨·EXP·추가 공급·시간 연장) — 이 PLAN이 끝난 뒤 [사용자] |
| 잠긴 종류는 판에 나오지 않는다(시작 공급·생성 요청 모두) | 종류 이정표(보라만 남으면 큰 보상, "n/7") — 블랙홀 성장 뒤, 저장(F04)과 함께 |
| 단계·풀·단계 표를 Core·Unity·데이터·콘솔에서 지운다 | 레이저를 노드로 여는 것(원작: 별과 함께) — 스킬 해금은 따로(SKILL_SYSTEM_PLAN D7) |
| 샘플 노드 트리에 해금 노드를 넣는다 [임시] | 누적 Gold·산 노드 수로 난이도 정하기 — 원작에서 보지 못했다 [사용자] |

## 3. 원작에서 가져오는 순서 [원작]

- 처음에는 소행성만 나온다. 소행성 질량을 올리면 색이 보라까지 밀린다.
- 행성 판은 벽돌 행성 + 보라 소행성으로 시작한다. 달은 행성과 함께 나온다. 혜성은 파란 행성부터 나온다.
- 별은 행성 뒤다. 폭발하는 별(초신성)은 파란 별부터 나온다.
- 전기 소행성은 원작 관찰이 없는 샘플 종류다 [임시, SKILL_SYSTEM_PLAN D4].

## 4. 규칙 [제안]

### 4.1 해금은 업그레이드 수치다

- 종류마다 수치 이름 `enemy.<종류>.unlock`을 둔다. 판 조립이 방장의 표로 `Apply(unlock, 기본값)`을 계산해 **1 이상이면 해금**이다.
- 기본값은 종류 정의에 둔다: 처음부터 나오는 종류는 1, 나머지는 0. 저작 칸은 "잠긴 채 시작"(`StartsLocked`, 기본 false)이고, 켜면 기본값이 0이다. 샘플은 소행성만 끈다.
- 해금 노드는 이 수치에 더하기 1을 준다. 한 노드가 여러 종류를 함께 열 수 있다(업그레이드 여러 개).
- 해금 여부는 판 구성(`EnemyComposition`)의 한 칸이다. 질량 단계·황금·시작 공급과 같이 판 조립 때 한 번 정해진다.
- 조건("소행성 질량이 N 이상이면 행성") 대신 노드로 연다. 원작의 순서는 트리의 선(어느 노드 뒤에 해금 노드가 있는가)으로 표현한다.

### 4.2 잠긴 종류는 판에 없다

- **시작 공급:** 잠긴 종류의 요청은 판 조립 때 뺀다. 시작 공급 추가(`start-supply`)도 해금된 종류에만 더한다.
- **생성 요청:** 판 안의 생성 요청(지금은 개발용 적 명령 콘솔)도 잠긴 종류는 거른다. 지금 풀 여과 장치가 "풀에 없는 종류"를 거르는 자리다.
- **동시 수 상한:** 종류마다의 최대 수(풀의 `MaxAlive`)는 지운다. 공급은 시작 공급과 개발용 명령뿐이고 수가 정해져 있다. 전체 상한(`MaxAliveEnemies`, 200 [임시])은 남는다. 블랙홀 성장이 반복 공급을 들이면 그때 다시 본다.

### 4.3 지우는 것

| 곳 | 지우는 것 |
|---|---|
| Core | `StageDefinition`, `EnemyPool`(Core), `PoolFilter`의 풀 판정, `GameContent.GetStage`·`StageCount`, `ContentData`의 단계·풀, 로더의 단계·풀 검사, `SessionAssembler`의 `stage` 인자·`FirstStage`, `GameSession.Stage`, `BattleRawData.Stage` |
| Unity | `StageTable`·`EnemyPool` 에셋 스크립트, `GameBootstrap`의 단계 표 칸, `BattleOrchestrator`의 단계, 조종 콘솔의 단계 버튼·표시, 수명 콘솔 원자료의 Stage 줄 |
| 데이터 | `StageTable.asset`, `Pools/*.asset` |

seed는 남는다(같은 콘텐츠·산 노드·seed·진행 시간이면 같은 판).

### 4.4 샘플 해금 노드 [임시]

| 노드 | 연다 | 앞 노드(선) | 원작 근거 |
|---|---|---|---|
| `planet-unlock` | 행성, 달 | `asteroid-mass-8` | 행성 판은 보라 소행성과 시작, 달은 행성과 함께 |
| `comet-unlock` | 혜성 | `planet-mass-4` | 파란 행성부터 [임시: 파랑이 되는 질량 단계] |
| `star-unlock` | 별 | `planet-mass-8` | 별은 행성 뒤 |
| `supernova-unlock` | 초신성 | `star-mass-4` | 파란 별부터 [임시] |
| `electric-asteroid-unlock` | 전기 소행성 | `asteroid-mass-4` | 원작 관찰 없음 [임시] |

- `planet-mass-1`은 `planet-unlock` 뒤로, `star-mass-1`은 `star-unlock` 뒤로 옮긴다. 열지 않은 종류의 질량부터 사는 일이 없게 한다.
- 가격과 격자 칸은 [임시]다. 행성·별 해금은 앞 노드와 그 종류의 질량 1 사이(30,000 · 200,000,000), 나머지는 앞 노드보다 조금 비싸게(전기 소행성 500 · 혜성 2,000,000 · 초신성 20,000,000,000). 칸은 앞 노드 옆의 빈칸이다.

## 5. 결정

| # | 항목 | 이 PLAN의 선택 | 근거 |
|---|---|---|---|
| D1 | 해금의 모양 | 업그레이드 수치(`unlock`), 기본값은 종류 정의, 1 이상이면 해금 | 판 구성의 다른 칸과 같은 길(표 → `EnemyComposition`). 새 구매 규칙이 필요 없다 |
| D2 | 종류마다의 동시 최대 수 | 지운다. 전체 상한만 | 공급 수가 정해져 있다. 블랙홀 성장 때 다시 본다 |
| D3 | 잠긴 종류의 생성 요청 | 거른다 | 풀 여과 장치의 "풀에 없는 종류"와 같은 자리 |
| D4 | 처음부터 나오는 종류 | 소행성만 [임시] | 원작 |
| D5 | "단계" 표시 | 없음. 종류 이정표(n/7)가 붙을 때 그것에서 계산한다 | 독립된 손잡이를 두지 않는다 |

## 6. 계약

지워지는 계약(풀·단계):
- `Content.ReportsPoolAndStageErrorsWithPath`, `Content.StageTableNumbersStagesFromOne`
- `Enemy.PoolFilterDropsKindsOutsideThePool`, `Enemy.PoolFilterCapsAliveCountPerKind`, `Enemy.BattleUsesItsStagePool`
- `Session.RejectsStageOutsideContent`

고쳐 쓰는 계약:
- `Session.RemembersStageAndSeed` → seed만(`Session.RemembersSeed`)
- `Enemy.FilteredSpawnsUseNoTierGoldenOrPlacement`, `Enemy.FilteredSpawnRequestsAreDropped` → 거르는 까닭을 "풀 밖"에서 "잠긴 종류·전체 상한"으로
- `Progress.FailedAssemblyLeavesProgressFree` → 실패 원인을 범위 밖 단계 대신 한계 밖 노드로

새 계약:
- `Enemy.OnlyUnlockedKindsAppear` — 잠긴 종류는 시작 공급·생성 요청 모두 나오지 않는다. 해금 노드를 사면 다음 판부터 나온다. 판 동안 바뀌지 않는다
- `Content.UnlockDefaultsComeFromTheKind` — 기본값이 1인 종류는 노드 없이 나온다. 잠긴 채 시작하는 종류는 해금 노드를 사야 나온다

## 7. 티켓

| 티켓 | 내용 | 선행 | 상태 |
|---|---|---|---|
| EU-001 | 이 PLAN | — | 완료 (사용자 검토) |
| EU-002 | Core·Unity·데이터: 해금 수치와 판 구성의 해금 칸, 잠긴 종류 거르기, 단계·풀·단계 표를 Core·Unity·데이터·콘솔에서 지우기, 종류 에셋의 해금 기본값, 계약 | EU-001 | 완료 |
| EU-003 | 데이터: 샘플 해금 노드와 트리 재배선(4.4절) | EU-002 | 완료 |
| EU-004 | 문서: INTEGRATED_GAMEPLAY_FLOW, BATTLE_COMPOSITION_PLAN 3절(단계 → 풀 대신 노드 → 해금), SYSTEM_CATALOG | EU-003 | 완료 |

## 8. 남은 결정

| 항목 | 지금 | 정할 때 |
|---|---|---|
| 블랙홀 성장(판 안의 레벨·EXP·추가 공급·시간) | 없음 | 이 PLAN 뒤 [사용자] |
| 종류 이정표와 그 보상 | 없음 | 블랙홀 성장 뒤, 저장(F04) |
| 파랑이 되는 질량 단계(혜성·초신성 해금 자리) | 4 [임시] | 원작 확인 |
| 반복 공급이 생길 때 종류마다의 동시 최대 수 | 없음 | 블랙홀 성장 |
