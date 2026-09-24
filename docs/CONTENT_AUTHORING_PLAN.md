# CONTENT AUTHORING PLAN — 대표 콘텐츠로 제작 흐름 검증

작성일: 2026-09-25  
대상 브랜치: `dev`

## 1. 목적

Core Gameplay Reference에서 확인한 시스템 경계를 바탕으로, 이제 **실제 콘텐츠를 추가하는 작업 흐름**을 검증한다.

이 단계의 목표는 범용 프레임워크를 먼저 만드는 것이 아니다.

> **대표 콘텐츠 몇 개를 끝까지 정의하고, 추가·수정 과정에서 반복해서 불편한 지점을 확인한 뒤 필요한 저작 도구를 만든다.**

기준 문서는 다음 네 개다.

- `GAME_RULES_MVP.md`: 현재 Battle의 최소 게임 법칙
- `SYSTEM_CATALOG.md`: 시스템 책임과 후속 작업 경계
- `REFERENCE_ANALYSIS.md`: 레퍼런스에서 확인한 사실과 설계 참고
- `BLACKHOLE_PERFORMANCE_DESIGN_PRINCIPLES.md`: 복잡도 상한과 성능 검증 원칙

현재 GAME RULES의 MVP 범위에는 Upgrade Tree, 상세 Enemy 콘텐츠, 두 번째 Skill 등이 포함되지 않는다.  
따라서 아래 작업은 Core MVP 규칙을 소급 변경하는 작업이 아니라 **후속 콘텐츠 제작 흐름을 검증하는 별도 단계**다.

---

## 2. 현재 코드에서 이미 있는 것

현재 Reference에는 다음 골격이 있다.

### 콘텐츠

`ContentData → ContentLoader → GameContent`

- Enemy 정의
- Passive Skill 정의
- 시작 Skill 구성
- Battle 시작 공급
- Growth 정의
- Upgrade Node 정의
- ID 참조 검증
- 잘못된 데이터의 진단

### Skill

현재 Skill은 한 가지 실행 형태만 검증했다.

```text
주기 도달
→ Owner Aim Point
→ 원형 범위
→ 범위 안 Enemy 전부 Damage
```

현재 구조가 잘못되었다는 뜻이 아니다.

**실행 방식이 다른 두 번째 Skill이 실제로 생기기 전까지 Skill 종류 계층을 미리 만들지 않았던 것**이 현재 Reference의 의도다.

### Upgrade

현재 Runtime에는 이미 다음 흐름이 있다.

```text
Node ID
→ Upgrade Definition 조회
→ 선행 조건 / 가격 확인
→ PlayerState에 구매 기록
→ 다음 Battle 조립 시 효과 적용
```

현재 노드 정의가 가진 정보:

- ID
- 가격
- 단일 선행 Node ID
- 효과 목록

현재 없는 정보:

- 플레이 화면의 좌표
- 연결선 표현
- 구역 / 카테고리
- 아이콘 / 이름 / 설명
- 여러 선행 조건의 AND / OR 정책
- 전용 그래프 편집 화면

즉 **구매 Runtime과 트리 Presentation/Authoring은 이미 분리할 수 있는 상태**다.

### Presentation

현재 `WorldView`, `Hud`, `SamplePresentation`은 Reference 확인용이다.

- 공격 범위
- 피격 표시
- 사망 / 흡수
- 번개
- HQ 성장
- 전투 HUD
- 단순 Upgrade 목록

을 확인할 수 있지만 제품 UI·아트·사운드 제작 구조는 아직 아니다.

---

## 3. 이번 단계에서 검증할 대표 콘텐츠

한 번에 콘텐츠 전체를 설계하지 않는다.

아래의 **작은 Vertical Slice**로 제작 흐름을 검증한다.

