# 문서 게시 현황

기준일: 2026-09-20. 추가 개발 중단과 현시점 소스·문서 게시를 분리해서 기록한다. 과거 게시 이력을 이번 반영 완료로 간주하지 않는다.

## 현재 게시 — 개발 중단 시점

사용자 요청으로 추가 개발 목표를 중단하고 현재 결과만 게시한다. [인계서](PROTOTYPE_HANDOFF.md)에 구현 범위·실행 방법·검증·미완료 항목을 정리했다. GitHub의 소스 커밋과 인계서 원문, 원본 Notion 기획서와 새 인계 페이지의 저장 결과를 원격에서 다시 확인했다. 두 서비스의 현시점 소스·문서 게시는 완료했으며 게임 완성·출시·main 병합·개발 재개를 의미하지 않는다.

- GitHub: [소스 게시 커밋 eb3779a](https://github.com/Sappheiros009/Co-op-Unity-Game/commit/eb3779aa2f6be7e0f165af961de7e92b6cabdb79)를 `codex/00-prototype-baseline`에 push했다. 직전 브랜치 기준 517개 파일 변경이며 원격 PR의 head SHA와 인계서 내용을 재확인했다. [PR #1](https://github.com/Sappheiros009/Co-op-Unity-Game/pull/1)을 생성해 이 작업에 연결했다. PR에는 앞선 기준선 커밋도 포함된다. `main`은 `f19a012` 그대로이며 자동 병합하지 않았다.
- Notion: 연결된 두 계정 중 개인 계정을 명시하여 `황혼의장막님의 워크스페이스`의 원본 접근을 확인했다. [슬라임 협동 게임 기획서](https://app.notion.com/p/7d4877dbd26d83ebaadb015fd0708d3e)에 확정 대기방·스토리 합의·추가 승인 원칙과 현재 문서 링크를 반영하고, 그 아래 [프로토타입 인계 — 2026-09-20](https://app.notion.com/p/3e0877dbd26d813a8e97da5412fcc584)를 생성했다. 기존 협업 프롬프트와 하위 페이지는 보존했다. 학교 계정을 해제하거나 새 워크스페이스에 대체 문서를 만들지 않았다.
- 대상: Unity 소스·16개 씬·설정·스크립트·기획·검증 요약. 제외: 실행 파일·Unity 캐시·원시 로그·개인 저장/IDE 설정·비밀 키·PEAK 추출 데이터. 외부 포함물은 [ThirdParty 고지](../Assets/ThirdParty/NOTICES.md)를 보존한다.
- 게임 증거: 2026-09-19 PlayMode 156/156, Windows 빌드, 2/3/4인 전멸·재도전, 최신 빌드 4인 완료 복귀·일반 로비 회귀. [TEST-0011](Testing/TEST-0011-NetworkWipeRetry.md)과 원본 XML·보고서·DLL 해시를 대조했다. 전체 게임·Steam/PlayFab·출시 검증이 아니다.
- 게시 검증: 로컬 Validate 974개 통과·실패 0, 검사기 자체 테스트 22/22 통과. Readiness는 `BLOCKED`이며 상세 결정·원격 게임/운영 검증이 남아 있다. 코드·문서 diff 검사는 통과했고 Unity 생성 씬의 공백은 재작성하지 않았다.
- 원격 CI: 게시 기록 커밋 `dc4501f`의 [PR 검사 Run 35451243666](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35451243666)는 `completed / success`다. `planning-validation`의 문서 검사·검사기 자체 시험·증거 업로드가 모두 성공했고 `release-readiness` 작업은 `skipped`였다. 문서 전용 워크플로이며 Unity 게임 CI나 출시 준비 성공이 아니다. 이후 게시 확인 문서 커밋의 CI는 해당 PR에서 별도 확인한다.

### Notion 저장 결과 확인 — 2026-09-20

- [원본 기획서](https://app.notion.com/p/7d4877dbd26d83ebaadb015fd0708d3e): 2D 로비·직접 걷는 3D 대기방·챕터 선택 지점·중앙 준비 장치·챕터별 이야기 복귀를 반영했다. 현재 접속자 전원 동의와 출구 시작 명단 고정을 구분하고, 개발 참고자료·승인 절차·인계 문서를 연결했다.
- [프로토타입 인계](https://app.notion.com/p/3e0877dbd26d813a8e97da5412fcc584): 구현 범위, 156개 게임 회귀·빌드·실제 2/3/4인 검사 근거, 실행 방법, 미완료·검증 한계, 게시 제외 대상을 기록했다. 원본 기획서 아래에 생성된 것과 본문을 다시 읽어 확인했다.
- [최신 기획·구조·파이프라인](https://app.notion.com/p/3dd877dbd26d809784b2ec15f25bb1c0): 기존 9개 하위 문서를 유지하고 최신 인계 기준과 소스·문서 안내를 추가했다.
- [개발 상태와 상세 결정 목록](https://app.notion.com/p/3dd877dbd26d812c863efbf21023bbf1): 과거의 게임 코드·씬 없음 상태를 현시점 결과로 교체하고 추가 개발 중단과 미완료 범위를 명시했다. 확정된 이야기 합의 기준을 미정 항목과 분리했다.
- [실제 검증 결과](https://app.notion.com/p/3dd877dbd26d812aa2d1f752f3168914): 최신 게임·문서·원격 CI 결과와 한계를 추가하고 기존 2026-09-17 검증은 과거 이력으로 구분했다.
- 네 페이지의 비동기 수정 완료 후 본문을 다시 읽어 목표 내용과 서식 정규화 대조를 통과했다. 기존 하위 페이지 보존을 확인했으며 응답에 잘림·알 수 없는 블록은 없었다. 질문·선택지 대신 확정 규칙과 인계 사실을 게시했다.
- 함께 전달된 `2인 1캐릭터 협동 조작 설계 도면 v0.1`은 `Coop Mod 설계도` 아래의 별도 문서다. 원본 슬라임 기획서와 혼합하거나 수정하지 않았다. 연결 확인은 실제 대상 페이지 접근으로 판정하며 계정 연결 화면만으로 게시 완료를 선언하지 않는다.

## 과거 게시 이력 — 2026-09-18~19

이하의 ‘현재’, ‘최신’, ‘미게시’ 표현과 검사 수는 각 과거 기록 시점을 뜻한다. 이번 개발 중단 시점의 상태는 위 현재 게시 절과 인계서를 우선한다.

## 반영 범위

### 후속 게시 대기 — 2026-09-19

사용자 요청: 통합 프로토타입 제작·검증 완료 후 GitHub와 Notion에도 추가한다. 현재 후속 프로토타입과 개발 사이트 참고자료는 로컬 변경이며, 아래 원격 게시 이력을 이번 결과로 간주하지 않는다. 구현·검증 완료 → 게시 대상/권한/라이선스 점검 → GitHub·Notion 반영 → 원격 내용 재확인 순서로 진행한다. 진행 상태는 [TASK-0005](Maintenance/TASK-0005-FullPlanningPrototype.md)에서 관리한다.

일반 로비에서 실제 로컬 서버를 생성·참가하고 3D 대기방을 왕복하는 후속 구현도 아직 로컬 변경이다. 당시 실행 파일·150개 회귀·2인/4인 메뉴 연결 증거는 [TEST-0010](Testing/TEST-0010-LocalLobbyEntry.md)에 있으며, 원격 GitHub·Notion 반영 완료를 뜻하지 않는다.

이후 전멸 임시 상태·위험 판정 보완과 156개 회귀는 [TEST-0011](Testing/TEST-0011-NetworkWipeRetry.md)에서 관리한다. 해당 로컬 소스·문서·실행 파일도 아직 GitHub·Notion에 게시하지 않았다.

### 기존 게시 기준선

- [통합 기획서](../Plan.md): 게임 규칙·콘텐츠·서버·저장·운영·프론트엔드 원칙을 17개 주제로 통합.
- [캐릭터·게임 에셋 제작 및 조달](DESIGN-0002-CharacterAssetProduction.md): Blender 제작 순서, 파츠 분리, 에셋 조달·라이선스 기준. 세부 표현은 사용자 승인 대기.
- [인게임 품질 설정](DESIGN-0003-QualitySettings.md): 품질 프리셋, 저사양 성능, 게임플레이 공정성 기준. 실제 최소 사양과 값은 프로파일링 대기.
- [문서 목록](README.md)·[폴더 지도](FOLDER_MAP.md)·[개발 상태](PROJECT_STATE.md): 상세 설계 문서를 찾을 수 있도록 연결하고 확정 결정권과 미정 세부 항목을 분리.
- [개발 협업 프롬프트](PROJECT_PROMPT.md): 사용자가 제공한 원문 참고 문서 유지.

## GitHub

- [Plan·통합 문서 구조 정리 커밋](https://github.com/Sappheiros009/Co-op-Unity-Game/commit/45476615feee64cc2a53e8f26fe809b058b16183): `Plan.md`를 17개 주제와 새 상세 설계 링크까지 반영.
- [Docs 인덱스·상태 정리 커밋](https://github.com/Sappheiros009/Co-op-Unity-Game/commit/67295366a1dec924fa55ec25b7185ce827c08769): `Docs/README.md`, `FOLDER_MAP.md`, `PROJECT_STATE.md` 반영.
- [프론트엔드 설계 추가 커밋](https://github.com/Sappheiros009/Co-op-Unity-Game/commit/9afde55191d04b588d11bfafcac136aabb9d274e): `DESIGN-0002/0003`과 품질·사용자 결정권 원칙 반영.

모든 변경은 `main`에 직접 반영했으며 강제 업데이트는 사용하지 않았다. 현재 원격 `Plan.md`는 291줄이며 17번 프론트엔드 결정권·품질 설정과 `DESIGN-0002/0003` 링크를 포함한다.

### GitHub Actions

- [Run 35347265897](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35347265897): 상태 기록 커밋 `4c2edcd`의 Planning validation 성공.
- [Run 35346784505](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35346784505): `codex/00-prototype-baseline` 최신 커밋 `9715860`의 Planning validation 성공. 문서·계약 검사 전용이며 Unity 게임 빌드는 하지 않는다.
- [Run 35345658890](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35345658890): 문서 게시 후속 커밋 `f977432`의 Planning validation 성공.
- [Run 5](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35251892943): Docs 정리 커밋에 의해 실행되었으며 이 상태 문서 작성 시점에는 진행 중이었다.
- [Run 4](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35251837113): 더 최신 커밋이 올라와 취소된 실행이다.
- [Run 3](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35248222352): 프론트엔드 설계 추가 커밋의 문서 검사·자체 시험 성공 근거.

로컬 최신 검증은 문서 검사 578개 통과·실패 0개, 검사기 자체 테스트 20/20 통과다. 이는 문서·계약·경로 검사 결과다. 출시 준비 검사는 9개 미정 결정과 자동화된 Unity 테스트·재현 빌드 단계, 서버·Steam·운영·출시 검증이 남아 `BLOCKED / 2`가 정상이다.

## Notion

- [통합 기획서](https://app.notion.com/p/7d4877dbd26d83ebaadb015fd0708d3e): 17번 `프론트엔드 결정권 및 품질 설정`을 유지하고, 16번 문서 목록에 `DESIGN-0002/0003` GitHub 링크를 추가했다.
- Notion 본문은 확정 규칙·책임·검증 기준 중심으로 유지한다. 캐릭터·몬스터·보스 외형, UI/UX, VFX·SFX·음악·애니메이션·카메라·조명·후처리의 세부 표현은 사용자 승인 전 미정으로 남긴다.
- [개발 협업 프롬프트 원문](https://app.notion.com/p/aa6877dbd26d8290bf0a812ca2fdb8ee)은 참고 문서로 보존하고 확정 기획과 분리한다.

## 검증의 한계

원격 게시 기준선에서는 Unity 빌드와 게임 실행을 확인하지 않았다. 이후 로컬 후속 구현에서 Windows 검증 빌드와 헤드리스 초기 기동까지 확인했지만 화면 흐름 전체 회귀·멀티플레이·PlayFab/Steam 런타임 연동·핵 탐지와 제재 효과·실제 게임 배포는 확인하지 않았다. [실제 검증 기록](Testing/TEST-0001-PlanningPipeline.md), [TASK-0004](Maintenance/TASK-0004-SourceBaselineIntegration.md), GitHub Actions 실행 결과를 구분해 확인한다.

## 로컬 후속 구현 상태

GitHub 게시 이후 해당 기획을 바탕으로 AI에게 요청해 로컬 Unity 프로토타입을 제작했다. 이 후속 구현에는 C# 프로토타입 코드, `Packages`, `ProjectSettings`, Windows 시험 빌드가 포함된다. 기준 커밋 `660b423`과 후속 검증 기록 커밋 `9715860`, `4c2edcd`를 `codex/00-prototype-baseline` 작업 브랜치에 게시했으며 GitHub `main`과 Notion은 아직 갱신하지 않았다.

2026-09-18 Unity 6000.6.1f1에서 16개 `.unity` 씬과 메타데이터를 재생성하고 Build Settings에 등록했다. 현재 소스로 별도 StandaloneWindows64 검증 빌드를 만들었으며 결과는 `Succeeded`, 오류는 0건이다. 새 실행 파일을 헤드리스로 짧게 기동해 엔진·입력·물리 초기화 로그와 예외 부재를 확인했지만 강제 종료한 스모크 검사이므로 화면 전환 전체 회귀를 뜻하지 않는다.

이 로컬 후속 구현과 검증 기록은 작업 브랜치에 게시했지만 아직 GitHub `main`과 Notion에는 반영하지 않았다. `Build`·Unity 캐시·`_UnityTemplate`은 게시 대상에서 제외하고, `Assets`·`Packages`·`ProjectSettings`와 관련 문서만 작업 브랜치에 통합했다. 이 절은 `main` 병합 전 원격 기준과 로컬 후속 작업의 차이를 기록한다.

2026-09-18 현재 `main` 아카이브와 로컬 파일을 비교한 결과, 원격 파일 67개는 모두 로컬에 존재했고 원격에만 있는 파일은 없었다. 로컬 추가분은 프로토타입 구현·씬·메타데이터·Unity 설정 및 후속 상태 문서다. 공식 Git for Windows로 `origin/main` 커밋 `f19a012d`를 fetch했고, 기준 커밋 `660b423`과 검증 기록 커밋 `9715860`, `4c2edcd`를 `codex/00-prototype-baseline`에 push했다. GitHub API에서도 브랜치가 `4c2edcd`를 가리키고, `main`은 `f19a012`로 그대로이며 이 브랜치의 PR은 없음을 확인했다.

로컬 후속 검증으로 `PrototypeExitScoringTests` PlayMode 4개가 모두 통과했으며, 테스트 어셈블리 분리 후 Windows 검증 빌드도 성공했다. 이는 출구 규칙과 로컬 빌드 재현성의 증거이지 서버 권한·멀티플레이·출시 검증의 증거는 아니다.
