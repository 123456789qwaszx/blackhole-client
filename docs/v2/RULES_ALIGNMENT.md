# 최신 게임 규칙과 구현의 정합성 점검

기준: 처음 점검은 dev `85e34bc`(2026-09-24, 코드 열람). M4·M5 완료 후 "단계" 열에 반영 상태를 적었다.

## 유지할 구조

Core/Sample/Unity 분리, Player 목록과 식별자, Player별 AimPoint·Skill, HQ 기준 좌표, IEnemyBehavior, 기본 정의/실행 수치 분리, 콘텐츠 경로 진단, 공통 계약 실행 구조는 유지한다. 전체 재작성 사유는 없다.

## 차이와 후속 작업

| 확인한 파일 | 실제 상태 | 필요한 변경 | 단계 |
|---|---|---|---|
| Core/Players/Player.cs | PlayerState 비어 있음. 주석은 M4 Gold/EXP 예정 | Gold와 구매 상태. 성장 EXP는 HQ로 | Gold는 M4 완료, 구매 상태는 M6 |
| Core/Hq/Hq.cs | 위치만, 성장 제외 주석 | EXP 누적 후 Level 계산. 진행 효과는 별도 책임 | EXP 누적은 M4, Level은 M5 완료. 진행 효과는 GrowthProgression(M5) |
| Core/Enemies/Enemy.cs | HP를 0까지 줄이지만 Death 상태 없음 | 즉시 사망 확정, 재피해/이동/선택 차단, 보상 1회 | M4 완료 |
| Core/World/World.cs | 이동 → 모든 Skill → 주기 출현 | 사망 처리 안전 지점, 생성 공급 변경, 후속 연쇄 큐 공급은 M5 완료, 연쇄 큐는 M7 |
| Core/Skills/PassiveSkill.cs | HP 0도 선택, 중심 기준, 타깃 목록 스냅샷 | Dead 필터와 적용 시 재확인. 크기 포함 판정은 기획 확인 | 필터·재확인은 M4 완료, 판정은 M6 전 결정 |
| Core/Enemies/EnemyDefinition.cs | HP·속도·Size만, Size는 반지름 | 보상 정의/실행 보상, 사망 효과 정의. 질량 공식은 미정 | 보상 정의는 M4 완료, 실행 보상은 M6, 사망 효과는 M7 |
| Core/Spawn/EnemySpawner.cs | Interval 생성, 최대 수 초과 틱 건너뜀, 보정 빈 목록 | 공급 시점/구성 결정과 생성 실행 분리, 구매 보정 연결 | 분리는 M5 완료(EnemySupply·EnemySpawner), 구매 보정은 M6 |
| Core/Session/SessionAssembler.cs | 매번 새 Player, 스킬/적 보정 출처 없음 | 진행 상태 입력과 새 전투 실행 상태 조립 구분 | M6 |
| Core/Session/TimeLimit.cs, SessionRunner.cs | 불변 Duration으로 클립·종료 | 세션별 시간 연장. 공유 정의 수정 금지 | M5 완료(TimeLimitRule.Limit) |
| Core/Session/GameSession.cs | Stop은 종료 상태/결과만 변경, World는 남음 | 업그레이드 진입 시 적 정리 흐름 추가. 종료≠처치 | M6 |
| Unity/SessionLauncher.cs | Stop → View.Reset → 새 조립 | 전투 종료 → 적/연출 정리 → 구매 → 다음 전투. 테스트 리셋과 구분 | M6 |
| Unity/WorldView.cs | 목록에서 사라진 View 즉시 삭제 | 사망 기록 읽고 연출. 중간 단계 사망 및 재사용 ID 처리 | M4 완료(순번으로 1회 소비, 재시작 때 초기화) |
| ContentData/Loader/Invariants/GameContent | Session/HQ/Enemy/Spawn/Skill 정의 | 보상·성장·공급·구매·사망 효과를 쓰는 단계에 추가/검증 | 보상은 M4, 성장·공급은 M5 완료. 구매는 M6, 사망 효과는 M7 |
| README.md | v1 액티브 공격·흡수 보상·13계약 | 현재 M3 실행 안내로 정정 | 이번 문서 반영 |

## 계약 변경 계획

- Session.RestartIsANewAssembly: 새 World/실행 Player 객체라는 기존 의미는 유지할 수 있다. 이를 Gold·구매 데이터까지 초기화해야 한다는 규칙으로 확대하지 않는다. M6에서 새 전투/새 진행의 서로 다른 계약 추가.
- (M5 완료) Skill.TicksOnScheduleWithoutInput 등은 “0.1초 첫 적 출현”을 전제로 피해 횟수를 계산한다. M5 초기 배치 후에는 0초 첫 틱 피해가 달라지므로 입력과 기대값을 함께 수정한다.
- (M5 완료) Enemy의 출현 주기·최대 수·긴 프레임 계약은 공급 정책과 생성 실행 계약으로 나눈다. 시간 경계를 피하기만 하지 말고 임계 시각 전/동일/후를 명시적으로 검증한다.
- World.PlayersAreAListNotASingleton, Skill.AimPointBelongsToPlayer, Skill.EachPlayerHasOwnSkills는 유지한다. 보상 미정 때문에 이 경계 검증까지 없애지 않는다.
- (M4 완료) 다인 테스트는 보상 없는 단위 조립 또는 테스트 전용 보상 대역을 사용한다. 실제 플레이의 1인 보상 구성은 시작 전에 검증하고, 처치 중 예외로 일부 보상만 적용되는 구조를 피한다.
- 구매 보정은 계산기 단위 확인만으로 완료하지 않는다. 구매 → 다음 전투 → 실제 스킬/생성 적/보상 변화까지 검증한다.
- Size가 현재 화면에만 반영되는 점을 명시한다. 피격 경계를 정하기 전 크기 업그레이드의 게임 효과가 구현됐다고 주장하지 않는다.

## 처음 점검의 한계

처음 점검에서는 소스·씬·테스트 코드를 수정하지 않았다. M0~M3의 완료 결과는 당시 요구사항 기준이며 새 GAME_RULES 전체를 충족한다는 뜻이 아니다. 첨부 인계서의 “미push”·환경·D1/D4·Player EXP 지시는 현재 문서로 대체한다.
