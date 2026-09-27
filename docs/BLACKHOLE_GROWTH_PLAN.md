# BLACKHOLE GROWTH PLAN — 판 안에서 블랙홀이 크고, 산 노드가 성장마다 판을 넓힌다

작성일: 2026-09-27

브랜치: `feature/블랙홀성장` (`integration/dev-all` `a05fe37`에서)

기준 문서: [GAME_RULES_MVP](GAME_RULES_MVP.md) 3.2·4·9·11·12·13·14절 · [REFERENCE_ANALYSIS](REFERENCE_ANALYSIS.md) 3.3·3.4 · [REFERENCE_ANALYSIS_ENEMY](REFERENCE_ANALYSIS_ENEMY.md) 4절 · [BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md) 2.1·4.8 · [ENEMY_UNLOCK_PLAN](ENEMY_UNLOCK_PLAN.md) · [INTEGRATED_GAMEPLAY_FLOW](INTEGRATED_GAMEPLAY_FLOW.md)

| 표기 | 뜻 |
|---|---|
| 원작 | 원작 공개 자료(개발자 답변·패치 노트·공략). 출처는 9절 |
| 규칙 | GAME_RULES_MVP에 이미 있는 규칙 |
| 사용자 | 사용자의 원작 플레이 관찰과 결정 |
| 제안 | 이 PLAN이 정한 것. 사용자가 바꾸면 따른다 |
| 임시 | 확인 전까지 쓰는 값 |
| 미정 | 결정 필요 |

## 1. 목표

1. 적을 부수면 블랙홀(HQ)이 EXP를 얻고, 임계값을 넘을 때마다 Level이 오른다 [규칙 3.2·9]. 원작은 이 값을 "matter"라 부른다 [원작].
2. **성장 노드를 산 뒤에는** Level이 오를 때마다 지금 판의 시간이 늘고 적이 더 나온다 [사용자]. 노드를 사기 전에는 Level만 오른다.
3. 늘어난 시간은 그 판의 것이다. 판은 언제나 기본 제한 시간 **14초**에서 시작한다 [사용자].
4. 성장은 판 안의 상태다. 판마다 Level 1부터 다시 큰다 [규칙 4, 원작].

## 2. 범위

| 한다 | 하지 않는다 |
|---|---|
| 적 색 등급마다의 EXP(판 조립 때 정해진다) | 원작의 "이정표"(한 판에서 정해진 Level에 닿으면 완료 보상, "n/7") — 다음 PLAN, 저장(F04)과 함께 |
| 블랙홀의 EXP·Level, 한 번에 여러 Level, Level 표(임계값) | 게임 완료(마지막 이정표)·에필로그 |
| 성장 효과를 주는 업그레이드 수치: Level마다 늘어나는 시간, Level마다 추가 공급 | 기본 제한 시간을 바꾸는 업그레이드 — 판은 늘 14초에서 시작한다 [사용자] |
| Step 5·6 자리, 성장으로 늘어난 시간을 반영한 뒤 종료 판정 | 파괴 뒤 확률 생성, 혜성 무리 같은 다른 공급 계기 |
| 샘플 성장 노드(시간·소행성 공급) | 블랙홀 크기가 공간(출현 띠·공전 거리·흡수)을 바꾸는 것 |
| 전투 화면의 Level·진행 막대, 블랙홀 그림의 크기, 결산의 도달 Level, 원자료 | 흡수 연출(부서진 조각이 빨려 드는 것) |

## 3. 원작에서 확인한 것

