# 블랙홀 키우기 — GAME RULES

작성일: 2026-09-24  
범위: **Core Gameplay / Playable MVP**
갱신: 2026-09-28 — `dev` `adb10cf` 기준 + 성장도 규칙(`feature/블랙홀성장도`, BATTLE_COMPOSITION_PLAN 8절)

> 이 문서는 블랙홀 키우기의 플레이가 성립하기 위한 최소 게임 법칙을 정의한다. 규칙이 충돌하면 이 문서가 기준이다.  
> 판 구성·보상의 세부는 [BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md), 스킬·사망 효과·노드 트리는 [SKILL_TREE_PLAN](SKILL_TREE_PLAN.md), 복잡도 제한은 [BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES](BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES.md)에서 다룬다. 밸런스 수치는 정하지 않는다.

---

## 1. 게임 한 줄 정의

플레이어는 **조준 위치를 움직여 자동 공격으로 천체를 파괴하고**,  
그 결과로 **판을 넘어 블랙홀(HQ)을 성장시키고, 성장 노드로 전투를 확장하며 이정표에 도달**한다.

---

## 2. Core Loop

```text
Battle Start
    ↓
Enemy 배치
    ↓
Aim Point 이동
    ↓
Passive Attack
    ↓
Damage / Death
    ↓
HQ EXP 획득
    ↓
HQ Growth
    ↓
Battle 확장
    ↓
Time End / Milestone
    ↓
Battle End
    ↓
Gold 결산·성장도 반영 → 업그레이드 → 다음 Battle
```

MVP는 이 한 사이클이 처음부터 끝까지 실제 플레이로 이어지는 것을 목표로 한다.

---

## 3. HQ — 게임 공간과 성장의 중심

### 3.1 Root Space

HQ는 블랙홀이며 Gameplay 공간의 논리적 Root다.

```text
HQ = (0, 0)
```

HQ를 기준으로 하는 공간 규칙은 모두 이 좌표계를 사용한다.

MVP에서는 다음이 HQ를 기준으로 한다.

- Enemy 초기 배치
- Enemy 공전
- 사망 후 흡수 연출의 도착점

각 시스템이 별도의 중심 좌표를 만들지 않는다.

### 3.2 HQ Growth

HQ의 성장은 두 가지다.

| | 성장도 (Growth Stage) | Level |
|---|---|---|
| 수명 | 판 밖 진행 상태. 판을 넘어 이어지고 줄지 않는다 | 판 하나. **매 판 Level 0, EXP 0에서 시작한다** |
| 오르는 때 | **결산 때 한 번**: 이번 판이 이번 성장도의 목표 Level에 닿았으면 +1. 한 판에 +1까지 | 판 중 EXP가 임계값을 넘을 때 |
| 정하는 것 | 이번 판의 Level EXP 표와 목표 Level, 적의 색 비율, 다음 종류의 기본 출현 비율, 이정표 | Level업마다 성장 효과(11절) |

- 성장도마다 Level EXP 표가 따로 있다. 새 진행은 성장도 0이다.
- 목표 Level에 못 닿은 판의 EXP·Level은 버린다. 판을 넘어 남는 것은 성장도·Gold·산 노드다.
- 앱 종료 후 영구 저장은 아직 없다.

```text
Enemy Death
    ↓
HQ EXP 증가
    ↓
Threshold 도달
    ↓
HQ Level 증가
```

한 번의 EXP 획득으로 여러 Threshold를 넘으면 여러 Level이 오를 수 있다.

HQ Level과 Player 공격력은 자동으로 연결되지 않는다.

MVP에서는 별도의 Player Level을 사용하지 않는다.

---

## 4. Battle Start

Battle이 시작되면 새로운 전투 실행 상태를 만든다.

전투 시작 시:

- Enemy를 배치한다.
- Battle Time을 시작한다.
- Passive Skill의 실행 상태를 시작한다.
- HQ를 Level 0, EXP 0으로 만든다. Level EXP 표와 목표 Level은 진행 상태의 성장도로 고른다.
- 성장도로 종류별 색 비율과 다음 종류의 기본 출현 비율을 정한다. 판 중에는 바뀌지 않는다.
- 산 노드로 종류 변환·특수 종류 생성 확률과 질량·황금·공급 수치를 한 번 계산한다.

