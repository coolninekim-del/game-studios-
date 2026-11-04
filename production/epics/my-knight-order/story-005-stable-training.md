# Story 005: 말 중심 내정(말·주인공 육성)

> **Epic**: 나만의 기사단
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 1` — 말 중심 내정 화면(말·주인공 육성)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.6 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 내정 — 말의 성장이 돌격 위력과 일기토 능력에 직접 반영; MVP 1; Build order 3; Who it's for: 성장의 쾌감), scoped to this story:*

- [ ] 내정 화면에서 말과 주인공의 능력치를 확인하고, 각각을 올리는 육성 행동을 선택할 수 있다.
- [ ] 내정 1회에 쓸 수 있는 자원·행동 수가 제한되며, 그 한도는 외부 데이터 값이다.
- [ ] 말 능력치 변화가 다음 전투의 돌격 위력(Story 001)과 충돌 판정 구간 폭(Story 002)에 반영된다.
- [ ] 말과 주인공의 능력치 변화가 다음 일기토의 결투 수치(Story 004)에 반영된다.
- [ ] 육성 직후 성장 전후 수치가 함께 표시되어 성장이 체감된다.

---

## Implementation Notes

- 말·주인공 능력치는 하나의 데이터 모델로 두고, 전투·일기토가 같은 모델을 읽는다.
- 능력치 → 전투/일기토 수치 변환식은 외부 데이터로 둔다.

---

## Out of Scope

- Story 006: 병력 육성
- 기사단 소속 영웅 육성(브리프 Out of scope, 후순위)

---

## Files

- `Assets/Scripts/Stable/`
- `Assets/Scripts/UI/Stable/`
- `Assets/Data/Stable/`
- `Assets/Tests/EditMode/Stable/StableIntegrationTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/Tests/EditMode/Stable/StableIntegrationTests.cs` OR playtest doc — advisory at `qa.level: minimal`. 내정 화면이 바뀌므로 화면 스크린샷을 `production/qa/evidence/`에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`, `production/epics/my-knight-order/story-002-charge-timing-input.md`, `production/epics/my-knight-order/story-004-boss-duel.md`
- Unlocks: `production/epics/my-knight-order/story-007-run-structure.md`