| 내용 | 근거 |
|---|---|
| 특정 노드를 산 뒤에는 블랙홀이 Level업할 때마다 시간이 늘고 적이 더 나온다. 노드를 사기 전에는 그런 효과가 없다 | 사용자 (2026-09-27) |
| Level업의 시간 연장은 그 판에만 3초를 더한다. 다음 판은 처음부터 끝까지 언제나 14초에서 시작한다 | 사용자 (2026-09-27) |
| 블랙홀이 성장하면 소행성이 추가되고 시간이 늘어난다. 추가 소행성 수를 늘리는 업그레이드가 초반에 있다 | 기존 플레이 관찰 (REFERENCE_ANALYSIS 3.3) |
| 성장할 때마다 다음 성장에 더 많은 matter가 필요하다 | 패치 1.6.0 (Roguelike: "Each time you grow the Black Hole you'll need to feed it more") [B1] |
| Level과 "다음 Level까지의 %"를 기록한다(12.56 = Level 12를 넘고 13까지 56%) | 패치 1.6.3 [B2] |
| 색 등급마다 돈과 EXP가 비선형으로 다르다 | 개발자 답변 (BATTLE_COMPOSITION_PLAN G2) |
| 이정표 7개는 "한 판에서 블랙홀을 충분히 여러 번 키워" 닿는다. 공략의 Level 범위: 10~20, 20~30, 30~35, 35~40, 40~41, 41~42 | 도전 과제 설명과 공략 [B3] |
| 이정표에 닿으면 완료 보상(큰돈)을 준다. "Milestone rewards now only round up" | 사용자 관찰 (BATTLE_COMPOSITION_PLAN 2.1), 2025-12-16 업데이트 [B4] |
| 데모 모드는 크기를 "n/10"으로 보여 주고, 막대로 다음 크기까지를 보여 준다("9/10의 95%") | 플레이어 보고 [B5] |
| 막대를 다 채우기 전에 천체가 떨어지면 성장하지 못한다(판의 천체는 유한하다) | 플레이어 보고 [B6], 패치 1.3 (REFERENCE_ANALYSIS_ENEMY) |

**원작에서 찾지 못한 것:** Level업마다 나오는 소행성 수, matter 곡선, 시작 Level, 행성이 열린 뒤에도 성장 공급이 소행성인가, 블랙홀 크기가 출현 거리를 바꾸는가. 값은 [임시]로 두고 플레이로 맞춘다.

**분석:**

- 성장 효과는 Level마다 다른 표가 아니라 **산 노드가 정하는 한 벌의 값**이다. Level업마다 같은 값이 온다. 판 구성(질량·황금·시작 공급·해금)처럼 판 조립 때 업그레이드 표에서 한 번 계산한다.
- 원작의 "이정표"는 GAME_RULES 11절의 Growth Milestone(시간·공급을 주는 Level)과 다른 것이다. 원작 이정표는 판의 목표이고 보상은 돈이다. 이 PLAN의 시간·공급은 성장 노드를 산 뒤의 모든 Level업에 온다. GAME_RULES 11절은 이 PLAN이 끝날 때 고쳐 쓴다(BG-005).

## 4. 규칙 [제안]

### 4.1 EXP

- 적 색 등급마다 EXP를 둔다(Gold 옆 칸). 판 조립 때 적 수치 표(`EnemyStatTable`)에 옮겨 적고, 판 동안 바뀌지 않는다 — Gold와 같은 길이다(BATTLE_COMPOSITION_PLAN).
- 질량 단계는 EXP를 곱하지 않는다. 질량이 오르면 윗 색이 나와 EXP가 저절로 오른다. 황금은 EXP를 곱하지 않는다 [사용자, D3].
- EXP는 사망이 확정되는 순간 블랙홀에 든다 [규칙 9]. 누가 부쉈는지 보지 않는다(사망 효과·파괴 요청도 같다). 전투 정리로 치운 적은 주지 않는다 [규칙 9].
- 블랙홀은 판에 하나다. 판 안의 모든 참가자가 함께 키운다(방장 모델, INTEGRATED_GAMEPLAY_FLOW 6절).

### 4.2 Level

