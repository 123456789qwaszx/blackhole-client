# M4 — Death → Reward와 흡수 연출

상태: 계획 · 선행: M3 · 결정 D4 승인

## 목표

Enemy의 HP가 0 이하가 되는 순간 Death가 확정되고, 그 순간 Gold와 EXP가 지급된다. 게임 규칙상 Enemy는 그때 끝난다. 블랙홀로 빨려드는 것은 화면이 사망 기록을 읽어 그 뒤에 보여 주는 연출이다.

```text
Core:          HP <= 0 → Death 확정 → Reward → Enemy gameplay state에서 제거
               └→ 이번 단계의 DeathRecord를 남김
Presentation:  DeathRecord 읽기 → 죽은 Enemy의 View를 블랙홀로 빨아들임 → View 제거
```

연출을 위해 Enemy를 게임 규칙상 살아 있는 것처럼 붙들어 두지 않는다. 이것이 이번 마일스톤에서 꼭 검증할 경계다.

## 검증할 경계

- **B6 Death → Reward**
  - HP <= 0 → Death 확정 → 보상 지급이 한 흐름에서 일어난다.
  - 같은 Enemy의 Death와 보상은 한 번뿐이다. 같은 틱에 여러 피해가 겹쳐도 마찬가지다.
  - **보상은 현재 단일 Player에게 지급한다.** v2는 Player가 1명이므로 보상 귀속 정책이 필요 없다. 멀티플레이의 Kill·Reward 귀속 정책(막타, 전원, 기여도, 공용 재화, 거리 등)은 미정이다. "피해 출처 Player → Kill Owner → Kill Owner에게 보상" 같은 일반 규칙을 만들지 않는다.
- **B1 Player 단위**: Gold와 EXP는 전역 값이 아니라 Player의 PlayerState에 쌓인다. "현재 단일 Player에게 지급"은 한 곳에 명시적으로 둔다. Player가 2명 이상인 판에서는 귀속 정책이 없다는 사실이 코드에서 드러나야 한다(조용히 첫 Player에게 주지 않는다). 드러내는 방식(판 조립 시 거부 등)은 이 마일스톤에서 정하고 결과에 남긴다.
- **B7 Presentation 분리**
  - 게임 규칙상 Enemy는 Death 확정 때 끝난다. 더 이상 피해·행동·선택의 대상이 아니다.
  - 화면은 DeathRecord를 읽고, 사망 위치에서 HQ로 빨려드는 연출을 재생한 뒤 View를 지운다.
  - 연출이 느려지거나 생략돼도 보상은 바뀌지 않는다. View 제거 시점은 Death 시점과 다르다.

## 범위

- Enemy 생명주기: Alive → Dead. Death 확정은 Enemy(상태의 주인)가 판정한다.
- 보상: Enemy 종류 정의에 보상 Gold/EXP를 이번에 추가하고(M2에서는 쓰이지 않아 미뤘다), 현재 단일 Player의 PlayerState에 더한다.
- 게임 규칙상 제거: Death 확정 단계에 Enemy 목록에서 뺀다.
- DeathRecord: 이번 단계에 죽은 Enemy의 식별자, 종류, 사망 위치. 화면이 매 프레임 읽는다. 화면이 읽는 방향(v1 패턴)을 유지하고, 이벤트 구독은 만들지 않는다.
- 흡수 연출: 사망 위치 → HQ, 일정 시간, 끝나면 View 제거. 연출 값은 표현 정의에만 둔다.
- HUD: Player의 Gold/EXP 표시.

## 비범위

- Level 계산과 레벨업 결과(D4). EXP만 보관한다.
- Gold 사용처(강화, 상점).
- 멀티플레이 보상 귀속 정책. 피해 출처 Player는 M3에서 기록만 한다.
- 종류별 사망 효과(폭발, 분열 등).
- HQ 성장(D1).

## [임시] 값

- Enemy 종류별 보상 Gold/EXP.
- 흡수 연출의 길이와 경로(Presentation 전용).

## 이 마일스톤에서 정할 것

- **DeathRecord의 수명**: "이번 단계"의 기록인지 "이번 프레임"의 기록인지 정한다. 한 프레임에 여러 단계가 진행되면(시간 분할) 화면이 중간 단계의 사망을 놓치지 않아야 한다. 후보: 프레임(Advance 한 번) 동안 누적하고, 다음 Advance 시작 때 비운다.
- **여러 Player 판의 처리**: 위 B1의 "드러내는 방식".
  - 주의: 2명 판을 만드는 계약이 이미 있다. M1 `World.PlayersAreAListNotASingleton`과 M3 `Skill.AimPointBelongsToPlayer`, `Skill.EachPlayerHasOwnSkills`다. 이 계약들은 보상과 무관한 경계(Player 구별, 조준점 소유)를 본다. "판 조립 시 거부"를 택하면 이 계약들과 충돌한다. 노출 시점을 보상 지급 순간으로 둘지, 계약을 바꿀지 함께 정한다.
- **단계 안에서 Death 판정의 위치**(M3 재점검에서 추가): 지금 단계 순서는 이동 → Skill(Player 순서, Skill 순서) → 출현이다.
  - 후보 1: 피해를 받는 즉시 Enemy가 Death를 확정한다.
  - 후보 2: 모든 Skill이 끝난 뒤 한 번에 확정한다.
  - 어느 쪽이든 같은 단계의 뒤 Skill이 이미 HP 0인 Enemy를 또 때릴 수 있는지가 함께 정해진다. "Death와 보상은 한 번"과 "Dead Enemy는 피해 대상이 아니다"를 둘 다 만족해야 한다.

## 작업 순서

1. Enemy Death 전이와 판정.
2. 현재 단일 Player에게 보상 지급(PlayerState에 Gold/EXP).
3. 게임 규칙상 제거.
4. DeathRecord와 그 수명.
5. 흡수 연출과 View 제거.
6. HUD의 Gold/EXP.
7. 계약 테스트(Death, 보상 1회, 제거, DeathRecord, 연출 독립성).

## 완료 기준

- **자동 계약**
  - HP가 0 이하가 되는 단계에서 Death가 확정되고, 같은 단계에 Gold/EXP가 Player의 PlayerState에 지급된다.
  - 같은 틱에 여러 피해가 겹쳐도 Death와 보상은 한 번이다.
  - Dead Enemy는 목록에서 빠지고, 더 이상 피해·선택·행동의 대상이 아니다.
  - 한 프레임에 여러 단계가 진행돼도 그 프레임의 DeathRecord에 모든 사망이 남는다.
  - 화면 처리 없이도(연출 없음) 보상 결과가 같다.
  - Player가 2명 이상인 판에서 보상 정책이 없다는 사실이 드러난다(조용히 한 명에게 주지 않는다).
- **플레이 확인**: Enemy가 죽으면 즉시 Gold/EXP가 오르고, 죽은 Enemy는 사망 위치에서 HQ로 빨려 들어간 뒤 사라진다. 연출 중인 Enemy는 다시 맞지 않는다.

## 재점검 항목

- 보상이 연출의 끝(View 제거)에 묶이지 않았는가?
- 보상 귀속 규칙을 몰래 만들지 않았는가(피해 출처를 수령자로 쓰지 않는가)?
- 흡수 관련 값(속도, 경로)이 게임 규칙 쪽에 새지 않았는가?
- Level·Gold 사용처를 몰래 정하지 않았는가?

## 결과

(완료 후 작성)
