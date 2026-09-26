# SKILL SYSTEM PLAN — 스킬 샌드박스를 참고해 이 레포의 전투에 스킬·사망 효과·처치 버프를 들이기

작성일: 2026-09-26

브랜치: `feature/스킬시스템` (`feature/업그레이드연결` `d1ce913`에서 분기: 적 + 노드 트리 + 업그레이드 + 화면 루프)

참고 레포: `blackhole-client_skillsystem` 브랜치 `skill-only-sandbox` `dae146f` (이하 "샌드박스")

기준 문서: [GAME_RULES_MVP](GAME_RULES_MVP.md) 6·8·10·13절 · [SYSTEM_CATALOG](SYSTEM_CATALOG.md) S02·S04·S05·S06·S09 · [CONTENT_DEFINITION](CONTENT_DEFINITION.md) 2절 · [SKILL_TREE_PLAN](SKILL_TREE_PLAN.md) 3절 · [REFERENCE_ANALYSIS](REFERENCE_ANALYSIS.md) 3절

| 표기 | 뜻 |
|---|---|
| 원작 | 원작 공개 자료(REFERENCE_ANALYSIS의 R 번호)와 사용자의 원작 플레이 관찰 |
| 문서 | 이 레포의 규칙 문서(GAME_RULES_MVP, CONTENT_DEFINITION, SKILL_TREE_PLAN) |
| 사용자 | 사용자의 요구와 결정 |
| 샌드박스 | 샌드박스가 정한 것. 원작·문서 근거가 없다(샌드박스 문서의 Hades·Diablo·LoL은 설계 참고일 뿐 원작 근거가 아니다) |
| 제안 | 이 PLAN이 정한 것. 사용자가 바꾸면 따른다 |
| 임시 | 확인 전까지 쓰는 값 |
| 미정 | 결정 필요 |

## 1. 목표

1. 샌드박스에서 확인한 **Breaker와 관통 레이저**를 이 레포의 판(`World.Step`의 2. Passive Attack 자리)에 넣는다. 스킬은 공전하는 실제 적을 공격한다.
2. **사망 효과**(연쇄 번개·폭발)와 **처치 버프**(공격 주기 감소·확정 치명타)를 적 종류의 특성으로 붙인다(4. Death Effect 자리).
3. 샌드박스 코드는 **복사하지 않는다.** 규칙과 흐름만 참고한다. 코드는 이 레포의 타입(`Point2`, `PlayerId`, `Enemy`, `Damage`, `World.DealDamage`, `BattleRandom`)과 콘텐츠 경로(에셋 → `ContentData` → `ContentLoader`)에 맞춰 다시 쓴다.

## 2. 범위

| 한다 | 하지 않는다 |
|---|---|
| Breaker, 관통 레이저 (Core) | 업그레이드 표 읽기(`breaker.*` 수치), 노드로 레이저 해금. 수치를 가져가는 시스템 연결은 모든 시스템이 끝난 뒤다 [사용자] |
| 조준점 입력: 마우스 → 참가자의 Aim Point | 처치 보상·결산 (`feature/처치보상`) |
| 사망 효과 2종, 처치 버프 2종 | 세 번째·네 번째 스킬. 명세가 아직 없다(SKILL_TREE_PLAN Q1) |
| 공격·효과 표시, 개발용 스킬 콘솔 | 최종 아트·사운드, 다인 보상·버프 귀속(F06) |
| 새로 보장하는 동작의 계약 | 샌드박스의 Step 쪼개기·프레임 예산(6절 D6) |

## 3. 두 레포 비교

### 3.1 구조

