# Story 005: 말 중심 내정(말·주인공 육성)

> **Epic**: 나만의 기사단
> **Status**: In Review
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-05

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

**Status**: [x] Created — `Assets/Tests/EditMode/Stable/StableIntegrationTests.cs` (15 cases) + `production/qa/evidence/story-005-*.png` + `story-005-stable-training-evidence.md` (lead sign-off pending)

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`, `production/epics/my-knight-order/story-002-charge-timing-input.md`, `production/epics/my-knight-order/story-004-boss-duel.md`
- Unlocks: `production/epics/my-knight-order/story-007-run-structure.md`

---

## Implementation Notes (2026-11-05)

- Decisions (user, 2026-11-05): the protagonist joins the battle as one mounted unit; a stable visit is limited
  by training actions; the horse gets a fifth stat, agility. The rules, numbers and file list below were
  approved as drafted.
- Profile (`stable_start_profile.json`): rider attack 30, rider health 100, horse speed 20, horse health 40,
  horse agility 10. One `KnightProfile` model; the battle (`KnightConversion.ToBattleUnit` / `AddToBattle`) and
  the duel (`KnightConversion.ToDuelist`) both read it.
- Stable (`stable_rules.json`): `actions_per_visit` 3; each action trains one stat: horse speed +5, horse
  agility +5, horse health +15, rider attack +3, rider health +15. A fourth training in a visit is refused and
  changes nothing. Each visit to the Stable scene is a new `StableSession` with all actions.
- Battle: the protagonist is one mounted unit at (`battle_start_x`, `battle_start_y`) = (0, 4), between the
  three foot units. Charge power = round(rider attack x (1 + horse speed x `battle_speed_power_scale` 0.02));
  health (troops) = rider health + horse health; defense `battle_defense` 10. A mounted unit's impact damage does
  not scale with its troop count (`UnitSetup.IsMounted`, `BattleSimulation`). Its charge window = 0.22 s x
  (1 + horse agility x `battle_agility_window_scale` 0.02) through Story 002's `IChargeWindowModifier`
  (`HorseWindowModifier`); foot units keep 0.22 s. Start: power 42, health 140, window 0.264 s. If the
  protagonist falls in battle only that unit leaves; Story 007 decides what it means for the run.
- Duel: lance power and health use Story 004's formulas with the profile. New: `DuelistStats.HorseAgility` and
  `duel_rules.json` `agility_window_scale` (0.02, optional, default 0): the protagonist's window = min(guard
  reveal, 0.3 s x (1 + agility x 0.02)), 0.36 s at the start. With agility 0 the duel behaves as before.
- Screen (`StableView`): the mounted protagonist, one button per training (value and gain), the actions left,
  and the next battle and duel numbers. After a training the stat and every changed number show
  "before -> after" in green. TO BATTLE / TO DUEL open those scenes with the trained profile, carried by
  `KnightProfileStore` until Story 007's run state replaces it. Without a stable visit the battle and duel use
  the start profile.
- Battle view: the protagonist is drawn as one horse and rider labelled HERO with its health. When it charges in
  the same impact group as foot units, the part of its window beyond theirs is drawn blue ahead of the gold
  zone ("blue: hero only"), because a tap there is a Success for the hero but Early for the foot units.
- Code: pure C# `KnightOrder.Stable.Core` (no engine references), `KnightOrder.Stable.Loading`
  (`StableDataLoader`, snake_case DTOs), `KnightOrder.UI.Stable` (`StableView`, `StableSceneLauncher`,
  `StableEvidenceCapture`), and `KnightOrder.UI.Shared` (`MountedSprites`, the horse and rider sprites moved
  out of `DuelView`). `Assets/Scenes/Stable.unity` is generated by `BattleSceneBuilder`, third in the build
  list; the player opens it with `--scene Stable`.
- Tests: written before the run; seen failing (6 of them) against code with the mounted-damage rule, the horse
  window modifier and the duel agility window disabled, then passing. Full EditMode run 65/65. The Story
  002-004 captures still run on the new build (checked into a scratch folder, retained evidence not
  replaced).
- Deviations from `.claude/rules/ui-code.md`, as in Stories 002-004: English text without localization,
  pointer input only (no keyboard / gamepad navigation), placeholder visuals.
- Not verified: a real mouse click / real touch device on the built player (scripted presses only, through
  the same `StableView.Train` / `GoToBattle`); returning from the battle or duel to the stable (Story 007).
