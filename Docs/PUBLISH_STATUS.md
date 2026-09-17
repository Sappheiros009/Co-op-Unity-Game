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

- [Run 5](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35251892943): Docs 정리 커밋에 의해 실행되었으며 이 상태 문서 작성 시점에는 진행 중이었다.
- [Run 4](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35251837113): 더 최신 커밋이 올라와 취소된 실행이다.
- [Run 3](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35248222352): 프론트엔드 설계 추가 커밋의 문서 검사·자체 시험 성공 근거.

로컬 최신 검증은 문서 검사 543개 통과·실패 0개, 검사기 자체 테스트 20/20 통과다. 이는 문서·계약·경로 검사 결과이며 Unity 게임 실행·멀티플레이·보안 효과·출시 성공을 의미하지 않는다. 출시 준비 검사는 Unity 프로젝트와 실행 증거가 없어 `BLOCKED / 2`가 정상이다.

## Notion

- [통합 기획서](https://app.notion.com/p/7d4877dbd26d83ebaadb015fd0708d3e): 17번 `프론트엔드 결정권 및 품질 설정`을 유지하고, 16번 문서 목록에 `DESIGN-0002/0003` GitHub 링크를 추가했다.
- Notion 본문은 확정 규칙·책임·검증 기준 중심으로 유지한다. 캐릭터·몬스터·보스 외형, UI/UX, VFX·SFX·음악·애니메이션·카메라·조명·후처리의 세부 표현은 사용자 승인 전 미정으로 남긴다.
- [개발 협업 프롬프트 원문](https://app.notion.com/p/aa6877dbd26d8290bf0a812ca2fdb8ee)은 참고 문서로 보존하고 확정 기획과 분리한다.

## 검증의 한계

게시·문서 검사·검사기 자체 시험은 완료했지만 Unity 빌드, 게임 실행·멀티플레이, PlayFab/Steam 런타임 연동, 핵 탐지·제재 효과, 실제 게임 배포는 이번 작업에 포함하지 않았다. [실제 검증 기록](Testing/TEST-0001-PlanningPipeline.md)과 GitHub Actions 실행 결과를 함께 확인한다.
