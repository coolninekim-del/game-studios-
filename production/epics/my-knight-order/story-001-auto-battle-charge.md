# Story 001: 기마돌격 오토배틀 전투

> **Epic**: 나만의 기사단
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-03

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 3` — 오토배틀 전투(기마돌격 충돌)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 전투; MVP 3; Build order 1), scoped to this story:*

- [ ] 내 기사단과 적 기사단이 배치된 전투 1판이 플레이어 입력 없이 시작부터 종료까지 자동 진행된다.
- [ ] 기마 병력이 적 병력을 향해 돌격하고, 충돌 순간에 충돌 이벤트가 발생한다(Story 002가 이 이벤트에 타이밍 입력을 건다).
- [ ] 충돌 피해는 돌격 위력과 상대 방어 수치로 계산되며, 모든 수치는 외부 데이터에서 읽는다(하드코딩 금지).
- [ ] 한쪽 병력이 0이 되면 전투가 끝나고 승/패와 양측 남은 병력을 반환한다.
- [ ] 같은 기사단 구성과 같은 시드에서는 전투 결과가 항상 같다(결정적).

---

## Implementation Notes

- 전투 시뮬레이션 로직은 MonoBehaviour와 분리된 순수 C# 클래스로 두어 EditMode 테스트가 가능하게 한다(의존성 주입).
- 난수는 시드를 주입받는 인터페이스로 받는다.
- 충돌 이벤트는 C# event(`OnChargeImpact`)로 노출한다.

---

## Out of Scope

- Story 002: 충돌 타이밍 입력과 판정
- Story 003: 화살 이벤트
- Story 006: 병력 수량·등급 육성

---

## Files

- `Assets/Scripts/Battle/Core/KnightOrder.Battle.Core.asmdef`
- `Assets/Scripts/Battle/Core/BattleSimulation.cs`
- `Assets/Scripts/Battle/Core/BattleUnit.cs`
- `Assets/Scripts/Battle/Core/BattleResult.cs`
- `Assets/Scripts/Battle/Core/BattleRules.cs`
- `Assets/Scripts/Battle/Core/BattleSetup.cs`
- `Assets/Scripts/Battle/Core/UnitSetup.cs`
- `Assets/Scripts/Battle/Core/BattleSide.cs`
- `Assets/Scripts/Battle/Core/BattleWinner.cs`
- `Assets/Scripts/Battle/Core/ChargeImpactEventArgs.cs`
- `Assets/Scripts/Battle/Loading/BattleDataLoader.cs`
- `Assets/Data/Battle/battle_rules.json`
- `Assets/Data/Battle/battle_sample_setup.json`
- (no test file — test evidence waived at `qa.level: minimal`)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Battle/AutoBattleTests.cs` — advisory at `qa.level: minimal`. 전투가 화면에 보이는 형태로 구현되면 실행 후 스크린샷을 `production/qa/evidence/`에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/my-knight-order/story-002-charge-timing-input.md`, `production/epics/my-knight-order/story-003-arrow-timing-input.md`, `production/epics/my-knight-order/story-005-stable-training.md`, `production/epics/my-knight-order/story-006-troop-training.md`

---

## Completion Notes
**Completed**: 2026-11-03
**Criteria**: 5/5 passing (AC-1, 2, 4, 5 by an executed .NET 8 harness outside the repo, not Unity; AC-3 by code inspection)
**Deviations**: None. Advisory: Unity compile of `BattleDataLoader.cs` and the asmdef not verified (no editor); float damage may differ across platforms if cross-device determinism is needed; seeded random interface not added (no randomness used).
**Test Evidence**: waived at qa.level minimal
**Reviewed by**: self, no independent review (Solo mode); engine-API check by unity-specialist during /dev-story
**Follow-ups (user-requested, not in AC)**: (1) damage should scale with troop count (use a data-driven curve to avoid snowballing); (2) retreat-and-recharge cycle with a data-driven `charges_before_melee` (1 = current behavior). Add as a new story ahead of Story 002.