| 대상 | 대표 사례 | 확인하려는 것 |
|---|---|---|
| Skill A | 현재 기본 자동 공격 | 기존 수치 편집·표현 연결 |
| Skill B | 실행 방식이 Skill A와 실제로 다른 1종 | Skill 정의가 무엇을 공통으로 가져야 하는지 |
| Upgrade Node | Skill 강화 / Skill 해금 / 공급 강화 | Node와 실행 로직의 분리 |
| Enemy A | 일반 Enemy | 기본 콘텐츠 추가 비용 |
| Enemy B | 특수 Enemy | 사망 효과·VFX·SFX 연결 |
| UI | Battle HUD + Upgrade Tree의 최소 상태 | Runtime 상태와 표시 데이터 경계 |
| Audio | 공격·피격·사망·특수 효과 | 이벤트 단위와 중첩 규칙 |

Skill B의 실제 게임 기획은 이 문서에서 확정하지 않는다.

다만 구조 검증용 후보는 Skill A와 **수치만 다른 Skill이면 안 된다.**  
다음 중 적어도 하나가 달라야 한다.

- 발동 조건
- 기준점
- 대상 선택
- 적용 결과
- 실행 횟수 / 종료 조건

두 번째 Skill의 요구가 생긴 뒤 그 차이에서 공통 구조를 추출한다.

---

## 4. Skill 정의에서 먼저 답할 질문

새 Skill을 구현하기 전에 아래를 콘텐츠 명세로 먼저 쓴다.

| 항목 | 질문 |
|---|---|
| ID | 다른 데이터가 무엇으로 이 Skill을 참조하는가? |
| Trigger | 언제 실행되는가? |
| Origin | 어디를 기준으로 실행되는가? |
| Target | 누구를 선택하는가? |
| Limit | 한 번에 최대 몇 대상 / 몇 회까지 영향을 주는가? |
| Result | Damage, Spawn, 상태 변경 등 무엇을 요청하는가? |
| Runtime Stat | 어떤 수치가 Battle 시작 시 확정되는가? |
| Presentation | 어떤 게임 결과를 화면·소리로 보여 주는가? |
| End | 연쇄·지속 효과라면 무엇 때문에 반드시 끝나는가? |

Skill은 구매 여부를 판단하지 않는다.

```text
Upgrade / Loadout
→ Skill을 사용할 수 있게 함

Skill Runtime
→ 이미 사용할 수 있는 Skill을 규칙에 따라 실행
```

을 분리한다.

---

## 5. Upgrade Node 정의에서 먼저 답할 질문

Node는 "버튼에 실행 로직을 넣는 구조"가 아니다.

```text
UI
→ NodeId로 구매 요청

Purchase Rule
→ Node 정의 조회
→ 구매 가능 여부 판정
→ 구매 기록

Battle Assembly / Spawn / Supply
→ 구매 결과를 해당 시스템의 실행 값에 적용
```

Node가 최소한 설명해야 하는 것은 다음이다.

| 항목 | 의미 |
|---|---|
| ID | 진행 상태가 저장하는 안정적인 식별자 |
| Cost | 구매 비용 |
| Prerequisite | 구매 가능 조건 |
| Effect | 무엇을 어떻게 변경하는가 |
| Target | 어떤 Skill / Enemy / Supply 등을 대상으로 하는가 |

트리 화면용 정보는 구매 규칙과 분리한다.

예:

```text
UpgradeNodeDefinition
- Id
- Cost
- Prerequisite
- Effects

UpgradeNodePresentation
- DisplayName
- Description
- Icon
- Position
- Group
```

이 둘을 실제로 별도 타입/파일로 만들지는 대표 노드를 저작한 뒤 결정한다.

---

## 6. Enemy 콘텐츠에서 먼저 답할 질문

Enemy 한 종류를 추가할 때 아래 차이를 명시한다.

| 항목 | 내용 |
|---|---|
| ID | 콘텐츠 식별자 |
| Base Stats | HP, 이동, Size 등 |
| Behavior | 현재는 HQ Orbit |
| Reward | HQ EXP 등 |
| Death Effect | 없음 / 특수 효과 |
| Presentation | Sprite, Hit, Death, Absorb |
| Audio | Hit, Death, Special |
| Supply | 어떤 공급 정의에서 등장하는가 |

