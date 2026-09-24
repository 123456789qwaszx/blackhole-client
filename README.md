# Black Hole Client

6인 팀의 시스템 분배에 앞서 책임·상태 소유권·확장 경계를 확인하는 Reference Implementation입니다.
게임 규칙과 수치는 실험용이며 최종 팀 기획이 아닙니다.

## 실행

1. **Unity 6000.6.2f1**로 프로젝트를 엽니다.
2. `Assets/Scenes/SampleScene.unity`를 열고 Play 합니다.
3. Game 창에 포커스를 두고 마우스를 대상 위에 올립니다.

| 입력 | 동작 |
|---|---|
| `1` | 조준점 주변 가장 가까운 대상 하나에 집중 공격 |
| `2` | 조준점 주변 범위 피해 + 중심 방향 당김 |
| `U` / Upgrade 버튼 | 흡수한 재화를 소비해 공격력 강화 |
| `P` / Pause 버튼 | 진행·출현·쿨다운을 함께 정지 / 재개 |
| `R` / Restart 버튼 | 이전 판을 종료하고 새로운 판 생성 |
| End session 버튼 | 결과를 확정하고 진행 종료 |

한 판은 60초입니다. 청록색 파편과 주황색 중량 대상이 서로 다른 속도로 공전합니다.
HP가 0이 되면 회색으로 바뀌어 중심으로 떨어지고, 블랙홀에 흡수된 순간 재화와 질량을 얻습니다.
질량은 흡수 반경을 키우며 재화는 공격력 강화에 사용합니다. 재시작하면 모두 초기화됩니다.

## 코드 읽는 순서

1. `Assets/BlackHole/Core/ReferenceGame.cs`: 실험용 콘텐츠와 조립
2. `GameSession.cs`: 한 판의 수명과 요청 허용 여부
3. `Playfield.cs`: 생성·이동·흡수와 스킬 요청 연결
4. `SkillLoadout.cs`, `FocusedStrike.cs`, `GravityPulse.cs`: 발동과 실제 확장점
5. `TargetState.cs`, `CombatResolver.cs`, `AbsorptionSystem.cs`, `GrowthState.cs`: 상태 변경과 확정 순서
6. `Assets/BlackHole/Unity`: 입력·화면 연결

설계 판단, 남은 질문, 팀 분배 후보는 [구현 결과 문서](docs/REFERENCE_IMPLEMENTATION.md)에 있습니다.

## 검증

Unity: **Window → General → Test Runner → EditMode → Run All**.

Unity 없이 순수 규칙을 확인하려면 .NET 8 SDK로 다음을 실행합니다.

```sh
dotnet run --project tests/CoreSmoke/CoreSmoke.csproj --configuration Release
```

두 경로는 같은 13개 계약 테스트를 실행합니다. 별도 NuGet 테스트 패키지는 필요 없습니다.
GitHub Actions의 `Core contracts`도 위 명령을 사용합니다.
이 검증은 Unity 호스트 컴파일·렌더링·입력 실행을 대체하지 않습니다.
