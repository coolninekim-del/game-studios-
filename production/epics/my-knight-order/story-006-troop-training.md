# Story 006: 병력 육성

> **Epic**: 나만의 기사단
> **Status**: In Review
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-05

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 2` — 병력 육성(수량·등급 한 축만)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.6 | **Risk**: HIGH
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

**Status**: [x] Created — `Assets/Tests/EditMode/Troops/TroopTests.cs` (24 cases), full EditMode run 89/89 passed

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`
- Unlocks: `production/epics/my-knight-order/story-007-run-structure.md`

---

## Implementation Notes (2026-11-05)

- Decisions (user, 2026-11-05): troops are paid for with gold; each of the three foot squads has its own troop
  count and grade; this story is logic and tests only (no screen, no scene change). The rules, numbers and file
  list below were approved as drafted.
- Roster (`troops_start_roster.json`): gold 100; three squads of 100 troops at grade 1. Squad i is the i-th foot
  (non-mounted) player unit of the battle setup.
- Rules (`troops_rules.json`): reinforcing costs `reinforce_gold_per_troop` (1) per soldier, up to
  `max_troops_per_squad` (150); promoting from grade 1 / 2 / 3 costs `promote_gold_costs` 60 / 120 / 200, so the
  highest grade is 4. A request without enough gold, above the squad maximum, at the highest grade or with a bad
  squad / count is refused with a `TroopActionResult` reason and changes nothing.
- Battle (`TroopDeployment.Deploy`): the setup's foot-unit values are grade 1 (power 32, defense 10 in the
  sample); grade g gives charge power x (1 + (g - 1) x `power_per_grade` 0.15) and defense + (g - 1) x
  `defense_per_grade` 2 (grade 2: 36.8 / 12). Troops come from the squad; a squad with 0 troops does not fight.
  The mounted protagonist is not a squad and is left as it is.
- Losses (`TroopDeployment.RecordSurvivors`): after the battle each squad keeps its unit's remaining troops, so
  losses last until the next stable visit. `TroopRoster.IsWipedOut` (all squads at 0) is Story 007's fail state.
- Code: pure C# `KnightOrder.Troops.Core` (no engine references), `KnightOrder.Troops.Loading`
  (`TroopDataLoader`, snake_case DTOs).
- Tests: written before the run; 5 seen failing against code with the gold check, the grade power bonus and the
  survivor write-back disabled, then passing. Full EditMode run 89/89.
- Not in this story (Story 007): earning gold, the troop part of the stable screen, wiring the roster into the
  battle scene and returning from the battle to the stable. Nothing player-observable changed, so there is no
  screenshot.
