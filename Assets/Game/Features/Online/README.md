# Online — 온라인 연결

## 다음 작업자용 빠른 안내

- 쉬운 이름: 연결과 서버 상태 전달.
- 수정 시작점: [PrototypeNetworkWorld.cs](PrototypeNetworkWorld.cs).
- 현재 담당: 서버 진행·입력 소비·복제; 연결은 PrototypeNetworkTransport.cs.
- 이 폴더만으로 처리하지 않는 범위: 개별 게임 규칙·실제 Steam/PlayFab 서비스 개통.
- 함께 확인: Player, Core, Save.
- 지시 예시: “서버와 클라이언트 위치 불일치 → Features/Online → PrototypeNetworkWorld.cs → Player, Core, Save와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

## 담당 범위

- Steam 인증, 친구 초대·방 목록·접속·종료·재합류를 PlayFab과 연결한다.
- PlayFab Multiplayer Servers의 전용 서버와 참가자 상태를 동기화한다.
- 방장 이탈 시 남은 참가자에게 방장 역할을 이전하며 서버 런·출구 타이머·점수를 유지한다.
- 중도 합류는 다음 안전 구간에서 처리한다. 서버 장애 복구·유예 시간·SDK·지역·예산은 상세 결정 대상이다.
- 핑·텍스트 채팅·음성 채팅의 연결을 담당하며 공개 매칭·음성 채팅은 초기 프로토타입에서 미룬다.

## 검증과 보안

서버가 인증 주체·소유권·상태·요청 빈도·중복을 확인하고 담당 기능이 게임 규칙을 검증한다. 방장은 일반 참가자이며 게임 판정권이나 전역 계정 제재 권한을 갖지 않는다.

현재 보호 범위는 서버 검증과 Steam Game Ban이다. 별도 PC 보호 제품이나 파일 삭제 기능은 넣지 않는다. 확인된 핵은 세션에서 즉시 추방하고 계정 제재는 Steam 계정과 증거를 운영자가 검토한다. 전화번호·IP·기기 일치만의 자동 제재는 하지 않는다.

Game Ban은 제재 수단이며 탐지 프로그램이 아니다. 서버 키와 제재 API는 안전한 서버에서만 사용한다. [구조 설계](../../../../Docs/ARCHITECTURE.md)와 [운영](../../../../Docs/Operations/README.md)을 따른다.

## 함께 확인할 폴더

- [StartandExit](../StartandExit/README.md): 참가자의 시작·종료·스테이지 전환을 맞춥니다.
- [Player](../Player/README.md): 이동과 상태 정보를 맞춥니다.
- [Cooperation](../Cooperation/README.md): 상호 협동 상태를 맞춥니다.
- [Items](../Items/README.md): 획득·소모 요청을 전달합니다.
- [Puzzles](../Puzzles/README.md): 장치 상태와 조작 요청을 전달합니다.
- [UI](../UI/README.md): 접속 상태와 채팅을 표시합니다.

## 문제가 생겼을 때

게임/서버 버전, 방장·참가자 역할, 서버 세션과 접속 순서, 끊김 시점, 각 화면의 상태 차이와 서버 요청 처리 기록을 확인합니다.

작업 지시 예시: “Online에서 플레이어 방장 이탈과 게임 서버 종료를 따로 재현하고, 선택된 복구 정책에 따른 남은 참가자의 화면·진행·팀 결과를 기록해주세요.”

작업 지시 예시: “Online에서 추방된 계정의 같은 도전 재접속과 권한 없는 추방 요청을 확인해주세요. 높은 핑·재전송·서비스 장애가 핵 확정으로 처리되는지도 분리해서 검증해주세요.”

## 구현 파일 안내