- 판은 Level 1, EXP 0에서 시작한다 [임시]. 이전 판의 Level은 이어지지 않는다 [규칙 4].
- Level 표는 **누적 EXP 임계값**의 목록이다. 줄 i가 Level (i + 2)에 닿는 값이고, 앞 줄보다 커야 한다. 성장 효과는 표에 두지 않는다(4.3).
- 한 번에 여러 임계값을 넘으면 여러 Level이 오른다 [규칙 3.2]. 성장 효과는 오른 Level마다 한 번씩이다.
- 표의 끝을 넘으면 Level은 더 오르지 않고 EXP만 쌓인다.
- 진행 막대: 지금 Level의 임계값에서 다음 임계값까지 몇 %인가. 마지막 Level이면 가득 찬 것으로 보인다.

### 4.3 성장 효과 [사용자]

Level업 한 번마다 이 판의 성장 효과가 한 번 온다. 값은 판 조립 때 산 노드로 정해지고, 기본값은 모두 0이다(노드를 사기 전에는 Level만 오른다).

| 수치 이름 | 뜻 | 기본값 | 한계 | 샘플 노드 [임시] |
|---|---|---|---|---|
| `hq.growth-time` | Level업마다 이 판의 제한 시간에 더하는 초 | 0 | ≥ 0 | `growth-time`: 더하기 3 [사용자: 3초] |
| `enemy.<종류>.growth-supply` | Level업마다 그 종류를 이만큼 더 요청한다 | 0 | 정수(반올림), ≥ 0 | `asteroid-growth-supply`: 소행성 더하기 4 [임시] |

- 늘어난 시간은 그 판에만 있다. 다음 판의 제한 시간은 다시 콘텐츠의 기본값(14초)이다 [사용자]. 제한 시간은 판(`TimeLimitRule`)의 것이고 공유 정의는 바뀌지 않는다.
- 성장 공급은 전투 시작 공급과 같은 생성 요청이다: 잠긴 종류는 거르고(해금된 종류의 수치만 쓴다), 전체 상한(`MaxAliveEnemies`)에 닿으면 버린다(ENEMY_UNLOCK_PLAN 4.2).
- 한 번의 Level업이 요청하는 수는 유한하고, Level 표가 유한하므로 한 판의 성장 공급도 유한하다 [규칙 12]. 종류마다의 동시 최대 수는 다시 두지 않는다 [제안 — ENEMY_UNLOCK_PLAN 8절 "반복 공급이 생길 때"의 답].
- 성장 공급의 요청 순서는 콘텐츠 종류 순서다.

### 4.4 한 Step 안의 순서 [규칙 13]

```text
1 적 이동 → 2 스킬 공격 → 3 파괴 요청 → 4 사망 효과
→ 5 EXP·Level 반영 → 6 성장 효과(오른 Level 수만큼 시간 연장·공급 요청)
→ 7 공급 처리(생성 여과 → 색 → 황금 → 위치) → 8 종료 판정
```

- 1~4에서 확정된 사망의 EXP는 5에서 Level로 반영된다. 사망 효과가 만든 사망도 같은 Step이다 [규칙 13].
- 6의 공급 요청은 같은 Step의 7에서 나온다. 나온 적은 다음 Step부터 공격받는다(지금 규칙).
- 6에서 늘어난 시간은 8보다 먼저다. 마지막 Step에서 성장하면 판이 이어진다 [규칙 13·14].
- 끝난 판에서는 성장하지 않는다 [규칙 14].

### 4.5 판 조립이 정하는 것

| 무엇 | 입력 | 판 동안 |
|---|---|---|
| 색 등급마다의 EXP | 종류 에셋의 색 등급 표 | 고정 |
| Level 표(임계값) | 블랙홀 성장 에셋 | 고정(공유 정의) |
| Level업마다의 시간·종류별 공급 | 산 노드(`hq.growth-time`, `enemy.<종류>.growth-supply`) | 고정 |
| 제한 시간 | 콘텐츠의 기본값 14초 [사용자] | 성장으로만 늘어난다 |

