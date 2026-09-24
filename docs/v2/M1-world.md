# M1 — 월드 뼈대: Session, Content, HQ, Player

상태: 완료(2026-09-24) · 선행: M0 · 결정 D1·D2 승인

## 목표

아무 Enemy도 Skill도 없는 한 판이 시작하고, 시간이 흐르고, 끝나고, 다시 시작한다. 그 판 안에 HQ와 Player(1명)가 있다. 이후 모든 시스템이 올라갈 바닥을 만든다.

## 검증할 경계

- **B1 Player 단위**: 판은 Player **목록**을 가진다. `SinglePlayerGame`이 아니라 `Players.Count == 1`인 게임이다. Player마다 PlayerState를 가진다. "현재 Player" 같은 전역 참조를 두지 않는다.
  - 코드에는 `Player └─ PlayerState`까지만 둔다. PlayerCharacter·CharacterDefinition·외형·클래스는 지금 게임플레이 책임이 없으므로 빈 클래스로도 만들지 않는다. "Player는 미래에 Character를 소유할 수 있다"는 GAME_RULES에만 남긴다.
- **B2 HQ 기준점**: HQ는 판 안의 대상이고 위치를 가진다. 다른 시스템은 원점이 아니라 HQ 위치를 읽는다. HQ와 Player는 다른 존재다.
- **B8 Session**: 시작 → 진행(시간 분할) → 종료 판정 → 결과 확정 → 재시작(새로 조립).
- **B9 Content**: 판 설정(시간 제한, HQ 위치)의 정의와 검증. 오류는 경로와 함께 모으고, 오류가 있으면 판을 시작하지 않는다.

## 범위

- Session: 시작, 일시정지, 종료, 재시작, 요청 허용 여부. 시간 분할은 v1 패턴(진행 전 제한 → 진행 → 진행 후 판정)을 다시 쓴다.
- 시간제 종료 판정 하나. 종료 조건을 바꿀 수 있는 자리를 두되, 두 번째 조건은 만들지 않는다.
- HQ: 위치만 가진다.
- Player 목록(1명), 각 Player의 식별자와 PlayerState(이 단계에서는 거의 비어 있다).
- Content: 판 설정 정의 + 로더 + 진단. v1의 방식을 간소화해 다시 쓴다. 샘플 콘텐츠는 `SampleContent`처럼 이름부터 샘플임을 드러내고 Core 규칙과 분리한다(D2).
- 호스트: 판 시작·종료·재시작, 비활성화 시 판 종료. 화면에는 HQ 원판과 남은 시간 정도만 둔다.

## 비범위

- Enemy, Skill, 보상, 연출.
- HQ 성장(D1), HQ HP.
- PlayerCharacter·Character 코드(빈 클래스 포함).
- 입력은 재시작·일시정지 정도만. 조준점(AimPoint)은 M3에서 다룬다.

## [임시] 값

- 한 판 길이(예: 60초).
- HQ 위치(월드 중앙).

## 작업 순서

1. Content: 판 설정 정의, 로더, 진단(최소).
2. Session과 시간 분할, 시간제 종료 판정.
3. HQ와 Player 목록·PlayerState.
4. 판 조립 진입점 하나(정의는 공유, 실행 상태는 판마다 새로).
5. 호스트: 시작·재시작 흐름, 최소 화면.
6. 계약 테스트(Session, Content, Player/HQ).

## 완료 기준

- **자동 계약**
  - 제한 시간을 넘겨 진행하지 않고, 종료 뒤 요청이 거절되며, 결과가 한 번만 확정된다.
  - 재시작한 판은 이전 판의 상태를 갖지 않는다. 같은 정의로 만든 두 판이 상태를 공유하지 않는다.
  - Player가 목록으로 존재하고, 각자 PlayerState를 가진다. Player를 찾을 때 "첫 번째 Player" 같은 전역 가정을 쓰지 않는다.
  - HQ 위치를 바꾼 정의로 판을 만들면 HQ가 그 위치에 있다.
  - 잘못된 판 설정은 경로와 함께 보고되고 판이 시작되지 않는다.
