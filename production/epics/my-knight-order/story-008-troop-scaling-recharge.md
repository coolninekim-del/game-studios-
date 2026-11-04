# Story 008: 병력 규모 반영과 재돌격 사이클

> **Epic**: 나만의 기사단
> **Status**: In Progress
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-04

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 3` — 오토배틀 전투(기마돌격 충돌)의 확장. Story 001 종료 시(2026-11-03) 사용자가 요청한 두 가지: ① 병력이 많을수록 강해져야 함, ② 물러났다가 다시 돌격하는 사이클.

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 전투, 돌격 충돌 타이밍 입력; MVP 3) and the user's Story 001 close request, scoped to this story:*

- [ ] 충돌 피해가 공격 측 병력 수에 따라 커진다. 다른 조건이 같으면 병력이 많은 쪽이 더 큰 피해를 준다.
- [ ] 병력 수 → 피해 배율 곡선의 파라미터는 외부 데이터이며, 선형부터 증가폭이 줄어드는 형태까지 데이터만으로 조정할 수 있다.
- [ ] 충돌 후 기마 유닛은 이탈해 데이터로 정한 거리만큼 물러났다가 다시 돌격한다.
- [ ] 제자리 공방으로 전환되기 전까지의 돌격 횟수는 외부 데이터 `charges_before_melee`로 정한다. 값이 1이면 Story 001과 같은 동작(첫 충돌 후 제자리 공방)이다.
- [ ] 재돌격으로 생기는 충돌마다 `OnChargeImpact`가 피해 적용 전에 발생해, Story 002가 매 충돌에 타이밍 입력을 걸 수 있다.
- [ ] 이탈 거리 등 사이클 수치는 모두 외부 데이터다.
- [ ] 변경 후에도 전투는 항상 종료되고(`max_ticks` 상한 유지), 같은 입력이면 같은 결과가 나온다.

---

## Implementation Notes

- 병력 배율 예: `(troops / reference_troops) ^ troop_exponent` — `reference_troops`, `troop_exponent`는 데이터(지수 1 = 선형, 1 미만 = 체감). 다른 형태가 더 단순하면 보고 후 결정.
- 유닛에 돌격 단계 상태(접근 → 충돌 → 이탈 → 재접근 → 제자리 공방)를 둔다. 순수 C#, UnityEngine 의존 금지(Story 001과 동일).
- 위치는 논리 격자 좌표만 사용(픽셀아트 아이소메트릭 투영은 표현 계층의 일).
- 새 데이터 키는 `BattleDataLoader`에서 검증한다.

---

## Out of Scope

- Story 002: 충돌 타이밍 입력과 판정
- Story 003: 화살 이벤트
- Story 006: 병력 보충·승급(내정)

---

## Files

- `Assets/Scripts/Battle/Core/BattleSimulation.cs`
- `Assets/Scripts/Battle/Core/BattleUnit.cs`
- `Assets/Scripts/Battle/Core/BattleRules.cs`
- `Assets/Scripts/Battle/Loading/BattleDataLoader.cs`
- `Assets/Data/Battle/battle_rules.json`
- (테스트 파일 없음 — `qa.level: minimal`에서 면제)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: waived at `qa.level: minimal` (advisory).

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`
- Unlocks: `production/epics/my-knight-order/story-002-charge-timing-input.md`, `production/epics/my-knight-order/story-003-arrow-timing-input.md` (008을 002보다 먼저 진행)
