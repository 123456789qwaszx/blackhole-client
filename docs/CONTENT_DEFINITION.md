# CONTENT DEFINITION — 대표 콘텐츠 명세

작성일: 2026-09-25

상위 계획: [CONTENT_AUTHORING_PLAN](CONTENT_AUTHORING_PLAN.md) — 이 문서는 **CA-001 산출물**이며, 5절은 CA-005의 결과다.

기준 규칙: [GAME_RULES_MVP](GAME_RULES_MVP.md) · 책임 구분: [SYSTEM_CATALOG](SYSTEM_CATALOG.md) · 원작 근거: [REFERENCE_ANALYSIS](REFERENCE_ANALYSIS.md) · 상한 질문: [BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES](BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES.md)

구현 확인 기준: dev `c0996b8`

> 두 번째 Skill, Upgrade Node, Enemy 종류별 설계는 GAME_RULES_MVP 16절의 범위 밖이다. 이 문서는 MVP 규칙을 바꾸지 않는다. 여기의 콘텐츠는 모두 **구조 검증용 후보**이며, 게임에 채택할지는 정하지 않았다.

## 1. 이 문서의 범위

### CA-001 완료 조건과 위치

| 완료 조건 | 위치 |
|---|---|
| 각 Skill의 Trigger / Origin / Target / Result / Limit | 2.2, 2.3 |
| 각 Node의 Target과 적용 시점 | 3.2 |
| 새 코드가 필요한 차이와 데이터만 추가하면 되는 차이의 구분 | 각 명세의 "변경" 열, 2.4 |
| 확정 콘텐츠와 구조 검증용 후보의 구분 | 아래 표기 |

### 표기

| 표기 | 의미 |
|---|---|
| 원작 | 원작에서 확인한 내용. 근거 등급은 REFERENCE_ANALYSIS를 따른다 |
| 결정 | 사용자가 확정한 후보의 동작 (2026-09-25). 게임 채택 결정은 아니다 |
| 임시 | 이 문서의 가정. 검토 후 확정하거나 바꾼다 |
| 제안 | 구조 제안. 채택 전에는 구현 약속이 아니다 |
| 미정 | 아직 정하지 않음 |

| 변경 | 의미 |
|---|---|
| 데이터 | 현재 ContentData에 값만 추가하면 된다 |
| 코드 | Runtime·Loader·표현 코드의 확장이 필요하다 |
| 있음 | 현재 Reference에 이미 있다 |

### 대표 콘텐츠

| 대상 | 후보 | 이유 |
|---|---|---|
| Skill A | Breaker (`sample-aura`) | 현재 기본 자동 공격 |
| Skill B | 관통 레이저 | 원작에 있는 두 번째 공격. Aim Point를 읽는 시점·공간 모양·발동 단계·획득이 Skill A와 다르다 |
| Node | 기본 공격 강화, 공격 범위 확장, 레이저 해금, 성장 공급 강화 (+ 레이저 굵기) | Skill 강화 / Skill 해금 / 공급 강화 / Skill과 무관한 노드 |
| Enemy A | `sample-light` | 수치만 다른 일반 적 |
| Enemy B | `sample-electric` | 사망 효과와 특수 표현을 가진 적 |

## 2. Skill 명세

### 2.1 항목

CONTENT_AUTHORING_PLAN 4절의 항목을 쓴다. **발동 단계**와 **획득**은 Skill B를 정의하며 더했다. Breaker는 한 Step 안에서 끝나고 시작 구성으로 받지만, 레이저는 예고와 발사 사이에 시간이 있고 노드로 얻는다.

| 항목 | 질문 |
|---|---|
| ID | 다른 데이터가 무엇으로 참조하는가 |
| Trigger | 언제 실행되는가 |
| Origin | 어디를 기준으로 실행되는가 |
| Target | 누구를 선택하는가 |
| Limit | 한 번에 최대 몇 대상, 몇 회까지 영향을 주는가 |
| Result | 무엇을 요청하는가 |
| 발동 단계 | 한 번의 실행이 즉시 끝나는가, 여러 단계에 걸치는가 |
| Runtime Stat | 어떤 수치가 Battle 시작 때 확정되는가 |
| 획득 | 처음부터 가지는가, 무엇으로 얻는가 |
| Presentation | 어떤 결과를 화면·소리로 보여 주는가 |
| End | 무엇 때문에 반드시 끝나는가 |

### 2.2 Skill A — Breaker

| 항목 | 정의 | 근거 | 변경 |
|---|---|---|---|
| ID | `sample-aura` | 현재 샘플 | 있음 |
| Trigger | 일정 주기. 첫 실행은 Battle 시작(0초). 대상이 없어도 주기를 소비한다 | GAME_RULES 6절 | 있음 |
| Origin | Player의 Aim Point. 없으면 빈 실행 | GAME_RULES 5·6절. 없을 때 처리는 임시 (S04) | 있음 |
| Target | Origin 반경 안의 살아 있는 Enemy 전부. Enemy 중심으로 판정 | GAME_RULES 6절. 중심 판정은 임시 (S04) | 있음 |
| Limit | 실행 1회에 대상마다 피해 1회. 대상 수는 살아 있는 Enemy 수 이하 | GAME_RULES 6절 | 있음 |
| Result | 피해. 사망은 공통 절차 (S05) | GAME_RULES 6·8절 | 있음 |
| 발동 단계 | 즉시. 대상 선택과 피해가 같은 Step에서 끝난다 | 현재 구현 | 있음 |
| Runtime Stat | 주기, 반경, 피해. 샘플 값 0.5초·1.2·3은 임시 | 현재 구현 | 데이터 |
| 획득 | 시작 구성 | 현재 구현 | 있음 |
| Presentation | Aim Point를 따라가는 범위 원(표시 반경 = 판정 반경). 실행마다 안쪽이 밝아진다. 적중음 (5절) | GAME_RULES 6절. 소리는 제안 | 원: 있음. 소리: 코드 |
| End | 실행 순간에 끝난다 | 현재 구현 | 있음 |

