# AUTHORING PAIN — 현재 형식 저작 시험

작성일: 2026-09-25

상위 계획: [CONTENT_AUTHORING_PLAN](CONTENT_AUTHORING_PLAN.md) — 이 문서는 **CA-002 산출물**이다. CA-003~005에서 나온 불편도 여기에 더하고, CA-006이 이 목록으로 첫 Editor 기능을 고른다.

입력: [CONTENT_DEFINITION](CONTENT_DEFINITION.md)의 대표 콘텐츠 (CA-001)

구현 확인 기준: dev `f7ee150`. 이번 시험은 레포 코드를 바꾸지 않았다.

## 1. 방법

- 대표 콘텐츠를 현재 저작 경로로 표현하고, 실제 `ContentLoader`로 읽었다. 현재 저작 경로는 `SampleContent`의 C# 코드가 `ContentData`를 채우는 것 하나다(`GameHost`가 `SampleContent.Create()`를 부른다).
- 레포 밖의 시험 프로그램이 레포의 Core·Sample 원본을 그대로 컴파일했다(CoreSmoke와 같은 방식). 사례마다 `SampleContent.Create()`에서 한 가지만 바꿔 Load하고, 필요하면 전투를 조립·진행했다.
- Unity 쪽(표현·HUD)은 실행하지 않고 코드를 읽어 판정했다.
- C# 컴파일·Domain Reload 시간은 이 레포 `Logs/Editor.log`에서 읽었다. 에디터 시작 때의 기록만 있다(AP1).
- 이 문서는 **현재 상태의 측정 기록**이다. 흉내 사례(L2·W2·U2·U3)처럼 현재 한계를 재현한 결과는 보존할 계약이 아니다. 앞으로 보장할 동작은 CA-003·CA-004에서 기존 계약 체계에 새 계약으로 더한다. 그래서 시험 프로그램은 레포에 넣지 않았다.

### 판정 기준

CA-001 검토에서 합의한 기준이다.

| 판정 | 기준 |
|---|---|
| 가능 | 현재 데이터 의미를 그대로 써서 표현된다 |
| 가능하지만 불편 | 의미를 왜곡하지 않고 표현되지만 저작 비용이 든다. 문자열 ID 반복, C# 직접 수정, 중복 입력, 검증 부재 등 |
| 불가능 | 필드를 다른 의미로 쓰거나 더미 값을 넣어야만 흉내 낼 수 있다 |

현재 저작 경로는 C# 직접 수정뿐이다. 그래서 이 기준으로는 **"가능"에 드는 항목이 하나도 없다.** 표에는 C# 외의 추가 비용을 따로 적었다.

## 2. 결과 — 표현 가능 여부

| 대표 콘텐츠 | 판정 | 근거 | C# 외의 추가 비용 |
|---|---|---|---|
| Breaker | 가능하지만 불편 | 샘플에 CA-001과 같은 값으로 있다 | 없음 |
| Enemy A (일반) | 가능하지만 불편 | 샘플에 같은 값으로 있다 | Enemy 하나에 소스 5곳, 파일 2개 (AP5). 성장 공급은 생성 코드가 반복한다 (AP4) |
| Enemy B (전기) | 가능하지만 불편 | 샘플에 같은 값으로 있다 | Enemy A와 같다 |
| breaker-damage | 가능하지만 불편 | 샘플에 같은 값으로 있다 | 노드 ID가 문자열이고 선행 참조로 반복된다 (AP6) |
| breaker-radius | 가능하지만 불편 | 샘플에 같은 값으로 있다 | 같다 |
| growth-supply | 가능하지만 불편 | 샘플에 같은 값으로 있다. 연장·공급 없는 일반 Level도 데이터로 표현된다 (G1) | 샘플의 성장 Level은 생성 코드가 전부 같은 공급으로 만든다. "Milestone마다"를 구분하려면 생성 코드를 고쳐야 한다 (AP4) |
| 관통 레이저 정의 | **불가능** | L1 거부, L2는 흉내 (3.1) | — |
| laser-unlock | **불가능** | U1 거부, U2·U3는 흉내 (3.2) | — |
| laser-width | **불가능** | W1 거부, W2는 흉내 (3.3) | — |
| 노드 이름·설명·아이콘 | **불가능** | 칸이 없다 (3.5) | — |
| Enemy 색 | 가능하지만 불편 | `SamplePresentation`의 코드 사전 | Unity 어셈블리의 C#. 누락되면 흰색으로 그려지고 검사가 없다 |
| Enemy 외형·파괴 효과·사망 효과 보유 표시 | **불가능** | 모든 Enemy를 같은 원판으로 그린다 (3.4) | — |
| 소리 (모든 이벤트) | **불가능** | 코드에 오디오가 전혀 없다 (3.4) | — |
| 연쇄 번개 표현 | 가능하지만 불편 | 적중마다 선을 그린다 | 색·굵기·시간이 전역 값 한 벌이고, 효과 종류 분기는 `WorldView` 코드에 있다 |
| Breaker 범위 표시 | 가능하지만 불편 | 크기는 Skill의 실행 반경을 읽는다 | 색은 모든 Skill이 함께 쓰는 한 벌이다 |
| 레이저 예고·발사 표현 | **불가능** | Skill 표현은 모두 원이다 (3.4) | — |

