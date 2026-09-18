# Tests — 구현 검증

게임의 자동 검증 코드가 위치합니다. 수동 검증 절차와 결과 기록은 Docs/Testing에서 관리합니다. 현재 `PrototypeExitScoringTests`가 확정된 출구 집계 규칙의 핵심 경계를 검증합니다.

## 관련 위치

- [Testing — 수동 검증과 결과](../../../Docs/Testing/README.md)
- [Sandbox — 시험 공간](../Levels/Sandbox/README.md)

## 파일과 작업 안내

현재는 폴더 안내 단계입니다. 실제 파일을 추가할 때 아래 표에 경로·역할·확인 방법을 함께 기록합니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeExitScoringTests.cs` | 최초 유효 도착, 중복 도착, 다운 참가자, 전원 조기 마감, 5초 마감 검증 | Unity Test Runner PlayMode |
| `SlimeCoop.Prototype.Tests.asmdef` | 프로토타입 테스트 어셈블리와 NUnit/Test Framework 연결 | Unity 프로젝트 임포트 |

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