노드를 모두 산 경우의 검사(`UpgradeContentCheck`)에 성장 수치의 한계(음수 아님)를 더한다. 성장 공급은 판 중에 자리가 나며 나오므로 시작 공급처럼 전체 상한과 합을 비교하지 않는다.

### 4.6 화면

- 전투 화면: Level과 진행 막대(4.2).
- 블랙홀 그림: HQ 자리(원점)의 원. Level마다 커진다. 그림일 뿐, 출현 띠·공전·Breaker와 무관하다 [제안, D5].
- 결산 화면: 도달 Level. 원자료(`BattleRawData`)가 도달 Level과 EXP를 가진다.
- 조종 콘솔: 적 설명창의 색 등급 줄에 EXP, 판 구성에 Level업마다의 공급. 개발용 콘솔에 Level·EXP·다음 임계값·Level업마다의 시간.

## 5. 결정

| # | 항목 | 선택 | 근거 |
|---|---|---|---|
| D1 | 시간·공급을 주는 때 | 성장 노드를 산 뒤의 모든 Level업. 값은 산 노드가 정한다 | 사용자 (2026-09-27) |
| D2 | EXP의 모양 | 색 등급의 칸, 판 조립 때 고정, 질량 단계는 곱하지 않는다 | Gold와 같은 길(판 구성). 단순한 규칙 [제안] |
| D3 | 황금이 EXP를 곱하는가 | 곱하지 않는다 | 사용자 |
| D4 | 성장 공급 업그레이드 | 수치 `enemy.<종류>.growth-supply`와 샘플 노드. 지금 시작 공급 노드(`start-supply`)는 그대로 둔다 | 사용자. 원작 도전 과제 "50개 이상으로 시작"이 시작 공급 업그레이드를 가리킨다 |
| D5 | 블랙홀 크기와 공간 | 그림만 커진다 | 원작 확인 없음(REFERENCE_ANALYSIS 5절 질문). 공간 규칙을 바꾸면 출현 띠·공전 계약이 모두 바뀐다 [제안] |
| D6 | 시작 Level | 1 [임시] | 규칙에 없음. 원작 기록(12.56)은 시작 Level을 보여 주지 않는다 |
| D7 | 코드 이름 | `Hq`(블랙홀), EXP·Level | GAME_RULES의 이름. 원작의 matter는 문서에만 적는다 [제안] |
| D8 | 기본 제한 시간 | 14초. 바꾸는 업그레이드는 없다 | 사용자 |

## 6. 샘플 값 [임시]

플레이로 맞춘다. 지금 전투 시작 공급은 16마리(소행성 8)다.

- 기본 제한 시간: 30초 → **14초** [사용자].
- 색 등급 EXP: 각 색의 Gold와 같은 값에서 시작한다(소행성 1·2·4·9·20·45…).
- Level 표 10줄: 임계값 10, 25, 50, 90, 150, 240, 380, 600, 950, 1500.
- 성장 노드 둘: `growth-time`(Level업마다 +3초 [사용자])과 `asteroid-growth-supply`(Level업마다 소행성 +4). 싼 초반 노드로 `start` 가까이에 둔다.

## 7. 계약

