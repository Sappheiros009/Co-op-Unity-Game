# 문서 게시 현황

기준일: 2026-09-18. 확정 기획을 `Plan.md`에 통합하고, 상세 설계·상태·운영 문서의 연결을 정리한 뒤 GitHub와 Notion을 같은 기준으로 갱신했다.

## 반영 범위

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

GitHub 게시 이후 해당 기획을 바탕으로 AI에게 요청해 로컬 Unity 프로토타입을 제작했다. 이 후속 구현에는 C# 프로토타입 코드, `Packages`, `ProjectSettings`, Windows 시험 빌드가 포함된다. 기준 커밋 `660b423`과 후속 문서 검증 커밋 `9715860`을 `codex/00-prototype-baseline` 작업 브랜치에 게시했으며 GitHub `main`과 Notion은 아직 갱신하지 않았다.

2026-09-18 Unity 6000.6.1f1에서 16개 `.unity` 씬과 메타데이터를 재생성하고 Build Settings에 등록했다. 현재 소스로 별도 StandaloneWindows64 검증 빌드를 만들었으며 결과는 `Succeeded`, 오류는 0건이다. 새 실행 파일을 헤드리스로 짧게 기동해 엔진·입력·물리 초기화 로그와 예외 부재를 확인했지만 강제 종료한 스모크 검사이므로 화면 전환 전체 회귀를 뜻하지 않는다.

이 로컬 후속 구현과 검증 기록은 작업 브랜치에 게시했지만 아직 GitHub `main`과 Notion에는 반영하지 않았다. `Build`·Unity 캐시·`_UnityTemplate`은 게시 대상에서 제외하고, `Assets`·`Packages`·`ProjectSettings`와 관련 문서만 작업 브랜치에 통합했다. 이 절은 `main` 병합 전 원격 기준과 로컬 후속 작업의 차이를 기록한다.

2026-09-18 현재 `main` 아카이브와 로컬 파일을 비교한 결과, 원격 파일 67개는 모두 로컬에 존재했고 원격에만 있는 파일은 없었다. 로컬 추가분은 프로토타입 구현·씬·메타데이터·Unity 설정 및 후속 상태 문서다. 공식 Git for Windows로 `origin/main` 커밋 `f19a012d`를 fetch했고, 기준 커밋 `660b423`과 검증 기록 커밋 `9715860`을 `codex/00-prototype-baseline`에 push했다. GitHub API에서도 브랜치가 `9715860`을 가리키고, `main`은 `f19a012`로 그대로이며 이 브랜치의 PR은 없음을 확인했다.

로컬 후속 검증으로 `PrototypeExitScoringTests` PlayMode 4개가 모두 통과했으며, 테스트 어셈블리 분리 후 Windows 검증 빌드도 성공했다. 이는 출구 규칙과 로컬 빌드 재현성의 증거이지 서버 권한·멀티플레이·출시 검증의 증거는 아니다.
