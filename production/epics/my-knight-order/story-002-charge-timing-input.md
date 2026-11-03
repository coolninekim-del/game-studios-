# Story 002: 돌격 충돌 타이밍 입력

> **Epic**: 나만의 기사단
> **Status**: Ready
> **Layer**: Core
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 4` — 돌격 충돌 타이밍 입력

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 전투 — 타이밍에 맞춰 입력하면 결과가 좋아진다; MVP 4), scoped to this story:*

- [ ] 돌격 충돌 직전에 판정 구간이 화면에 표시된다.
- [ ] 판정 구간 안에서 탭(PC는 클릭)하면 성공 판정, 구간 밖 입력이나 미입력은 기본 처리된다.
- [ ] 성공 판정 시 해당 충돌의 돌격 위력에 외부 데이터의 배율이 적용된다.
- [ ] 성공/실패가 시각·청각 피드백으로 즉시 표시된다.
- [ ] 판정 구간 폭은 외부 데이터 값이며, 말 능력치로 보정할 수 있는 훅을 둔다(Story 005에서 연결).
- [ ] 터치와 마우스 클릭이 같은 입력으로 동작한다.

---

## Implementation Notes

- 입력은 새 Input System 사용(레거시 Input Manager 금지 — `docs/engine-reference/unity/VERSION.md` Deprecated Systems).
- 판정 로직(구간 안/밖 판단, 배율 적용)은 순수 C#으로 분리해 테스트 가능하게 한다.
- Story 001의 `OnChargeImpact` 이벤트에 연결한다.

---

## Out of Scope

- Story 003: 화살 대응 입력
- Story 005: 말 능력치에 따른 판정 구간 보정 값

---

## Files

- `Assets/Scripts/Battle/Timing/`
- `Assets/Tests/EditMode/Battle/ChargeTimingTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: 판정 구간 표시와 성공/실패 피드백의 retained screenshot in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-002-charge-timing-input-evidence.md` (not waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`
- Unlocks: `production/epics/my-knight-order/story-005-stable-training.md`