| 항목 | 이 레포 (`feature/업그레이드연결`) | 샌드박스 (`skill-only-sandbox`) |
|---|---|---|
| 목적 | 전투 루프(업그레이드 화면 ↔ 전투), 적·노드 트리·업그레이드 | 스킬 공격과 천체 사망 효과를 눈으로 확인 |
| 어셈블리 | `BlackHole.Core`(엔진 없음), `Unity`, `Sample`, `Authoring`, `Editor` | `BlackHole.Skills`(엔진 없음), `SkillTestPack`, `SkillSandbox`, `SkillTests` |
| 적 | `Core.Enemy`: 번호·HP·공전 위치·`LastDamageSource`. 피해는 `World.DealDamage`로 들어가고, `EnemyRoster`가 사망 1회·사망 기록·처치 수를 처리한다 | `TestPack.EnemyTarget`: 고정 위치와 HP. 스킬이 `target.Hit`을 바로 부르고, 사망은 `DeathEffects`가 `IsAlive`를 훑어 찾는다 |
| 적 정의 | `EnemyDefinition(Id, BaseStats, Behavior)`. `EnemyKind` 에셋 → `ContentLoader` | `EnemyDefinition(Id, Health, DeathEffect)`. `EnemyCatalogAsset` → `EnemyCatalog.Load`. **이름이 겹친다** |
| 스킬 | 없다. `e195b40`에서 지웠고 `World` 주석에 Step 자리만 남아 있다 | `BreakerRuntime`, `LaserRuntime`, `SkillCatalog` 검증 |
| 사망 효과·버프 | 없다. `World.HasPendingDeathProcessing`은 늘 false | `DeathEffects`(연쇄·폭발), `PlayerBuffs`(주기 감소·확정 치명타) |
| 조준 | 없다 | `SkillBattle.Aim`, 마우스 |
| 참가자 | `PlayerId` 구조체. 판은 `PlayerState` 목록을 받는다 | `int playerId`. 전투(`SkillBattle`) 하나에 플레이어 하나 |
| 시간 | `GameSession.Advance`: 프레임당 한 Step. 제한 시간에서 자른다 | 공격·예고·버프 만료 시각에서 Step을 나누고(최대 0.05초) 프레임당 4 Step까지 처리한다. 남은 시간은 다음 프레임으로 넘긴다 |
| 난수 | `BattleRandom` 하나(출현 배치) | battle seed와 `PlayerId`로 방향·치명타 스트림을 따로 만든다 |
| 수치 원본 | 에셋 → `ContentData` → `ContentLoader`(진단) | `SkillCatalogAsset`(스킬 + 플레이어 공통 전투 수치), `EnemyCatalogAsset` |
| 업그레이드 | `GameSession.UpgradesOf(PlayerId)`. 노드 수치 이름은 `breaker.damage`·`speed`·`radius`·`crit-chance` [임시] | 없다(`9253ee4`에서 뺐다) |
| 표현 | `EnemyView`(적 스프라이트). 공격 표시 없음 | `SkillEffectsView`(Breaker 원, 레이저 예고·발사선, 폭발·번개·적중 표시), IMGUI 콘솔 |

### 3.2 git 관계

- 샌드박스는 이 레포의 CA-005 무렵 사본을 새 저장소의 `Initial commit`(`53ed4b5`)으로 시작했다. 두 레포에 공통 커밋이 없고 파일 경로도 다르다(`Assets/BlackHole/SkillSystem/`). **병합이나 cherry-pick으로는 가져올 수 없다.**
- 사본이라 GUID가 겹친다. 샌드박스의 `Unity/GameHost.cs.meta`는 이 레포의 `Unity/GameHost.cs.meta`와 GUID가 같다. `SampleScene.unity.meta`, `Settings` 에셋도 같다. 폴더째 복사하면 Unity가 한쪽 GUID를 바꾸고 씬 참조가 깨진다. 그래서 새 파일에는 새 `.meta`를 쓰고, 샌드박스의 `.asset`·씬·`.meta`는 가져오지 않는다.
- 이 레포에도 예전 스킬 코드가 있었다(M3 `PassiveSkill`, CA-003 레이저). `e195b40`에서 지웠다. 샌드박스는 그 뒤 따로 다시 짠 것이다.

### 3.3 측정 (2026-09-26, .NET SDK 10.0.302)

- 이 레포 `d1ce913`: CoreSmoke **60 passed**.
- 샌드박스 `dae146f`: CoreSmoke **19 passed, 3 failed**. 샌드박스 인수인계 문서에는 "실행 통과 미확인"으로 적혀 있었고, 이번에 처음 실행했다.
  - `Crit.LaserChecksBuffOnFire`: 기대 14, 실제 10. 계약 데이터의 레이저 피해는 5인데, 기대값은 피해 3(SO의 값)으로 계산돼 있다.
  - `Crit.DirectionStreamIsIndependent`, `Visual.LaserFireMatchesTelegraph`: `Skills[0]`을 레이저로 형변환한다. 그런데 스킬을 꺼도 목록에 남기 때문에 `[0]`은 Breaker다.
  - 셋 다 계약 쪽 오류로 보인다. 계약은 이 레포 형식으로 새로 쓰므로(8절) 샌드박스에서 고치지 않는다.
- 샌드박스의 Unity EditMode와 Play는 확인하지 않았다.

## 4. 샌드박스 규칙을 출처별로 가르기

샌드박스의 규칙을 그대로 옮기지 않는다. 원작·문서에 근거가 있는 것과 샌드박스가 정한 것을 나눈다.

