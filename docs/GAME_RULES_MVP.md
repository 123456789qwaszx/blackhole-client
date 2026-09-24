# 블랙홀 키우기 — GAME RULES

작성일: 2026-09-24  
범위: **Core Gameplay / Playable MVP**

> 이 문서는 블랙홀 키우기의 플레이가 성립하기 위한 최소 게임 법칙을 정의한다.  
> 콘텐츠 목록, 업그레이드 설계, 적 종류 설계, 밸런스 수치는 별도 단계에서 다룬다.

---

## 1. 게임 한 줄 정의

플레이어는 **조준 위치를 움직여 자동 공격으로 천체를 파괴하고**,  
그 결과로 **블랙홀(HQ)을 성장시켜 현재 전투의 규모와 시간을 확장**한다.

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
Time End
    ↓
Battle End
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

HQ는 `EXP`와 `Level`을 가진다.

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
- HQ의 해당 Battle 성장 상태를 준비한다.

이전 Battle의 다음 실행 상태는 그대로 이어받지 않는다.

- Enemy
- Enemy HP
- Enemy Position
- Skill Timer
- Death / Absorb Presentation

전투 바깥에서 어떤 진행 상태를 유지할지는 MVP 이후 별도로 정의한다.

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

Player는 기본 Passive Skill 하나를 가진다.

Passive Skill은 일정 주기로 자동 발동한다.

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

MVP의 핵심 보상은 `HQ EXP`다.

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

MVP에서는 Death 시 추가 효과를 발생시키는 Enemy가 존재할 수 있다.

현재 기준 효과는 Chain Lightning이다.

```text
Special Enemy Death
    ↓
Chain Lightning
    ↓
다른 Enemy Damage
```

Death Effect로 죽은 Enemy도 일반적인 Death 규칙을 따른다.

단 다음 규칙을 둔다.

> **Death Effect는 Death Effect를 가진 Enemy에게 피해를 주지 않는다.**

따라서 같은 종류의 Death Effect가 다시 같은 종류의 Death Effect를 계속 발생시키지 않는다.

모든 연쇄 효과에는 반드시 끝나는 조건이 존재해야 한다.

---

## 11. Growth Milestone

모든 HQ Level Up이 Battle을 변화시키는 것은 아니다.

일반 Level과 **Growth Milestone**을 구분한다.

MVP에서 Growth Milestone에 도달하면:

```text
Growth Milestone
    ├─ Battle Time 증가
    └─ Enemy 추가 공급
```

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

구체적인 Threshold, 연장 시간, 공급량은 Balance 영역이므로 이 문서에서 정하지 않는다.

---

## 12. Enemy Supply

Enemy는 단순한 시간 경과만으로 계속 생성되지 않는다.

MVP의 Supply Trigger는 두 가지다.

```text
1. Battle Start
2. Growth Milestone
```

따라서:

```text
X  N초마다 자동 Spawn
X  시간이 길어질수록 무제한 Spawn

O  Battle Start Supply
O  Growth Milestone Supply
```

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
5. HQ EXP / Level 반영
6. Growth Milestone
7. Enemy Supply
8. Battle End 판정
```

이 순서가 의미하는 것은 다음과 같다.

- 해당 Step에서 발생한 Death는 같은 Step의 성장에 반영된다.
- Death Effect가 만든 추가 Death도 같은 성장 판정에 반영된다.
- 해당 Step의 성장으로 시간이 연장되면 그 결과를 반영한 뒤 Battle End를 판정한다.
- Growth Milestone으로 공급된 Enemy는 공급된 이후의 Gameplay에 참가한다.

---

## 14. Battle Time과 End

Battle에는 제한 시간이 있다.

Battle 진행에 따라 시간이 감소한다.

Growth Milestone은 현재 Battle의 시간을 연장할 수 있다.

Gameplay Step의 결과를 모두 처리한 뒤 종료 여부를 판정한다.

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

새 Battle에서는 전투 실행 상태를 새로 만든다.

MVP에서는 **전투를 정상 종료한 뒤 다시 시작할 수 있으면 된다.**

---

## 16. MVP에서 다루지 않는 것

다음은 현재 GAME RULES의 범위 밖이다.

- Upgrade Tree
- Upgrade Effect
- Gold Economy
- Enemy 종류별 상세 설계
- Enemy 콘텐츠 목록
- Balance 수치
- 장기 Progression
- Permanent Save
- Character 전투
- HQ HP / Defeat
- Multiplayer
- Network
- 두 번째 이후 Passive Skill
- 두 번째 이후 Death Effect

핵심 Battle Loop가 검증된 이후 별도 문서에서 정의한다.

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

→ Growth Milestone에서 시간이 늘어난다.

→ Growth Milestone에서 Enemy가 추가된다.

→ 플레이가 잘 될수록 Battle 규모가 커지는 것을 체감한다.

→ 최종적으로 시간이 끝난다.

→ Battle이 종료된다.

→ 새 Battle을 다시 시작할 수 있다.
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
7. **HQ는 Level이 오르며 특정 Level은 Growth Milestone이 된다.**
8. **Growth Milestone은 현재 Battle의 시간과 Enemy 공급을 확장한다.**
9. **Enemy는 시간 경과만으로 무한히 Spawn되지 않는다.**
10. **Death Effect에는 반드시 끝나는 규칙이 있다.**
11. **한 Step의 결과와 Growth를 반영한 뒤 Battle End를 판정한다.**
12. **Battle Cleanup은 Enemy Kill이 아니다.**
13. **새 Battle은 새로운 전투 실행 상태로 시작한다.**

---

## 19. 한 문장 기준

> **블랙홀 키우기는 조준 위치를 움직여 자동 공격으로 적을 파괴하고, 그 결과 HQ를 성장시켜 제한 시간과 적 공급을 확장하면서 한 Battle 안에서 더 큰 전투를 만들어 가는 게임이다.**
