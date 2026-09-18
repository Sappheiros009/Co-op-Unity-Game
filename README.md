# 슬라임 협동게임

2~4인의 슬라임이 이동·퍼즐·장치로 고향을 향해 나아가는 1인칭 3D 협동 모험.

처음 읽는 사람은 [통합 기획서](Plan.md)에서 게임 규칙과 제작 범위를 확인한다. 문서는 확정 사항 중심으로 정리하며 구현 상태는 별도로 표시한다.

| 확인할 내용 | 문서 |
|---|---|
| 게임 경험·규칙·콘텐츠·서비스 | [Plan.md](Plan.md) |
| 어디에서 무엇을 수정할지 | [폴더 지도](Docs/FOLDER_MAP.md) |
| 출구·팀 점수·재도전 | [출구와 팀 점수](Docs/DESIGN-0001-StageExit.md) |
| 서버·기능·저장·보안 책임 | [시스템 구조](Docs/ARCHITECTURE.md) |
| 검사·빌드·출시 절차 | [파이프라인](Docs/PIPELINE.md) |
| 구현 상태와 미정 사항 | [개발 상태](Docs/PROJECT_STATE.md) |
| 실제 검사 결과 | [검증 기록](Docs/Testing/TEST-0001-PlanningPipeline.md) |
| Notion·GitHub 동기화 | [게시 상태](Docs/PUBLISH_STATUS.md) |
| 변경·버그·장애 관리 | [유지보수](Docs/Maintenance/README.md) · [버그](Docs/Bugs/README.md) · [운영](Docs/Operations/README.md) |

## 현재 범위

GitHub의 기획 기준을 바탕으로 로컬 컴퓨터에 Unity 프로토타입 코드·`Packages`·`ProjectSettings`와 Windows 시험 빌드를 생성했다. 후속 구현은 `codex/00-prototype-baseline` 브랜치의 커밋 `660b423`으로 GitHub에 게시했으며 `main`은 변경하지 않았다. 2026-09-18에 Unity 6000.6.1f1로 16개 `.unity` 씬과 메타데이터를 재생성하고 현재 소스의 별도 Windows 검증 빌드를 성공했으며, 헤드리스 초기 기동 로그에서 엔진·물리·입력 초기화까지 확인했다. 버튼 전환 전체 회귀, PlayFab·Steam 연동, 실제 멀티플레이와 출시는 여전히 미검증·미배포다.

## 문서 검사

PowerShell 7.2 이상에서 실행한다.

```powershell
pwsh -NoProfile -File ./ProjectPipeline.ps1 -Mode Validate -WriteReport
pwsh -NoProfile -File ./Docs/Testing/Test-ProjectPipeline.ps1
pwsh -NoProfile -File ./ProjectPipeline.ps1 -Mode Readiness
```

Validate는 문서·계약·경로 검사다. Readiness는 미정 결정, 재현 가능한 Unity 테스트·빌드, 서버·출시 검증이 없어 현재 BLOCKED가 정상이다.

## 작업 지시

담당 폴더, 현재 증상, 기대 결과, 재현 조건, 실제 변경 파일, 검증 방법을 함께 기록한다. 예: StartandExit에서 전멸 후 런 점수 초기화와 영구 챕터 해금 보존을 확인하고 Save의 전후 데이터와 연결한다.

[문서 목록](Docs/README.md) · [작업 지침](AGENTS.md) · [개발 원칙 원문](Docs/PROJECT_PROMPT.md) · [Notion 기획서](https://app.notion.com/p/7d4877dbd26d83ebaadb015fd0708d3e)
