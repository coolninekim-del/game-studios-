# Story 006: 병력 육성

> **Epic**: 나만의 기사단
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 2` — 병력 육성(수량·등급 한 축만)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 내정 — 병력 편성·보강; MVP 2; Out of scope: 병과 다양화), scoped to this story:*

- [ ] 병력은 수량과 등급 두 값만 가진다(병과 구분 없음).
- [ ] 내정에서 병력을 보충(수량 증가)하거나 승급(등급 증가)할 수 있으며, 비용은 외부 데이터 값이다.
- [ ] 전투에서 잃은 병력 수가 다음 내정까지 유지된다.
- [ ] 등급이 오르면 전투 수치(Story 001)가 외부 데이터에 정의된 만큼 증가한다.
- [ ] 병력 수량이 0이면 기사단 전멸 상태를 보고한다(Story 007의 실패 조건).

---

## Implementation Notes

- 병력 상태와 보충·승급 계산은 순수 C# 클래스로 둔다.
- 자원이 부족하면 보충·승급이 거부되고 상태가 변하지 않는다.

---

## Out of Scope

- Story 005: 말·주인공 육성
- 병과 다양화(브리프 Out of scope)

---

## Files

- `Assets/Scripts/Troops/`
- `Assets/Data/Troops/`
- `Assets/Tests/EditMode/Troops/TroopTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Troops/TroopTests.cs` — advisory at `qa.level: minimal`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`
- Unlocks: `production/epics/my-knight-order/story-007-run-structure.md`
