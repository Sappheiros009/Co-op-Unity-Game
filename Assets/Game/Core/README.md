# Core — 초기화와 공통 기반

시작 시 공통 초기화와 여러 기능이 함께 쓰는 기본 자료형·연결 규약을 둡니다. 시작·종료를 포함한 회차의 게임 규칙은 Features/StartandExit가 담당합니다. 기능의 소유자가 명확한 코드는 그 기능 폴더에 둡니다.

## 관련 위치

- [StartandExit — 회차 진행](../Features/StartandExit/README.md)
- [Features — 기능 담당 목록](../Features/README.md)

## 현재 프로토타입 파일

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `Prototype/PrototypeGame.cs` | Chapter01 로컬 프로토타입 흐름과 상태 표시를 조정 | `PrototypeChapter01.unity` 실행 |
| `Prototype/PrototypeParticipant.cs` | 프로토타입 참가자 상태와 출구 진입 표시를 연결 | Chapter01에서 캡슐 참가자 상태 확인 |
| `Prototype/PrototypeVisuals.cs` | 캡슐·기본 재질·3D 카메라·2D 직교 카메라·스프라이트 생성 보조 | Lobby·WaitingRoom·Chapter01 실행 |
| `Prototype/PrototypeChapterCatalog.cs` | Chapter01~07 씬 이름·지역명·placeholder 색상·스토리 씬 연결을 단일 기준으로 관리 | 대기실 챕터 선택과 챕터 씬 생성 결과 대조 |

현재 코드는 로컬 프로토타입 공통 기반입니다. 서버 권한·실제 네트워크 상태의 소유권을 대체하지 않습니다.

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