수치만 다른 Enemy와 **새 행동/새 사망 효과가 필요한 Enemy를 구분**한다.

수치 변형은 데이터 추가만으로 끝나야 한다.  
새 동작은 코드 확장 작업으로 분리한다.

---

## 7. Presentation / Audio 이벤트 경계

아트와 사운드는 Gameplay 결과를 다시 결정하지 않는다.

```text
Gameplay
→ Attack 발생
→ Damage
→ Death
→ Death Effect
→ Growth

Presentation / Audio
→ 위 결과를 소비해 보여 주고 들려줌
```

사운드 파일 목록보다 먼저 **재생 단위**를 결정한다.

예를 들어 자동 공격이 Enemy 20개를 동시에 맞힐 때:

- Skill 발동 소리는 Tick당 1회인가?
- 피격 소리는 대상마다 필요한가?
- 동일 프레임의 Death 소리는 몇 개까지 재생하는가?
- Chain Lightning은 적중마다 재생하는가, 효과 실행당 재생하는가?
- 동시 재생 상한을 넘으면 어떤 소리를 생략하는가?

이 값은 아직 확정하지 않는다.

먼저 이벤트 종류와 필요한 동시성 정책을 목록화하고, 실제 리소스를 연결하면서 결정한다.

---

## 8. 저작 도구를 만드는 순서

처음부터 Graph Editor를 만들지 않는다.

### 단계 A — 데이터 직접 저작

대표 Skill / Node / Enemy를 현재 데이터 형식으로 직접 추가한다.

확인:

- 어떤 필드를 반복 입력하는가?
- ID 오타를 얼마나 자주 내는가?
- 참조 관계를 텍스트로 보기 어려운가?
- 배치 정보를 코드에서 관리하기 어려운가?
- 어떤 수정에서 플레이 확인까지 시간이 오래 걸리는가?

### 단계 B — Inspector 수준 보조

단순 수치와 리소스 연결이 문제라면 Inspector 또는 간단한 Editor UI로 해결한다.

후보:

- ID 선택 드롭다운
- 누락 참조 표시
- 범위 / 비용 등 값 검증
- 콘텐츠 검증 실행 버튼
- 대표 Battle 실행 버튼

### 단계 C — 관계 / 공간 편집

Node 수와 연결 수정이 실제 병목이 되면 Graph UI를 만든다.

후보:

- Node 이동
- 연결선 편집
- Group / Zone 배치
- 잠금 상태 미리보기
- 잘못된 연결 즉시 표시

> **툴의 기능은 예상해서 넣지 않는다. 실제 저작에서 반복해서 발생한 비용을 제거한다.**

---

## 9. 첫 작업 티켓

### CA-001 — 대표 콘텐츠 명세

**목표**  
Skill A / Skill B / Enemy A / Enemy B / Upgrade Node 4종의 콘텐츠 명세를 작성한다.

**완료 조건**

- 각 Skill의 Trigger / Origin / Target / Result / Limit가 적혀 있다.
- 각 Node의 Target과 적용 시점이 적혀 있다.
- 새 코드가 필요한 차이와 데이터만 추가하면 되는 차이가 구분되어 있다.
- 확정 콘텐츠와 구조 검증용 후보를 구분한다.

### CA-002 — 현재 ContentData로 직접 저작

**목표**  
CA-001의 대표 콘텐츠를 현재 저작 형식으로 표현해 본다.

**완료 조건**

- 표현 가능한 항목과 불가능한 항목이 구분된다.
- Loader의 진단으로 잘못된 ID와 수치를 찾을 수 있다.
- 불편 사항을 `Authoring Pain` 목록으로 기록한다.

### CA-003 — 두 번째 Skill의 최소 절단

**목표**  
Skill B 때문에 실제로 필요한 차이만 Runtime에 추가한다.

**금지**

- 범용 Effect Engine
- 필요하지 않은 Skill 상속 계층
- 미래 Skill을 예상한 수십 개의 enum
- 새 Entity를 무제한 생성하는 실행 방식

**완료 조건**