원작 Breaker의 강화 축에는 치명타도 있다(REFERENCE_ANALYSIS 3.1). 이번 명세에는 넣지 않는다 [미정].

### 2.3 Skill B — 관통 레이저

**원작** (사용자 플레이 관찰): 화면 바깥의 무작위 지점에서 마우스 포인터를 향해 쏜다. 레일건처럼 얇은 직선이 먼저 생기고, 발사하면 굵어지며 화면을 가로질러 경로의 적에게 피해를 준다. 원작의 천체 효과 "레이저 별"과 같은 것인지는 확인하지 않았다.

레이저에는 역할이 다른 세 점이 있다.

| 점 | 역할 |
|---|---|
| HQ `(0,0)` | 좌표계의 원점. 레이저의 목표도 경유점도 아니다 |
| 시작점 | 발사 원점. 전투 영역 경계 위의 무작위 지점 |
| Aim Point Snapshot | 조준 대상. 예고 시작 순간 소유 Player의 Aim Point를 저장한 값 |

| 항목 | 정의 | 근거 | 변경 |
|---|---|---|---|
| ID | `sample-laser` (Reference 샘플) | — | 데이터 |
| Trigger | 일정 주기마다 1발. 시각은 아래 발사 시간표를 따른다 | 주기 1발은 결정. 시간표는 임시 | 코드 |
| Origin | 시작점과 Aim Point Snapshot. 방향은 시작점 → Snapshot이며 예고 시작 순간 확정된다 | 시작점은 원작. Snapshot과 방향 고정은 결정 | 코드 |
| Target | 시작점에서 확정 방향으로 전투 영역을 관통하는 유한 선분. 유효 폭과 겹치는 살아 있는 Enemy 전부 | 관통은 원작. 겹침 판정은 임시 (아래) | 코드 |
| Limit | 발사 1회에 대상마다 피해 1회. 대상 수는 살아 있는 Enemy 수 이하 | 결정 | 코드 |
| Result | 피해. 사망 효과를 가진 Enemy도 맞는다 (GAME_RULES 10절의 면역은 Death Effect 피해에만 적용된다) | 1회는 결정. 면역 범위는 GAME_RULES | 있음 (피해 절차) |
| 발동 단계 | 예고 → 발사 → 소멸. 방향은 예고 시작 때 확정, 피해는 발사 순간에만 | 결정 | 코드 |
| Runtime Stat | 주기, 피해, 폭, 예고 시간 | 항목은 임시. 값은 미정 | 코드 |
| 획득 | 처음에는 없다. 해금 노드를 산 뒤의 Battle부터 가진다 | 결정 | 코드 |
| Presentation | 예고: 얇은 선. 발사: 폭만큼 굵어졌다가 사라진다(표시 굵기 = 판정 폭). 예고음·발사음 | 외형은 원작. 소리는 제안 | 코드 |
| End | 발사 순간에 끝난다. 이후의 굵은 선은 표현이다 | 결정 | 코드 |

#### 발사 시간표 [임시]

T는 예고 시간, I는 주기다.

```text
t = 0          첫 예고 시작 — 시작점 선택, Aim Point Snapshot, 방향 확정
t = T          첫 발사
t = I          두 번째 예고 시작
t = T + I      두 번째 발사
t = T + k·I    (k+1)번째 발사
```

- 주기 I는 **발사와 다음 발사 사이의 시간**이다. 예고는 0, I, 2I, …에 시작한다.
- 첫 예고를 0초에 두는 것은 레이저의 임시 선택이다. Breaker의 "첫 Tick 0초"는 MVP 기본 공격의 규칙(GAME_RULES 6절)이며, 이후 모든 Skill의 공통 법칙이 아니다.
- T가 I보다 길면 예고가 겹친다. 동시 예고는 최대 ⌈T / I⌉개다.

#### 좌표로 옮기기 [임시]

"화면 바깥"은 화면의 개념이고, Gameplay는 HQ `(0,0)` 기준 공간만 안다(GAME_RULES 3.1). HQ 기준으로 정할 것은 **시작점을 어디서 뽑는가**뿐이다. 방향은 Aim Point가 정한다.

```text
전투 영역  = HQ 중심, 반지름 R인 원
시작점 S   = 전투 영역 경계 위의 무작위 지점
조준 A     = 예고 시작 순간의 Aim Point Snapshot
방향 d     = S → A (예고 중 변경 없음)
경로       = S에서 d 방향으로 나아가 다시 경계에 닿는 곳까지의 선분
```

- 경로는 보통 HQ를 지나지 않는다. A가 S와 HQ를 잇는 직선 위에 있을 때만 지난다.
- 경로를 경계에서 끝내는 것은 "화면을 가로지른다"는 원작 관찰을 옮긴 임시 선택이다. 이렇게 하면 길이가 항상 유한하다.
- R은 레이저 정의의 `BoundaryRadius`에 둔다(CA-003). 노드가 바꾸는 Skill 수치(`PiercingLaserStats`)가 아니다. 다른 시스템이 같은 경계를 쓰게 되면 그때 공유 값으로 옮긴다.
- R 원이 화면을 모두 덮어야 시작점이 화면 밖에 있다. "R ≥ 지원 화면비에서 HQ 중심 화면의 반대각선" 조건을 S02·S09 담당과 합의한다 [미정].
- Enemy는 HQ 근처에서 생성되어 거리를 유지하며 공전하므로 전투 영역 밖으로 나가지 않는다. 배치 규칙이 바뀌면 다시 확인한다.

