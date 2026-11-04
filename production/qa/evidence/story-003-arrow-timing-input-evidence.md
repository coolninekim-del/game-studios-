# Story 003 Evidence — 화살 대응 타이밍 입력

> **Story**: `production/epics/my-knight-order/story-003-arrow-timing-input.md`
> **Captured**: 2026-11-04, Windows player built with Unity 6000.6.7f1
> **Command**: `Builds/Win64/KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir <abs>/production/qa/evidence --evidence-story 003`
> (`ChargeTimingEvidenceCapture`: volley group 1 tapped inside the arrow window, group 2 not tapped, group 3
> tapped before the window; the sample battle has two volley groups, so group 3 is in the restarted battle)

Player log of the run:

```
Evidence capture: group 1 judged Success at tick 4
Evidence capture: group 2 judged Miss at tick 11
Evidence capture: battle ended, restarting it.
Evidence capture: group 3 judged Early at tick 4
```

Data used: `arrow_damage` 12, `arrow_success_damage_multiplier` 0.25, `arrow_window_seconds` 0.25,
`arrow_cue_lead_seconds` 1.0. Each volley group is three volleys, one per lane (each enemy targets the player
unit in its own row).

## Screenshots

| File | Shows |
|------|-------|
| `story-003-both-cues.png` | 화살 큐와 돌격 큐가 동시에 표시: 적에게서 날아오는 화살, 대상 아군의 청록 수축 링(화살), 어두운 수축 링(돌격), 하단 바는 더 빠른 화살 큐를 표시(`ARROWS!`) |
| `story-003-window-open.png` | 화살 판정 구간이 열림: 화살이 대상에 거의 도달, 링과 화살이 금색, 하단 바의 구간이 흰색으로 강조 |
| `story-003-block.png` | 성공: `BLOCK x0.25` 팝업, 청록 번쩍임, 아군 병력 300 → 291 (줄마다 화살 1발 × floor(12 × 0.25) = 3, 세 줄 합계 9) |
| `story-003-hit.png` | 미입력: `ARROW HIT` 팝업, 빨강 번쩍임, 화살 기본 피해 |
| `story-003-early.png` | 구간 전 입력: `EARLY` 팝업, 주황 번쩍임, 아군 병력 300 → 264 (줄마다 12, 세 줄 합계 36, 기본 피해) |

Audio feedback (block: two high tones, hit or early: two falling low tones) cannot be shown in a screenshot;
it is played by `ChargeTimingBattleView.HandleArrowJudged` on every arrow judgement.

## Automated tests

`Assets/Tests/EditMode/Battle/ArrowTimingTests.cs` — 12 tests; full EditMode run 32/32 passed (Unity 6000.6.7f1,
after the tile change), including Story 002's 14. Covers volley damage from data, no arrows at `arrow_range` 0, volleys
only at charging units between impact and arrow range, block / early / miss damage, an arrow and a charge on
the same tick judged independently (one tap between the two windows: arrow Success, charge Early; one tap
inside both: both Success), and the arrow data keys.

The Story 002 capture (`--evidence-story 002`) was re-run on the same build and still judged Success / Miss /
Early at ticks 6 / 12 / 15; its screenshots were replaced with the isometric view.

## Observations

- Re-captured 2026-11-04 on the isometric tile view after the Story 001 one-unit-per-tile change; the first
  capture had all lanes stacked on one cell.
- Placeholder visuals (generated pixel foot soldiers, diamond tiles, rings, English feedback text) until the
  pixel-art assets exist.
- Pointer input: scripted taps call the same `ChargeTimingController.Tap()` as the `<Pointer>/press` action.
  A real mouse click and a real touch device were **not** exercised in this capture.

## Sign-off

- [ ] Lead sign-off — pending (user)
