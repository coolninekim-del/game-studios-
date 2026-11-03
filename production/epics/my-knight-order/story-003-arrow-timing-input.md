# Story 003: 화살 대응 타이밍 입력

> **Epic**: 나만의 기사단
> **Status**: Ready
> **Layer**: Core
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 5` — 화살 대응 타이밍 입력

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 전투 — 화살 타이밍에 맞춰 입력; MVP 5), scoped to this story:*

- [ ] 기마돌격 중 적 화살이 날아오는 이벤트가 발생하고, 도달 직전에 판정 구간이 표시된다.
- [ ] 판정 구간 안에서 화살을 탭(PC는 클릭)하면 화살 피해를 막거나 줄인다.
- [ ] 실패 또는 미입력 시 돌격 병력이 외부 데이터에 정의된 화살 피해를 입는다.
- [ ] 화살 판정과 돌격 충돌 판정(Story 002)이 겹쳐도 각각 독립적으로 판정된다.
- [ ] 성공/실패가 시각·청각 피드백으로 즉시 표시되고, 터치와 클릭이 같은 입력으로 동작한다.

---

## Implementation Notes

- Story 002의 판정 로직을 재사용한다(판정 구간 폭·배율만 데이터로 다름).
- 화살 이벤트 발생 시점과 피해량은 외부 데이터.

---

## Out of Scope

- Story 002: 돌격 충돌 판정
- 궁병 병과 자체(병과 다양화는 Out of scope)

---

## Files

- `Assets/Scripts/Battle/Timing/`
- `Assets/Data/Battle/`
- `Assets/Tests/EditMode/Battle/ArrowTimingTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: 화살 판정 구간과 피드백의 retained screenshot in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-003-arrow-timing-input-evidence.md` (not waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`
- Unlocks: `production/epics/my-knight-order/story-007-run-structure.md`