CONTENT_DEFINITION 6.1의 예상과 다른 점:

- 예상에서 "가능"이던 여섯 항목은 C# 직접 수정 비용 때문에 모두 "가능하지만 불편"이 됐다.
- 예상에서 한 줄이던 "Enemy 외형·소리"를 나눴다. 색은 가능하지만 불편하고, 외형·효과·소리는 불가능하다.
- 연쇄 번개 표현과 Breaker 범위 표시를 새로 확인했다.

## 3. 불가능 항목이 막히는 자리

### 3.1 관통 레이저

`SkillData`의 칸은 `Id, Origin, Radius, Interval, Damage`뿐이다. 폭과 예고 시간을 적을 칸이 없고, `Origin`은 `OwnerAimPoint`만 받는다.

**L1 — 정직한 입력** (새 기준점 종류, 반경 없음):

```text
Skills[laser].Origin: 알 수 없는 기준점 종류 'BattleEdge'. 가능한 값: OwnerAimPoint.
Skills[laser]: 유한한 양수가 필요하다. (Parameter 'radius')
```

**L2 — 흉내** (기준점 `OwnerAimPoint`, `Radius`에 반폭 0.2): 로드에 성공한다. 전투를 조립하면 레이저가 **Aim Point 중심, 반경 0.2의 원형 공격**으로 0초에 실행된다. `WorldView`도 모든 Skill을 범위 원으로 그리므로 화면에는 작은 원이 하나 더 보인다. 데이터가 통과했을 뿐, 다른 Skill이 실행된다.

막히는 자리: `SkillData`·`PassiveSkillStats`의 칸, `ContentLoader.ParseOrigin`, `PassiveSkill.Tick`, `WorldView.SynchronizeSkills`.

### 3.2 laser-unlock

**U1 — 정직한 입력** (효과 종류 `SkillUnlock`):

```text
Upgrades[laser-unlock].Effects[0].Kind: 알 수 없는 업그레이드 효과 종류 'SkillUnlock'. 가능한 값: SkillDamageAdd, SkillRadiusAdd, SkillIntervalMultiply, EnemyHealthMultiply, GoldMultiply, HqExpMultiply, GrowthSupplyAdd.
```

**U2 — 흉내 A** (`StartingSkills`에 레이저 추가): 로드에 성공하고, 구매 없이 첫 전투부터 레이저를 가진다. 획득 조건이 사라진다.

**U3 — 흉내 B** (시작 구성에 없는 레이저에 `SkillDamageAdd 0.0001`을 거는 노드): 로드에 성공하고 구매할 수 있다. Gold 100에서 `breaker-damage`와 이 노드를 사면 60이 남는다. 다음 전투의 Skill은 `sample-aura`뿐이다. **Gold 30을 받고 아무것도 하지 않는 노드**가 Loader와 구매 규칙을 모두 통과한다.

막히는 자리: `UpgradeEffectKind`에 해금이 없다. `SessionAssembler.GiveStartingSkills`는 시작 구성만 준다. `UpgradeEffect`는 양수 값을 요구한다. 시작 구성에 없는 Skill을 대상으로 하는 효과를 검사하는 규칙이 없다.

### 3.3 laser-width

**W1 — 정직한 입력** (효과 종류 `SkillWidthAdd`):

