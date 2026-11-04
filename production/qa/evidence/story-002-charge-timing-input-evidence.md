# Story 002 Evidence — 돌격 충돌 타이밍 입력

> **Story**: `production/epics/my-knight-order/story-002-charge-timing-input.md`
> **Captured**: 2026-11-04, Windows player built with Unity 6000.6.7f1
> **Command**: `Builds/Win64/KnightOrder.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 --evidence-dir <abs>/production/qa/evidence`
> (`ChargeTimingEvidenceCapture`: group 1 tapped inside the window, group 2 not tapped, group 3 tapped before the window)

Player log of the run:

```
Evidence capture: group 1 judged Success at tick 6
Evidence capture: group 2 judged Miss at tick 12
Evidence capture: group 3 judged Early at tick 15
```

## Screenshots

| File | Shows |
|------|-------|
| `story-002-window-open.png` | 충돌 직전 판정 구간: 공격 유닛 주위 금색 링(수축하는 큐 링이 판정 링에 겹침), 하단 바의 판정 구간이 열려 흰색으로 강조, 마커가 구간에 진입 |
| `story-002-success.png` | 성공: `SUCCESS x1.5` 팝업, 유닛과 판정 구간이 초록으로 번쩍임, 적 병력 300 → 200 |
| `story-002-miss.png` | 미입력: `MISS` 팝업, 빨강 번쩍임, 기본 위력 |
| `story-002-early.png` | 구간 밖(이른) 입력: `EARLY` 팝업, 주황 번쩍임, 기본 위력 |

Audio feedback (success: two rising tones, fail: one low tone) cannot be shown in a screenshot; it is played
by `ChargeTimingBattleView.HandleImpactJudged` on every judgement.

## Automated tests

`Assets/Tests/EditMode/Battle/ChargeTimingTests.cs` — 14/14 passed in EditMode (Unity 6000.6.7f1).

## Observations

- In `battle_sample_setup.json` every unit is equidistant (Chebyshev) to all enemies, so Story 001's
  lowest-index tie-break sends all three lanes to the row-1 unit and the units stack on one cell. The view
  draws them truthfully, which makes the battle look like 1 vs 1 with overlapping troop labels. Not a
  Story 002 defect; worth a Story 001 targeting tweak or a different sample layout.
- Placeholder visuals (generated squares/rings, English feedback text) until pixel-art isometric assets exist.
- Pointer input: scripted taps in this run call the same `ChargeTimingController.Tap()` as the
  `<Pointer>/press` action. A real mouse click and a real touch device were **not** exercised in this capture.

## Sign-off

- [ ] Lead sign-off — pending (user)