- `Growth.ExpComesFromConfirmedDeathsOnly` — 사망 순간 색 등급의 EXP가 들고(황금도 같은 EXP), 전투 정리는 주지 않는다. 사망 효과·파괴 요청의 사망도 준다
- `Growth.OneGainCanRaiseSeveralLevels` — 한 Step에서 임계값 여러 개를 넘으면 그만큼 오르고, 성장 효과는 오른 Level마다 한 번씩
- `Growth.WithoutGrowthNodesOnlyTheLevelRises` — 성장 수치가 기본값이면 Level은 올라도 시간·적은 그대로
- `Growth.LevelUpExtendsThisBattleAndRequestsSupply` — 성장 노드를 사면 Level업마다 시간이 늘고 공급이 같은 Step에 나온다(잠긴 종류·전체 상한은 걸러진다)
- `Growth.GrowthOnTheLastStepKeepsTheBattleGoing` — 마지막 Step에서 성장하면 종료 판정 전에 시간이 늘어 판이 이어진다
- `Growth.EachBattleStartsFresh` — 새 판은 Level 1, EXP 0이고, 제한 시간은 콘텐츠의 기본값이다(앞 판에서 늘어난 시간은 없다)
- `Growth.StopsAtTheLastLevel` — 표 끝에서는 EXP만 쌓이고 성장 효과도 없다
- `Growth.EndedBattleDoesNotGrow` — 끝난 판은 성장하지 않는다
- `Content.ReportsGrowthErrorsWithPath` — 임계값 순서, 음수 EXP
- `Composition.LoadCheckFindsNodesTheContentCannotTake`(더함) — 음수인 성장 시간, 출현 배치 없는 공급 수 노드
- `Session.RawDataRecordsReachedLevel` — 원자료에 도달 Level과 EXP

## 8. 티켓

| 티켓 | 내용 | 선행 | 상태 |
|---|---|---|---|
| BG-001 | 이 PLAN | — | 완료 (사용자 검토) |
| BG-002 | Core: 색 등급 EXP, Level 표 정의와 로더 검사, 성장 수치(`hq.growth-time`, `enemy.<종류>.growth-supply`)와 판 조립, 판의 블랙홀(EXP·Level), Step 5·6, 제한 시간 연장, 원자료, 계약 | BG-001 | 완료 |
| BG-003 | Unity·데이터: 종류 에셋 EXP 칸과 값, 블랙홀 성장 에셋과 GameBootstrap 연결, 기본 제한 시간 14초, 전투 화면 Level·막대, 블랙홀 그림, 결산 도달 Level, 콘솔 | BG-002 | — |
| BG-004 | 데이터: 샘플 성장 노드 둘(6절) | BG-002 | — |
| BG-005 | 문서: INTEGRATED_GAMEPLAY_FLOW, SYSTEM_CATALOG S07, GAME_RULES 11절(성장 효과는 성장 노드를 산 뒤의 Level업마다) | BG-003 | — |

## 9. 출처

- **사용자 (2026-09-27)**: 성장 효과는 특정 노드를 산 뒤 Level업마다 온다. 시간 연장은 그 판에만 3초. 판은 언제나 14초에서 시작한다. 황금은 EXP를 곱하지 않는다. 성장 공급 업그레이드를 넣는다.
- **B1 · 패치 1.6.0 (2026-05-08), New Roguelike Game Mode**: Steam 뉴스(ISteamNews appid 3694480). "You beat this mode by simply growing the Black Hole 20 times", 성장마다 더 먹여야 한다.
- **B2 · 패치 1.6.3 (2026-05-13)**: 게임 오버 화면에 Level의 %를 보인다. "12.56 = Level 12를 넘고 13까지 56%".
- **B3 · 공략 "100% Achievements"** (steamcommunity sharedfiles 3695869030): 이정표 도전 과제 7개("Grow the Black Hole enough times in one session to reach the … Milestone")와 공략자가 적은 Level 범위.
- **B4 · 2025-12-16 업데이트 "LIVE: New Game Mode - The Line"**: "Milestone rewards now only round up".
- **B5 · 토론 "Demo Mode achievement"** (discussions/0/844005624123643612): "4/10 size", "95% of 9/10", "the matter gains of 9/10 are a fraction of 8/10".
- **B6 · 토론 "Not enough planets"** (discussions/0/800090666243879121): 막대를 75~95% 채운 채 행성이 떨어진다. 개발자가 행성 재생성 확률을 올린 핫픽스.
- 이전 구현: `e367f14`(M5 HQ 성장, Level 노드·성장 진행)는 `e195b40`에서 지웠다. 배울 거리로만 보고 옮기지 않는다.