#### 겹침 판정 [임시]

지금은 Breaker와 같이 Enemy 중심으로 본다. `경로까지의 거리 ≤ 폭 / 2`. Breaker의 중심 판정도 확정 규칙이 아니라 샘플 선택이다(S04).

굵기 강화가 있으므로 Enemy 크기를 포함하는 판정이 더 자연스러울 수 있다.

```text
distance(Enemy 중심, 경로) ≤ Enemy 반지름 + 폭 / 2
```

CA-003은 중심 판정을 그대로 두었다. Enemy 크기를 더할지는 [미정]이다.

#### 발동 단계의 규칙

| 상황 | 규칙 | 근거 |
|---|---|---|
| 예고 중 방향 | 예고 시작 순간 확정. 이후 Aim Point가 움직여도 이번 발사는 바뀌지 않는다 | 결정 |
| 다음 발사 | 새 시작점과 그 예고 시작 순간의 Aim Point로 다시 정한다 | 원작 |
| 예고 중 적 이동 | 적은 계속 움직인다. 대상은 발사 순간의 위치로 고른다 | 임시 (발사 순간 1회에서 따라 나옴) |
| 발사의 Step 위치 | Step 순서의 Passive Attack 단계에서 발사한다 | GAME_RULES 13절 |
| Aim Point가 없을 때 | 예고를 만들지 않는다. 그 예고에 해당하는 발사는 없다 | 임시 |
| Aim Point가 전투 영역 밖이거나 시작점과 같을 때 | 경로가 정해지지 않으므로 빈 실행. 화면이 전투 영역 안쪽이면 일어나지 않는다 | 임시 |
| Battle 종료 | 예고 중인 레이저는 발사하지 않는다 | GAME_RULES 14절: 종료 후 새 결과 없음 |
| 새 Battle | 예고 상태를 넘기지 않는다 | GAME_RULES 4·15절 |

#### 무작위와 재현

현재 Core에는 무작위가 없다(출현 각도도 고정 간격이다). 레이저 시작점에는 무작위가 필요하다. 기준 상황을 반복 재현하려면(S12) Battle마다 seed를 가진 난수를 두고, seed를 조립 입력으로 받는다. CA-003에서 `BattleRandom`으로 반영했고, 계약 테스트는 seed를 고정한다. 게임 호스트가 쓴 seed를 기록·지정하는 방법은 아직 없다.

#### 상한

| 질문 | 레이저의 답 |
|---|---|
| Trigger | 주기 |
| Maximum | 발사 1회의 적중 ≤ 살아 있는 Enemy 수. 동시 예고 ≤ ⌈예고 시간 / 주기⌉ |
| Propagation | 레이저가 다시 레이저를 부르지 않는다. 레이저로 죽은 전기 천체는 사망 효과를 일으키고, 그 연쇄는 기존 종료 규칙을 따른다 |
| End | 발사 순간 |
| Entity Growth | 새 Entity 없음. 예고 상태만 있고 수가 유한하다 |
| Presentation | 화면의 선 수 ≤ 동시 예고 수 + 소멸 중인 선 수 |

### 2.4 A와 B의 차이 — 코드가 필요한 것

| 항목 | Breaker | 관통 레이저 | 필요한 것 | 변경 |
|---|---|---|---|---|
| Aim Point를 읽는 때 | 실행마다 현재 값을 범위 중심으로 | 예고 시작 때 한 번 저장해 방향을 정한다 (Snapshot) | 기준점을 **언제** 읽는지도 실행 방식의 일부다. 기준점 종류 값만 늘려서는 해결되지 않는다 | 코드 |
| 마우스 이동의 영향 | 다음 실행 위치가 바로 바뀐다 | 예고 중인 발사는 바뀌지 않는다 | (위와 같음) | — |
| 공간 모양 | 점 + 원 | 시작점 + 방향 + 선분 | 대상 판정이 Skill 종류마다 다르다 | 코드 |
| Runtime 상태 | 타이머 | 타이머 + 예고 상태(시작점, 확정 방향) | 실행과 실행 사이에 남는 Skill 상태 | 코드 |
| Runtime Stat | 주기·반경·피해 | 주기·피해·폭·예고 시간 | 반경은 공통 수치가 아니다. `PassiveSkillStats`와 노드 효과 이름이 Breaker의 모양을 공통 이름처럼 쓰고 있다 (6절 P1) | 코드 |
| 획득 | 시작 구성 | 노드 해금 | 획득 상태(산 노드)와 Skill 실행의 분리. Player의 Skill 목록을 조립할 때 해금을 반영 | 코드 |
| 무작위 | 없음 | 시작점 | seed 난수 | 코드 |
| 주기·피해 값 | 같은 모양 | 같은 모양 | — | 데이터 |

`PassiveSkillDefinition`에는 기준점 종류와 반경·주기·피해만 있고, 나머지는 `PassiveSkill.Tick`에 고정되어 있다. `UpgradeModifiers`도 모든 Skill이 같은 `PassiveSkillStats`를 가진다고 전제한다. 이 차이는 CA-003에서 **Skill B 때문에 필요한 만큼만** 나눈다. 레포에는 같은 모양의 선례가 두 개 있다.