| # | 규칙 | 출처 | 이 PLAN |
|---|---|---|---|
| 1 | Breaker: 조준점 중심 원, 범위 안 전부, 주기 공격, 첫 Tick 0초, 빈 Tick도 소비 | 문서 (GAME_RULES 6) | 따른다. 첫 Tick은 판의 첫 Step이다(시작 직후 첫 진행, 예전 M3 구현과 같다) |
| 2 | 조준점이 없으면 빈 Tick | 문서 (S04의 샘플 선택) | 따른다 |
| 3 | 레이저: 경계 원 위 무작위 시작점, 예고 시작 때 조준점을 저장, 예고가 끝나면 관통 발사, 첫 예고 0초 | 원작 (사용자 관찰) + 문서 (CONTENT_DEFINITION 2.3 [임시]) | 따른다. 경계 반지름은 레이저 정의의 `BoundaryRadius`다. 노드가 바꾸는 수치가 아니다(CONTENT_DEFINITION) |
| 4 | 조준점이 경계 밖이면 예고를 만들지 않는다 | 샌드박스 | 따른다 [임시] |
| 5 | 사망 효과를 가진 적은 사망 효과의 피해를 받지 않는다 | 문서 (GAME_RULES 10), 원작 (초신성끼리 피해 없음, R7) | 따른다 |
| 6 | 연쇄 번개: 가까운 적부터, 다시 맞히지 않음, 한 번 이동 ≤ 반경, 최대 횟수 | 원작은 "주변에 연쇄 번개"뿐(R3). 세부는 S06의 샘플 선택 | 따른다 [임시] |
| 7 | 폭발: 반경 안의 일반 적 전부에게 1회 | 원작 초신성(폭발로 보임, 공식 미확인, R7·R8) | 따른다 [임시] |
| 8 | 처치 버프는 그 처치가 난 Step의 공격에 들지 않고 다음 Step부터 적용 | Step 순서(2 공격 → 4 사망 효과)에서 따라 나온다(SKILL_TREE_PLAN 3.3) | 따른다 |
| 9 | **치명타 확률·배율과 공격 간격 배율은 플레이어 공통 수치다** (`PlayerCombatStats`, Breaker·레이저 모두) | 샌드박스 | **따르지 않는다.** 문서는 "Skill은 개별이고 모든 Passive Skill 대상은 두지 않는다"(SKILL_TREE_PLAN 3.1)이다. 노드 수치도 `breaker.crit-chance`로 스킬별이다. 원작의 치명타는 Breaker의 강화 축이다(REFERENCE_ANALYSIS 3.1). 치명타는 Breaker의 수치로 둔다 |
| 10 | **처치 버프는 처치자의 모든 스킬에 적용** | 샌드박스 | **따르지 않는다.** 원작에서 달은 Breaker를 강화하고(R7·R9), 혜성은 커서 공격을 항상 치명타로 만든다(R8). 두 버프 모두 Breaker 대상이다 |
| 11 | **버프는 마지막으로 타격한 플레이어가 받는다** | 샌드박스 | **따르지 않는다.** 처치 귀속은 미정이다(`Enemy.LastDamageSource` 주석). 지금은 단일 참가자에게 명시적으로 주고, 여러 참가자는 미정으로 적는다 |
| 12 | 치명타는 공격 1회에 한 번 판정한다. 맞은 적이 없으면 굴리지 않는다. 방향·치명타 난수를 나눈다 | 샌드박스 | 따른다 [샌드박스 선택, 원작 미확인] |
| 13 | 확정 치명타의 배율 = max(플레이어 배율, 버프 배율) | 샌드박스 (샌드박스 문서 7절과 코드가 서로 다르다) | 단순화한다 [제안]. 버프는 치명타를 확정하기만 하고, 배율은 Breaker의 `CritMultiplier`다 |
| 14 | 같은 종류 버프를 다시 얻으면 남은 시간은 max, 강한 값만 남기고 곱으로 쌓지 않는다 | 샌드박스 (원작에서는 달 지속 시간이 강화 대상이라는 것만 확인, R9) | 따른다 [샌드박스 선택] |
| 15 | 공격 주기가 바뀌어도 타이머의 진행률을 유지한다 | 샌드박스 | 따른다. 남은 시간이 아니라 진행 속도를 바꾸면 저절로 된다 |
| 16 | 유효 공격 간격은 최소 0.1초 | 샌드박스 | 지금은 두지 않는다. 간격을 더 줄이는 것은 업그레이드뿐인데 아직 잇지 않는다. 업그레이드 연결 때 되먹임 상한과 함께 정한다(SKILL_TREE_PLAN 3.3) |
| 17 | Step을 최대 0.05초로 나누기, 프레임당 4 Step, 남은 시간 이월, 버프 만료 시각에서 나누기 | 샌드박스 | 지금은 가져오지 않는다(D6) |
| 18 | 처치자가 없는 사망은 효과를 내지 않는다 | 샌드박스 | 미정(D5) |
| 19 | 샘플 수치(HP 14·6, 피해, 반경 2.5, 5초, 0.5배, 2배) | 샌드박스 | [임시]로 옮긴다 |

