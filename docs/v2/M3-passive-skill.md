# M3 — 첫 Passive Skill

> 2026-09-24 규칙 갱신: 아래 본문과 결과는 완료 당시 기록이다. 현재 지시는 [PLAN](PLAN.md)과 [GAME_RULES](../GAME_RULES.md)를 따른다. HP 0 잔존은 M4에서 제거한다. 실제 구매 보정은 M6, 중심 기준 피격은 그때 크기 의미를 확인한다. 초기 배치(M5) 후 0초 틱의 실제 피해 기대값을 재검토한다. 종전 M5 실험은 M8로 이동했다.


상태: 완료 · 선행: M2

## 목표

소유 Player의 조준점(AimPoint)을 중심으로 한 원 안의 Enemy가 일정 주기마다 피해를 받는다. 지금 AimPoint는 호스트가 마우스 위치로 채운다. 아무 키도 누르지 않는다. 원은 실제 공격 범위를 보여 준다. 이 단계에서는 HP가 줄기만 하고, Death와 보상은 M4에서 다룬다.

```text
Host/Input(마우스 위치) → Player 1의 AimPoint → Passive Skill
```

## 검증할 경계

- **B1 Player 단위**: Skill은 소유 Player에 속한다. AimPoint도 Player의 값이다.
  - `Player.MousePosition`처럼 Player와 마우스를 붙이지 않는다. 지금 PC 1인이라 Player 1의 AimPoint가 마우스에서 오는 것일 뿐, 4명이 각자 마우스를 가진다는 뜻이 아니다. 나중에는 PlayerCharacter 위치, 게임패드 조준, 네트워크 Player의 조준이 AimPoint를 채울 수 있다.
  - 입력 추상화 프레임워크는 만들지 않는다. 값의 소유 관계만 바르게 둔다.
- **B5 Passive Skill 실행**
  - 자기 주기로 자동 실행된다(버튼 없음).
  - 기준점을 외부에서 받는다. 첫 Skill은 소유 Player의 AimPoint를 쓰고, Skill 실행 구조는 AimPoint를 누가 채우는지 모른다.
  - 공격 틱마다 범위 안의 Enemy를 전부 공격한다.
  - Skill의 수치(반경, 주기, 피해량)는 기본 정의 + 보정에서 계산한다(M2의 Runtime Stat과 같은 방식, 지금은 보정 없음).
  - Skill을 **실행**하는 구조와 **얻는** 구조가 분리되어 있다. 지금은 첫 Skill을 처음부터 가진 것으로 두고, 획득 구조는 만들지 않는다.
- **B7 Presentation**: 원의 크기 = 실제 공격 반경. 화면은 수치를 따로 적지 않고 Skill의 현재 반경을 읽는다.

## 범위

- Passive Skill 정의: 기준점 종류, 반경, 공격 주기, 피해량.
- Skill 실행 상태: 주기 타이머, 계산된 수치.
- 기준점 공급: 호스트가 마우스 위치를 규칙 좌표로 바꿔 Player 1의 AimPoint로 넣고, Skill이 소유 Player의 AimPoint를 읽는다.
- 범위 선택과 피해 요청: Skill은 Enemy HP를 직접 쓰지 않고 피해를 요청한다. 피해에는 **출처 Player**를 기록한다. 누가 피해를 줬는지 알 수 있게 하려는 것이며, 보상 귀속 규칙으로 쓰지 않는다(귀속 정책은 미정).
- 긴 프레임에서 여러 틱이 지나가는 경우의 처리(Session의 시간 분할 안에서).
- 화면: 마우스를 따라다니는 원(실제 반경). 피격 표시는 간단히.

## 비범위

- Death, 보상, 흡수 연출(M4).
- 두 번째 Skill, Skill 획득·선택·성장 시스템.
- PlayerCharacter 기준 Skill, HQ 기준 Skill(구조만 막지 않는다).
- 액티브 스킬, 쿨다운 버튼.

## [임시] 값

- 첫 Skill의 반경, 공격 주기, 피해량.

## 확정된 해석

- **주기 타이머는 범위에 Enemy가 없어도 계속 돈다**(2026-09-24 확인). 0.0초, 0.5초, 1.0초, … 틱마다 범위 안의 Enemy를 공격하고, 그 순간 범위가 비어 있으면 아무 일도 없이 지나간다. "적이 없으면 공격을 보류했다가 들어오는 순간 공격" 같은 별도 규칙은 만들지 않는다.
- 첫 틱은 판 시작(0초)이다(같은 날 예시 "0.0초 Attack Tick"을 따른다).

## 이 마일스톤에서 정하는 것(M2 재점검에서 추가)

