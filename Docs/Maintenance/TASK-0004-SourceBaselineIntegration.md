# TASK-0004 — GitHub 기획 기준과 로컬 Unity 프로토타입 소스 통합 준비

- 상태: 검증 대기
- 우선순위: P0 기준선 정리
- 작성·변경일: 2026-09-18
- 담당 위치: 프로젝트 전체, `Assets`, `Packages`, `ProjectSettings`, `Docs`
- 근거: GitHub의 기획안을 기준으로 AI가 로컬 컴퓨터에 제작한 Unity 프로토타입을 이후 작업 세션들이 사용할 수 있는 하나의 소스 기준선으로 정리한다.

## 현재 결정 → 이유 → 영향 → 다음 결정

- GitHub `main`의 기획·문서 이력을 상위 기준으로 유지하고, 로컬 Unity 프로토타입은 아직 게시되지 않은 후속 구현으로 취급한다.
- 2026-09-18에 확인한 GitHub `main` 아카이브는 문서·폴더 골격 67개 파일이며 Unity `Packages`, `ProjectSettings`, 프로토타입 코드·씬은 포함하지 않는다. 로컬에는 원격 파일이 모두 존재하고, 로컬에만 있는 파일은 구현·Unity 설정·메타데이터·문서 보강분이다.
- `Assets`, `Packages`, `ProjectSettings`와 구현 상태 문서는 향후 원격 통합 대상이다.
- `Assets/Game/Tests/PrototypeExitScoringTests.cs`는 먼저 확정된 출구 집계 규칙을 자동 검증하는 로컬 테스트 기준선이다.
- `Build`, `Library`, `Logs`, `UserSettings`, `_UnityTemplate`과 IDE 생성 파일은 재생성 가능한 로컬 산출물이라 Git 게시 대상에서 제외한다.
- 원격 이력을 로컬 Git에 안전하게 연결할 수 있는 인증·fetch 경로가 확보되면 원격 변경과 로컬 구현을 비교해 병합·커밋·푸시한다. 현재는 원격 커밋을 재작성하거나 로컬의 태생 없는 `master`에 독립 커밋을 만들지 않는다.

## 변경 범위

| 실제 파일·설정 | 변경 내용 | 관련 기능 |
|---|---|---|
| `Assets/Game/Editor/PrototypeSceneBuilder.cs` | 로비·대기실·챕터 7개·스토리 인터루드 7개 총 16개 씬과 Build Settings 재생성 | Editor, Levels |
| `Assets/Game/**/*.meta` | Unity 6000.6.1f1 임포트로 소스·폴더·씬 GUID 생성 | 프로젝트 전체 |
| `ProjectSettings/EditorBuildSettings.asset` | 16개 씬을 실제 진행 순서로 등록 | Editor, StartandExit, Story |
| `Packages/manifest.json`, `Packages/packages-lock.json` | 현재 Unity 패키지 의존성 해석 및 CLI 편집 파이프라인 도구 등록 | Editor |
| `.gitignore` | Unity 캐시·빌드·임시 템플릿·IDE 산출물 제외 | 저장소 운영 |
| `README.md`, `Docs/FOLDER_MAP.md`, `Docs/PROJECT_STATE.md`, `Docs/PUBLISH_STATUS.md` | GitHub 기획 기준과 로컬 구현·검증·미게시 상태를 구분 | 문서·운영 |
| `Assets/Game/Tests/PrototypeExitScoringTests.cs`, `SlimeCoop.Prototype.Tests.asmdef` | 출구 규칙의 자동 검증 경계 추가 | Tests, StartandExit |
| `Assets/Game/SlimeCoop.Prototype.Runtime.asmdef`, `Assets/Game/Editor/SlimeCoop.Prototype.Editor.asmdef` | 런타임·Editor 코드의 명시적 어셈블리 경계 구성 | Core, Editor, Tests |

## 완료 확인

- 실행 환경: Unity 6000.6.1f1, Unity CLI 1.0.0-beta.8, Windows StandaloneWindows64.
- 씬 생성: `SlimeCoop.Prototype.Editor.PrototypeSceneBuilder.BuildScenes` 성공. `.unity` 16개, C# 15개 모두 `.meta` 보유, Build Settings 16개 등록.
- 규칙 테스트: PlayMode 4개 통과(`tests=4`, `failures=0`, `errors=0`, `skipped=0`). 세부 결과는 [TEST-0002](../Testing/TEST-0002-PrototypeExitRules.md).
- 현재 소스 빌드: `SlimeCoop.Prototype.Editor.PrototypeBuild.BuildWindows`를 `Build/Verification/SlimeCoopPrototype.exe`로 별도 실행. `Succeeded`, `Errors: 0`, 검증 폴더 파일 225개, 107,054,791바이트.
- 초기 기동: 검증 빌드를 `-batchmode -nographics`로 12초 실행. Unity 6000.6.1f1, Input System, PhysX, Null graphics device 초기화 후 검사기가 종료했으며 로그에 관리 예외나 크래시는 없었다. 종료 코드 `-1`은 12초 제한 후 검사기가 프로세스를 강제 종료한 결과다.
- 기획 검사: 최종 문서 상태에서 578개 통과·실패 0개. 검사기 자체 시험 20/20 통과.
- 출시 준비 검사: `BLOCKED / 2`. 9개 기획 결정, 자동화된 Unity EditMode·PlayMode·멀티클라이언트·재현 빌드 단계, 서버·Steam·운영·출시 검증이 남아 있다. 이번 수동 검증 빌드는 성공했지만 아직 출시 준비 파이프라인의 재현 빌드 단계로 연결되지 않았다.
- 증거 위치: `Logs/scene-regeneration.log`, `Logs/verification-build.log`, `Build/Verification/smoke-player.log`. 모두 로컬 검증 산출물이며 Git에는 포함하지 않는다.
- 아직 확인하지 않은 범위: 로비→대기실→각 챕터→스토리 인터루드→대기실의 수동 전체 회귀, 실제 네트워크·PlayFab·Steam, 원격 Git 병합·CI·Notion 게시, 최종 에셋·성능.
- 이번 실행은 PlayMode 출구 규칙 4개에 한정한다. 전체 기능의 EditMode/PlayMode 회귀와 멀티클라이언트 테스트는 아직 없다.

## 완료 조건

현재 소스 재생성과 빌드 검증은 완료했다. 원격 GitHub 이력을 fetch해 충돌을 검토하고 구현 소스를 `main`에 반영한 뒤 원격 CI 결과를 확인하기 전까지 작업 상태는 `검증 대기`로 유지한다.

## 변경 이력

| 날짜 | 변경 내용 | 결과·관련 기록 |
|---|---|---|
| 2026-09-18 | 원격 기획과 로컬 구현의 기준 관계 명시, 씬·메타데이터 재생성, 별도 Windows 빌드와 헤드리스 초기 기동 검증 | 씬 16개, C# 메타 누락 0, 빌드 오류 0; 원격 통합은 대기 |

[작업 목록](README.md) · [현재 상태](../PROJECT_STATE.md) · [게시 상태](../PUBLISH_STATUS.md) · [통합 기획서](../../Plan.md)
