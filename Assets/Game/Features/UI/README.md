# UI — 화면과 조작 안내

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
| `PrototypeWaitingRoomController.cs` | 3D 슬라임 대기실 표현 초기화, Chapter01~07 선택 조작 | `PrototypeWaitingRoom.unity` 실행 및 챕터 버튼 조작 |
| `PrototypeHud.cs` | Chapter01~07의 상태·출구·점수 안내 오버레이와 컷신 전환 안내 | 각 `PrototypeChapter0X.unity` 실행 |

공통 2D 스프라이트·카메라·캡슐 생성 보조는 `Assets/Game/Core/Prototype/PrototypeVisuals.cs`에 있습니다. 현재 UI는 흐름 확인용 오버레이이며, 최종 색상·폰트·레이아웃·아이콘 스타일은 사용자 승인 후 결정합니다.

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)