이전 Battle의 다음 실행 상태는 그대로 이어받지 않는다.

- Enemy
- Enemy HP
- Enemy Position
- Skill Timer
- Death / Absorb Presentation

Gold·산 노드·성장도는 전투 사이에 유지한다. 이전 판의 EXP·Level, 늘어난 제한 시간, 처치 버프는 이어받지 않는다.

---

## 5. Player Input

MVP에서 전투 중 플레이어가 직접 조작하는 핵심 입력은 **Aim Point 이동**이다.

현재 Aim Point는 마우스 포인터 위치다.

```text
Mouse Position
    ↓
Aim Point
```

플레이어가 공격 버튼을 반복 입력하지 않는다.

입력은 **공격 위치**를 결정하고, 공격 시점은 Passive Skill의 규칙이 결정한다.

---

## 6. Passive Attack

Player의 공격은 모두 Passive Skill이며 일정 주기로 자동 발동한다. 지금 스킬은 둘이다.

- **Breaker** — 기본 공격. 아래 규칙을 따른다.
- **관통 레이저** — 경계 원 위의 무작위 지점에서 Aim Point를 향해 예고한 뒤, 그 직선 위의 Enemy 전부를 관통한다. 예고를 시작할 때 방향이 고정된다. 세부는 SKILL_TREE_PLAN 3.2.

Breaker:

```text
Attack Interval 도달
    ↓
현재 Aim Point
    ↓
Attack Range 판정
    ↓
범위 안 Enemy 전부 Damage
```

규칙:

- 범위 안의 Enemy는 모두 피해를 받는다.
- 대상이 없어도 해당 Attack Tick은 소비된다.
- 빈 Tick을 저장하거나 다음 공격으로 이월하지 않는다.
- MVP의 첫 Attack Tick은 Battle 시작 시점에 발생한다.
- 화면에 표시되는 공격 범위와 실제 판정 범위는 같은 값을 사용한다.
- 치명타는 Breaker의 수치다. 모든 스킬의 공통 수치가 아니다.

---

## 7. Enemy의 MVP 동작

MVP의 Enemy는 HQ 주변에 생성된다.

현재 기본 행동은 HQ `(0,0)`을 중심으로 공전하는 것이다.

```text
Spawn around HQ
    ↓
Orbit around HQ
```

Enemy는 현재 Player를 추적하지 않는다.

각 Enemy는 최소한 다음 실행 상태를 독립적으로 가진다.

- HP
- Position
- Alive / Dead

MVP에서는 추가 행동 상태나 행동 전환을 만들지 않는다.

---

## 8. Damage와 Death

Enemy의 HP가 `0 이하`가 되는 순간 Death가 확정된다.

```text
Damage
    ↓
HP <= 0
    ↓
Death
```

규칙:

- 하나의 Enemy는 한 번만 죽는다.
- 죽은 Enemy는 즉시 Gameplay 대상에서 제외된다.
- 죽은 Enemy는 이동하지 않는다.
- 죽은 Enemy는 다시 공격 대상이 되지 않는다.
- 죽은 Enemy는 다시 보상을 만들지 않는다.

사망 후 파괴·흡수 연출이 남아 있더라도 Gameplay에서는 이미 죽은 상태다.

```text
Gameplay Lifetime
≠
Presentation Lifetime
```

---

## 9. Death Result

Enemy Death가 확정되는 순간 그 사망의 게임 결과도 확정한다.

사망 순간 그 적의 `Gold`는 판의 처치 Gold 합계에, 색 등급의 `EXP`는 HQ에 적립한다. EXP에는 질량·황금 배율을 곱하지 않는다. 진행 상태에는 결산 때 한 번 반영한다: Gold를 더하고, 이 판이 목표 Level에 닿았으면 성장도를 1 올린다. 판의 EXP는 버린다.

```text
Enemy Death
    ↓
HQ EXP
```

흡수 연출이 끝날 때까지 기다리지 않는다.

