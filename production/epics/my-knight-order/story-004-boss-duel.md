# Story 004: 보스 일기토

> **Epic**: 나만의 기사단
> **Status**: Ready
> **Layer**: Core
> **Type**: Visual/Feel
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: `Brief MVP feature 6` — 보스 일기토(주인공+말, 수동 조작 비중 큼)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6.3 LTS | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Core loop: 보스 일기토; Player goal & fail state; MVP 6; Build order 2), scoped to this story:*

- [ ] 보스 전투에서는 기사단 오토배틀 대신 주인공 대 보스의 1:1 결투 화면으로 진행된다.
- [ ] 결투 내내 플레이어 입력이 결과를 좌우한다. 입력 없이 두면 패배한다.
- [ ] 주인공과 말의 능력치가 결투 수치(공격·체력 등)에 반영된다. 이 스토리에서는 외부 데이터의 고정 테스트 값을 쓴다.
- [ ] 주인공 체력이 0이 되면 일기토 패배 결과를 반환한다(Story 007에서 런 종료로 연결).
- [ ] 보스 체력이 0이 되면 승리와 성장 보상 결과를 반환한다.
- [ ] 결투 수치는 모두 외부 데이터에서 읽는다.

---

## Implementation Notes

- 결투의 구체적 행동 세트(공격·방어·회피 등)는 이 스토리에서 프로토타입으로 정하고, 결정한 내용을 `design/game-brief.md`에 한 줄로 반영할지 사용자에게 묻는다.
- 결투 판정 로직은 순수 C#으로 분리해 테스트 가능하게 한다.

---

## Out of Scope

- Story 005: 내정 육성 결과를 결투 수치에 실제로 연결
- Story 007: 보스 일기토 등장 위치와 런 종료 처리

---

## Files

- `Assets/Scripts/Duel/`
- `Assets/Data/Duel/`
- `Assets/Tests/EditMode/Duel/DuelTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**: 결투 화면과 승/패 결과의 retained screenshot in `production/qa/evidence/` + sign-off in `production/qa/evidence/story-004-boss-duel-evidence.md` (not waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (고정 테스트 값으로 독립 시작 가능)
- Unlocks: `production/epics/my-knight-order/story-005-stable-training.md`, `production/epics/my-knight-order/story-007-run-structure.md`