## 5. 가져오는 방법

### 5.1 원칙

- **규칙은 Core에 둔다.** `Core/Skills/`, `Core/DeathEffects/`에 두고 네임스페이스는 `BlackHole.Core`다. 샌드박스처럼 어셈블리를 따로 만들지 않는다. `World.Step`이 스킬을 불러야 하는데 Core가 다른 어셈블리를 참조하면 순환이 생기고, `Point2`·`PlayerId`도 두 벌이 된다.
- **표적 경계(`ISkillTarget`)와 TestPack은 가져오지 않는다.** 표적은 `Core.Enemy`이고, 피해는 `World.DealDamage(enemy, Damage)` 하나로 들어간다. `World` 주석이 이미 정해 둔 입구다. 사망 1회·사망 기록·처치 수는 `EnemyRoster`가 이미 처리한다.
- 두 스킬을 묶는 인터페이스를 만들지 않는다. 샌드박스와 같은 판단이고, CA-003의 금지 항목이다.
- 표현은 기록을 읽기만 한다. 같은 기록을 두 번 그리지 않도록 기록마다 순번을 둔다(`DeathRecord.Sequence`와 같은 방식). 이 레포는 정지 중에 `Advance`가 기록을 비우지 않기 때문이다.

### 5.2 대응표

| 샌드박스 | 이 레포에서 |
|---|---|
| `Skills.Point2` | `Core.Point2` 그대로 |
| `int playerId` | `PlayerId` |
| `ISkillTarget`·`IDeathEffectTarget`·`EnemyTarget` | `Core.Enemy` |
| `target.Hit(damage, playerId)` | `World.DealDamage(enemy, new Damage(amount, owner))` |
| `SkillType` + 칸이 평평한 `SkillStats`(안 쓰는 칸은 0) | `BreakerDefinition`·`LaserDefinition`. 생성할 때 `DefinitionGuard`로 검증한다(`EnemyStats`와 같다) [제안] |
| `SkillCatalog.Load` + `SkillDiagnostic` | `ContentData.Skills` → `ContentLoader`(`ContentDiagnostic`) |
| `SkillCatalogAsset` | Unity 스킬 설정 에셋 하나. Breaker 칸과 레이저 칸을 따로 둔다 → `ContentData` [제안]. 칸을 나누면 "종류에 맞는 칸만 채우기"(AUTHORING_PAIN AP8)가 생기지 않는다 |
| `BreakerRuntime`·`LaserRuntime` | `BreakerSkill`·`LaserSkill` (Core/Skills) |
| `SkillRuntime.Create` | 판 조립(`SessionAssembler`)이 참가자마다 콘텐츠의 스킬을 만든다 |
| `SkillBattle`(조립·순서·켜기끄기) | 조립은 `SessionAssembler`, 순서는 `World.Step`, 켜기·끄기는 스킬 자신(`SetEnabled`: 끄면 주기와 예고를 버린다) |
| `SkillBattle.Aim` | 판 안의 참가자의 `AimPoint` (5.4) |
| `PlayerCombatStats` | 가져오지 않는다(4절 9). `CritChance`·`CritMultiplier`는 Breaker 정의에 둔다 |
| `SkillDamage`(치명타 판정, `SkillHit` 기록) | Breaker 안의 치명타 판정과 적중 기록 |
| `SeededSkillRandom` | `BattleRandom`. 판 seed에서 방향·치명타 스트림을 나눈다 [제안] |
| `DeathEffectDefinition`(평평, 종류별 칸) | `EnemyDefinition.DeathEffect`. 저작 형식은 `EnemyBehaviorData`처럼 종류 이름 + 칸이고, Loader가 종류별 정의를 만든다 [제안] |
| `Skills.EnemyCatalog` | 가져오지 않는다. `EnemyKind` 에셋에 사망 효과 칸을 더한다 |
| `DeathEffects.Resolve`(모든 적을 훑음) | 효과 보유 적이 피해로 처음 죽는 순간(`World.DealDamage`) 대기열에 넣고, Step 4에서 사망 순서대로 처리한다(`World.DeathEffects`) |
| `PlayerBuffs`(int → 상태) | 참가자마다의 버프 상태. 대상은 Breaker |
| `LastVisuals`·`PendingShots`·`EffectHit`·`EffectActivation` | 순번 있는 기록: Breaker 원, 레이저 예고·발사, 번개 적중, 폭발, 치명타 적중 |
| `GameHost`(IMGUI 콘솔·마우스·고정 적 5개) | 스킬 콘솔(`ConsoleParts`), 조준 입력, 스킬 화면. 적은 지금처럼 공급으로 나온다 |
| `SkillEffectsView` | `Unity/Skills/SkillView`. `EnemyView`처럼 `Reset`·`IsClear`를 두어 전투 정리 단계의 "Presentation cleared"에 든다 |
| `SkillContracts` 22개 | `Tests/EditMode/Contracts/`에 새로 쓴다. 새로 보장하는 동작만 둔다(8절) |