```text
Upgrades[laser-width].Effects[0].Kind: 알 수 없는 업그레이드 효과 종류 'SkillWidthAdd'. 가능한 값: SkillDamageAdd, SkillRadiusAdd, SkillIntervalMultiply, EnemyHealthMultiply, GoldMultiply, HqExpMultiply, GrowthSupplyAdd.
```

**W2 — 흉내** (`SkillRadiusAdd`로 레이저의 "반경"을 늘림): 로드에 성공한다. L2의 흉내 레이저가 원형 공격이기 때문에 통하는 것이다. 실제 레이저에는 반경이 없다.

막히는 자리: 효과 종류 이름에 수치 이름이 들어 있다. `UpgradeModifiers.Apply`는 `PassiveSkillStats`의 세 칸만 안다.

### 3.4 표현

- `ContentData`의 칸은 `Session, Hq, Enemies, Spawn, StartSupply, Growth, Skills, StartingSkills, Upgrades`다. 표현 칸은 없다. Core가 표현을 모르는 것은 의도된 경계다.
- 표현 값은 전부 `SamplePresentation`(Unity 어셈블리)의 C#에 있다. Enemy 색 3개(`SampleContent`의 ID 상수가 키), 모든 Skill이 함께 쓰는 범위 색 한 벌, 번개 색·굵기·시간 한 벌, HQ 색과 크기다.
- `WorldView`는 모든 Enemy를 같은 원판 스프라이트로, 모든 Skill을 범위 원으로 그린다. 사망 효과 연출은 `ChainLightningDefinition` 분기 하나다.
- 오디오 코드는 없다(`Assets/BlackHole`에서 `Audio` 검색 결과 0건).

외형·파괴 효과·소리는 넣을 자리가 없어서, 데이터 추가가 아니라 새 코드가 필요하다(CA-005).

### 3.5 노드 표시 정보

`UpgradeData`의 칸은 `Id, Price, Requires, Effects`다. `Id`에 이름을 넣는 것은 저장 키의 의미를 바꾸는 왜곡이다. 구매 화면의 설명 문장은 `Hud.Describe`가 효과에서 만든다.

## 4. Loader 진단 확인

CA-002 완료 조건 "Loader의 진단으로 잘못된 ID와 수치를 찾을 수 있다"의 확인이다.

| 사례 | 진단 | 판단 |
|---|---|---|
| T1 시작 공급 Enemy ID 오타 | `StartSupply[0].Enemy: 정의되지 않은 Enemy ID 'sample-lihgt'.` | 찾는다 |
| T2 선행 노드 ID 오타 | `Upgrades[1].Requires: 정의되지 않은 선행 노드 ID 'breaker-damge'.` 와 `Upgrades[2].Requires: 선행 노드를 따라가면 시작 노드에 닿지 않는다(순환): 'growth-supply'.` | 찾지만 두 번째 진단은 **거짓 순환**이다. 실제로는 순환이 없고, 선행 사슬이 끊겼을 뿐이다 |
| T3 효과 대상 Skill ID 오타 | `Upgrades[breaker-damage].Effects[0].Target: 대상 Skill ID 'sample-aurra'가 정의되지 않았다.` | 찾는다 |
| T4 효과 종류 오타 | `...Effects[0].Kind: 알 수 없는 업그레이드 효과 종류 'SkillDamageAd'. 가능한 값: ...` | 찾고, 가능한 값까지 보여 준다 |
| T5 시작 Skill ID 오타 | `StartingSkills[0]: 정의되지 않은 Skill ID 'sample-aurra'.` | 찾는다 |
| T6 음수 반경 | `Skills[sample-aura]: 유한한 양수가 필요하다. (Parameter 'radius')` | 찾는다. 칸 이름이 경로가 아니라 메시지 끝에 있다 |
| T7 성장 임계값 역전 | `Growth: Levels[2]의 임계값 3는 앞 노드의 14보다 커야 한다. (Parameter 'levels')` | 찾는다 |
| T8a Enemy 복사 후 ID 그대로 | `Enemies[3]: Enemy ID 'sample-light'가 중복됐다.` | 찾는다 |
| T8b Enemy ID 변경, 참조는 그대로 | 진단 10개. 시작 공급 1개와 `Growth.Levels[0]`~`[8]`의 같은 오류 9개 | 찾지만 **소스에서 고칠 곳은 2곳**이다(시작 공급, 성장 생성 코드). 경로는 생성된 데이터의 위치이고 C# 소스 줄이 아니다. 표현 사전의 키도 옛 ID로 남지만 진단은 없다 |
| T9 음수 반경 + 선행 오타 + 공급 오타 | `Skills[sample-aura]: 유한한 양수가 필요하다. (Parameter 'radius')` 하나 | 나머지 두 오류는 보고되지 않는다. 정의 단계에 오류가 있으면 참조 단계를 검사하지 않는 현재 정책대로다 |
| T10 성장 Level 하나의 공급 오타 | `Growth.Levels[4].Supply[0].Enemy: 정의되지 않은 Enemy ID 'sample-lihgt'.` | 찾는다 |
| U1b 정직한 레이저 + 해금 노드 | 레이저 정의 오류 2개만 | T9와 같다. 해금 노드의 오류는 보고되지 않는다 |