Battle 종료 때문에 살아 있는 Enemy를 제거하는 것은 Death가 아니다.

```text
Battle Cleanup
≠
Enemy Kill
```

따라서 Cleanup으로 제거된 Enemy는 Death Result를 만들지 않는다.

---

## 10. Death Effect

Death 시 추가 효과를 발생시키는 Enemy가 있다. 효과는 Enemy 종류에 붙는 특성이다.

| 효과 | 결과 |
|---|---|
| Chain Lightning | 가까운 Enemy로 옮겨 가며 피해. 최대 횟수가 있고 같은 대상을 다시 맞히지 않는다 |
| Explosion | 죽은 자리 반경 안의 Enemy 전부에게 한 번 피해 |
| 공격 주기 감소 (처치 버프) | 일정 시간 Breaker의 공격 주기가 줄어든다 |
| 확정 치명타 (처치 버프) | 일정 시간 Breaker의 공격이 모두 치명타다 |

```text
Special Enemy Death
    ↓
Death Effect (Step 4)
    ├─ 다른 Enemy Damage
    └─ 처치 버프 → 다음 Step의 공격부터
```

Death Effect로 죽은 Enemy도 일반적인 Death 규칙을 따른다.

단 다음 규칙을 둔다.

> **Death Effect는 Death Effect를 가진 Enemy에게 피해를 주지 않는다.**

따라서 같은 종류의 Death Effect가 다시 같은 종류의 Death Effect를 계속 발생시키지 않는다.

처치 버프는 피해를 만들지 않는다. 판 안의 모든 참가자의 Breaker가 받고, 다시 받아도 곱으로 쌓이지 않는다.

모든 연쇄 효과에는 반드시 끝나는 조건이 존재해야 한다.

---

## 11. Growth Effect

HQ Level Up은 **Growth Effect**를 통해 현재 Battle을 확장한다.

Growth Effect는 Level마다 다른 것이 아니라, 전투 사이에 산 **성장 노드**가 정하는 한 벌의 값이다.
성장 노드를 사기 전에는 Level만 오르고 시간·공급은 늘지 않는다. 이정표 도달에 따른 종료는 성장 노드 구매와 무관하다.
성장 노드를 산 뒤에는 **Level Up마다** 같은 Growth Effect가 온다. 단, 이정표를 넘은 Step은 그 Step 전체의 성장 효과를 건너뛴다.

```text
HQ Level Up (성장 노드를 산 뒤)
    ├─ 현재 Battle의 Time 증가
    └─ Enemy 추가 공급
```

늘어난 Time은 그 Battle에만 있다. 다음 Battle은 언제나 기본 제한 시간에서 시작한다.

이 규칙으로 플레이어의 성공이 현재 전투를 확장한다.

```text
Enemy Kill
    ↓
HQ EXP
    ↓
HQ Growth
    ↓
Time + Enemy 증가
    ↓
더 큰 Battle
```

구체적인 Threshold, 연장 시간, 공급량은 Balance 영역이므로 이 문서에서 정하지 않는다([BATTLE_COMPOSITION_PLAN](BATTLE_COMPOSITION_PLAN.md) 4절의 샘플).

### 이정표(Milestone)

- 이정표는 정해진 성장도(예: 10)와 고정 보상 Gold다.
- 이정표 바로 앞 성장도의 판에서 목표 Level에 닿으면, Step 5에서 남은 시간과 관계없이 판을 끝낸다. Step 6·7과 시간 연장은 하지 않는다. 남은 적은 모두 빨려 든다(정리, 처치 아님).
- 결산은 그 판의 처치 Gold **대신** 이정표 고정 보상을 주고, 성장도를 +1 해 이정표 성장도가 된다.
- 성장도는 줄지 않으므로 지난 이정표는 다시 지급하지 않는다. 이정표는 노드를 자동 구매하지 않는다.
- 이정표 성장도부터 다음 종류가 기본 비율로 나온다(예: 성장도 10부터 행성).

---

## 12. Enemy Supply

Enemy는 단순한 시간 경과만으로 계속 생성되지 않는다.

MVP의 Supply Trigger는 두 가지다.