| 선례 | 정의 | 실행 연결 | 데이터 |
|---|---|---|---|
| Enemy 행동 | `EnemyBehaviorDefinition`의 하위 정의 | `EnemyBehaviors.Standard` 분기 | ContentLoader의 종류 이름 |
| 사망 효과 | `DeathEffectDefinition`의 하위 정의 | `DeathEffects.Resolve` 분기 | ContentLoader의 종류 이름 |

같은 모양이면 Skill 종류는 지금 있는 두 가지(원형 주기 공격, 관통 레이저)뿐이다 [제안]. CONTENT_AUTHORING_PLAN CA-003이 금지하는 범용 Effect Engine이나 미래 Skill을 예상한 종류 목록은 만들지 않는다.

**CA-003 반영.** 정의는 `PassiveSkillDefinition` 아래 `BreakerSkillDefinition`·`PiercingLaserDefinition`, 실행 상태는 `PassiveSkill` 아래 `BreakerSkill`·`PiercingLaserSkill`로 나눴다. 연결은 `PassiveSkills.Create` 한 곳이다. `SkillOrigin`은 없앴고, 데이터는 `SkillData.Kind`(`Breaker`, `PiercingLaser`)로 종류를 고른다. Breaker 모양이던 `PassiveSkillStats`는 `BreakerStats`가 됐다. 무작위는 전투마다 seed로 만드는 `BattleRandom` 하나다. 지금 있는 Skill 수치 효과 세 종류는 Breaker만 대상으로 받는다. 레이저를 대상으로 하면 콘텐츠 오류다. 레이저 대상 효과는 CA-004에서 정한다.

## 3. Upgrade Node 명세

### 3.1 항목

| 항목 | 의미 | 현재 |
|---|---|---|
| ID | 진행 상태가 저장하는 식별자 | 있음 |
| Cost | Gold 가격 | 있음 |
| Prerequisite | 먼저 사야 하는 노드 하나 | 있음 |
| Effect | 무엇을 어떻게 바꾸는가 (여러 개) | 있음 |
| Target | 어떤 Skill / Enemy / 공급을 대상으로 하는가 | 있음 |
| 적용 시점 | 전투 조립 / 출현 / 공급 요청 | 있음 (효과 종류마다 고정) |
| 표시 | 이름, 설명, 아이콘 | 없음. HUD가 효과에서 설명 문장을 만든다 |
| 배치 | 좌표, 연결선, 구역 | 없음 |

표시·배치 정보는 구매 규칙과 분리한다. 별도 타입이나 파일로 만들지는 대표 노드를 저작한 뒤 정한다(CONTENT_AUTHORING_PLAN 5절). 아래 표의 "표시 이름"은 그때 쓸 값의 예다.

### 3.2 대표 노드

가격과 수치는 전부 [임시]다.

| ID | 표시 이름 | Prerequisite | Cost | Effect | Target | 적용 시점 | 변경 |
|---|---|---|---:|---|---|---|---|
| breaker-damage | 기본 공격 강화 | 없음 | 10 | 피해 +2 | Skill `sample-aura` | 전투 조립 | 있음 |
| breaker-radius | 공격 범위 확장 | breaker-damage | 15 | 반경 +0.3 | Skill `sample-aura` | 전투 조립 | 있음 |
| laser-unlock | 관통 레이저 | breaker-damage | 30 | 해금 | Skill `sample-laser` | 전투 조립 | 있음 (CA-004) |
| growth-supply | 성장 공급 강화 | breaker-radius | 25 | Milestone 공급 수 +2 | Enemy `sample-light` | 공급 요청 | 있음 |
| laser-width | 레이저 굵기 | laser-unlock | 20 | 폭 +0.2 | Skill `sample-laser` | 전투 조립 | 있음 (CA-004) |

- 앞의 네 개가 Skill 강화 / Skill 전용 수치 / Skill 해금 / Skill과 무관한 노드를 하나씩 보여 준다.
- `laser-width`는 확인용으로 더했다. 해금한 뒤에만 의미가 있는 강화이고, Breaker에 없는 수치를 바꾼다(6절 P1·P9).

### 3.3 Skill 해금 규칙 [임시]

- Player의 Skill은 시작 구성과 해금한 Skill이다. 해금 Skill은 콘텐츠의 노드 순서로 뒤에 붙고, Step 안에서 이 순서로 실행된다(현재 World는 Player 순서 → 각 Player의 Skill 목록 순서로 실행한다).
- 보장하는 것은 콘텐츠와 조립이 정한 **안정적인 순서**뿐이다. 지금은 피해만 있어 순서가 결과를 거의 바꾸지 않지만, 상태 효과 같은 결과가 생기면 순서가 게임 결과가 된다. Skill 우선순위 시스템은 그때까지 만들지 않는다.
- 시작 구성에 이미 있는 Skill을 해금하는 노드는 콘텐츠 오류다.
- 해금 전 Skill을 대상으로 하는 강화 노드는, 선행을 따라가면 그 Skill의 해금 노드에 닿아야 한다. 닿지 않으면 콘텐츠 오류다. 산 강화가 효과 없이 남는 일을 막는다.
- 획득 상태는 PlayerState의 구매 기록이다. Skill 실행은 구매 여부를 모른다(CONTENT_AUTHORING_PLAN 4절).

**CA-004 반영.** 위 규칙을 그대로 구현했고, 두 가지를 더했다.

- 해금은 값이 없는 효과 `SkillUnlock`이다. 값을 적으면 콘텐츠 오류다.
- [임시] 한 Skill의 해금 노드는 하나다. 여러 경로로 여는 해금은 아직 없다.