아래 로컬 프로토타입 코드가 있다. 시험 동료 시뮬레이션과 실제 별도 프로세스 통신을 구분한다. Steam·PlayFab 운영 환경은 아직 연결하지 않았다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| [PrototypeSession.cs](PrototypeSession.cs) | 일반 실행의 로컬 런·연결 변화 시뮬레이션 | [기존 통합 검사](../../../../Docs/Testing/TEST-0003-FullPrototype.md) |
| [PrototypeNetworkRoom.cs](PrototypeNetworkRoom.cs) | 전용 서버의 접속 명단·준비·챕터 선택·방장 역할 | [방 모델 테스트](../../Tests/PrototypeNetworkRoomTests.cs) |
| [PrototypeNetworkWaitingServer.cs](PrototypeNetworkWaitingServer.cs) | 대기방의 실제 참가자·이동·충돌·근접 장치 권한 | [멀티 대기방 검사](../../../../Docs/Testing/TEST-0008-NetworkWaitingRoom.md) |
| [PrototypeNetworkWaitingClient.cs](PrototypeNetworkWaitingClient.cs) | 1인칭 대기방·서버 위치 표시·준비창·로비 이탈 | [표시/권한 검사](../../Tests/PrototypeNetworkWaitingTests.cs) |
| [PrototypeNetworkWaitingState.cs](PrototypeNetworkWaitingState.cs), [PrototypeNetworkInputChannel.cs](PrototypeNetworkInputChannel.cs) | 대기방 입력 세대·서버 위치 계약과 공통 입력 수명·순서·한도 | [멀티 대기방 검사](../../../../Docs/Testing/TEST-0008-NetworkWaitingRoom.md) |
| [PrototypeNetworkInputBuffer.cs](PrototypeNetworkInputBuffer.cs) | 연결별 입력 바인딩·순서·수명·한도·단발 소비 | [입력 회귀](../../Tests/PrototypeNetworkInputTests.cs), [서버 연결 결과](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md) |
| [PrototypeNetworkTransport.cs](PrototypeNetworkTransport.cs) | loopback UDP 접속 승인·바인딩·제한된 명령·서버 스냅샷 | [다중 프로세스 검사](../../../../Docs/Testing/TEST-0004-LocalNetwork.md) |
| [PrototypeNetworkWorld.cs](PrototypeNetworkWorld.cs) | 전용 서버 챕터·입력 tick·전환 조정자. Bootstrap에 연결 | [실제 2/3/4인 챕터 시작·이동 검사](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md) |
| [PrototypeNetworkWorldState.cs](PrototypeNetworkWorldState.cs) | 참가자·동적 소품·획득물 상태 계약과 값 검사 | [복제 회귀](../../Tests/PrototypeNetworkReplicaTests.cs) |
| [PrototypeNetworkReplica.cs](PrototypeNetworkReplica.cs) | 표시 전용 맵·자기 카메라·서버 위치/상태 적용, 클라이언트 판정 비활성화 | [복제 회귀](../../Tests/PrototypeNetworkReplicaTests.cs) |
| [PrototypeNetworkClientWorld.cs](PrototypeNetworkClientWorld.cs) | 조작 입력·서버 HUD·설정·기억·이야기 동의 화면 | [실제 검증 범위와 남은 연결](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md) |
| [PrototypeNetworkProgress.cs](PrototypeNetworkProgress.cs) | 서버 완료/기억을 참가자 로컬 저장에 한 번 반영 | [결과 저장·복귀](../../../../Docs/Testing/TEST-0009-NetworkCompletion.md) |
| [PrototypeNetworkBootstrap.cs](PrototypeNetworkBootstrap.cs) | 2D 메뉴/실행 인자 진입·승인 후 3D 전환·빈 서버 종료 | [일반 로비 검사](../../../../Docs/Testing/TEST-0010-LocalLobbyEntry.md) |
| [PrototypeLocalConnection.cs](PrototypeLocalConnection.cs), [PrototypeLocalServerProcess.cs](PrototypeLocalServerProcess.cs) | 로컬 입력 검증·숨김 전용 서버 실행·생성 서버 식별 | [일반 로비 검사](../../../../Docs/Testing/TEST-0010-LocalLobbyEntry.md) |
| [PrototypeLocalLobbyQa.cs](PrototypeLocalLobbyQa.cs) | 일반 로비 버튼의 별도 프로세스 생성·참가·이탈·재참가 검사 | [TestLocalLobby.ps1](../../../../TestLocalLobby.ps1) |
| [PrototypeNetworkQa.cs](PrototypeNetworkQa.cs) | 시험 인자가 있을 때만 실행되는 별도 프로세스 검사·보고서 | [다중 프로세스 검사](../../../../Docs/Testing/TEST-0004-LocalNetwork.md) |
| [PrototypeNetworkQa.Lifecycle.cs](PrototypeNetworkQa.Lifecycle.cs) | 서버 시험 배치를 이용한 정산·동의·저장·복귀 검사. 코스 이동 완주 아님 | [시험 범위와 근거](../../../../Docs/Testing/TEST-0009-NetworkCompletion.md) |
| [PrototypeNetworkQa.Wipe.cs](PrototypeNetworkQa.Wipe.cs) | 1구간 시험 배치·실제 위험 지대 이동·전멸·2D/3D 복귀·새 런·영구 저장 보존 검사 | [전멸 재도전 검사](../../../../Docs/Testing/TEST-0011-NetworkWipeRetry.md) |

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeSession.cs`: 로컬 요청·이탈·재접속·방장 이전·추방 시뮬레이션. 실제 Steam 인증·PlayFab 전용 서버·네트워크·Game Ban은 미구현이며 로컬 검증 결과로 대체하지 않는다.

별도 경로인 `PrototypeNetwork*`는 `127.0.0.1`에만 접속·수신하는 개발용 전용 서버다. Netcode for GameObjects 2.13.2와 Unity Transport 6.6.0을 Unity Package Manager로 설치했다. 이 시험용 조합은 계약의 정식 SDK 결정이 아니다. 클라이언트가 보내는 슬롯/계정 ID를 신뢰하지 않고 승인된 연결 ID에 참가자를 바인딩한다. 최대 인원·버전·이름 형식·패킷 길이·요청 순서·빈도·방장 권한을 검사한다. 로컬 계정 이름은 실제 인증 증거가 아니다.

대기실 상태와 출발 명단에 더해 서버 챕터 시작·참가자별 입력 이동·표시 전용 복제를 연결했다. 별도 서버의 직접 걷는 3D 대기방도 같은 임시 공간을 사용하며, 서버가 이동·장치 접근을 검사한다. 최신 대기방 검증은 [TEST-0008](../../../../Docs/Testing/TEST-0008-NetworkWaitingRoom.md), 챕터 복제는 [TEST-0007](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md)에서 관리한다. 시작·이동 검사만으로 전체 코스의 퍼즐·출구·점수·스토리 완주를 입증하지 않는다. 런 중 신규 접속은 명시적으로 거부하며 안전 구간 중도 합류·재접속은 후속 구현 대상이다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
