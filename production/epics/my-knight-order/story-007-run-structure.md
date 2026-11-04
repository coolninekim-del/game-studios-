# Story 007: 런 구조

> **Epic**: 나만의 기사단
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 7` — 런 구조(내정↔전투 교대, 적 기사단 자동 생성, 승패)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.6 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Player goal & fail state; MVP 7; Build order 4; Out of scope: 영구 메타 성장), scoped to this story:*

- [ ] 런은 내정과 전투가 교대로 진행되며, 총 횟수(브리프 기준 15~20회)는 외부 데이터 값이다.
- [ ] 보스 일기토의 등장 위치는 외부 데이터로 정의되며, 런의 마지막은 최종 보스(적 기사단장) 일기토다.
- [ ] 적 기사단은 런 진행도에 따라 강해지도록 자동 생성된다(생성 규칙은 외부 데이터).
- [ ] 일기토에서 지거나 기사단이 전멸하면 런이 끝나고 패배 화면이 표시된다.
- [ ] 최종 보스를 격파하면 승리 화면이 표시된다.
- [ ] 새 런은 항상 같은 초기 상태에서 시작한다(메타 성장 없음).

---

## Implementation Notes

- 런 상태(현재 회차, 다음 노드 종류)는 순수 C# 상태 머신으로 두어 테스트 가능하게 한다.
- 씬 전환은 상태 머신의 결과를 따른다.

---

## Out of Scope

- 영구 메타 성장, 세이브/이어하기(브리프에 없음)
- 기사단 소속 영웅 육성

---

## Files

- `Assets/Scripts/Run/`
- `Assets/Data/Run/`
- `Assets/Tests/EditMode/Run/RunStructureTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: `Assets/Tests/EditMode/Run/RunStructureTests.cs` OR playtest doc — advisory at `qa.level: minimal`. 승리·패배 화면 스크린샷을 `production/qa/evidence/`에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/my-knight-order/story-001-auto-battle-charge.md`, `production/epics/my-knight-order/story-002-charge-timing-input.md`, `production/epics/my-knight-order/story-003-arrow-timing-input.md`, `production/epics/my-knight-order/story-004-boss-duel.md`, `production/epics/my-knight-order/story-005-stable-training.md`, `production/epics/my-knight-order/story-006-troop-training.md`
- Unlocks: None
