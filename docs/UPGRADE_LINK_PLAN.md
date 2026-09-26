# UPGRADE LINK PLAN — 업그레이드 화면과 전투를 한 루프로 잇고, 판이 업그레이드 표를 받게 하기

작성일: 2026-09-26

브랜치: `feature/업그레이드연결` (`d512756`: 노드 트리 + 업그레이드 + 적 시스템)

기준 문서: [SYSTEM_CATALOG](SYSTEM_CATALOG.md) F01·F02·F07 · [NODE_SYSTEM_SURVEY](NODE_SYSTEM_SURVEY.md) 9절 · `feature/노드트리`의 NODE_TREE_SCREEN_PLAN(업그레이드 화면과 콘솔, 플레이 확인 완료)

| 표기 | 뜻 |
|---|---|
| 사용자 | 사용자의 요구와 결정 |
| 원작 | 원작 관찰(REFERENCE_ANALYSIS, SKILL_TREE_PLAN 4.1) |
| 제안 | 이 PLAN이 정한 것. 사용자가 바꾸면 따른다 |
| 미정 | 결정 필요 |

## 1. 목표

1. `feature/노드트리`에서 확인한 업그레이드 화면·개발용 콘솔·선 목록·치트를 이 브랜치로 가져온다. 적·전투·업그레이드는 그대로 둔다.
2. 업그레이드 화면과 전투를 원작처럼 한 루프로 잇는다: 업그레이드 화면 → 전투 → 전투 끝 → 업그레이드 화면 [원작].
3. 판을 조립할 때 참가자마다 산 노드로 업그레이드 표(`UpgradeTable`)를 한 번 만들어 판이 들고 있게 한다. 표를 읽는 시스템은 잇지 않는다 [사용자: 가져가는 시스템 연결은 모든 시스템이 완성된 뒤].

## 2. 범위

| 한다 | 하지 않는다 |
|---|---|
| 노드트리 브랜치의 화면·콘솔·치트·선 목록 가져오기 | 표를 읽는 시스템(적 수치, 스킬 수치) 잇기 |
| 화면 전환: 업그레이드 ↔ 전투 | 처치 보상·결산(Gold 벌기)의 합류 (`feature/처치보상`) |
| 판 조립이 참가자별 업그레이드 표를 만들어 판이 들고 있기 | 참가자 여럿일 때 판 공유 수치에 누구의 표를 쓸지(F06 [미정]) |
| 이 판의 업그레이드 표를 개발용 콘솔에 보이기 | 저장(F04), 최종 아트 |

## 3. 가져오는 방법 [제안]

`feature/노드트리`를 **병합하지 않는다.** 그 브랜치의 정리 커밋(`265c91a`)이 적·전투·업그레이드를 지웠기 때문에, 병합하면 이 브랜치에서도 지워진다.

대신 더하는 커밋 둘만 이 브랜치에 다시 적용한다(cherry-pick 후 충돌을 손으로 푼다).

| 커밋 | 가져오는 것 | 이 브랜치에서 맞출 것 |
|---|---|---|
| `279591d` (NS-002) | `NodeGraph.Links`, `ProgressCheats`, 계약 2개 | `PlayerState`에는 전투 중 여부(`InBattle`)가 있다. 치트는 전투 중에 거부한다(5절) |
| `23cdd70` (NS-003~005) | `NodeTreeView`, `UpgradeScreen`, `ScreenFlow.Upgrade`, `UpgradeConsole`, 임시 화면 | 화면 흐름·임시 화면·GameHost에는 전투 쪽이 그대로 있다. 둘을 함께 둔다. 노드 목록 콘솔(`NodeConsole`)은 업그레이드 화면으로 대신하므로 지운다 |

`NODE_TREE_SCREEN_PLAN`은 `feature/노드트리`의 기록으로 두고 이 브랜치로 옮기지 않는다. 이 PLAN이 이 브랜치의 기준이다.

## 4. 화면 루프 [원작, 제안]

```text
시작 ─▶ 업그레이드 화면 ──(Start battle)──▶ 오케스트레이터 시작 ─▶ 전투 화면
            ▲                                                      │
            └──── 오케스트레이터 정리 완료 ◀── 시간 종료 / End battle ─┘
```

- **화면은 전투 시스템의 상태를 따른다.** 판이 돌고 있으면 전투 화면, 판이 없으면(정리까지 끝나면) 업그레이드 화면이다. 전투 시작·종료 콘솔로 시작하거나 끝내도 같은 규칙으로 바뀐다.
- 업그레이드 화면에 `Start battle` 버튼을 더한다. 누르면 오케스트레이터에 시작을 요청할 뿐이다(순서와 책임은 오케스트레이터에 있다).
- 전투 화면의 `End battle`과 시간 종료는 지금처럼 오케스트레이터가 정리한다. 정리가 끝나면 업그레이드 화면으로 돌아간다.
- 원작은 시간이 끝나면 결산 화면을 거쳐 트리로 간다. 결산 화면은 처치 보상이 합류할 때 더한다.

## 5. 진행 상태와 전투 [제안]

- 이 브랜치에는 "진행 상태는 전투 밖에서만 바뀐다"는 규칙이 있다(`InBattle`, 전투 중에는 살 수 없다).
- 치트도 같은 규칙을 따른다: 전투 중에는 전체 해금·전체 잠금·Gold 빼기를 거부한다. 콘솔 버튼도 전투 중에는 꺼진다.
- Gold 더하기(`EarnGold`)는 결산이 부르는 입구라 규칙상 전투 밖에서만 부른다. 콘솔의 `+` 버튼도 전투 중에는 꺼진다.

