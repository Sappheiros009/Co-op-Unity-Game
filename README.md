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
| 중단 시점 결과·실행 방법·남은 작업 | [프로토타입 인계서](Docs/PROTOTYPE_HANDOFF.md) · [실행 안내](Docs/PROTOTYPE_GUIDE.md) · [기획 충족도](Docs/PROTOTYPE_COVERAGE.md) |
| 실제 검사 결과 | [검증 기록](Docs/Testing/TEST-0001-PlanningPipeline.md) |
| Notion·GitHub 동기화 | [게시 상태](Docs/PUBLISH_STATUS.md) |
| 변경·버그·장애 관리 | [유지보수](Docs/Maintenance/README.md) · [버그](Docs/Bugs/README.md) · [운영](Docs/Operations/README.md) |

## 현재 범위

2026-09-20 사용자 요청으로 추가 개발 목표를 중단하고 현재까지의 작업을 게시한다. 완성·출시 선언이 아니다. 현재 소스는 `codex/00-prototype-baseline` 작업 브랜치에서 관리하며 `main` 병합 및 Notion 반영 여부는 [게시 상태](Docs/PUBLISH_STATUS.md)를 따른다.

Unity 6000.6.1f1 기반 **2D 로비 → 직접 걷는 1인칭 3D 대기방 → 선택한 Chapter01~07 → 이야기 → 대기방**의 16개 씬과 기능 확인용 기본 도형·분리된 캐릭터 파츠를 포함한다. 시험 동료 모드와 같은 PC의 별도 서버·실제 2~4인 클라이언트 모드를 구분한다. 전멸 시 임시 진행·점수를 초기화하고 2D 로비로 복귀하며 영구 진행·설정은 유지한다.

2026-09-19 전체 PlayMode 156/156, Windows 빌드, 실제 2/3/4인 전멸·재도전 및 최신 빌드의 4인 로비·완료 복귀 회귀가 통과했다. 서버 시험 배치를 포함하는 검사이며, 7챕터 실제 입력 완주·다른 PC 접속·Steam/PlayFab·저사양 성능·최종 아트·출시는 미검증 또는 미구현이다. [검증 범위와 근거](Docs/Testing/TEST-0011-NetworkWipeRetry.md)를 확인한다.

## 실행 시작점

Unity 6000.6.1f1에서 이 저장소를 열고 `Assets/Game/Levels/Lobby/PrototypeLobby.unity`를 더블클릭한 뒤 Play를 누른다. 빈 Untitled 씬에서 실행하지 않는다. 상세 조작·빌드·같은 PC 멀티 실행은 [실행 안내](Docs/PROTOTYPE_GUIDE.md)를 따른다. `Build`의 Windows 실행 파일·캐시·개인 저장·원시 로그는 이 소스 게시에 포함하지 않는다.

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
