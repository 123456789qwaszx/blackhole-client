# Black Hole Client

6인 팀의 시스템 분배에 앞서 책임·상태 소유권·변경 영향을 확인하는 개인 Reference Implementation입니다. 팀에 이식할 완성본은 아닙니다.

## 현재 상태

dev의 M0~M3가 완료됐습니다(구현 기준 `85e34bc`). 최신 게임 규칙을 문서에 반영했으며 M4 이후는 재계획 상태입니다.
현재 실행에는 주기적 적 생성·HQ 공전·마우스 조준 패시브 공격이 있습니다. HP가 0인 적은 아직 남습니다. 사망·Gold/HQ EXP·성장·구매·흡수 연출은 후속 구현입니다.

## 실행

Unity 6000.6.2f1로 열고 `Assets/Scenes/SampleScene.unity`를 Play 합니다.

| 입력 | 현재 동작 |
|---|---|
| 마우스 | 첫 패시브 스킬의 조준점. 범위 공격은 자동 |
| P | 일시정지/재개 |
| R | 전투를 새로 조립하는 테스트용 재시작 |

현재 화면 수치는 샘플입니다. 다음 전투에서 구매 상태를 유지하는 흐름은 아직 없습니다.

## 문서

1. [게임 규칙](docs/GAME_RULES.md)
2. [전체 PLAN](docs/v2/PLAN.md)
3. [실제 코드와 규칙의 차이](docs/v2/RULES_ALIGNMENT.md)
4. [현재 인계](docs/v2/HANDOFF.md)
5. [다음 M4](docs/v2/M4-death-reward.md)

v1 코드는 `reference-v1` 태그, 문서는 [docs/v1](docs/v1/SYSTEM_CATALOG.md)에 보존합니다.

## 코드 읽는 순서

- `Assets/BlackHole/Sample/SampleContent.cs`: 임시 콘텐츠.
- `Assets/BlackHole/Core/Session/SessionAssembler.cs`: 전투 조립.
- `Assets/BlackHole/Core/Session/GameSession.cs`, `SessionRunner.cs`: 수명과 시간.
- `Assets/BlackHole/Core/World/World.cs`: 이동 → Skill → 출현.
- `Assets/BlackHole/Core/Skills/PassiveSkill.cs`, `Enemies/Enemy.cs`: 피해와 상태.
- `Assets/BlackHole/Unity/SessionLauncher.cs`, `WorldView.cs`: 전환과 화면.

## 검증

Unity EditMode와 .NET 8 CoreSmoke가 같은 계약을 실행합니다.

```sh
dotnet run --project tests/CoreSmoke/CoreSmoke.csproj -c Release
```

M3 작업 기록은 31개 계약 통과입니다. 이번 문서 반영에서 게임 테스트를 새로 실행한 결과는 아닙니다. CoreSmoke는 Unity 호스트·입력·렌더링 검증을 대체하지 않습니다.