정리:

- **찾는 것:** ID 오타(공급·효과 대상·시작 Skill·선행), 효과 종류 오타, 양수가 아닌 수치, 임계값 순서, 중복 ID. 모두 경로와 함께 보여 준다.
- **현재 진단 정책의 저작 비용:** 참조 규칙은 개별 정의가 모두 올바를 때, 노드 사이 규칙은 노드가 모두 올바를 때 검사한다. 이는 `ContentContracts`에 적힌 현재 계약이며 결함이 아니다. 이번 시험은 그 비용을 드러냈다. 오류가 두 단계에 걸쳐 있으면 고치고 확인하는 일을 두 번 해야 한다(T9, U1b → AP2). 정책을 바꿀지는 AP 우선순위로 정한다.
- **진단 품질 문제:** 선행 오타 하나가 뒤 노드에 거짓 "순환" 진단을 만든다(T2 → AP3). `ContentInvariants.ReachesRoot`가 끊긴 선행 사슬과 실제 순환을 구분하지 않고, 두 경우에 같은 메시지를 쓴다. 이는 현재 정책과 무관하다. CA-002에서는 고치지 않는다. Loader 개선 티켓이 생기면 이 최소 재현을 `ContentContracts`에 넣는다.
- **그 밖의 비용:** 생성 코드가 만든 데이터에서는 오류가 불어나고, 경로가 소스 줄과 맞지 않는다(T8b). 수치 오류는 칸 이름이 메시지 끝에만 있다(T6).
- **찾지 못하는 것:** 의미 왜곡(L2·W2·U2), 효과가 없는 구매(U3), 표현 데이터의 ID 누락(흰색으로 그려짐).
- **진단을 보는 자리:** `GameHost`가 Play를 시작할 때(`Awake`) Unity Console에 남긴다. 저작자가 Play 없이 볼 방법은 없다. 이번 시험처럼 Unity 밖에서 Core를 컴파일해 Load하면 볼 수 있다는 것은 확인했다.

## 5. Authoring Pain 목록

CA-006의 기록 형식이다: 작업 → 반복 횟수 → 실수 가능성 → 확인 비용 → 자동화하면 줄어드는 비용.

