# Epic: 나만의 기사단

> **Slug**: my-knight-order
> **Tier**: minimal — built from `design/game-brief.md`, no GDD / ADR / control manifest

## Goal

말과 주인공을 키워 기마돌격과 일기토로 적 기사단을 꺾는, 내정과 전투가 교대하는 로그라이크 오토배틀러.

## Scope (brief MVP)

1. 말 중심 내정 화면(말·주인공 육성)
2. 병력 육성(수량·등급 한 축만)
3. 오토배틀 전투(기마돌격 충돌)
4. 돌격 충돌 타이밍 입력
5. 화살 대응 타이밍 입력
6. 보스 일기토(주인공+말, 수동 조작 비중 큼)
7. 런 구조(내정↔전투 교대, 적 기사단 자동 생성, 승패)

## Order (brief Build order)

1. 돌격 충돌 타이밍 전투 한 판
2. 일기토 한 판
3. 말 육성이 ①②의 결과에 반영되는지 확인
4. 내정↔전투 런 루프와 적 기사단 생성

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 기마돌격 오토배틀 전투 | Logic | Ready | N/A (minimal) |
| 002 | 돌격 충돌 타이밍 입력 | Visual/Feel | In Review | N/A (minimal) |
| 003 | 화살 대응 타이밍 입력 | Visual/Feel | In Review | N/A (minimal) |
| 004 | 보스 일기토 | Visual/Feel | In Review | N/A (minimal) |
| 005 | 말 중심 내정(말·주인공 육성) | Integration | Ready | N/A (minimal) |
| 006 | 병력 육성 | Logic | Ready | N/A (minimal) |
| 007 | 런 구조 | Integration | Ready | N/A (minimal) |
| 008 | 병력 규모 반영과 재돌격 사이클 (002보다 먼저) | Logic | Ready | N/A (minimal) |