PlayerState에는 Skill 목록을 저장하지 않는다. 산 노드 ID가 진행 상태이고, 전투 조립이 `시작 구성 + 산 해금 노드의 Skill`로 Skill 목록을 매번 다시 만든다. 레이저 강화는 `LaserDamageAdd`, `LaserIntervalMultiply`, `LaserWidthAdd` 셋이다. 6절 P1의 첫 방향(수치마다 효과 종류)을 택했고, Breaker 효과(`Skill…`)와 이름을 합치지 않았다.

### 3.4 구매 흐름

```text
UI: NodeId로 구매 요청
  ↓
Purchase Rule: 정의 조회 → 조건 검사(전투 중 아님, 미보유, 선행 보유, Gold 충분)
               → Gold 차감, ID 기록
  ↓
UI: 결과(구매됨 / 잠김 / Gold 부족 / 이미 보유)로 표시를 갱신
  ↓
다음 Battle 조립: 산 노드의 효과를 콘텐츠 노드 순서로 쌓아 반영
```

실패하면 Gold와 구매 기록을 바꾸지 않고, 기록은 ID로 남는다(현재 구현). 다만 현재 구매 화면은 NodeId가 아니라 노드 정의 객체를 넘긴다(`Hud.cs`의 `ShopRequest` → `SessionLauncher.Purchase`). CA-004의 "UI는 NodeId와 구매 결과만으로 동작" 조건에서 바꿀 자리이며, CA-002에서는 고치지 않는다.

**CA-004 반영.** `ShopRequest`는 `NodeId`만 담는다. 구매는 `UpgradePurchase.TryPurchase(state, content, nodeId)`가 노드를 찾고 조건을 본다. 콘텐츠에 없는 ID는 `UnknownNode`이며 Gold와 기록을 바꾸지 않는다.

### 3.5 F01 착수 전 결정 항목의 상태

| 항목 | 상태 |
|---|---|
| 가격 | 미정 (Balance) |
| 선행 AND/OR | 선행 하나. 대표 노드 중 둘 이상의 선행이 필요한 것은 없다 (CA-004 조건) |
| 중복 구매 | 노드는 한 번만 산다. 다음 단계는 별도 노드다 (현재 구현) |
| 효과 합성 | 산 순서와 무관하게 콘텐츠 노드 순서로 쌓는다 (현재 구현). 같은 수치에 더하기와 곱하기가 섞이면 순서가 결과를 바꾼다 [미정] |
| 적용 시점 | 3.2의 열 |

## 4. Enemy 명세

CONTENT_AUTHORING_PLAN 6절의 항목을 쓴다. 수치는 Reference 샘플 값이며 전부 [임시]다.

### 4.1 Enemy A — 일반 천체

| 항목 | 정의 | 변경 |
|---|---|---|
| ID | `sample-light` | 있음 |
| Base Stats | HP 10, 이동 1.5, Size 0.3 | 데이터 |
| Behavior | HQ Orbit, 반시계 | 데이터 (행동은 있음) |
| Reward | HQ EXP 1 (Gold 2는 후속 경제 F04) | 데이터 |
| Death Effect | 없음 | 데이터 |
| Presentation | 기본 천체 외형, 피격 번쩍임, 일반 파괴, 흡수 | 코드 (5.5) |
| Audio | 사망: `break-small`. 피격음은 공격 쪽이 맡는다 (5.2) | 코드 |
| Supply | 시작 공급, Milestone 공급 | 데이터 |

**수치 변형은 데이터 추가만으로 끝나야 한다**(CONTENT_AUTHORING_PLAN 6절). 규칙 쪽은 지금도 그렇다. 새 수치 변형 Enemy를 ContentData에 넣으면 Loader가 검증하고 공급에 쓸 수 있다. **표현 쪽은 아니다.** 색이 `SamplePresentation`의 코드 사전에 있어서, 새 종류는 코드를 고치지 않으면 흰색으로 그려진다(6절 P6).

### 4.2 Enemy B — 전기 천체

| 항목 | 정의 | 변경 |
|---|---|---|
| ID | `sample-electric` | 있음 |
| Base Stats | HP 10, 이동 1.2, Size 0.35 | 데이터 |
| Behavior | HQ Orbit, 반시계 | 데이터 |
| Reward | HQ EXP 2 (Gold 3은 후속 경제) | 데이터 |
| Death Effect | 연쇄 번개: 피해 10, 거리 2.5, 최대 3회 | 데이터 (효과는 있음) |
| Presentation | 전기 외형과 사망 효과 보유 표시, 피격 번쩍임, 방전 파괴, 번개 선 | 번개 선: 있음. 나머지: 코드 |
| Audio | 사망: `break-electric`, 번개: `lightning-zap` | 코드 |
| Supply | 시작 공급, Milestone 공급 | 데이터 |

사망 효과 보유 표시가 필요한 이유: 사망 효과를 가진 적은 효과 피해를 받지 않는다(GAME_RULES 10절). 특수 천체가 많아지면 번개에 반응하지 않는 적도 많아지므로 화면으로 알려야 한다(REFERENCE_ANALYSIS 4절).

| 질문 | 연쇄 번개의 답 (현재 구현) |
|---|---|
| Trigger | 전기 천체의 사망 |
| Maximum | 발동 1회에 최대 3회 적중 |
| Propagation | 사망 효과를 가진 적은 맞지 않으므로 효과가 효과를 부르지 않는다 |
| End | 최대 적중 수 또는 거리 안에 대상 없음 |
| Entity Growth | 없음 |
| Presentation | 적중마다 선 1개 |

## 5. Presentation / Audio 이벤트 — CA-005

CA-001 때 초안으로 쓴 절을 CA-005 결과로 고쳤다. 가설이던 재생 정책은 임시 소리를 연결해 시험 값으로 구현했다. 값은 아직 들어 보고 정한 것이 아니다(5.3).