| # | 작업 | 반복 횟수 (이번 시험) | 실수 가능성 | 확인 비용 | 자동화하면 줄어드는 비용 |
|---|---|---|---|---|---|
| AP1 | 수치 하나를 바꾸고 확인 | 모든 수정 | 낮음. 형식 오류는 C# 컴파일러가 잡는다 | 관찰: 에디터 시작 로그에서 스크립트 컴파일 1.6~3.5초, Domain Reload 약 2.3초. 편집 중 재컴파일 로그는 확보하지 못했으므로 같은 비용이라고 확정하지 않는다. 이 프로젝트는 Play 진입 때 Domain Reload를 끈다. **프로그래머만 입력할 수 있다** | 데이터 파일이나 Inspector에서 고치면 컴파일이 없어지고 비프로그래머도 입력한다 |
| AP2 | 두 단계에 걸친 오류 고치기 (현재 진단 정책의 비용) | T9에서 확인 2회 | — | 확인마다 C# 수정 비용(AP1)과 Play 진입 | 모든 진단을 한 번에 보여 주는 검증 실행, 또는 정책 변경 |
| AP3 | 선행 오타의 원인 찾기 (진단 품질 문제) | T2에서 진단 2개 중 1개가 거짓 순환 | 원인이 아닌 노드를 고치게 된다 | 두 노드의 선행을 모두 따라가 읽어야 한다 | 끊긴 사슬과 순환을 구분한 진단 |
| AP4 | 성장 Level마다 다른 값 넣기 (일반 Level과 Milestone 구분 포함) | Level 9개가 생성 코드 한 곳에서 나온다 | 생성 코드를 고치면 모든 Level이 함께 바뀐다 | 진단 경로와 소스 줄이 다르다 (T8b에서 진단 10개, 수정 2곳) | Level 표를 행 단위로 편집 |
| AP5 | 수치 변형 Enemy 하나 추가 | 소스 5곳, 파일 2개: `SampleContent`의 상수·정의·시작 공급·성장 생성, `SamplePresentation`의 색 | 표현 누락. 검사가 없어 흰색으로 그려진다 | Play해서 눈으로 봐야 안다 | 표현 연결 누락을 검증에 포함, ID를 목록에서 선택 |
| AP6 | 노드 선행 연결 | `"breaker-damage"` 문자열 4회 (정의 1, 선행 3). Enemy·Skill ID는 상수지만 노드 ID는 문자열 그대로다 | 오타 | Loader가 잡는다 (AP3 주의) | 선행을 노드 목록에서 고른다 |
| AP7 | 불가능한 콘텐츠를 흉내 내기 | L2·W2·U2·U3 네 흉내가 모두 Loader를 통과했다 | 흉내가 게임에 들어간다. U3는 Gold 30을 받고 아무것도 하지 않는다 | 플레이로 발견해야 한다 | 해당 없음. 흉내를 쓰지 않고 구조를 CA-003·CA-004에서 바꾼다 |

## 6. 다음 티켓으로 넘기는 것

| 티켓 | 이번 시험에서 확인한 막힌 자리 |
|---|---|
| CA-003 | `SkillData` 칸, `ContentLoader.ParseOrigin`, `PassiveSkill.Tick`, `WorldView.SynchronizeSkills`. 흉내 레이저는 원형 공격으로 실행된다 (L2) |
| CA-004 | `UpgradeEffectKind`에 해금이 없다. `GiveStartingSkills`는 시작 구성만 준다. 효과는 양수 값을 요구한다. 효과 없는 구매를 검사하지 않는다 (U3). 효과 이름에 수치 이름이 들어 있다 (W1·W2) |
| CA-005 | 표현 데이터를 둘 자리가 없고 오디오가 없다. Skill 표현 색은 한 벌이고 번개 표현은 전역 값이다 |
| CA-006 | 5절의 AP1~AP7 |

측정 기록과 계약은 역할이 다르다.

| 자리 | 역할 |
|---|---|
| 이 문서 | CA-002 시점의 현재 구조를 측정한 기록 |
| 기존 계약 63개 | 유지되어야 할 기존 동작 |
| CA-003·CA-004의 새 계약 | "불가능" 항목을 하나씩 "가능"으로 바꾸며, 새로 보장할 동작만 더한다 |
| Loader 진단 개선을 정할 때 | 그때 `ContentContracts`에 진단 회귀 계약을 더한다 (예: T2의 거짓 순환) |

## 7. CA-003 뒤 재측정

같은 시험 프로그램을 CA-003 코드에 다시 돌렸다. 입력은 `SkillData.Origin` 대신 `Kind`를 쓰도록만 바꿨다. 2~6절은 CA-002 시점의 기록으로 그대로 둔다.

| 항목 | CA-002 | CA-003 뒤 | 근거 |
|---|---|---|---|
| 관통 레이저 정의 | 불가능 | **가능하지만 불편** | L1: 종류 `PiercingLaser`로 로드되고, 시작 구성에 넣으면 `PiercingLaserSkill`로 실행되어 0초에 예고 1발을 만든다. 저작은 여전히 C#이다(AP1). 종류에 맞지 않는 칸은 검사하지 않는다(AP8) |
| laser-unlock | 불가능 | 불가능 (CA-004) | U1은 그대로 알 수 없는 효과 종류다. U2(시작 구성에 넣기)는 여전히 통과하며, 이제 진짜 레이저가 구매 없이 생긴다. U3(더미 수치 효과)는 이제 거부된다 |
| laser-width | 불가능 | 불가능 (CA-004) | W1은 그대로다. W2(`SkillRadiusAdd` 흉내)는 이제 거부된다 |
| 레이저 예고·발사 표현 | 불가능 | 불가능 (CA-005) | 표현은 아직 없다. 예고 중인 발사는 읽기 전용 `PiercingLaserSkill.PendingShots`(시작점·끝점·남은 예고 시간)로 읽을 수 있다. 발사한 순간 목록에서 빠지므로 발사 표현에 필요한 기록은 아직 없다 |
| Loader 진단 T1~T10 | — | 같음 | 출력이 CA-002와 같다 |

