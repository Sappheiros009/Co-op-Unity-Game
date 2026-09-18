# BUG-0001 — 프로토타입 씬 전환 이름 불일치

상태: 해결

## 위치와 영향

- 상태: 해결
- 우선순위: P0
- 영향: 진행 차단
- 담당 폴더: `Assets/Game/Features/UI`, `Assets/Game/Editor`
- 함께 확인할 폴더: `Assets/Game/Levels/Lobby`, `Assets/Game/Levels/Episode01/Chapter01_Mine`
- 관련 씬·실제 파일·설정: `PrototypeLobby.unity`, `PrototypeWaitingRoom.unity`, `PrototypeChapter01.unity`, `ProjectSettings/EditorBuildSettings.asset`
- 관련 작업·운영 문제·검증 기록: [TASK-0002](../Maintenance/TASK-0002-PrototypeScenes.md)

## 재현 환경

- 발견·마지막 확인 시각: 2026-09-18
- 게임 버전·빌드 식별자·커밋: Unity Editor 6000.6.1f1, 로컬 프로토타입 빌드
- 저장·데이터 스키마 버전: 해당 없음
- 플랫폼·기기·입력 장치·언어: Windows PC, Unity Editor Game 뷰, 한국어 Editor
- 참가 인원·호스트/클라이언트 역할: 해당 없음, 로컬 단일 프로세스
- 챕터·스테이지·생성 시드 또는 배치 조건: Lobby에서 WaitingRoom 전환
- 세션·플레이어·객체·요청 식별자: 해당 없음
- 지연·손실·접속/종료 조건: 해당 없음

## 재현 순서

1. `PrototypeLobby.unity`를 열고 Play 모드로 진입한다.
2. `Enter Slime Waiting Room` 버튼을 클릭한다.
3. `WaitingRoom` 씬을 찾을 수 없다는 `SceneManager.LoadScene` 오류가 발생한다.

- 기대 결과와 기획 근거: 로비에서 3D 슬라임 대기실로 전환한다.
- 실제 결과: 코드가 `WaitingRoom`을 요청했지만 등록된 씬 파일명은 `PrototypeWaitingRoom.unity`이므로 전환에 실패했다. 대기실의 Chapter01 전환도 같은 유형으로 `Chapter01_Mine`을 요청하고 있었다.
- 재현 빈도: 해당 코드 상태에서 매번 재현
- 최초 정상/문제 버전: 문제 상태 확인은 Unity 6000.6.1f1
- 로그·영상·스크린샷 위치: 사용자 Unity Console 오류와 해당 컨트롤러 소스

## 원인과 조치

- 확인된 사실: `EditorBuildSettings.asset`에는 `PrototypeLobby.unity`, `PrototypeWaitingRoom.unity`, `PrototypeChapter01.unity`가 모두 활성화되어 있었다.
- 추정 원인: 씬 파일의 `Prototype` 접두사를 런타임 로드 문자열에 반영하지 않았다.
- 확인된 원인: `SceneManager.LoadScene` 문자열과 실제 씬 이름이 불일치했다.
- 임시 대응과 적용 범위: 임시 대응 없이 실제 로드 문자열을 등록된 씬 이름과 일치시켰다.
- 수정한 실제 파일·설정: `PrototypeLobbyController.cs`의 로드 대상 2곳을 `PrototypeWaitingRoom`으로 변경하고, `PrototypeWaitingRoomController.cs`의 로드 대상 2곳을 `PrototypeChapter01`로 변경했다.
- 다른 기능·저장·온라인에 미치는 영향: 로컬 씬 전환 문자열만 수정했다. 서버 판정·저장·온라인 기능에는 영향을 주지 않는다.

## 해결 검증

- 동일 조건 재현 결과: 수정된 코드가 Unity Editor 스크립트 컴파일을 통과했다.
- 관련 회귀 검증: `EditorBuildSettings.asset`의 세 씬 등록 경로와 로드 문자열을 대조했고, StandaloneWindows64 빌드가 `Errors: 0`으로 성공했다.
- 로컬/실제 멀티플레이/배포 환경 중 확인한 범위: Unity Editor 컴파일·빌드 설정·로컬 Windows 빌드
- 아직 확인하지 않은 범위: Unity Editor에서 실제 버튼을 수동 클릭하는 화면 전환 QA, 실제 멀티플레이·PlayFab·Steam·배포
- 검증 결과 기록: [TASK-0002](../Maintenance/TASK-0002-PrototypeScenes.md), `Build/SlimeCoopPrototype.exe`, `Build/PrototypeRuntimeSmoke-SceneNames.log`
- 해결 또는 보류 판단: 씬 이름 불일치 문제는 해결. 수동 화면 전환 QA는 별도 확인 대상으로 남긴다.

## 이력

| 시각 | 상태·조치 | 담당자·근거 |
|---|---|---|
| 2026-09-18 | 재현 확인: `WaitingRoom`을 Build Profile에서 찾지 못함 | 사용자 Console 오류 |
| 2026-09-18 | 실제 등록 씬 이름 확인 및 로드 문자열 수정 | `EditorBuildSettings.asset`, UI 컨트롤러 2개 |
| 2026-09-18 | Unity 재컴파일·Windows 빌드 성공, 오류 0 | `PrototypeBuild.BuildWindows` 결과 |

[버그 목록](README.md) · [작업 기록](../Maintenance/TASK-0002-PrototypeScenes.md) · [폴더 지도](../FOLDER_MAP.md)