| CA-005 완료 조건 | 위치 |
|---|---|
| Attack / Hit / Death / Absorb / Death Effect / Growth 이벤트가 구분된다 | 5.1 |
| 같은 프레임 다중 이벤트의 재생 정책을 결정할 질문이 기록된다 | 5.3 |
| Presentation을 끄더라도 Gameplay 결과가 동일하다 | 5.4. 계약 `World.ReadingRecordsDoesNotChangeGameplay` |

### 5.1 이벤트와 Core가 주는 것

표현은 Core의 상태와 기록을 읽기만 한다. 연출과 소리를 끄거나 늦춰도 게임 결과는 같다(S09).

| 이벤트 | Core가 주는 것 | CA-005 |
|---|---|---|
| Attack — Breaker 실행 | 실행 횟수 `BreakerSkill.TickCount`, 마지막 틱이 맞힌 수 `LastTickHitCount` | 맞힌 수를 더했다 |
| Attack — 레이저 예고 | 예고 중인 발사 `PiercingLaserSkill.PendingShots` (시작점·끝점·남은 예고 시간) | CA-003에서 있음 |
| Attack — 레이저 발사 | 발사 기록 `World.LaserFires` (번호·주인·경로·굵기·맞힌 수) | 더했다 |
| Hit | HP 감소 | 그대로 (화면이 HP 변화를 비교) |
| Death | 사망 기록 (번호·위치·종류·크기) | 그대로 |
| Absorb | 사망 기록에서 시작하는 표현 전용 사건 | 그대로 |
| Death Effect | 번개 적중 기록 (번호·출발·도착·대상) | 그대로. 발동 묶음 번호는 더하지 않았다 (5.3) |
| Growth — Level | HQ Level | 그대로 (값 비교) |
| Growth — Milestone·시간 연장 | 판의 제한 시간 `TimeLimit.Limit` | 기록을 더하지 않고 제한 시간이 늘어난 것으로 알아챈다. 시간 없이 공급만 주는 Milestone은 구별하지 못한다 [임시] |
| Cleanup 제거 | 목록에서 사라짐 | 그대로. 사망 연출을 만들지 않는다 |

새 Core 상태는 둘뿐이다. 레이저 발사 기록은 레이저 전용이고, 맞힌 수는 Breaker 전용이다. 모든 Skill을 위한 공격 기록은 만들지 않았다.

### 5.2 이벤트별 화면과 소리

소리는 레포에 오디오 에셋이 없어 코드로 합성한 임시 소리다(`SampleSounds`).

| 이벤트 | 화면 | 소리 | 재생 단위 |
|---|---|---|---|
| Breaker 실행 | 범위 원 안쪽이 밝아짐 (있음) | 맞힌 적이 있을 때만 `breaker-hit` | 실행 1회 |
| 레이저 예고 | 얇은 선. 발사에 가까울수록 진해진다 (CA-005) | `laser-charge` | 예고 1회 |
| 레이저 발사 | 판정 굵기 그대로의 선이 0.2초 동안 옅어진다 (CA-005) | `laser-fire` | 발사 1회 |
| Hit | 짧은 번쩍임, 남은 HP 비율만큼 본래 색 (있음) | 없음. 공격 쪽 소리가 맡는다 | — |
| Death — Enemy A | 흡수 시작 (있음). 파괴 효과는 없다 | `break-small` | 사망 1개 |
| Death — Enemy B | 흡수 시작 (있음). 방전 파편은 없다 | `break-electric` | 사망 1개 |
| Absorb | HQ로 이동하며 작아짐 (있음) | 없음 | — |
| Death Effect | 적중마다 선 (있음) | `lightning-zap` | 한 프레임에 한 번 (5.3) |
| Growth — Level | HQ가 커짐 (있음) | `hq-grow` | Level 상승 1회 |
| Growth — Milestone·시간 연장 | 없음. 남은 시간 강조는 하지 않았다 | `milestone` | 제한 시간이 는 프레임 1회 |
| Cleanup | 즉시 제거 | 없음 | — |

피격음을 적 종류가 아니라 공격 쪽에 두는 이유: 한 번의 실행이 여러 적을 맞힐 때 적마다 소리를 내면 재생 수가 적 수에 비례한다.

하지 않은 표현: Enemy별 외형과 파괴 효과, 사망 효과 보유 표시, Milestone의 남은 시간 강조. 모두 새 표현 코드와 리소스가 필요하다.

### 5.3 재생 정책 — 질문과 시험 값

CONTENT_AUTHORING_PLAN 7절은 재생 정책을 "실제 리소스를 연결하면서 결정"한다고 정했다. CA-005는 임시 소리를 연결해 아래 값을 **시험 값**으로 구현했다(`BattleAudio`, `SampleSounds`). 들어 보고 정한 값이 아니다.

