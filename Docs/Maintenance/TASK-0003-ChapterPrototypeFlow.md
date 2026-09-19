# TASK-0003 — Chapter01~07 및 스토리 컷신 프로토타입 흐름

- 상태: 완료
- 우선순위: P0 프로토타입 진행
- 요청일: 2026-09-18
- 담당 위치: Unity 프로젝트의 `Assets/Game/Core`, `Assets/Game/Features`, `Assets/Game/Levels`, `Assets/Game/Editor`
- 근거: 로비에서 대기실로 이동하고, 대기실에서 원하는 Chapter01~07을 선택하며, 챕터 종료 후 영상씬을 거쳐 다시 대기실로 돌아오는 프로토타입 흐름을 만든다는 사용자 결정.

## 현재 결정 → 영향 → 미정

- Lobby는 기존 2D 표현을 유지하고, WaitingRoom과 Chapter01~07은 3D 표현을 사용한다.
- 일곱 챕터는 공통 테스트 경로를 공유하며, 지역별로 큰 색상 팔레트만 다르게 적용한다. 최종 맵 구조·에셋·기믹은 별도 결정한다.
- 각 챕터 종료 후 `StoryInterlude_Chapter0X`로 이동한다. 현재는 실제 영상 대신 영상 삽입 위치와 Continue 버튼만 제공한다.
- 컷신을 계속하면 `PrototypeWaitingRoom`으로 복귀한다. 대기실에서 다른 챕터를 다시 선택할 수 있다.
- 실제 영상·자막·Timeline·스킵 동의 범위, 챕터별 고유 맵·몬스터·보스·네트워크는 아직 미정 또는 미구현이다.

## 프로토타입 흐름

`PrototypeLobby → PrototypeWaitingRoom → PrototypeChapter0X → PrototypeStoryInterlude_Chapter0X → PrototypeWaitingRoom`

챕터의 출구 5초 집계가 종료되면 1.5초 동안 클리어 안내를 표시한 뒤 해당 챕터 컷신 placeholder로 자동 이동한다. 컷신에서는 버튼 또는 Enter로 대기실에 복귀한다.

## 변경 범위

| 실제 파일·설정 | 변경 내용 | 관련 기능 |
|---|---|---|
| `Assets/Game/Core/Prototype/PrototypeChapterCatalog.cs` | 7개 챕터의 씬 이름·경로·지역명·색상 팔레트·컷신 연결을 단일 기준으로 관리 | Core, Levels |
| `Assets/Game/Core/Prototype/PrototypeGame.cs` | 챕터 번호를 보유하고 출구 집계 후 컷신으로 자동 전환 | StartandExit, Story |
| `Assets/Game/Features/MapGeneration/PrototypeMapBuilder.cs` | 공통 테스트 맵을 챕터 팔레트로 생성·재연결 | MapGeneration |
| `Assets/Game/Features/UI/PrototypeWaitingRoomController.cs` | Chapter01~07 선택 버튼 제공 | UI, Lobby |
| `Assets/Game/Features/Story/PrototypeStoryInterludeController.cs` | 챕터별 영상 placeholder 및 대기실 복귀 | Story, UI |
| `Assets/Game/Editor/PrototypeSceneBuilder.cs` | 로비·대기실·챕터 7개·컷신 7개 총 16개 씬 생성 및 등록 | Editor |
| `Assets/Game/Editor/PrototypeBuild.cs` | 16개 씬을 Windows 빌드에 포함 | Editor |

## 검토 범위

- 서버·클라이언트 책임: 로컬 단일 프로세스 테스트이며 서버 판정·실제 네트워크를 구현하지 않는다.
- 데이터·저장·복구: 챕터 선택·컷신 이동의 영구 저장과 재접속 복구는 구현하지 않는다.
- 성능: primitive와 단순 재질 기반이며 최종 저사양 성능을 대표하지 않는다.
- 영상: 실제 영상 파일·라이선스·인코딩·자막·Timeline은 영상 자산과 사용자 승인 후 정한다.

## 완료 확인

- 실행 환경·게임 버전·검증 방법: Unity 6000.6.1f1 Editor에서 `PrototypeSceneBuilder.BuildScenes`로 씬을 생성하고 `PrototypeBuild.BuildWindows`로 StandaloneWindows64를 빌드한다.
- 기대 결과: 대기실에서 7개 챕터 버튼을 볼 수 있고, 선택한 챕터 실행 후 컷신 placeholder를 거쳐 대기실로 돌아온다.
- 실제 결과: Unity 실제 템플릿에서 16개 씬(로비 1, 대기실 1, 챕터 7, 스토리 컷신 placeholder 7)을 생성하고 Build Profile에 모두 등록했다. Windows Standalone 빌드는 `Errors: 0`으로 성공했으며, 빌드 실행 파일의 무화면 초기 실행에서 로그상 `Error`, `Exception`, `Crash`, `MissingReference`, `NullReference` 0건을 확인했다.
- 아직 확인하지 않은 범위: 실제 영상 재생, 수동 버튼 전환 전체 회귀, 멀티플레이·PlayFab·Steam, 최종 에셋·성능.

## 이력

| 날짜 | 변경 내용 | 결과·관련 기록 |
|---|---|---|
| 2026-09-18 | 7개 챕터 팔레트 프로토타입과 챕터별 스토리 컷신 placeholder 구조 추가 | 16개 씬 생성·등록, Windows 빌드 성공, 무화면 초기 실행 오류 0건 |

[작업 목록](README.md) · [현재 상태](../PROJECT_STATE.md) · [통합 기획서](../../Plan.md)
