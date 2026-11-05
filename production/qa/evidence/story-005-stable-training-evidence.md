# Story 005 Evidence — 말 중심 내정(말·주인공 육성)

> **Story**: `production/epics/my-knight-order/story-005-stable-training.md`
> **Captured**: 2026-11-05, Windows player built with Unity 6000.6.7f1
> **Command**: `Builds/Win64/KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir <abs>/production/qa/evidence --evidence-story 005`
> (`StableEvidenceCapture`, Stable scene opened by `StableSceneLauncher`: horse speed trained, then horse agility
> and horse health, a fourth training tried; TO BATTLE; then the Duel scene)

Player log of the run:

```
Evidence capture: profile start: rider attack 30, rider hp 100, horse speed 20, horse hp 40, horse agility 10
Evidence capture: trained HorseSpeed 20 -> 25
Evidence capture: fourth training refused, actions left 0
Evidence capture: profile trained: rider attack 30, rider hp 100, horse speed 25, horse hp 55, horse agility 15
Evidence capture: hero cue before its window: True
Evidence capture: battle hero charge power 45, health 155, charge window 0.286s, foot unit window 0.220s, captured inside the hero-only part: True
Evidence capture: duel hero lance power 45, health 155, window 0.390s
```

Data used (`stable_rules.json`, `stable_start_profile.json`, `battle_charge_timing.json`, `duel_rules.json`):
start power round(30 × (1 + 20 × 0.02)) = 42, health 100 + 40 = 140, battle window 0.22 × (1 + 10 × 0.02) =
0.264 s, duel window 0.3 × (1 + 10 × 0.02) = 0.36 s. Trained (speed 25, agility 15, horse health 55): power
round(30 × 1.5) = 45, health 155, battle window 0.22 × 1.3 = 0.286 s, duel window 0.3 × 1.3 = 0.39 s.

## Screenshots

| File | Shows |
|------|-------|
| `story-005-stable.png` | 내정 화면 첫 상태: 말 탄 주인공, 육성 버튼 5개(현재값과 증가량), `TRAINING ACTIONS 3 / 3`, 다음 전투·일기토 수치(42 / 140 / 0.264s, 42 / 140 / 0.360s) |
| `story-005-trained-speed.png` | 말 속도 육성 직후: 버튼과 안내 줄에 `20 -> 25`, 오른쪽에 돌격 위력·창 위력 `42 -> 45`(초록), 행동 2 / 3 |
| `story-005-actions-spent.png` | 행동 3회 소진: 버튼 비활성(회색), 마지막 육성 `40 -> 55`, 체력 `140 -> 155`, 네 번째 육성 거절 `No training actions left` |
| `story-005-battle-hero-cue.png` | 육성한 능력치로 시작한 전투: 보병 사이의 말 탄 주인공 `HERO 155`, 타이밍 바의 금색 구간(보병 0.22s) 앞 파란 구간(주인공만 0.286s), 안내 `blue: hero only` |
| `story-005-battle-hero.png` | 같은 돌격에서 파란 구간이 열린 순간(흰색): 이때 탭하면 주인공만 성공 |
| `story-005-duel.png` | 육성한 능력치로 시작한 일기토: `HERO 155 / 155`, 판정 구간(0.39s)이 열린 상태 |

Audio is unchanged from Stories 002-004 (the stable screen plays no sound).

## Automated tests

`Assets/Tests/EditMode/Stable/StableIntegrationTests.cs` — 15 cases, plus 1 new case in
`Assets/Tests/EditMode/Duel/DuelTests.cs` (horse agility widens the duel window); full EditMode run 65/65 passed
(Unity 6000.6.7f1). Covers: the action limit (a fourth training is refused and changes nothing); a training
raises its stat by its gain and reports before / after without touching the caller's profile; horse speed
raises the mounted protagonist's battle damage (36 → 39 on its first impact); a mounted unit's damage does not
scale with troops; horse agility widens only the protagonist's charge window (0.26 s vs 0.2 s for a foot unit);
training raises the next duel's lance power, health and window; the stable's numbers equal what the battle and
duel use; a taken start cell throws; the shipped data files load together; invalid stable data throws.
Six of these were seen failing against code with the mounted-damage rule, the horse window modifier and the
duel agility window disabled, then passing on the real code.

## Observations

- Placeholder visuals (generated pixel horse and rider, diamond tiles, English text) until the pixel-art
  assets and localization exist.
- The battle groups impacts per tick, so the protagonist often charges together with a foot unit. The gold zone
  is the foot units' window; the blue part ahead of it is the protagonist's extra window.
- The protagonist's `HERO 155` label can overlap a neighbouring unit's troop count when two units stand on
  adjacent tiles.
- The Story 002, 003 and 004 captures were run again on this build into a scratch folder (4, 5 and 6 shots,
  duel won and lost as before); their retained evidence was not replaced.
- Pointer input: scripted presses call the same `StableView.Train` / `GoToBattle` as the buttons. A real
  mouse click and a real touch device were **not** exercised. Returning from the battle or duel to the
  stable is Story 007.

## Sign-off

- [ ] Lead sign-off — pending (user)