U3·W2가 받는 진단:

```text
Upgrades[laser-unlock].Effects[0]: SkillDamageAdd는 Breaker 종류의 Skill만 대상으로 한다. 'laser'는 PiercingLaserDefinition이다. (Parameter 'skill')
Upgrades[laser-width].Effects[0]: SkillRadiusAdd는 Breaker 종류의 Skill만 대상으로 한다. 'laser'는 PiercingLaserDefinition이다. (Parameter 'skill')
```

L2(종류 `Breaker`에 반경 0.2)는 여전히 로드된다. 이제는 종류가 데이터에 드러나므로 레이저를 흉내 낸 것이 아니라 "이름이 laser인 Breaker"다.

### 추가된 불편과 관찰

| # | 작업 | 반복 횟수 | 실수 가능성 | 확인 비용 | 자동화하면 줄어드는 비용 |
|---|---|---|---|---|---|
| AP8 | Skill 종류에 맞는 칸만 채우기 | `SkillData` 칸 8개 중 Breaker는 5개, 레이저는 7개를 읽는다 | Breaker에 `Width`를 적거나 레이저에 `Radius`를 적어도 진단 없이 무시된다 (코드 읽기로 확인) | 플레이로 발견해야 한다 | 종류를 고르면 그 종류의 칸만 보이는 편집 |

AP1 보강 관찰(CA-003): Unity Editor가 열린 채로 편집 뒤 재컴파일이 한 번 기록됐다. 여러 파일을 한꺼번에 바꾼 뒤였고, Core·Sample·Tests·Unity 네 어셈블리가 다시 컴파일됐다. `Tundra build success (3.32 seconds)`, `Domain Reload Profiling: 3942ms`. 한 번의 관찰이므로 대표값으로 쓰지 않는다.

## 8. CA-004 뒤 재측정

같은 시험 프로그램에 CA-004의 정직한 입력을 더해 다시 돌렸다. 해금은 값 없는 `SkillUnlock`이고, 굵기는 `LaserWidthAdd`에 선행 `laser-unlock`이다. 노드는 ID로 샀다. 2~7절은 그대로 둔다.

| 항목 | CA-003 뒤 | CA-004 뒤 | 근거 |
|---|---|---|---|
| laser-unlock | 불가능 | **가능하지만 불편** | U1: 로드된다. `breaker-damage`와 `laser-unlock`을 ID로 사면 다음 전투의 Skill이 `sample-aura`, `laser`(주기 1.5, 피해 5, 굵기 0.4)가 된다. 저작은 여전히 C#이다 |
| laser-width | 불가능 | **가능하지만 불편** | W1: 로드된다. 세 노드를 사면 다음 전투의 레이저 굵기가 0.4에서 0.6이 된다 |
| 레이저 예고·발사 표현 | 불가능 | 불가능 (CA-005) | 바뀌지 않았다 |
| Loader 진단 T1~T10 | — | 같음 | T4의 "가능한 값" 목록에 새 효과 종류 넷이 붙은 것 말고는 같다 |

흉내와 잘못된 입력이 받는 진단:

```text
U1b  Upgrades[laser-unlock].Effects[0]: SkillUnlock는 값을 받지 않는다. 값을 비워 둬야 한다. (Parameter 'value')
W1b  Upgrades[laser-width].Effects[0]: 시작 구성에 없는 Skill 'laser'를 바꾼다. 선행을 따라가면 이 Skill의 해금 노드에 닿아야 한다.
U3   Upgrades[laser-unlock].Effects[0]: SkillDamageAdd는 Breaker 종류의 Skill만 대상으로 한다. 'laser'는 PiercingLaserDefinition이다. (Parameter 'skill')
W2   Upgrades[laser-width].Effects[0]: SkillRadiusAdd는 Breaker 종류의 Skill만 대상으로 한다. 'laser'는 PiercingLaserDefinition이다. (Parameter 'skill')
```

