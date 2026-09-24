# M0 — v1 정리와 빈 뼈대

상태: 완료(2026-09-24) · 선행: 전체 PLAN 승인

## 목표

v1 코드를 보존한 채 작업 트리에서 치우고, v2가 올라갈 빈 뼈대를 만든다. 이 마일스톤에는 게임 규칙이 없다.

## 범위

1. **v1 보존**
   - v1 코드가 남아 있는 마지막 커밋에 태그 `reference-v1`을 달고 원격에 올린다.
   - v1 문서를 `docs/v1/`로 옮긴다: `SYSTEM_CATALOG.md`, `systemization-baseline.md`, `REFERENCE_IMPLEMENTATION.md`. 문서 안의 상대 링크를 고친다.
   - `GAME_RULES.md`는 `docs/`에 그대로 둔다.
2. **v1 코드 제거**
   - `Assets/BlackHole/`의 Core·Unity·Tests 코드와 `.meta`를 지운다.
   - 씬(`SampleScene.unity`)에서 v1 컨트롤러 참조를 지운다. 씬 파일은 Unity 에디터에서 정리하는 것이 안전하므로 방법을 사용자와 정한다.
3. **빈 뼈대**
   - 어셈블리 두 개: `BlackHole.Core`(엔진 참조 없음), `BlackHole.Unity`(호스트, Core와 Input System 참조). 테스트 어셈블리 하나.
   - 계약 테스트 실행기: `tests/CoreSmoke`를 v2 경로에 맞춘다. 계약은 **시스템별 파일**로 나눈다(v1은 한 파일 1,000줄이었다).
   - CI(`Core contracts`)가 새 경로로 돈다.
   - 빈 씬에서 호스트 컴포넌트 하나가 켜지고 꺼지는 것까지만 확인한다.

## 비범위

- 게임 규칙, 콘텐츠, 화면 요소.
- v1 코드의 부분 이식. 패턴은 M1부터 필요한 곳에서 다시 쓴다.

## 작업 순서

1. 태그 `reference-v1` 생성.
2. v1 문서를 `docs/v1/`로 이동하고 링크를 고친다.
3. v1 코드를 삭제한다(`.meta` 포함).
4. 빈 어셈블리 정의, 테스트 어셈블리, CoreSmoke 경로, CI를 정리한다.
5. 씬의 v1 컴포넌트 참조를 제거하고 빈 호스트를 둔다.

## 결정(2026-09-24)

- 씬 정리는 **에디터에서** 한다. v1 코드를 지운 뒤 사용자가 누락 스크립트를 지우고 빈 호스트를 붙여 씬을 저장한다.
- `reference-v1` 태그는 **원격에도 올려** 보존한다.

## 완료 기준

- `git show reference-v1`로 v1 코드와 문서를 볼 수 있다.
- 작업 트리에 v1 코드가 없다. v1 문서는 `docs/v1/`에 있고 링크가 맞다.
- Unity에서 컴파일 오류가 없고, 씬에 누락 스크립트가 없다.
- CoreSmoke와 CI가 새 경로에서 실행된다(계약 0개여도 성공).

## 재점검 항목

- 뼈대에 v1의 임의 가정(원점 = 블랙홀, 단일 Player 필드 등)이 남지 않았는가?
- 폴더·어셈블리 구조가 M1~M4의 경계(B1~B9)를 담기에 맞는가?

## 결과

완료: 2026-09-24

**한 일**

| 커밋·태그 | 내용 |
|---|---|
| 태그 `reference-v1` (`165fb44`) | v1 코드가 남아 있는 마지막 커밋. 원격에 올렸다 |
| `5d3acb3` | v1 문서 3개를 `docs/v1/`로 이동, 링크 정리. GAME_RULES 4절을 "v1 Reference와 다른 점"으로 변경 |
| `b8fcbb2` | v1 코드 삭제. 어셈블리 정의 3개는 GUID와 함께 유지. 계약 등록부(`Contracts`), 판정 도우미(`Expect`), 실행기 자체 확인 계약 1개, EditMode 래퍼(`ContractTests`), 빈 호스트(`GameHost`), CoreSmoke 경로 갱신. 사용자가 에디터에서 v1 누락 스크립트를 지우고 `GameHost`를 붙인 씬과 Unity가 만든 ProjectSettings 파일을 이 커밋에 합쳤다(amend) |

**검증**

- `git show reference-v1`로 v1을 볼 수 있다. 작업 트리에 v1 코드가 없다.
- CoreSmoke: 1개 통과. Unity와 같은 경계로 나눈 컴파일: 경고 0, 오류 0(Core에 스크립트가 없는 상태 포함).
- 사용자 확인: 컴파일 오류 없음, Play 시 `[GameHost] enabled` / `disabled`, EditMode 테스트 통과.
- 씬 파일에 v1 컨트롤러 GUID가 없고 `GameHost` GUID가 있다.
- CI(`Core contracts`)는 경로가 그대로라 push 후 실행될 것이다. 아직 push하지 않아 원격 실행은 확인 전이다.

**발견**

- 스크립트가 없는 어셈블리(`BlackHole.Core`)는 Unity가 빌드하지 않지만, 참조하는 어셈블리에 오류를 내지 않았다.
- 씬 저장 때 Unity 6이 씬을 새 형식으로 다시 직렬화해 차이가 크다(126줄). 내용상 변경은 컴포넌트 교체와 이름뿐이다.
- Unity가 `ProjectSettings/SceneTemplateSettings.json`을 새로 만들었다. 사용자가 씬과 함께 커밋했다.

**계획과 달라진 점**: 없음.