| 질문 (CONTENT_AUTHORING_PLAN 7절) | 시험 값 [임시] | 아직 확인할 것 |
|---|---|---|
| Skill 발동 소리는 Tick당 1회인가? | 실행 1회에 한 번. 맞힌 수는 세기로 나타낸다(1명 70% → 5명 이상 100%). 아무도 맞히지 않은 Breaker 틱은 소리가 없다 | 0.5초 주기 적중음이 거슬리는지 |
| 피격 소리는 대상마다 필요한가? | 없다. 피격은 화면의 번쩍임, 소리는 공격 쪽 | 20개 동시 적중 장면 |
| 동일 프레임의 Death 소리는 몇 개까지인가? | 소리 종류마다 한 프레임 최대 수: `break-small` 3, `break-electric` 2 | 동시 사망 기준 상황 (S12) |
| Chain Lightning은 적중마다인가, 실행당인가? | 한 프레임에 한 번. 발동 묶음 번호가 없어 "발동 1회에 한 번"을 프레임 상한으로 근사한다 | 두 번개가 같은 프레임에 날 때 하나로 들리는 것이 괜찮은지. 안 되면 발동 번호를 더한다 |
| 동시 재생 상한을 넘으면 무엇을 생략하는가? | 목소리 12개. 모두 울리는 중이면 새 재생을 버린다 | 상한을 낮춰 두고 들리는 차이 |
| 새 Battle에서 남은 소리는? | 새 판을 시작할 때 모두 멈춘다. 구매 화면으로 갈 때는 끊지 않는다 | 재시작 반복 |
| 일시정지 중의 소리는? | 새 사건이 없으므로 새 소리가 없다. 울리던 짧은 소리는 끝까지 간다 [미정] | S09 결정 |

한 프레임의 재생 수는 "소리 종류 수 × 종류별 상한"과 목소리 수(12)를 넘지 않는다. 동시 사망이 많아도 소리 수가 규칙으로 막힌다.

### 5.4 표현을 꺼도 결과가 같다

- 화면(`WorldView`)과 소리(`BattleAudio`)는 기록을 번호로 소비하고 상태를 읽기만 한다. 게임 상태를 바꾸는 호출이 없다.
- 계약 `World.ReadingRecordsDoesNotChangeGameplay`가 이를 확인한다. 같은 seed의 두 전투 중 하나는 매 프레임 모든 기록과 실행 상태를 읽고, 다른 하나는 읽지 않는다. 결과(HQ EXP, 남은 Enemy, 각 HP)가 같아야 한다. 읽기가 기록을 소비하거나 상태를 바꾸는 방식으로 바뀌면 깨진다.
- 발사 기록은 사망 기록처럼 다음 Advance가 시작할 때 비운다. 긴 프레임 하나에 발사가 여러 번 있어도 모두 남는다(계약 `Laser.FireRecordedOncePerFire`).

### 5.5 표현 데이터의 자리

표현 데이터는 Gameplay 정의와 따로 두고 정의 ID로 연결한다. Core는 표현을 모른다(현재 구조).

| 연결 키 | 표현 데이터 | 지금 있는 자리 |
|---|---|---|
| Enemy ID | 외형, 색, 파괴 효과, 파괴음 | 색은 `SamplePresentation`, 파괴음은 `SampleSounds`의 코드 사전. 외형·파괴 효과는 없다 |
| Skill 종류 | 범위·선 표시, 실행 효과, 실행음 | `WorldView`가 종류(Breaker, 관통 레이저)로 나눠 그린다. 색·굵기·시간은 `SamplePresentation`, 소리는 `SampleSounds` |
| 사망 효과 종류 | 효과 연출, 효과음 | `WorldView`의 번개 분기, `SampleSounds` |
| 전투 이벤트 | Level·Milestone·시간 연장의 연출과 소리 | HQ 크기는 `SamplePresentation`, 소리는 `SampleSounds` |

모두 Unity 어셈블리의 C#이다. ID가 틀려도 흰색이나 일반 파괴음으로 넘어갈 뿐 오류가 나지 않는다(AUTHORING_PAIN AP6·AP10).

## 6. CA-002 전의 예상

위 명세를 현재 ContentData와 코드에 넣는다고 가정하고 적은 예상이다. **실제로 입력해 본 결과가 아니다.** 실제 결과는 [AUTHORING_PAIN](AUTHORING_PAIN.md)(CA-002)에 있다.

CA-002는 코드를 고치는 단계가 아니다. 레이저가 현재 구조에 들어가지 않아도 Skill 구조를 먼저 바꾸지 않는다. 그대로 넣어 보고 어디서 막히는지 기록한다. 구조는 그 결과를 보고 CA-003에서 자른다.

### 6.1 예상 표현 가능 여부

이 표를 실제 결과로 바꾸는 것이 CA-002의 핵심 산출물이다.

| 대표 콘텐츠 | 현재 형식 | 예상 이유 |
|---|---|---|
| Breaker | 가능 | 그대로 입력 |
| Enemy A (일반) | 가능 | 데이터만 |
| Enemy B (전기) | 가능 | 데이터만 (사망 효과 종류가 있음) |
| breaker-damage | 가능 | 데이터만 |
| breaker-radius | 가능 | 데이터만 |
| growth-supply | 가능 | 데이터만 |
| 관통 레이저 정의 | 불가 | Skill 형식이 원형 공격으로 고정 |
| laser-unlock | 불가 | Skill 획득 상태가 없음 |
| laser-width | 불가 | 공통 Stats에 폭이 없음 |
| Enemy 외형·소리 | 불가 | Presentation 데이터가 없음 (색만 코드에 있음) |
| 레이저 예고·발사 표현 | 불가 | 실행 경로를 전달할 상태·기록이 없음 |

### 6.2 예상 불편

반복 횟수·실수 가능성·확인 비용·자동화로 줄어드는 비용(CA-006의 기록 형식)은 CA-002에서 입력한 뒤 채운다.