```text
1. Battle Start
2. HQ Level Up (Growth Effect)
```

따라서:

```text
X  N초마다 자동 Spawn
X  시간이 길어질수록 무제한 Spawn

O  Battle Start Supply
O  Growth Effect Supply (Level Up마다)
```

생성 한 마리마다 전체 상한을 먼저 검사하고, 통과하면 **종류 변환 사슬 → 특수 종류 선택 → 색 → 황금 → 위치·수치 적용** 순서로 처리한다. 변환과 특수 선택은 수량 추가가 아니라 요청 한 마리의 종류 교체다. 걸러진 요청은 버리고 몫을 소비하지 않는다.

색은 성장도, 질량 노드는 HP·Gold 계수, 변환 비율은 성장도의 기본값 + 변환 노드, 특수 종류는 확률 노드가 각각 정한다. 종류 해금 수치·진행도 단계·적 풀은 없다.

파괴 뒤 같은 종류 생성은 다음 PLAN의 세 번째 공급 계기이며 아직 구현하지 않았다.

모든 Supply에는 명확한 Trigger가 존재한다.

한 번의 Trigger가 공급하는 수량은 유한해야 한다.

---

## 13. 한 Gameplay Step의 처리 순서

동일한 순간에 여러 사건이 발생하더라도 결과가 달라지지 않도록 MVP의 처리 순서를 고정한다.

```text
1. Enemy Action
2. Passive Attack
3. Damage / Death
4. Death Effect
5. HQ EXP / Level 반영 — 이정표 앞 성장도에서 목표 Level이면 여기서 종료
6. Growth Effect
7. Enemy Supply
8. Battle End 판정
```

이 순서가 의미하는 것은 다음과 같다.

- 해당 Step에서 발생한 Death는 같은 Step의 성장에 반영된다.
- Death Effect가 만든 추가 Death도 같은 성장 판정에 반영된다.
- 이정표에 닿으면 Step 6·7과 시간 연장을 건너뛰고 종료한다.
- 이정표에 닿지 않았으면 공급 후 시간 연장을 적용하고 시간 종료를 판정한다.
- Growth Effect로 공급된 Enemy는 공급된 이후의 Gameplay에 참가한다.

---

## 14. Battle Time과 End

Battle에는 제한 시간이 있다.

Battle 진행에 따라 시간이 감소한다.

Growth Effect는 현재 Battle의 시간을 연장할 수 있다. 늘어난 시간은 그 Battle에만 있다.

시간 종료는 Gameplay Step의 결과와 시간 연장을 반영한 뒤 판정한다. 이정표 종료는 Step 5 직후 우선하며, End battle 요청으로도 종료할 수 있다.

```text
Gameplay Step 완료
    ↓
Time 확인
    ↓
Time <= 0
    ↓
Battle End
```

Battle End 이후에는 새로운 공격, Death, Growth 등의 Gameplay 결과를 만들지 않는다.

남아 있는 Enemy는 보상 없이 정리한다.

---

## 15. Restart

새 Battle은 이전 Battle의 실행 상태를 되감아 재사용하는 것이 아니다.

```text
Battle End
    ↓
Cleanup
    ↓
New Battle
```

새 Battle에서는 전투 실행 상태를 새로 만들고(Level 0), 결산된 성장도·산 노드·Gold를 이어받는다.

MVP에서는 **전투를 정상 종료한 뒤 다시 시작할 수 있으면 된다.**

### 전투 사이 — 노드 구매

- 결산이 끝나면 업그레이드 화면으로 돌아온다. Gold로 노드를 산다.
- 노드는 전투 중에 살 수 없다. 진행 상태(Gold·산 노드·성장도)는 전투 밖에서만 바뀐다.
- 산 노드는 다음 Battle을 조립할 때 한 번 반영되고, 그 Battle 동안 바뀌지 않는다.
- 진행 상태는 방장의 것 하나다. 방장이 노드를 사고, 그 결과(스탯·처치 버프·결산)를 판 안의 모든 참가자가 함께 쓴다. 지금 참가자는 방장 한 명이다.
- 노드 그래프·합성 규칙은 SKILL_TREE_PLAN 4·5절.

