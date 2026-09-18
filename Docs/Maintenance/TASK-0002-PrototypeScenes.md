# TASK-0002 — 로비·대기실·Chapter01 프로토타입 씬 제작

- 상태: 완료
- 우선순위: P0 프로토타입 진행
- 요청일: 2026-09-18
- 담당 위치: Unity 프로젝트의 `Assets/Game/Core`, `Assets/Game/Features`, `Assets/Game/Levels`, `Assets/Game/Editor`
- 근거: 로비창은 2D, 슬라임 대기실과 Chapter01은 3D로 구성하고, 캐릭터·몬스터는 캡슐 placeholder로 두며 맵까지 생성한다는 사용자 결정.

## 현재 결정 → 이유 → 영향 → 다음 결정

- 현재 결정: 로비는 직교 카메라와 스프라이트 기반 2D 표현으로 만들고, 슬라임 대기실과 Chapter01은 원근 카메라 기반 3D 표현으로 만든다.
- 이유와 원하는 사용자 경험: 진입·메뉴 화면은 가볍고 읽기 쉬운 2D 흐름으로 확인하고, 준비 공간과 실제 플레이 공간은 3D 공간감과 이동 동선을 먼저 검증한다.
- 영향: 씬 저장 방식, 카메라 설정, 로비의 임시 장식, 대기실·Chapter01의 기본 지오메트리와 캡슐 배치에 영향을 준다. 생성 도구가 Editor API로 배치·카메라·조명·placeholder 에셋을 저장하고, Play 시에는 저장 오브젝트를 재연결하므로 Scene 뷰에서 Play 없이 확인할 수 있다. 최종 프론트엔드 스타일·에셋·품질 설정은 사용자 결정권을 유지한다.
- 대안과 장단점: 로비까지 3D로 만들면 공간 연출을 미리 확인할 수 있지만 프로토타입의 핵심 흐름 검증에 필요한 비용과 표현 범위가 늘어난다. 현재는 사용자가 지정한 2D/3D 구분을 적용한다.
- 다음 결정 또는 미정 사항: 최종 로비 UI 스타일, 캐릭터·몬스터·보스 외형, SFX·VFX·음악·애니메이션, 실제 맵 생성 규칙, 최소 사양과 품질 프리셋 값은 승인·프로파일링 후 정한다.

## 변경 범위

| 실제 파일·설정 | 현재 동작 | 바꿀 내용 | 관련 기능 |
|---|---|---|---|
| `Assets/Game/Features/UI/PrototypeLobbyController.cs` | 로비 표현과 씬 전환을 담당 | 저장된 2D 표현 재사용 및 구형·누락 씬 보정 생성 | UI, Lobby |
| `Assets/Game/Features/UI/PrototypeWaitingRoomController.cs` | 대기실 캡슐·3D 공간과 챕터 시작 흐름을 담당 | 저장된 3D 표현 재사용 및 구형·누락 씬 보정 생성 | UI, Lobby |
| `Assets/Game/Core/Prototype/PrototypeVisuals.cs` | 공통 캡슐·카메라·재질 생성 보조 | 2D 직교 카메라·스프라이트 생성 보조 추가 | Core, UI |
| `Assets/Game/Features/MapGeneration/PrototypeMapBuilder.cs` | 고정형 3D Chapter01 테스트 맵 저장 배치와 런타임 상태 연결 | 저장된 바닥·벽·장애물·위험 요소·출구·캡슐 재연결, 누락 시 보정 생성 | MapGeneration, Chapter01 |
| `Assets/Game/Features/Player/PrototypeCapsulePlayer.cs` | 1인칭 캡슐 조작 | 로컬 이동·시점·출구 접근 흐름 제공 | Player |
| `Assets/Game/Features/Monster/PrototypeCapsuleMonster.cs` | 캡슐 몬스터 표시와 단순 동작 | 기능 확인용 위험 요소 제공 | Monster |
| `Assets/Game/Features/StartandExit/PrototypeExitScoring.cs` | 출구 진입·팀 점수 흐름 표시 | 로컬 프로토타입 출구 흐름 제공 | StartandExit |
| `Assets/Game/Editor/PrototypeSceneBuilder.cs` | 3개 씬의 저장 오브젝트·placeholder 에셋과 빌드 순서 생성 | Lobby → WaitingRoom → Chapter01 등록 | Editor |
| `Assets/Game/Editor/PrototypeBuild.cs` | 등록 씬을 Windows 빌드 | StandaloneWindows64 빌드 경로 제공 | Editor |

## 필요한 검토