- **"원 안의 Enemy"의 판정**: Enemy의 중심이 원 안에 있는가, 크기(반지름)까지 포함해 원과 겹치는가. 가이드는 정하지 않았다. 가장 단순한 **중심 기준**으로 두고 [임시]로 표시한다. 크기 기준으로 바꾸면 판정 한 곳만 바뀌게 둔다.
- **AimPoint가 Core에 들어오는 길**: 호스트가 매 프레임 진행 전에 `GameSession`을 통해 Player의 AimPoint를 넣는다. 한 프레임 순서는 입력 읽기 → 재시작 → 일시정지 → AimPoint 갱신 → 진행 → 화면 갱신이다.
- **Skill의 기준점 종류**: Skill 정의가 기준점 종류를 데이터로 가진다. 지금 값은 "소유 Player의 AimPoint" 하나다. 모든 Skill이 AimPoint 기반이라고 가정하지 않게 하려는 것이며, 두 번째 종류는 만들지 않는다.
- **처음 가진 Skill**: 콘텐츠의 시작 Skill 목록을 판 조립 때 각 Player에게 준다. 획득 구조가 아니라 고정된 시작 구성이다(가이드 10절).
- **피해 출처 기록**: 피해에 출처 Player를 담고, Enemy는 마지막으로 피해를 준 Player를 기록만 한다. 보상 귀속에는 쓰지 않는다.

## 이 마일스톤에서 확인할 질문

- **AimPoint가 없으면?** 마우스가 화면 밖이거나 장치가 없을 때다. 그 틱은 아무 일도 없이 지나간다. [임시]로 표시한다.
- **피해를 받은 Enemy의 HP가 0 이하가 되면?** M3에서는 HP만 기록하고 Death 전이는 M4에서 연결한다. M3 동안 HP가 0 이하인 Enemy를 계속 두는 것은 임시 상태이며 M4에서 해소된다.

## 작업 순서

1. Passive Skill 정의와 로더·진단.
2. 호스트 입력 → Player 1의 AimPoint → Skill 실행 상태.
3. 주기 타이머와 틱 처리(시간 분할 안).
4. 범위 선택과 피해 요청(출처 Player 포함).
5. Enemy 쪽 피해 수용(HP 감소만).
6. 화면: 범위 원.
7. 계약 테스트(Skill 주기, 범위, 기준점, 출처).

## 완료 기준

- **자동 계약**
  - 공격 주기마다 원 안의 Enemy 전부가 피해를 받고, 원 밖은 받지 않는다.
  - 키 입력 없이 동작한다.
  - 테스트에서 Player의 AimPoint를 다른 위치로 넣으면 공격 위치가 바뀐다. Skill 코드는 AimPoint의 출처(마우스)를 모르고, Core에 마우스라는 개념이 없다.
  - 긴 프레임에서도 틱 수가 경과 시간과 맞다.
  - 피해에 출처 Player가 들어 있다.
  - Skill 수치를 보정하면(테스트용) 공격 반경·주기·피해량이 바뀐다.
- **플레이 확인**: 마우스를 따라 원이 움직이고, 원 안의 Enemy가 주기적으로 피해를 받는다(색 변화 등). 원의 크기가 실제 공격 범위와 맞는다.

## 재점검 항목

- Skill이나 Player가 마우스라는 입력 장치를 알고 있지 않은가(AimPoint라는 역할로만 아는가)?
- Skill 실행 구조에 획득·선택 로직이 섞이지 않았는가?
- 화면의 원 크기가 Skill의 실제 수치에서 오는가?
- 피해 출처 기록이 보상 귀속 규칙처럼 쓰이고 있지 않은가?

## 결과

완료: 2026-09-24

**한 일**

- `Skills/`
  - PassiveSkillDefinition(Id, 기준점 종류 `SkillOrigin`, 기본 수치). `SkillOrigin`의 값은 `OwnerAimPoint` 하나다.
  - PassiveSkillStats(반경, 주기, 피해량), PassiveSkillStatCalculator와 IPassiveSkillModifier. 실행 수치 = 기본 + 보정이며, M2 Enemy와 같은 방식이다.
  - PassiveSkill(실행 상태)
    - 연속 타이머. 첫 틱은 0초이고, 긴 단계에서는 지나간 틱을 모두 처리한다.
    - `TryGetOrigin`(기준점을 찾는 유일한 자리). `IsInside`(원 안 판정의 유일한 자리).
    - `TickCount`(읽기 전용).
- `Combat/Damage.cs`: 피해량과 출처 Player. 출처는 기록일 뿐이다.
- Enemy: `ApplyDamage`는 HP를 줄이고 0 아래로는 내려가지 않는다. `LastDamageSource`는 기록만 한다.
- Player와 GameSession
  - Player: `AimPoint`(`Point2?`)와 `Skills`.
  - `GameSession.SetAimPoint(PlayerId, Point2?)`: 참가하지 않은 Player면 거부(false)한다.
