# Core — 초기화와 공통 기반

시작 시 공통 초기화와 여러 기능이 함께 쓰는 기본 자료형·연결 규약을 둡니다. 시작·종료를 포함한 회차의 게임 규칙은 Features/StartandExit가 담당합니다. 기능의 소유자가 명확한 코드는 그 기능 폴더에 둡니다.

## 관련 위치

- [StartandExit — 회차 진행](../Features/StartandExit/README.md)
- [Features — 기능 담당 목록](../Features/README.md)

## 현재 프로토타입 파일

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `Prototype/PrototypeGame.cs` | 7챕터의 로컬/서버용 생성 맵 구성·참가자 등록·정산·전환 조정 | PlayMode 씬 회귀, [서버 참가자 검사](../../../Docs/Testing/TEST-0005-ServerPlayerControl.md) |
| `Prototype/PrototypeParticipant.cs` | 프로토타입 참가자 상태와 출구 진입 표시를 연결 | Chapter01에서 캡슐 참가자 상태 확인 |
| `Prototype/PrototypeVisuals.cs` | 캡슐·기본 재질·3D 카메라·2D 직교 카메라·스프라이트 생성 보조 | Lobby·WaitingRoom·Chapter01 실행 |
| `Prototype/PrototypeChapterCatalog.cs` | Chapter01~07 씬 이름·지역명·placeholder 색상·스토리 씬 연결을 단일 기준으로 관리 | 대기실 챕터 선택과 챕터 씬 생성 결과 대조 |
| `Prototype/PrototypeTuning.cs`, `Prototype/Resources/PrototypeBalance.json` | 미승인 시험 수치의 별도 데이터 | PlayMode·최종 배점과 구분 |
| `Prototype/PrototypeCaptureRunner.cs` | 명시적 QA 인자가 있을 때만 카메라/UI 오프스크린 캡처·균일색 검사 | Windows QA 실행, 직접 이미지 검수 |
| `Prototype/PrototypeFrameCapture.cs` | 일반/멀티 대기실에서 공유하는 URP 오프스크린 캡처·렌더 자원 복원 | QA PNG 직접 검수. 실제 창·FPS 증거와 구분 |

현재 코드는 로컬 프로토타입 공통 기반입니다. 서버 권한·실제 네트워크 상태의 소유권을 대체하지 않습니다.

`ConfigureServerPlayers`는 시험 동료 대신 독립 플레이어를 생성하고, `ServerStageEnded`는 서버 조정자에게 구간 종료를 알리는 연결 지점입니다. 로컬 씬 자동 이동과 분리했지만 실제 네트워크 서버 조정자는 아직 연결 전입니다.

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