## 6. 판이 업그레이드 표를 받는 자리

```text
진행 상태(산 노드) ─┐
노드 트리 ──────────┼─▶ SessionAssembler.CreateBattle ─▶ GameSession.UpgradesOf(PlayerId) : UpgradeTable
콘텐츠 ─────────────┘        (참가자마다 NodePurchase.UpgradesFor를 한 번)      판이 끝날 때까지 같다
```

- 판 조립이 노드 트리를 받아, 참가자마다 산 노드의 업그레이드로 표를 **한 번** 만든다. 판(`GameSession`)이 참가자 ID로 표를 내준다.
- 표는 판이 끝날 때까지 바뀌지 않는다. 전투 중에는 살 수 없으니(5절) 산 노드도 바뀌지 않는다.
- 노드 트리를 넘기지 않는 조립(지금의 계약들)은 빈 표를 받는다. 기존 계약이 그대로 통과한다.
- `BattleSystem`의 시작 단계 첫 칸("Receive upgraded stats")이 이 일을 한다. 오케스트레이터와 GameHost는 노드 트리를 넘길 뿐이다.
- **참가자별 표**다. 적처럼 판에 하나뿐인 수치에 누구의 표를 쓸지는 표를 읽는 시스템을 이을 때 정한다(F06 [미정]).
- 확인용: 전투 시작·종료 콘솔에 이 판의 업그레이드 표를 보여 준다. 수치마다 `기본값 0 → a, 기본값 1 → b`로 적는다(노드 도구의 미리보기와 같은 방식). 가져가는 시스템의 기본값은 아직 모른다.

## 7. 코드 구조

| 곳 | 파일 | 바뀌는 것 |
|---|---|---|
| Core | `NodeGraph`, `Players/ProgressCheats`, `PlayerState` | 가져오기(3절). 치트는 전투 중에 거부 |
| Core | `Session/SessionAssembler`, `Session/GameSession` | 노드 트리를 받아 참가자별 표를 만들고, 판이 `UpgradesOf(PlayerId)`로 내준다 |
| Unity | `Screens/NodeTreeView`, `Screens/UpgradeScreen`, `Flow/ScreenFlow.Upgrade`, `Console/UpgradeConsole` | 가져오기. 업그레이드 화면에 `Start battle` 버튼 |
| Unity | `Flow/ScreenFlow` | 전투 시스템 상태를 따라 두 화면을 바꾼다 |
| Unity | `Battle/BattleSystem`, `Battle/BattleOrchestrator` | 노드 트리를 받아 조립에 넘긴다 |
| Unity | `Console/BattleLifecycleConsole` | 이 판의 업그레이드 표를 보인다 |
| Unity | `Screens/PlaceholderScreens`, `GameHost` | 두 화면을 만들고, 시작 화면을 업그레이드 화면으로 |
| Unity | `Console/NodeConsole` | 지운다(업그레이드 화면이 대신한다) |

## 8. 계약

새로 보장하는 동작만 더한다.

- 가져온 것: `Node.GraphListsEachLinkOnce`, `Progress.CheatsBypassPurchaseButGoldStaysNonNegative`.
- `Progress.CheatsAreRefusedDuringBattle`: 전투 중에는 치트가 진행 상태를 바꾸지 않는다.
- `Session.UpgradesAreFixedPerParticipantAtAssembly`: 판 조립 때 참가자마다 자기 산 노드로 표가 만들어지고, 판이 끝날 때까지 같다. 노드 트리 없이 조립하면 빈 표다.

화면 전환은 Unity 쪽이라 플레이로 확인한다.

## 9. 확인 방법

1. CoreSmoke 계약 통과, Unity 밖 컴파일(Core·Sample·Unity·Authoring·Editor), Unity 에디터 로그의 빌드 성공.
2. 플레이 (사용자):
   - 업그레이드 화면으로 시작한다. 콘솔로 Gold를 넣고 노드를 몇 개 산다.
   - `Start battle` → 전투 화면으로 바뀌고 적이 나온다. 전투 시작·종료 콘솔에 이 판의 업그레이드 표(산 노드의 수치)가 보인다.
   - 전투 중에는 업그레이드 콘솔 버튼이 꺼져 있다.
   - 시간 종료 또는 `End battle` → 정리 뒤 업그레이드 화면으로 돌아온다. 산 노드와 Gold는 그대로다.
   - 노드를 더 사고 다시 시작하면, 새 판의 업그레이드 표에 반영된다.

## 10. 티켓

| 티켓 | 내용 | 선행 | 상태 |
|---|---|---|---|
| UL-001 | 가져오기: NS-002·NS-003~005를 이 브랜치에 적용, 노드 목록 콘솔 지우기, 치트의 전투 중 거부 | — | |
| UL-002 | 화면 루프: 업그레이드 ↔ 전투, `Start battle` 버튼 | UL-001 | |
| UL-003 | 판이 업그레이드 표를 받는 자리, 전투 시작·종료 콘솔 표시, 계약 | UL-001 | |
| UL-004 | 문서(SYSTEM_CATALOG F02·F07 상태), 확인 | UL-002·003 | |

## 11. 남은 결정

| 항목 | 지금 | 정할 때 |
|---|---|---|
| 판 공유 수치(적·공급)에 누구의 표를 쓸지 | 참가자별 표만 만든다 | 표를 읽는 시스템을 이을 때 (F06) |
| 결산 화면 | 없음 (전투 끝 → 바로 업그레이드 화면) | 처치 보상 합류 |
| 표를 읽는 시스템과 수치 이름 | 없음 (노드의 `breaker.*`는 [임시] 이름) | 모든 시스템이 완성된 뒤 |