### 5.3 Step 자리

```text
World.Step(delta)
  1. Enemy Action   살아 있는 적이 움직인다                                  (지금 있음)
  2. Passive Attack 참가자 순서로, 참가자 안에서는 콘텐츠의 스킬 순서로 [제안]
                    이번 Step의 공격은 Step 시작 때의 버프로 한다. 버프 시간은 공격 뒤에 준다 [제안]
  3. Damage / Death 피해 사망은 DealDamage 순간에 확정된다(지금과 같다). 파괴 요청 처리   (지금 있음)
  4. Death Effect   이번 Step의 사망 기록 중 효과가 있는 것을 사망 순서대로 처리한다
                    효과 피해도 DealDamage로 준다. 버프는 받는 참가자에게 붙는다
  7. Enemy Supply   생성 요청 처리                                          (지금 있음)
```

- `DealDamage`는 죽은 적을 `World.Enemies`에서 바로 뺀다. 그래서 스킬은 대상을 먼저 모은 뒤 피해를 준다. 샌드박스도 그렇게 했다.
- 효과를 가진 적은 효과 피해를 받지 않는다. 그래서 효과로 죽은 적은 대기열에 새 효과를 넣지 않고, 대기열은 한 번 훑으면 끝난다.
- 효과 처리가 Step 안에서 끝나므로 `HasPendingDeathProcessing`은 계속 false다.
- 사망 효과는 죽은 적의 효과 정의와 위치, 효과 피해의 출처가 필요하다. 대기열에 넣을 때 셋을 함께 담는다(`DeathRecord`는 바꾸지 않는다). 출처는 마지막 피해의 출처이며 기록일 뿐이다. 파괴 요청으로 죽은 적은 출처가 없어 넣지 않는다(D5).
- Step 밖에서 준 피해로 죽은 효과 보유 적은 다음 Step까지 대기열에 남는다(`HasPendingDeathProcessing`). 판 정리는 처리되지 않은 효과를 버린다.

### 5.4 판 안의 참가자 [제안]

- `World`에 참가자마다의 상태를 둔다: `PlayerId`, `AimPoint`(`Point2?`), 스킬 목록, 버프. 이름은 `BattlePlayer`로 제안한다. 전투 밖 진행 상태인 `PlayerState`와 다르고, 캐릭터도 아니다.
- 호스트가 프레임마다 로컬 참가자의 조준점을 넣는다(`GameSession.SetAimPoint(PlayerId, Point2?)` [제안]). 마우스는 지금 쓰는 출처일 뿐이다.
- 모든 참가자가 콘텐츠의 스킬을 전부 받는다. 레이저를 노드로 여는 것은 업그레이드 연결 때 한다(D7).

## 6. 결정