- World: 단계 순서는 이동 → Skill(Player 순서, Skill 순서) → 출현.
- Content
  - Skill 정의와 시작 Skill 목록, 그리고 로더 섹션.
  - ContentInvariants: Enemy와 Skill의 ID 유일성을 같은 코드(`Index<T>`)로 검사한다. 시작 Skill의 실재·중복도 검사한다.
- SessionAssembler: 시작 Skill을 모든 Player에게 같은 구성으로 준다. 획득 구조가 아니다.
- Sample: `sample-aura`(반경 1.2, 0.5초, 피해 3)를 시작 Skill로 둔다.
- Unity
  - HostInput: 마우스를 규칙 좌표 `Aim`으로 바꾼다. 장치가 없거나 화면 밖이면 null이다.
  - GameHost: `MouseAimPlayer = LocalPlayers[0]`로 배선한다. 프레임 순서는 입력 → 재시작 → 일시정지 → AimPoint → 진행 → 화면이다.
  - SceneSpace: 규칙 좌표와 장면 좌표를 오가는 변환의 유일한 자리. 화면과 입력이 같이 쓴다.
  - WorldView
    - 범위 원: 기준점은 `TryGetOrigin`, 크기는 `Stats.Radius`에서 온다. 틱마다 안쪽이 번쩍인다.
    - Enemy: 남은 HP 비율로 색이 바뀌고, 맞으면 잠깐 번쩍인다.
  - HUD: Player별 조준점과 틱 수.
- 계약 9개 추가(Skill 8, Content 1). 합계 31개.

**검증**

- CoreSmoke 31개 통과.
- 변이 검사: 아래를 하나씩 깨뜨리면 해당 계약이 실패한다. 코드는 복원했다.
  - 첫 틱을 0초에서 주기 뒤로 옮김
  - 빈 틱을 보류함
  - 범위와 상관없이 전원을 맞힘
  - 피해 출처를 Player 1로 고정함
- Unity와 같은 경계로 나눈 컴파일: 경고 0, 오류 0.
- Core에 "Mouse"가 나오지 않는다. 마우스는 Unity 호스트의 HostInput과 GameHost 배선에만 있다.
- 사용자 플레이 확인
  - Unity 재컴파일 시 오류 0건, EditMode 31개
  - 원이 마우스를 따라감, 원의 크기, 빈 틱 표시, 피격 표시
  - HP 0인 Enemy가 남음(M4 전 임시 상태)
  - 화면 밖 조준, 일시정지, 재시작

**발견**

- 첫 틱 0초는 피해로 관찰할 수 없다. 판 시작 때는 Enemy가 없기 때문이다. 그래서 틱 수(`TickCount`)를 읽기 전용으로 열었다. 계약은 이 값으로 첫 틱을 확인하고, 화면은 이 값으로 빈 틱까지 표시한다.
- 실행 중인 Skill이 보정된 수치를 쓰는지는 판을 거쳐 확인할 수 없다. 판에 보정의 출처가 없기 때문이다(미정). 계산기 단위로 확인했다. M2 Enemy와 같은 한계이며, 보정을 실제로 연결하는 실험은 M5에서 한다.
- 단계 안에서 출현이 Skill 뒤에 있다. 그래서 막 나온 Enemy는 다음 단계부터 맞는다. 단계는 1/30초 이하라 체감되지 않지만, M4에서 Death 판정을 단계 어디에 둘지 정할 때 같은 순서 문제가 생긴다.
- 같은 단계에서 여러 Skill이 차례로 피해를 준다. 앞 Skill이 HP를 0으로 만든 Enemy도 뒤 Skill이 또 때릴 수 있다. M3에서는 HP만 기록하므로 문제가 없고, M4의 "Death와 보상은 한 번"과 함께 정한다.
- 원 크기(표시)와 판정은 같은 반경을 쓴다. 다만 판정은 Enemy 중심 기준이라 원 가장자리에 걸친 큰 Enemy는 맞지 않을 수 있다. 크기 포함 판정으로 바꾸면 `IsInside` 한 곳만 바뀐다.

**계획과 달라진 점**

- `PassiveSkill.TickCount`와 `SceneSpace`를 추가했다. 둘 다 새 규칙이 아니다. 앞의 것은 관찰용이고, 뒤의 것은 변환을 한 곳에 모은 것이다.
- 일시정지 중에도 조준점은 갱신된다(원이 마우스를 따라감). 게임 진행이 멈춰 있어 규칙 효과는 없다.
- 새 [임시] 규칙: "원 안" = Enemy 중심 기준. 조준점이 없으면 그 틱은 빈 틱. PLAN 5절 표에 반영했다.
