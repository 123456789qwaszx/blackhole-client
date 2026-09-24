# Black Hole Client

6인 팀의 시스템 분배에 앞서 책임·상태 소유권·변경 영향을 확인하는 개인 Reference Implementation입니다. 팀에 이식할 완성본은 아닙니다.

## 현재 상태

dev의 M0~M6이 완료됐습니다. 다음은 M7(적의 사망 효과와 연쇄)이며, 착수 전에 정할 것을 확인하는 단계입니다.
현재 실행에는 판 시작 배치, HQ 공전, 마우스 조준 패시브 공격, 사망 즉시 Gold(Player)와 HQ EXP 지급, 흡수 연출, HQ 성장(Level마다 적 추가·시간 연장·HQ 크기 증가), 전투가 끝난 뒤의 업그레이드 구매와 다음 전투가 있습니다. 노드 저작 툴은 별도 작업입니다.

## 실행

Unity 6000.6.2f1로 열고 `Assets/Scenes/SampleScene.unity`를 Play 합니다.

| 입력 | 현재 동작 |
|---|---|
| 마우스 | 첫 패시브 스킬의 조준점. 범위 공격은 자동 |
| P | 일시정지/재개 |
| R | 새 진행(Gold·구매까지 초기화) |

전투가 끝나면(시간 종료 또는 End session) 구매 화면이 뜹니다. 노드를 사고 "Next battle"로 다음 전투를 시작하면 Gold와 구매가 이어집니다. 화면 수치는 샘플입니다.

## 문서

1. [게임 규칙](docs/GAME_RULES.md)
2. [레퍼런스 분석](docs/REFERENCE_ANALYSIS.md)
3. [전체 PLAN](docs/v2/PLAN.md)
4. [실제 코드와 규칙의 차이](docs/v2/RULES_ALIGNMENT.md)
5. [현재 인계](docs/v2/HANDOFF.md)
6. [다음 M7](docs/v2/M7-death-effects.md)

v1 코드는 `reference-v1` 태그, 문서는 [docs/v1](docs/v1/SYSTEM_CATALOG.md)에 보존합니다.

## 코드 읽는 순서

- `Assets/BlackHole/Sample/SampleContent.cs`: 임시 콘텐츠.
- `Assets/BlackHole/Core/Session/SessionAssembler.cs`: 전투 조립.
- `Assets/BlackHole/Core/Session/GameSession.cs`, `SessionRunner.cs`: 수명과 시간.
- `Assets/BlackHole/Core/World/World.cs`: 이동 → Skill → 성장 진행 → 공급된 적 생성.
- `Assets/BlackHole/Core/Hq/`, `Progression/GrowthProgression.cs`, `Supply/EnemySupply.cs`: HQ 성장, 성장 효과, 공급.
- `Assets/BlackHole/Core/Skills/PassiveSkill.cs`, `Enemies/Enemy.cs`: 피해와 상태.
- `Assets/BlackHole/Unity/SessionLauncher.cs`, `WorldView.cs`: 전환과 화면.

## 검증

Unity EditMode와 .NET 8 CoreSmoke가 같은 계약을 실행합니다.

```sh
dotnet run --project tests/CoreSmoke/CoreSmoke.csproj -c Release
```

M6 기준 계약 55개가 통과합니다. CoreSmoke는 Unity 호스트·입력·렌더링 검증을 대체하지 않습니다.