| # | 항목 | 이 PLAN의 선택 | 근거 / 확인할 것 |
|---|---|---|---|
| D1 | 분기 기준 | `feature/업그레이드연결` | 적·전투·세션이 있어야 스킬이 실제 적을 친다. `feature/적시스템`보다 뒤이고, 업그레이드 화면 ↔ 전투 루프도 있다. `feature/처치보상`은 들어 있지 않다(11절) |
| D2 | 치명타와 버프의 대상 | Breaker의 수치, Breaker 대상 | 4절 9·10. 샌드박스 명세와 다르다. **사용자 확정 (2026-09-26)** |
| D3 | 버프를 받는 참가자 | 단일 참가자에게 명시적으로 준다. 여러 참가자는 미정 — 참가자가 둘 이상이면 아무도 받지 않는다(`feature/처치보상`의 결산과 같은 전제) | 4절 11. 귀속 정책은 F06 |
| D4 | 샘플에서 효과를 가진 적 [임시] | 달 = 공격 주기 감소, 혜성 = 확정 치명타. 전기 소행성(`electric-asteroid`, 연쇄 번개)·초신성(`supernova`, 폭발)은 새 종류 에셋으로 만들어 세 단계 풀과 전투 시작 공급에 넣는다 | 달·혜성은 원작과 짝을 맞춘 해석이다 [분석]. 사용자의 "처치 시 버프, 처치 시 공격 주기 감소"(SKILL_TREE_PLAN 1절)에 대응한다. `EnemyKind` 주석대로 효과는 종류가 아니라 종류에 붙는 특성이다 |
| D5 | 처치자가 없는 사망(개발용 파괴 요청)의 효과 | 미정. 제안: 효과를 내지 않는다(샌드박스와 같다) | 파괴 요청은 개발용 적 명령 콘솔만 쓴다 |
| D6 | 시간 진행 | 지금처럼 프레임당 한 Step. 한 Step 안에서 공격이 여러 번 돌 수 있다(타이머 반복) | 샌드박스의 Step 나누기는 버프 만료 정밀도와 긴 프레임 때문이다. 필요해지면 S12(성능)와 함께 정한다. 버프 만료는 Step 단위로 판정한다 |
| D7 | 참가자가 받는 스킬 | 콘텐츠의 스킬 전부. 콘솔에서 스킬마다 켜고 끈다 | 원작은 레이저를 트리에서 연다. 노드 해금은 업그레이드 연결 때 한다 |
| D8 | 샌드박스 문서 | 옮기지 않는다 | 채택한 규칙은 이 PLAN 4절에 출처와 함께 남긴다 |

## 7. 코드 구조

| 곳 | 파일 | 바뀌는 것 | 티켓 |
|---|---|---|---|
| Core | `Skills/BreakerDefinition`, `Skills/BreakerSkill`(Tick 기록 `BreakerTick` 포함) | 새로 만든다 | SK-002 |
| Core | `Skills/LaserDefinition`, `Skills/LaserSkill` | 새로 만든다 | SK-003 |
| Core | `World/World` | 참가자 목록, Step 2·4 자리 | SK-002·005 |
| Core | `World/BattlePlayer` [제안] | 조준점·스킬 | SK-002 |
| Core | `Skills/BreakerSkill`, `Skills/BreakerDefinition` | 처치 버프 상태(남은 시간·주기 배율)는 대상인 Breaker가 가진다. 치명타 확률·배율, Tick의 `IsCritical`, 치명타 난수 스트림 | SK-006 |
| Core | `Skills/BreakerSkill`, `Skills/LaserSkill` | 켜기·끄기(`SetEnabled`, 개발용 콘솔만 부른다) | SK-004 |
| Core | `Session/SessionAssembler`, `Session/GameSession` | 참가자마다 스킬을 만든다, `SetAimPoint` | SK-002 |
| Core | `Common/BattleRandom` | 판 seed에서 스트림 나누기 | SK-003 |
| Core | `Content/ContentData`, `ContentLoader`, `GameContent` | 스킬 저작 형식·검증, 적의 사망 효과 칸 | SK-002·003·005 |
| Core | `DeathEffects/DeathEffectDefinition`, `DeathEffects/DeathEffects`, `Enemies/EnemyDefinition` | 효과 정의(SK-005 연쇄·폭발, SK-006 버프 둘), 대기열과 처리·기록(`LightningHit`, `ExplosionBlast`), 적 종류의 효과 | SK-005·006 |
| Unity | `Enemies/DeathEffectView`, `Battle/LineStrokes` | 번개 선·폭발 원. 선 그리기 부품은 스킬 화면과 함께 쓴다 | SK-005 |
| Unity | `Content/SkillSetup`(새 에셋), `Content/EnemyKind` | 스킬 수치, 적 종류의 사망 효과 칸 | SK-002·005 |
| Unity | `Skills/AimInput`, `Skills/SkillView` | 마우스 → 조준점, 공격 표시 | SK-002·003 |
| Unity | `Battle/BattleSystem`, `Battle/BattleOrchestrator`, `GameHost` | 스킬 화면·조준의 시작·정리 자리(오케스트레이터 주석에 이미 비워 둔 자리) | SK-002 |
| Unity | `Console/SkillConsole` | 켜기·끄기, 수치 창, 버프 남은 시간 | SK-004·006 |
| Data | `Data/SkillSetup.asset`, `Data/Enemies/*.asset`, `Data/Pools/*.asset` | 샘플 값 [임시] | SK-002·005 |
| Tests | `Contracts/SkillContracts`, `Contracts/DeathEffectContracts` | 8절 | 각 티켓 |