| # | 예상 불편 | 원인 | CA-002에서 확인할 것 |
|---|---|---|---|
| P1 | 레이저에는 반경이 없는데 Skill 강화 효과 이름이 반경·주기·피해를 가정한다 | `SkillRadiusAdd`처럼 수치 이름이 효과 종류에 박혀 있다 | `laser-width`를 표현하려면 무엇을 고쳐야 하는가 |
| P2 | Skill을 해금할 방법이 없다 | Skill은 시작 구성으로만 받는다 | `laser-unlock`은 표현 불가로 기록 |
| P3 | 해금 효과에는 수치가 없는데 효과는 양수 값을 요구한다 | 효과 하나가 값 하나를 가진다고 가정한다 | 의미 없는 값을 적게 되는가 |
| P4 | Skill 종류를 데이터로 고를 수 없다 | Skill 동작이 한 가지로 고정 | 레이저는 표현 불가로 기록 (CA-003 대상) |
| P5 | 노드 이름·설명·아이콘을 적을 곳이 없다 | 노드 데이터가 규칙 칸만 가진다 | 표시 정보를 어디에 두고 싶어지는가 |
| P6 | 표현·소리 데이터의 ID 오타를 잡지 못한다 | 표현 데이터가 코드 안의 문자열 사전이다 | Enemy 하나를 추가할 때 코드를 몇 곳 고치는가 |
| P7 | 레이저의 예고·발사와 확정 경로를 화면이 읽을 수 없다 | Skill 실행 상태 중 공개된 것이 `TickCount`뿐이다 (5.4) | 레이저 표현을 표현 불가로 기록 |
| P8 | 무작위 시작점을 재현할 수 없다 | Core에 seed 난수가 없다 | CA-003에서 확인 |
| P9 | 해금 전 Skill의 강화 노드가 해금보다 먼저 팔릴 수 있다 | 선행과 해금의 관계를 검사하는 규칙이 없다 | Loader가 잡는가 |
| P10 | 모든 콘텐츠를 C# 코드로 쓴다 | `SampleContent`가 ContentData를 코드로 채운다 | 수치 하나를 바꾸고 플레이로 확인하기까지의 시간 |

P1을 푸는 두 방향 (선택은 CA-003·CA-004에서):

| 방향 | 데이터 예 | 장점 | 비용 |
|---|---|---|---|
| 수치마다 효과 종류 (현재) | Kind `SkillWidthAdd`, Target `laser` | 지금 코드를 그대로 쓴다 | Skill마다 효과 종류가 는다. 대상 Skill에 그 수치가 있는지 따로 검사해야 한다 |
| 효과 종류 + 수치 이름 | Kind `SkillStat`, Target `laser`, Stat `Width`, Op `Add` | 도구가 대상 Skill의 수치 목록을 보여 줄 수 있다 | Skill 종류마다 수치 이름 목록이 필요하다 |

Skill이 두 개인 지금은 첫 방향으로도 충분하다. CA-003의 "미래 Skill을 예상한 수십 개의 enum 금지"와 두 방향 모두 충돌하지 않는다. CA-004는 첫 방향을 택해 레이저 효과 셋(`LaserDamageAdd`, `LaserIntervalMultiply`, `LaserWidthAdd`)을 더했다.

예상 불편이 CONTENT_AUTHORING_PLAN 8절의 어느 단계에 해당하는지만 적어 둔다. 첫 Editor 기능은 CA-006에서 실제 비용으로 고른다.

| 단계 | 해당할 수 있는 불편 |
|---|---|
| B — Inspector 수준 보조 | P6(ID 선택·누락 표시), P9(검증 실행), P1·P3(대상 Skill의 수치만 선택), P10(대표 Battle 실행) |
| C — 관계·공간 편집 | P5의 배치 정보. 노드 수와 연결 수정이 병목이 될 때 |

## 7. 미정 항목

| 항목 | 정할 곳 | 관련 |
|---|---|---|
| 레이저의 주기·피해·폭·예고 시간 값 | Balance | 2.3 |
| 전투 영역 경계 R의 값과 화면 크기의 관계 (자리는 CA-003에서 레이저 정의로 정함) | S02·S09 | 2.3 |
| 경로를 전투 영역 경계에서 끝낼지 | 원작 확인 후 CA-003 | 2.3 |
| 레이저 판정: 중심 기준 또는 Enemy 반지름 + 반폭 (CA-003은 중심 기준 유지) | 미정 | 2.3 |
| Breaker의 중심 판정과 외곽 겹침 판정 | S04 | 2.2 |
| 레이저의 첫 예고 시점 (임시 0초) | 원작 확인 | 2.3 |
| 원작 레이저와 레이저 별의 관계 | 원작 확인 | 2.3 |
| 치명타 | 원작 확인 후 기획 | 2.2 |
| 같은 수치에 더하기·곱하기가 섞일 때의 순서 | F01 | 3.5 |
| P1의 방향 (CA-004에서 수치마다 효과 종류로 정함. Skill이 늘어 실제 반복이 생기면 다시 본다) | 결정됨 | 6절 |
| 재생 정책의 값 | CA-005, 리소스 연결 시 | 5.3 |

## 8. 다음 티켓과의 연결

| 티켓 | 이 문서에서 넘기는 것 |
|---|---|
| CA-002 | 1절의 대표 콘텐츠를 현재 ContentData로 입력한다. 코드 구조는 고치지 않는다. 6.1의 표와 6.2의 불편을 실제 기록으로 바꿔 AUTHORING_PAIN에 둔다 |
| CA-003 | 2.4의 "코드" 차이. Aim Point Snapshot과 예고 상태, 선분 판정, 전투 영역 경계, seed 난수. 레이저 표현용 상태는 예고 중인 발사까지만 (발사 기록은 CA-005) |
| CA-004 | 3.3의 해금 규칙, 3.4의 NodeId 요청, P1의 방향 |
| CA-005 | 5절 전체. CA-005에서 발사 기록·Breaker 맞힌 수를 더하고, 레이저 표현과 임시 소리로 재생 정책 시험 값을 연결했다 |
| CA-006 | 6절의 기록 형식을 채운 목록 |