U1b는 CA-002의 입력(해금에 값 1)을 그대로 넣은 것이다. W1b는 굵기 노드의 선행을 해금 노드가 아닌 `breaker-damage`로 둔 것이다. U2(시작 구성에 레이저를 넣기)는 여전히 통과한다. 이는 "처음부터 레이저를 가진다"는 올바른 콘텐츠이고, 해금의 흉내로 쓸 이유가 없어졌다.

샘플 트리(`SampleContent`)에는 해금·레이저 강화 노드를 넣지 않았다. 레이저 표현이 없어, 사면 보이지 않는 공격이 생기기 때문이다. 표현이 생기는 CA-005에서 넣는다.

### 추가된 불편

| # | 작업 | 반복 횟수 | 실수 가능성 | 확인 비용 | 자동화하면 줄어드는 비용 |
|---|---|---|---|---|---|
| AP9 | 값이 없는 효과(해금) 쓰기 | 해금 노드마다 | `UpgradeEffectData.Value`가 float라 "값 없음"을 0으로 적는다. 샘플의 도우미 `Effect(kind, value, target)`도 값을 요구해 0을 직접 적게 된다. 1을 적으면 Loader가 잡는다 | Loader가 경로와 함께 보고한다 | 종류를 고르면 값 칸이 사라지는 편집 |

## 9. CA-005 뒤 재측정

CA-005는 레이저 표현과 소리를 붙였다. 표현은 Unity 쪽이라 시험 프로그램이 닿지 않는다. 그래서 2절처럼 코드를 읽어 판정했다. 사용자가 Unity Play에서 레이저 선과 소리가 보이고 들리는 것을 확인했다(2026-09-25). 재생 정책 값을 들어 보며 정한 것은 아니다(CONTENT_DEFINITION 5.3).

| 항목 | CA-004 뒤 | CA-005 뒤 | 근거 |
|---|---|---|---|
| 레이저 예고·발사 표현 | 불가능 | **가능하지만 불편** | `WorldView`가 `PendingShots`로 예고 선을, `World.LaserFires`로 발사 선을 그린다. 색·굵기·시간은 `SamplePresentation`의 C#이다 |
| 사건별 소리 배정 | 불가능 | **가능하지만 불편** | `BattleAudio`가 사건을 읽고 `SampleSounds`의 소리를 낸다. Enemy별 파괴음은 코드 사전이다 |
| 실제 오디오 리소스 연결 | — | **불가능** | 소리는 코드로 합성한 임시 소리뿐이다. 오디오 파일을 연결할 자리가 없다 |
| 재생 정책 값 (종류별 한 프레임 상한, 목소리 수, 세기) | — | 가능하지만 불편 | `SampleSounds`의 C# 값이다 |
| Enemy 외형·파괴 효과·사망 효과 보유 표시 | 불가능 | 불가능 | 바뀌지 않았다. 모든 Enemy는 같은 원판이다 |
| 샘플 트리의 레이저 노드 | 넣지 않음 | 넣음 | 레이저가 보이게 되어 `laser-unlock`(30, 선행 `breaker-damage`), `laser-width`(20, 선행 `laser-unlock`)를 넣었다 |

### 추가된 불편

| # | 작업 | 반복 횟수 | 실수 가능성 | 확인 비용 | 자동화하면 줄어드는 비용 |
|---|---|---|---|---|---|
| AP10 | Enemy 하나에 파괴음 붙이기 | 표현 사전 2곳: `SamplePresentation`(색), `SampleSounds`(파괴음). AP5의 5곳에 더해진다 | 키를 빠뜨려도 흰색·일반 파괴음으로 넘어간다. 검사가 없다 | Play해서 보고 들어야 안다 | 표현 연결 누락을 검증에 포함 |
| AP11 | 표현 코드 확인 | 표현을 고칠 때마다 | 계약은 Unity 어셈블리에 닿지 않는다. 컴파일과 Play로만 확인한다 | 이번 작업에서는 Editor가 다시 컴파일하지 않아, Unity가 만든 csproj 사본에 새 파일을 더해 Unity 밖에서 컴파일했다 | 표현이 읽는 기록은 Core 계약으로 확인하고, 화면·소리 자체는 기준 상황에서 보는 절차(S12)로 확인 |