- 서버·클라이언트 책임: 현재 구현은 로컬 단일 프로세스 테스트이며 서버 판정·네트워크 소유권을 구현하지 않는다. 정식 책임은 `Docs/ARCHITECTURE.md`와 서버 연동 작업에서 검토한다.
- 데이터·저장·복구: 프로토타입은 영구 저장·재접속·런 복구를 검증하지 않는다.
- 로그·분석: 씬 생성·빌드 출력과 무창 부팅 로그를 확인한다. 실제 플레이 텔레메트리와 제재 증거는 별도 설계한다.
- 성능: 캡슐·기본 재질 기반 구조이므로 최종 저사양 성능을 대표하지 않는다. 최종 에셋 적용 후 프로파일링이 필요하다.
- 적용하지 않는 항목: PlayFab·Steam 연동, 실제 멀티플레이, 최종 아트, 완성형 UI·SFX·VFX·애니메이션, 정식 랜덤 맵은 이번 작업 범위에 포함하지 않는다.

## 완료 확인

- 실행 환경·게임 버전·검증 방법: Unity 6000.6.1f1 Editor에서 `PrototypeSceneBuilder.BuildScenes`로 저장형 씬을 생성하고 `PrototypeBuild.BuildWindows`로 StandaloneWindows64를 빌드했다. 생성된 씬의 오브젝트·카메라·조명·렌더러 참조를 다시 읽어 저장 여부를 확인했고, 빌드 무창 부팅으로 런타임 재연결 경로를 점검했다. 문서 검사는 `ProjectPipeline.ps1 -Mode Validate -WriteReport`로 실행했다.
- 기대 결과: 2D 로비에서 3D 대기실로 이동하고, 대기실에서 3D Chapter01 테스트 맵을 시작할 수 있으며 Windows 빌드가 생성된다.
- 실제 결과: 저장형 씬 생성 성공. Lobby는 `GameObject 14 / Camera 1 / SpriteRenderer 11`, WaitingRoom은 `GameObject 12 / Camera 1 / Light 1`, Chapter01은 `GameObject 35 / Camera 1 / Light 1 / Renderer 참조 23`으로 저장되어 Scene 뷰에서 확인 가능하다. Windows 빌드 성공(`Errors: 0`), 빌드 무창 부팅은 12초 기동 후 정상 종료 대기 없이 안전 종료했으며 로그에서 `Error|Exception|Crash|MissingReference|NullReference` 일치 항목이 없었다. 문서 계약 검사 `547/547`, 검사기 자기 테스트 `20/20`도 통과했다.
- 재현·증거 위치: `Assets/Game/Editor/PrototypeSceneBuilder.cs`, `Assets/Game/Editor/PrototypeBuild.cs`, 생성된 `Assets/Game/Levels/Lobby/PrototypeLobby.unity`, `Assets/Game/Levels/Lobby/PrototypeWaitingRoom.unity`, `Assets/Game/Levels/Episode01/Chapter01_Mine/PrototypeChapter01.unity`, `Build/PrototypeRuntimeSmoke-Baked.log` 및 `Build/SlimeCoopPrototype.exe`.
- 아직 확인하지 않은 범위: 시각적 클릭·장면 전환의 수동 QA, 실제 멀티플레이·PlayFab·Steam, 저사양 하드웨어 프로파일링, 최종 에셋 적용, 출시 패키지 검증.
- 완료 판단과 이유: 사용자가 지정한 2D/3D 씬 구분과 캡슐·맵 기반의 로컬 프로토타입 산출물 및 빌드 검증을 완료했다. 미검증 범위는 완료로 승격하지 않고 상태 문서에 남겼다.

## 변경 이력

| 날짜 | 변경 내용 | 결과·관련 기록 |
|---|---|---|
| 2026-09-18 | 로비를 2D 표현으로 전환하고 대기실·Chapter01의 3D 프로토타입을 유지·재생성 | 저장형 씬·placeholder 에셋 생성, Windows 빌드와 무창 부팅 점검 성공; 문서 계약 검사 `547/547`, 자기 테스트 `20/20` 통과 |
| 2026-09-18 | 로비·대기실의 씬 전환 로드 이름을 실제 등록 씬 이름과 일치시킴 | 잘못된 로드 문자열 0건, 올바른 로드 문자열 4건; Windows 빌드 성공과 무창 기동 로그 오류 0건; [BUG-0001](../Bugs/BUG-0001-PrototypeSceneNameMismatch.md) |

[작업 목록](README.md) · [현재 상태](../PROJECT_STATE.md) · [통합 기획서](../../Plan.md)