---

## 16. MVP에서 다루지 않는 것

다음은 현재 GAME RULES의 범위 밖이다.

- Gold 가격·보상 밸런스의 상세 설계 (처치·이정표 결산 규칙은 포함)
- Balance 수치
- 블랙홀 EXP·산 노드 이외의 장기 Progression
- Permanent Save
- Character 전투
- HQ HP / Defeat
- Multiplayer (방장 한 명이 참가자인 경우만 다룬다)
- Network
- 세 번째 이후 Passive Skill
- 파괴 뒤 같은 종류 생성 (세 번째 Supply Trigger)
- 판 기본 시간을 늘리는 노드

노드 트리·업그레이드와 Enemy 종류는 규칙이 정해져 구현되어 있다(SKILL_TREE_PLAN, BATTLE_COMPOSITION_PLAN). 위 목록은 별도 문서에서 정의한다.

---

## 17. Playable MVP 완료 조건

다음 흐름이 실제 플레이로 모두 연결되면 Core Gameplay MVP가 성립한다.

```text
Battle Start

→ HQ 주변에 Enemy가 배치된다.

→ Player가 Mouse로 Aim Point를 움직인다.

→ 공격 범위가 Aim Point를 따라간다.

→ Passive Attack이 자동으로 발생한다.

→ 범위 안 Enemy가 Damage를 받는다.

→ HP가 0이 된 Enemy가 Death 처리된다.

→ 죽은 Enemy는 Gameplay에서 즉시 제외된다.

→ Death Presentation이 별도로 보인다.

→ HQ가 EXP를 얻는다.

→ HQ Level이 오른다.

→ 이정표에 닿지 않은 Step에서는 산 성장 노드에 따라 Level Up마다 시간이 늘어난다.

→ 같은 조건에서 성장 공급이 추가된다(전체 상한 적용).

→ 플레이가 잘 될수록 Battle 규모가 커지는 것을 체감한다.

→ 시간이 끝나거나 이정표에 닿는다.

→ Battle이 종료된다.

→ 처치 Gold 또는 이정표 보상을 한 번 결산하고, 목표 Level에 닿았으면 성장도가 1 오른다.

→ 성장도를 이어받고 Level 0에서 새 Battle을 시작할 수 있다.
```

---

## 18. 팀 공통 규칙

팀원이 Core Gameplay를 설명할 때는 아래 문장을 기준으로 한다.

1. **HQ는 Gameplay 공간의 Root `(0,0)`다.**
2. **Player는 Aim Point를 움직이고 공격은 Passive로 실행된다.**
3. **MVP의 Enemy는 HQ를 중심으로 공전한다.**
4. **HP가 0 이하가 되는 순간 Death와 그 결과가 확정된다.**
5. **Gameplay Death와 Presentation Lifetime은 분리된다.**
6. **Enemy Death는 HQ EXP를 만든다.**
7. **Level은 매 판 0에서 시작한다. 성장도는 결산 때만, 한 판에 최대 1 오르고 판을 넘어 이어진다. 성장도가 그 판의 Level 표와 색 비율을 정한다.**
8. **산 성장 노드는 Level Up마다 현재 Battle의 시간·공급을 확장한다. 이정표 도달 Step은 제외한다.**
9. **Enemy는 시간 경과만으로 무한히 Spawn되지 않는다.**
10. **Death Effect에는 반드시 끝나는 규칙이 있다.**
11. **이정표 앞 성장도에서 목표 Level에 닿으면 성장 효과 전에 종료하고 처치 Gold 대신 고정 보상을 결산한다. 그 외에는 성장 효과 뒤 시간 종료를 판정한다.**
12. **Battle Cleanup은 Enemy Kill이 아니다.**
13. **새 Battle은 새 실행 상태와 결산된 진행 상태로 시작한다.**

---

## 19. 한 문장 기준

> **블랙홀 키우기는 자동 공격으로 적을 파괴해 블랙홀을 판마다 이어 키우고, 노드로 전투의 시간·공급·종류를 바꾸며 이정표에 도달하는 게임이다.**