- Skill A의 기존 계약이 유지된다.
- Skill B의 차이를 보여 주는 계약이 추가된다.
- 최대 실행량과 종료 조건을 설명할 수 있다.

### CA-004 — Node 콘텐츠 절단

**목표**  
대표 Node를 실제 Skill / Supply 변경에 연결한다.

**완료 조건**

- UI는 NodeId와 구매 결과만으로 동작할 수 있다.
- Skill 강화 Node가 Skill 실행 로직을 직접 알지 않는다.
- Skill 해금 Node를 도입한다면 획득 상태와 Skill 실행을 분리한다.
- 현재 단일 `Requires`로 부족한 실제 콘텐츠가 생기기 전에는 AND / OR 구조를 추가하지 않는다.

### CA-005 — Presentation / Audio 이벤트 표

**목표**  
대표 전투의 시각·사운드 피드백 이벤트를 정리한다.

**완료 조건**

- Attack / Hit / Death / Absorb / Death Effect / Growth 이벤트가 구분된다.
- 같은 프레임 다중 이벤트의 재생 정책을 결정할 질문이 기록된다.
- Presentation을 끄더라도 Gameplay 결과가 동일하다.

### CA-006 — Authoring Pain Review

**목표**  
CA-001~005 작업에서 실제 반복 비용을 수집하고 첫 Editor 기능을 선택한다.

**완료 조건**

각 불편에 대해 다음을 기록한다.

```text
작업
→ 반복 횟수
→ 실수 가능성
→ 확인 비용
→ 자동화하면 줄어드는 비용
```

그 뒤 가장 비용이 큰 한 가지를 첫 Editor 기능으로 만든다.

---

## 10. 성능 원칙 적용

새 콘텐츠는 기능 목록과 함께 아래 상한을 설명해야 한다.

| 질문 | 예 |
|---|---|
| Trigger | 무엇이 실행을 시작하는가? |
| Maximum | 한 번에 최대 몇 번 실행되는가? |
| Propagation | 결과가 같은 효과를 다시 일으키는가? |
| End | 무엇 때문에 반드시 끝나는가? |
| Entity Growth | 새 Entity 수가 플레이 시간에 따라 무한 증가하는가? |
| Presentation | 다중 결과를 그대로 모두 표현해야 하는가? |

특히:

- Spawn은 명시된 사건에서만 발생한다.
- 연쇄에는 종료 조건이 있다.
- 성장감을 Entity 수 증가에만 의존하지 않는다.
- Gameplay Lifetime과 Presentation Lifetime을 분리한다.
- 최적화 기술을 먼저 정하지 않고 기준 상황에서 측정한다.

---

## 11. 지금 하지 않는 것

대표 콘텐츠를 만들기 전에 다음을 선행 구현하지 않는다.

- 범용 Skill / Effect 프레임워크
- 범용 상태 머신
- 전체 Upgrade Tree 제품 UI
- Graph Editor 완성본
- 저장 포맷 확정
- 전체 Enemy 콘텐츠 목록
- 전체 밸런스 표
- Object Pool / ECS 도입을 전제로 한 구조 변경
- 미사용 확장 포인트
- 두 번째 콘텐츠가 요구하지 않은 다중 선행 조건 정책

---

## 12. 이번 단계의 종료 조건

이 단계는 “Editor가 완성되면” 끝나는 것이 아니다.

다음 흐름이 실제로 가능하면 첫 절단이 끝난다.

```text
대표 콘텐츠 정의
↓
현재 데이터 형식으로 입력
↓
검증
↓
Battle에서 실행
↓
UI / Presentation / Audio 연결
↓
Upgrade Node로 결과 변경
↓
다음 콘텐츠를 추가해 봄
↓
반복 비용 확인
↓
필요한 Editor 기능 1개 선정
```

최종 질문은 이것이다.

> **“팀원이 새 콘텐츠 하나를 추가하려면 무엇을 알아야 하고, 어디를 수정하며, 어떤 오류를 도구가 대신 잡아 주어야 하는가?”**
