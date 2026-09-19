# Tests — 구현 검증

## 다음 작업자용 빠른 안내

- 쉬운 이름: 개발자 검사 코드.
- 수정 시작점: [PrototypeSceneFlowTests.cs](PrototypeSceneFlowTests.cs).
- 현재 담당: 게임 규칙·화면 흐름 검사 코드.
- 이 폴더만으로 처리하지 않는 범위: 실제 검사 결과 보관은 Docs/Testing, 정상 플레이 코드 아님.
- 함께 확인: Core, Online, Docs/Testing.
- 지시 예시: “씬 전환 회귀 조건 추가 → Tests → PrototypeSceneFlowTests.cs → Core, Online, Docs/Testing와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

게임의 자동 검증 코드가 위치합니다. 수동 검증 절차와 결과 기록은 Docs/Testing에서 관리합니다. 현재 `PrototypeExitScoringTests`가 확정된 출구 집계 규칙의 핵심 경계를 검증합니다.

## 관련 위치

- [Testing — 수동 검증과 결과](../../../Docs/Testing/README.md)
- [Sandbox — 시험 공간](../Levels/Sandbox/README.md)

## 파일과 작업 안내

현재 출구·로컬 규칙·실제 씬 흐름의 PlayMode 회귀를 제공한다. 구현 파일과 실제 실행 결과를 구분한다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeExitScoringTests.cs` | 최초 유효 도착, 중복 도착, 다운 참가자, 전원 조기 마감, 5초 마감 검증 | Unity Test Runner PlayMode |
| `SlimeCoop.Prototype.Tests.asmdef` | 프로토타입 테스트 어셈블리와 NUnit/Test Framework 연결 | Unity 프로젝트 임포트 |
| `PrototypeRulesTests.cs` | 명단·마감·요청·저장·인원·입력 방향 규칙 | Unity Test Runner PlayMode |
| `PrototypeSceneFlowTests.cs` | 실제 로비 버튼·7챕터·7인터루드·동료 통로·장치·부활·전멸 | Unity Test Runner PlayMode |
| [PrototypeWaitingRoomTests.cs](PrototypeWaitingRoomTests.cs) | 직접 걷는 3D 대기방·근접 선택·준비 출발·벽/자기 충돌체·설정 복귀·해금·월드 표지판 | PlayMode, 가림/설정 복귀 12회 반복 및 세 위치의 표지판 방향·줄바꿈 검사 |
| `PrototypeMotorTests.cs` | 실제 CharacterController 이동·벽·앉기·점프·공동 밀기 | Unity Test Runner PlayMode |
| `PrototypeCourseTests.cs` | 42구간 정산·스토리, 2/4인 부유물, 필수 열쇠 복구 | Unity Test Runner PlayMode |
| `PrototypePersistenceTests.cs` | 최초 쓰기 전 원본 검사·오류 분리·재시도·키 파일 검증 | Unity Test Runner PlayMode, 분리된 임시 저장 |
| `PrototypeSettingsUiTests.cs` | 버튼 적용·확인·15초 복원·실패 복원·저장 안내 | Unity Test Runner PlayMode |
| `PrototypeInteractionTests.cs` | 구조·취소·차폐·끌어올리기·회복팩·보호 시간 | 가상 키보드 입력 스냅샷과 실제 충돌체, 배치 입력 소유권만 주입 |
| `PrototypeNetworkRoomTests.cs` | 서버 방 정원·고정 명단·준비·권한·중복·이탈 | 순수 방 모델 검사. 실제 통신은 루트 `LocalNetwork.ps1` 별도 실행 |
| [PrototypeNetworkInputTests.cs](PrototypeNetworkInputTests.cs) | 연결 바인딩·입력 JSON·순서·단절·단발 소비·한도·고정 명단 | 순수 입력 버퍼 검사. 챕터 통신 검증 아님 |
| [PrototypeServerPlayerTests.cs](PrototypeServerPlayerTests.cs) | 2/3/4인 독립 물리 제어·키보드/카메라 격리·실제 플레이어 구조/끌어올리기·전환 알림 | 실제 생성 맵에서 서버 제어 입력을 주입하는 PlayMode 검사 |
| [PrototypeNetworkReplicaTests.cs](PrototypeNetworkReplicaTests.cs) | 클라이언트 표시 전용 권한·자기 카메라·서버 상태·구간 재생성·방장 바인딩·키 안내 | PlayMode. 실제 통신은 `LocalNetwork.ps1 -Scenario World`와 구분 |
| [PrototypeNetworkReplicaTests.cs](PrototypeNetworkReplicaTests.cs)의 전멸 회귀 | 임시 상태 1회 초기화·반복 결과·2D 결과·새 3D 구간·영구 저장 보존 | [TEST-0011](../../../Docs/Testing/TEST-0011-NetworkWipeRetry.md). 실제 다중 프로세스는 `WipeFixture`로 별도 검사 |
| [PrototypeHazardTests.cs](PrototypeHazardTests.cs) | 실제 위험 볼륨 내 체력 감소·다운·이탈·위로 점프·출구 보호 | [TEST-0011](../../../Docs/Testing/TEST-0011-NetworkWipeRetry.md). 강제 피해 함수 호출이 아닌 고정 틱·서버 위치 판정 |
| [PrototypeHazardServerMotorProbe.cs](PrototypeHazardServerMotorProbe.cs) | `PrototypeServerPlayerTests` 전용 실제 FixedUpdate 입력 어댑터 | [TEST-0011](../../../Docs/Testing/TEST-0011-NetworkWipeRetry.md). 테스트 어셈블리 전용이며 일반 빌드에는 포함하지 않음 |
| [PrototypeNetworkWaitingTests.cs](PrototypeNetworkWaitingTests.cs) | 대기방 실제 명단·독립 이동·입력 세대·장치 근접 권한·표시 전용 제어 | [TEST-0008](../../../Docs/Testing/TEST-0008-NetworkWaitingRoom.md). PlayMode와 실제 프로세스 결과를 구분 |
| [PrototypeNetworkProgressTests.cs](PrototypeNetworkProgressTests.cs) | 서버 결과·고정 인원·반복·모순 거부·저장 잠금/원본 보호·기억 보존 | [TEST-0009](../../../Docs/Testing/TEST-0009-NetworkCompletion.md). 시험 배치 전환과 실제 입력 완주를 구분 |
| [PrototypeLocalLobbyTests.cs](PrototypeLocalLobbyTests.cs) | 이름/포트·생성 서버 식별·취소 후 재시도·이름별 저장 잠금·2D 로비 보존 | [TEST-0010](../../../Docs/Testing/TEST-0010-LocalLobbyEntry.md). 일반 메뉴의 실제 프로세스 시험은 `TestLocalLobby.ps1`로 분리 |

최신 실행 결과와 검증 경계: [TEST-0003](../../../Docs/Testing/TEST-0003-FullPrototype.md).

별도 프로세스 통신·거부·방장 이전: [TEST-0004](../../../Docs/Testing/TEST-0004-LocalNetwork.md).

서버 플레이 입력 분리·물리 제어와 실제 통신 연결 전 경계: [TEST-0005](../../../Docs/Testing/TEST-0005-ServerPlayerControl.md).

직접 걷는 대기방과 이야기 전원 동의 규칙, 이후 전체 회귀: [TEST-0006](../../../Docs/Testing/TEST-0006-WalkableWaitingRoom.md).

서버 챕터 실행·클라이언트 상태 복제와 실제 2/3/4인 시작/이동 검사: [TEST-0007](../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md).

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
