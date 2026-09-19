# TEST-0002 — 프로토타입 출구 집계 규칙

상태: 완료

## 정보

- 관련 작업: [TASK-0004](../Maintenance/TASK-0004-SourceBaselineIntegration.md)
- 담당 폴더·실제 파일: `Assets/Game/Features/StartandExit/PrototypeExitScoring.cs`, `Assets/Game/Core/Prototype/PrototypeParticipant.cs`, `Assets/Game/Tests/PrototypeExitScoringTests.cs`
- 기획 근거: [출구와 팀 점수 설계](../DESIGN-0001-StageExit.md), [통합 기획서](../../Plan.md)
- 실행 환경: Unity 6000.6.1f1, Windows, Unity Test Framework, PlayMode
- 실행 명령: `unity test <project> --editor-version 6000.6.1f1 --mode PlayMode --report-format junit --output tmp/exit-tests-final.junit.xml`
- 최종 결과: `tmp/exit-tests-final.junit.xml` — tests 4, failures 0, errors 0, skipped 0, 5.184초

## 검증 내용

| 번호 | 사전 상태·행동 | 기대 결과 | 실제 결과·증거 | 판정 |
|---|---|---|---|---|
| 1 | 한 참가자가 처음 출구에 도착한 뒤 같은 참가자가 다시 등록 | 최초 도착으로 5초 집계가 시작되고 중복 도착은 증가하지 않음 | `FirstArrivalStartsWindowAndDuplicateArrivalDoesNotIncreaseCount` 통과, 도착 1명 | 통과 |
| 2 | 다운된 참가자가 출구 등록을 시도 | 유효 도착으로 인정하지 않음 | `DownedParticipantCannotStartOrJoinSettlement` 통과, 도착 0명 | 통과 |
| 3 | 시작 명단 4명이 모두 도착 | 5초 전에 조기 정산하고 팀 점수를 한 번 생성 | `StageStartRosterSettlesEarlyWhenAllFourArrive` 통과, `IsSettled=true` | 통과 |
| 4 | 한 명 도착 후 5초 대기 | 5초 창 만료 시점에 집계 마감 | `SettlementClosesAfterFiveSecondWindow` 통과, `SettlementReason`에 `5-second` 포함 | 통과 |

## 필요한 예외·회귀 검증

- 기존 정상 기능: 16개 씬 생성·Build Settings 등록과 별도 Windows 빌드 성공은 TASK-0004에서 확인했다.
- 동시·중복 요청: 중복 도착 단위 테스트만 확인했다. 실제 네트워크 동시 요청은 미검증이다.
- 종료·끊김·재접속·중도 합류: 로컬 프로토타입 범위 밖이다.
- 성능·장시간 실행: 측정하지 않았다.

## 결론

- 확정된 최초 유효 도착, 중복 방지, 다운 참가자 제외, 시작 명단 전원 조기 마감, 최대 5초 집계 규칙의 로컬 구현 경계를 4개 PlayMode 테스트로 고정했다.
- 테스트 통과 후 런타임·Editor 어셈블리 분리 상태에서 Windows 검증 빌드도 성공했고, 헤드리스 초기 기동에서 Input System·PhysX 초기화를 확인했다.
- 이 테스트는 점수 공식의 최종 값, 서버 권한, 실제 멀티플레이·PlayFab·Steam을 확정하거나 검증하지 않는다.

[검증 목록](README.md) · [작업 기록](../Maintenance/TASK-0004-SourceBaselineIntegration.md)