## 8. 계약

새로 보장하는 동작만 더한다. 샌드박스의 한계를 재현하는 시험은 두지 않는다.

| 티켓 | 계약 |
|---|---|
| SK-002·003 | `Skill.SkillsAreOptionalButValidatedAtLoad` — 스킬 칸이 없으면 그 스킬 없이 판이 돌고, 잘못된 수치는 경로와 함께 보고 |
| SK-002 | `Skill.BreakerHitsEveryAliveEnemyInsideAimRadius` — 첫 Step에 Tick, 범위 안 전부, 밖은 제외, 조준점 없으면 빈 Tick |
| SK-002 | `Skill.DamageGoesThroughWorldOnce` — 같은 Tick에 겹쳐도 사망 기록·처치 수는 한 번 |
| SK-002 | `Skill.NothingCarriesIntoNextBattle` — 끝난 판은 공격하지 않고, 새 판의 타이머·예고는 처음부터 |
| SK-003 | `Skill.LaserAimsAtTelegraphStartAndPiercesOnFire` — 예고 뒤 조준점이 움직여도 경로가 같다. 경로 폭 안의 적 전부 |
| SK-003 | `Skill.LaserSkipsAimOutsideBoundaryAndStopsWithTheBattle` — 조준점이 없거나 경계 밖이면 그 주기는 예고 없이 지나가고, 끝난 판의 예고는 발사하지 않는다 |
| SK-003 | `Skill.LaserStartIsReproducibleAndSeparateFromSpawns` — 같은 seed면 같은 시작점. 레이저 난수는 따로 돌아 출현 배치가 뽑는 횟수에 흔들리지 않는다(치명타 스트림은 SK-006) |
| SK-004 | `Skill.DisabledSkillDropsItsTimerAndPendingShots` — 끈 스킬은 공격하지 않고 예고 중인 발사를 버린다. 다시 켜면 켠 뒤 첫 Step부터 처음처럼 돈다 |
| SK-005 | `Death.EffectsAreValidatedAtLoad` — 알 수 없는 종류·잘못된 수치를 경로와 함께 보고, 종류 이름이 비면 효과 없음 |
| SK-005 | `Death.EffectDamageSkipsEffectOwners` — 연쇄·폭발 모두 |
| SK-005 | `Death.ChainEndsAtHopLimitWithoutRevisit` — 가장 가까운 순, 재방문 없음, 최대 횟수 |
| SK-005 | `Death.EffectKillsCountInTheSameStep` — 효과 사망도 같은 Step의 사망·처치 수. 파괴 요청 사망은 효과 없음, 판 정리가 남은 효과를 버림 |
| SK-006 | `Buff.StartsNextStepAndExpires` — 처치 Step의 공격에는 없고, 다음 Step부터, 시간이 끝나면 원래대로 |
| SK-006 | `Buff.HasteKeepsTimerProgress` — 주기 절반에서 얻으면 남은 절반만 빨리 찬다 |
| SK-006 | `Buff.KillBuffsApplyOnlyToBreaker` — 주기 감소·확정 치명타는 Breaker에만. 레이저의 주기·피해는 그대로. 확정 치명타는 Breaker의 배율을 한 번 |
| SK-006 | `Buff.GoesToTheSingleParticipantOnly` — 참가자가 둘 이상이면 아무도 받지 않는다(D3) |
| SK-006 | `Skill.BreakerCriticalIsOneRollPerTickOnItsOwnStream` — Tick마다 한 번 판정해 맞은 적 모두 같은 결과, 치명타 확률이 레이저 시작점을 바꾸지 않는다 |

마지막 계약은 D2(Breaker에만)를 지킨다.

## 9. 확인 방법