- **플레이 확인**: 판이 시작되고, 남은 시간이 줄고, 끝나고, 재시작된다. 컴포넌트를 껐다 켜면 새 판이 한 번만 시작된다.

## 재점검 항목

- Player 목록이 실제로 전역 가정 없이 쓰이는가? 호출하는 쪽이 늘 `[0]`을 쓰고 있지 않은가?
- HQ가 "원점" 상수로 새고 있지 않은가?
- Session이 M2 이후 시스템을 끼울 자리(단계마다의 진행 순서)를 가졌는가?

## 결과

완료: 2026-09-24

**한 일**

- Core
  - `Content/`: ContentData(저작 형식) → ContentLoader(경로별 진단, 부분 통과 금지) → GameContent. 정의 생성자의 규칙을 로더가 그대로 호출한다(DefinitionGuard).
  - `Session/`: GameSession(판 상태·결과·요청 허용), SessionRunner(경과 시간, 시간 분할), TimeLimitDefinition·TimeLimitRule(진행 전 LimitStep, 진행 후 TryEnd), SessionAssembler(조립 진입점).
  - `Hq/`: HqDefinition·Hq(위치만).
  - `Players/`: PlayerId, Player, PlayerState(비어 있음).
  - `World/`: World(HQ, Player 목록, 한 단계의 처리 순서 — 지금은 비어 있음).
- Sample: 별도 어셈블리 `BlackHole.Sample`의 SampleContent. [임시] 값(60초, HQ (0, 0))은 여기에만 있다.
- Unity: GameHost(조립 루트·수명·프레임 순서), SessionLauncher(판 교체, 참가자 목록을 받음), HostInput(R/P), Hud(IMGUI), WorldView(HQ 원판, 카메라가 HQ를 봄), SamplePresentation(표현 값).
- 계약 12개: Harness 1, Content 3, Session 5, World 3.

**검증**

- CoreSmoke 12개 통과. 진행 전 제한을 일부러 제거하면 `Session.NeverAdvancesPastTimeLimit`가 실패하는 것을 확인했다.
- Unity와 같은 경계로 나눈 컴파일(Core, Sample, Tests, Unity): 경고 0, 오류 0.
- 사용자 플레이 확인: HUD 표시, 일시정지, 종료·결과, 재시작, 컴포넌트 재활성화, EditMode 12개 통과, Console 오류 없음.

**발견**

- 카메라가 월드 원점에 고정되어 있었다. 원점 가정이 화면 쪽에 남아 있던 것이다. 가이드 5절("HQ = 게임의 시각적 중심")에 맞게 WorldView가 카메라를 HQ 위치에 맞추도록 고쳤다.
- 누가 참가하는지는 콘텐츠가 아니라 판 설정이다. SessionAssembler가 참가 Player 목록을 인자로 받고, 호스트(GameHost)가 로컬 1명을 넘긴다. 비었거나 중복된 참가자는 조립 오류다.
- 게임 코드와 호스트 코드에 `[0]`이나 "첫 Player"로 고르는 곳이 없다. Player는 Id로 찾는다(`World.TryGetPlayer`).
- 시간 분할은 지금 관찰할 대상이 없다(World.Step이 비어 있음). 경과 시간 잘라내기만 계약으로 확인된다. 단계 분할의 효과는 M2의 Enemy 이동에서 확인한다.
- PlayerState는 비어 있다. 사용자 지시대로 `Player └─ PlayerState` 구조를 두었고, M4에서 Gold/EXP가 들어온다.

**계획과 달라진 점**

- 샘플 분리를 이름·폴더가 아니라 **별도 어셈블리**로 강제했다(D2 강화). Core가 샘플 값을 참조하면 컴파일되지 않는다.
- 카메라가 HQ를 보도록 했다(계획에 없던 원점 가정 제거).
