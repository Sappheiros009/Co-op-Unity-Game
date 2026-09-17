# 문서 게시 현황

기준일: 2026-09-17. 확정된 질문·선택지·답변을 읽기 쉬운 기획서로 전환하고 GitHub·Notion을 같은 기준으로 갱신했다.

## 반영 범위

- [통합 기획서](../Plan.md): 16개 주제의 게임 규칙·콘텐츠·서버·저장·운영 명세.
- [출구와 팀 점수](DESIGN-0001-StageExit.md): 완료된 질문·답변 없이 규칙·예외·검증 사례만 유지.
- [개발 상태](PROJECT_STATE.md): 미정 수치·정책과 구현 확인 범위를 분리.
- [구조](ARCHITECTURE.md)·[파이프라인](PIPELINE.md)·[폴더 지도](FOLDER_MAP.md)·기능 README·검사 계약 동기화.
- [개발 협업 프롬프트](PROJECT_PROMPT.md): 최초 사용자 원문 유지.

## GitHub

[기획·관리 문서 게시 커밋](https://github.com/Sappheiros009/Co-op-Unity-Game/commit/b8305b9ff5cc477afcf0a2c3316795b274fc974f)에 검토한 65개 파일을 반영했다. 기존 main 이력을 이어서 갱신했으며 강제 업데이트하지 않았다. 원격 Plan.md를 다시 읽어 로컬 게시본과 완전 일치함을 확인했다.

[GitHub Actions 실행](https://github.com/Sappheiros009/Co-op-Unity-Game/actions/runs/35231874123)은 완료·성공이다. Validate, 검사기 자체 시험, 검증 리포트 업로드가 모두 성공했다. 선택 실행인 출시 준비 job은 실행하지 않았으며 로컬 Readiness는 BLOCKED / 2다.

## Notion

| 문서 | 저장한 내용 |
|---|---|
| [통합 기획서](https://app.notion.com/p/7d4877dbd26d83ebaadb015fd0708d3e) | 기존 질문지 본문을 제거하고 확정 기획 16개 주제로 교체. 원문 프롬프트·상세 문서 묶음 하위 페이지 보존 |
| [00 기획 문서 안내](https://app.notion.com/p/3dd877dbd26d81d7ac8cf144fdc3368f) | 읽는 순서와 핵심 규칙 |
| [01 개발 상태와 상세 결정 목록](https://app.notion.com/p/3dd877dbd26d812c863efbf21023bbf1) | 미정 사항·실제 구현 상태 |
| [02 출구와 팀 점수 설계](https://app.notion.com/p/3dd877dbd26d81a582f0ddfa64cfcb4c) | 확정 규칙·상태 흐름·검증 사례 |
| [03 폴더 지도](https://app.notion.com/p/3dd877dbd26d8108ba8de7050419b6b4) | 기능·지역·관리 책임과 증상별 조사 위치 |
| [04 시스템 구조와 보안 설계](https://app.notion.com/p/3dd877dbd26d81d490c5e2a8ec607c5d) | PlayFab·서버 판정·저장·Steam 제재 경계 |
| [05 개발·검증·배포 파이프라인](https://app.notion.com/p/3dd877dbd26d81188941e66dd13f0ef2) | 실행 명령·단계별 통과 기준·운영 절차 |
| [06 프로젝트 작업 원칙](https://app.notion.com/p/3dd877dbd26d81549ef1ccffb18e076b) | 결정·자료·제작·검증 원칙 |
| [07 폴더 명칭 기준](https://app.notion.com/p/3dd877dbd26d81c0a784f9c1a34f7078) | 확정 명칭과 대소문자 |
| [08 실제 검증 결과](https://app.notion.com/p/3dd877dbd26d812aa2d1f752f3168914) | 로컬 검사·20개 자체 시험·원격 CI 근거 |
| [09 확정 기획서 개정 안내](https://app.notion.com/p/3de877dbd26d806da42edda321510454) | 최신 결정 통합과 이전 메모 정리 기준 |
| [개발 협업 프롬프트 원문](https://app.notion.com/p/aa6877dbd26d8290bf0a812ca2fdb8ee) | 기존 원문 페이지 유지, 본문 변경 없음 |

Notion 본문은 읽기 좋은 명세로 게시하고 상세 파일·README·실행 코드에는 GitHub 링크를 연결했다. 완료된 질문·보기·답변은 현재 게시 문서에서 제거했다. 과거 Git 커밋과 서비스의 정상 복구 이력까지 삭제한 것은 아니다.

## 검증의 한계

게시·문서 검사·검사기 자체 시험은 완료했지만 Unity 빌드, 게임 실행·멀티플레이, PlayFab/Steam 런타임 연동, 보안 효과, 실제 게임 배포는 이번 작업에 포함하지 않았다. [실제 검증 기록](Testing/TEST-0001-PlanningPipeline.md)을 함께 확인한다.
