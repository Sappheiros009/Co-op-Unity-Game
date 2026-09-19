# UI — 화면과 조작 안내

## 다음 작업자용 빠른 안내

- 쉬운 이름: 플레이어가 보는 화면.
- 수정 시작점: [PrototypeLobbyController.cs](PrototypeLobbyController.cs).
- 현재 담당: 로비·대기방·HUD·설정 표시; PrototypeSettings.cs의 로컬 설정.
- 이 폴더만으로 처리하지 않는 범위: 서버 클리어·점수·제재 판정.
- 함께 확인: Player, Online, Story, Audio.
- 지시 예시: “대기방 조작 안내 수정 → Features/UI → PrototypeLobbyController.cs → Player, Online, Story, Audio와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

## 담당 범위

- 로비·준비·설정 화면, 상태창, 소지품, 스태미나, 상호작용 안내를 담당합니다.
- 각 기능이 결정한 상태를 표시하고 사용자의 요청을 전달합니다. 체력·아이템·문 개방의 실제 규칙은 해당 기능이 담당합니다.

## 함께 확인할 폴더

- [Player](../Player/README.md): 플레이어 상태를 표시합니다.
- [Interaction](../Interaction/README.md): 대상 이름과 조작 가능 여부를 표시합니다.
- [Items](../Items/README.md): 소지품을 표시합니다.
- [StartandExit](../StartandExit/README.md): 현재 진행과 결과를 표시합니다.
- [Online](../Online/README.md): 접속·채팅 상태를 표시합니다.
- [Story](../Story/README.md): 대사와 선택지를 표시합니다.
- [Save](../Save/README.md): 사용자 설정을 불러오고 저장을 요청합니다.

## 문제가 생겼을 때

화면 크기, 언어, 입력 장치, UI가 받은 값과 실제 기능 값을 비교합니다. 글자 누락은 Localization도 확인합니다.

작업 지시 예시: “UI에서 조준 대상 이름과 조작 키 안내가 겹치는 현상을 해상도·언어별로 확인해주세요.”

## 현재 프로토타입 구현 파일

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeLobbyController.cs` | 2D 로비 표현 초기화, 게임 시작 조작, 대기실 씬 전환 | `PrototypeLobby.unity` 실행 및 Enter/Start 조작 |
| [PrototypeLocalNetworkPanel.cs](PrototypeLocalNetworkPanel.cs) | 같은 PC 서버 만들기/참가·이름/포트·취소/재시도 | [TEST-0010](../../../../Docs/Testing/TEST-0010-LocalLobbyEntry.md) |
| `PrototypeWaitingRoomController.cs` | 직접 걷는 3D 대기방, Chapter01~07 근접 선택과 준비 장치 출발 | `PrototypeWaitingRoom.unity`에서 이동 → 선택 지점 E → 준비 장치 E |
| `PrototypeHud.cs` | Chapter01~07의 상태·출구·점수 안내 오버레이와 컷신 전환 안내 | 각 `PrototypeChapter0X.unity` 실행 |

공통 2D 스프라이트·카메라·캡슐 생성 보조는 `Assets/Game/Core/Prototype/PrototypeVisuals.cs`에 있습니다. 현재 UI는 흐름 확인용 오버레이이며, 최종 색상·폰트·레이아웃·아이콘 스타일은 사용자 승인 후 결정합니다.

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeRecordsPanel.cs`: 기억 페이지와 챕터·배점 버전별 로컬 팀 순위표. 대기방은 큰 고정 메뉴를 제거하고 3D 공간의 장치를 이용한다. 걷는 화면에는 현재 선택·인원·조작 안내만 남긴다. UI 스타일은 시험용이며 최종 승인 대상이다.

`PrototypeWaitingRoomStation.cs`: 장치 종류·챕터 번호·월드 라벨과 거리·시야·가림 검사. 표지판만 플레이어를 향해 수평 회전하며 실제 장치·충돌체는 움직이지 않는다. [PrototypeWaitingRoomWalker](../Player/PrototypeWaitingRoomWalker.cs)는 대기방의 1인칭 걷기·달리기·점프·앉기·충돌을 담당한다. [대기방 검사](../../Tests/PrototypeWaitingRoomTests.cs)는 챕터 선택과 준비를 분리하고 멀리서 사용하거나 벽 너머 사용하지 못하는지, 표지판이 뒤집히거나 자동 줄바꿈되지 않는지 검사한다.

`PrototypeUi.cs`: Canvas·TMP 한글·EventSystem 공통 구성. LobbyController·WaitingRoomController·Hud: 화면 흐름. Settings·SettingsPanel: 로컬 품질·음량·키 변경·적용 취소. 외형은 사용자 미승인 시험 표현.

[PrototypeWaitingRoomLayout](PrototypeWaitingRoomLayout.cs)은 일반 로컬 실행과 실제 참가자 서버의 방·벽·챕터/준비 장치를 공유한다. 멀티 대기방의 표시·준비창은 [NetworkWaitingClient](../Online/PrototypeNetworkWaitingClient.cs), 서버의 위치·접근 판정은 Online에서 맡는다. 로컬 시험 인원/동료 장치를 멀티 기능으로 표시하지 않는다. 근거: [TEST-0008](../../../../Docs/Testing/TEST-0008-NetworkWaitingRoom.md).

`PrototypeSaveNotice.cs`는 미저장·원본 보호 상태를 로비/대기실/HUD에서 표시한다. 설정 → 소리·접근성의 재시도는 현재 메모리의 미저장 진행을 저장하며 새 게임으로 초기화하지 않는다. `PrototypeSettingsPanel.cs`는 미리보기 → 유지 확인을 구분하고, 값 재편집 시 재확인·15초 경과/닫기 시 복원·쓰기 실패 시 이전 값 복원을 수행한다. 키 파일은 기본 키와의 충돌, 중복 행동, 예약 키, 잘못된 키 값을 검사한다.

실제 서버 참가자의 챕터 HUD는 [PrototypeNetworkClientWorld](../Online/PrototypeNetworkClientWorld.cs)가 담당한다. 서버의 상태·본인 소지품·팀 명단을 읽고 키 안내는 각 클라이언트 설정으로 표시한다. 안내 배경은 밝은 캐릭터와 글자가 겹칠 때의 대비를 확보한다. 챕터 1의 시험 배치 완료·이야기 동의·복귀는 [TEST-0009](../../../../Docs/Testing/TEST-0009-NetworkCompletion.md)에서 확인했으며 전체 코스 실제 입력 완주와 구분한다.

검증 위치: [PrototypeSettingsUiTests.cs](../../Tests/PrototypeSettingsUiTests.cs), [PrototypePersistenceTests.cs](../../Tests/PrototypePersistenceTests.cs).

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