1. CoreSmoke 계약 통과. Unity 밖 컴파일(Core·Sample·Unity·Authoring·Editor). Unity 에디터 로그(`Logs/Editor.log`)의 빌드 성공.
2. 플레이 (사용자):
   - 업그레이드 화면에서 `Start battle`을 누른다. 커서 자리에 Breaker 원이 주기마다 보이고, 원 안의 공전하는 적이 맞아 죽는다.
   - 레이저: 화면 밖에서 커서 쪽으로 얇은 예고선이 생기고, 굵은 선으로 발사돼 경로의 적이 맞는다. 예고 중 커서를 옮겨도 그 발사의 경로는 그대로다.
   - 스킬 콘솔에서 스킬을 끄면 공격이 멈추고, 다시 켜면 처음부터 돈다.
   - 전기 소행성을 부수면 가까운 적으로 번개가 이어진다. 초신성을 부수면 반경 안의 적이 맞는다. 효과를 가진 적은 번개·폭발에 맞지 않는다.
   - 달·혜성을 부수면 콘솔에 버프 남은 시간이 뜬다. Breaker가 빨라지거나 치명타 표시가 나고, 레이저는 그대로다.
   - 전투를 끝내면 공격·효과 표시가 남지 않고 업그레이드 화면으로 돌아온다.

## 10. 티켓

| 티켓 | 내용 | 선행 | 상태 |
|---|---|---|---|
| SK-001 | 이 PLAN: 두 레포 비교, 규칙 출처 가르기, 가져오는 방법 | — | 완료 (사용자 검토, D2 확정) |
| SK-002 | 조준점과 Breaker: 판 안의 참가자·조준점, Breaker 정의·저작 형식·에셋, Step 2, 조준 입력, Breaker 원 표시, 계약 | SK-001 | 구현(계약 64개 통과, Unity 밖 빌드 성공), 플레이 확인 대기 |
| SK-003 | 관통 레이저: 정의·저작 형식, 예고·발사, 경계 반지름, 난수 스트림, 예고·발사선 표시, 계약 | SK-002 | 구현(계약 67개 통과, Unity 밖 빌드 성공), 플레이 확인 대기 |
| SK-004 | 스킬 콘솔: 스킬마다 켜기·끄기, 수치 창 (왼쪽 가운데, 고른 켜짐은 판 사이에 이어짐) | SK-003 | 구현(계약 68개 통과, Unity 밖 빌드 성공), 플레이 확인 대기 |
| SK-005 | 사망 효과: 적 종류의 사망 효과 칸, Step 4, 연쇄 번개·폭발, 샘플 종류와 풀 [임시], 번개·폭발 표시, 계약 | SK-002 | 구현(계약 72개 통과, Unity 밖 빌드 성공), 플레이 확인 대기 |
| SK-006 | 처치 버프와 Breaker 치명타: 달·혜성, 참가자 버프, 치명타 판정, 콘솔의 남은 시간, 치명타 표시, 계약 | SK-004·005 | 구현(계약 77개 통과, Unity 밖 빌드 성공), 플레이 확인 대기 |
| SK-007 | 문서: SYSTEM_CATALOG S02·S04·S06 상태와 이 PLAN의 티켓 상태 | SK-006 | 대기 |

- SK-002 뒤에는 레이저 쪽(SK-003·004)과 사망 효과 쪽(SK-005)을 따로 진행할 수 있다.
- 커밋은 티켓마다 하나다. 에셋·씬 변경은 그 변경을 만든 커밋에 함께 넣는다.

## 11. 다른 브랜치와 겹치는 곳

- **`feature/처치보상`**: `EnemyDefinition`·`EnemyKind`·`World`·`SessionAssembler`·`ContentLoader`를 크게 바꿨다(색 등급, 황금, `Loadout`). SK-005의 사망 효과 칸도 같은 파일을 건드린다. 효과는 따로 떨어진 칸과 파일(`Core/DeathEffects/`)에 둬서, 나중에 합칠 때 충돌을 줄인다.
- **업그레이드 연결 뒤**: Breaker가 `UpgradesOf(owner)`에서 `breaker.damage`·`speed`·`radius`·`crit-chance`를 읽는 자리는 판 조립 때 스킬을 만드는 한 곳이다. 지금은 잇지 않는다.

## 12. 남은 결정

| 항목 | 지금 | 정할 때 |
|---|---|---|
| 여러 참가자일 때 버프 귀속 (D3) | 단일 참가자만 | F06 |
| 처치자 없는 사망의 효과 (D5) | 효과 없음 [제안, SK-005에서 그대로 구현] | 파괴가 개발용 명령 밖에서도 쓰일 때 |
| 유효 간격 하한·버프 되먹임 상한 | 없음 | 업그레이드 연결 |
| Step 나누기·프레임 예산 (D6) | 프레임당 한 Step | S12 기준 상황을 정할 때 |
| 연쇄·폭발의 세부 규칙과 수치 | 샌드박스 값 [임시] | 원작 확인 또는 밸런스 |
| 달이 Breaker를 "어떻게" 강화하는가 | 공격 주기 감소 [분석] | 원작 확인 |
