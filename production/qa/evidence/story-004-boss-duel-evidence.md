# Story 004 Evidence — 보스 일기토

> **Story**: `production/epics/my-knight-order/story-004-boss-duel.md`
> **Captured**: 2026-11-05, Windows player built with Unity 6000.6.7f1
> **Command**: `Builds/Win64/KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir <abs>/production/qa/evidence --evidence-story 004`
> (`DuelEvidenceCapture`, Duel scene opened by `DuelSceneLauncher`: duel 1 pass 1 tapped inside the window at the
> open side, pass 2 inside the window at the shield, pass 3 before the window at the open side, later passes inside
> the window at the open side until won; duel 2 restarted and left without input)

Player log of the run:

```
Evidence capture: pass 1 Success, boss -42, hero -0, hp hero 140 boss 118
Evidence capture: pass 2 Success (shield), boss -10, hero -0, hp hero 140 boss 108
Evidence capture: pass 3 Early, boss -16, hero -35, hp hero 105 boss 92
Evidence capture: pass 4 Success, boss -42, hero -0, hp hero 105 boss 50
Evidence capture: pass 5 Success, boss -42, hero -0, hp hero 105 boss 8
Evidence capture: pass 6 Success, boss -42, hero -0, hp hero 105 boss 0
Evidence capture: duel 1 won, growth +3
Evidence capture: pass 1 Miss, boss -0, hero -35, hp hero 105 boss 160
Evidence capture: pass 2 Miss, boss -0, hero -35, hp hero 70 boss 160
Evidence capture: pass 3 Miss, boss -0, hero -35, hp hero 35 boss 160
Evidence capture: pass 4 Miss, boss -0, hero -35, hp hero 0 boss 160
Evidence capture: duel 2 (no input) lost after 4 passes
```

Data used (`duel_rules.json`, `duel_sample_setup.json`): protagonist rider 30 attack / 100 hp, horse 20 speed /
40 hp, so lance power round(30 × (1 + 20 × 0.02)) = 42 and health 140; boss 25 / 120 with the same horse, so power
35 and health 160. Success × 1.0, early × 0.4 (floor(42 × 0.4) = 16), shield × 0.25 (floor(42 × 0.25) = 10).

## Screenshots

| File | Shows |
|------|-------|
| `story-004-window-open.png` | 판정 구간이 열림: 보스 상단에 금색 방패와 `GUARD HIGH`, 하단 바의 구간이 흰색, 화면 위/아래 탭 안내 |
| `story-004-perfect.png` | 성공(열린 쪽, 하단 겨눔): `PERFECT -42`, `PARRIED`, 보스 160 → 118, 주인공 체력 그대로, 창이 아래로 기울어 있음 |
| `story-004-shield.png` | 구간 안에서 방패 쪽을 찌름: `SHIELD -10`, `PARRIED`, 보스 118 → 108 |
| `story-004-early.png` | 구간 전 입력: `EARLY -16`, `HIT -35`, 주인공 140 → 105 |
| `story-004-victory.png` | 보스 체력 0: `VICTORY +3 growth` 배너(성장 보상 결과) |
| `story-004-defeat.png` | 입력 없이 4패스: 주인공 체력 0, `NO STRIKE` / `HIT -35`, `DEFEAT` 배너 |

Audio feedback (perfect: three rising tones, shield: two flat tones, early / no strike: a low tone, hit: two
falling tones) cannot be shown in a screenshot; it is played by `DuelView.HandlePassResolved` on every pass.

## Automated tests

`Assets/Tests/EditMode/Duel/DuelTests.cs` — 17 tests; full EditMode run 49/49 passed (Unity 6000.6.7f1),
including Stories 001-003's 32. Covers: no input loses without a reward; window / early / shield / no-tap damage;
taps before the shield reveal, second taps and taps after the impact are ignored; perfect play wins with the
growth reward; lance power and health from rider and horse stats (a faster horse hits harder); both falling on
one pass and the pass cap are defeats; the shield pattern repeats; one large time step equals many small ones;
the shipped data files (no input loses, perfect play wins); and invalid duel data throws.

## Observations

- Placeholder visuals (generated pixel horses and riders, diamond tiles, English text) until the pixel-art
  assets exist.
- A hit flashes the rider toward white in its own colour, so both sides stay recognisable when both are hit.
- Pointer input: scripted taps call the same `DuelSimulation.Tap` as the `<Pointer>/press` action, with the aim
  chosen by the screen half. A real mouse click and a real touch device were **not** exercised in this capture.

## Sign-off

- [ ] Lead sign-off — pending (user)
