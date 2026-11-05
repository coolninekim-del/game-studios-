# Story 003: 화살 대응 타이밍 입력

> **Epic**: 나만의 기사단
> **Status**: In Review
> **Layer**: Core
> **Type**: Visual/Feel
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-04

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 5` — 화살 대응 타이밍 입력

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.6 | **Risk**: HIGH
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

**Status**: [x] Created — `production/qa/evidence/story-003-*.png` + `story-003-arrow-timing-input-evidence.md` (lead sign-off pending)

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`
- Unlocks: `production/epics/my-knight-order/story-007-run-structure.md`

---

## Implementation Notes (2026-11-04)

- Decisions (user, 2026-11-04): a Success reduces arrow damage by a data multiplier (0 = full block);
  volleys are fired by distance, not by a fixed tick.
- Core (Story 001 simulation, `BattleSimulation`): after movement, each living enemy unit whose arrow cooldown
  is 0 fires at its nearest player unit when that unit is `Charging` and its distance is in
  `(impact_range, arrow_range]`. `OnArrowVolley` is raised before damage; damage =
  `floor(arrow_damage * DamageMultiplier)`, applied together with the tick's impact damage.
  Data: `arrow_range`, `arrow_damage`, `arrow_interval_ticks` in `battle_rules.json` (0 range = no arrows).
- Timing: `ChargeTimingController` predicts volleys with the same `Clone()` lookahead as Story 002 and judges
  them with the same `ChargeTimingJudge.Judge`; only the window, cue lead and multiplier differ
  (`arrow_window_seconds`, `arrow_cue_lead_seconds`, `arrow_success_damage_multiplier` in
  `battle_charge_timing.json`). Event `OnArrowJudged`; pending events carry a `TimingCueKind`.
- One tap input for both kinds: a tap goes to the soonest visible untapped cue (all cues of that tick). An
  arrow and a charge on the same tick are judged each against its own window (EditMode test:
  a tap between the two windows is arrow Success + charge Early).
- View: an arrow flies from the archer and lands at the resolve moment; cyan shrinking ring on the target;
  BLOCK / EARLY / ARROW HIT popups and separate tones; the timing bar labels the soonest cue's kind.
- Sample data: each enemy targets the player unit in its row, so one volley group is one volley per lane; a
  sample battle has two volley groups (arrows only fly at charging units).
- Not verified: a real mouse click / real touch device on the built player (scripted taps only); audio.
