# Maintenance — 유지보수 작업 관리

기능 개선, 구조 정리, 의존성·도구 업데이트와 문서 변경을 관리합니다. 잘못된 동작은 Bugs, 배포 후 영향과 복구는 Operations의 기록을 연결합니다.

## 사용 방법

1. [TASK_TEMPLATE.md](TASK_TEMPLATE.md)를 참고해 실제 작업을 `TASK-번호-짧은제목.md`로 작성합니다.
2. 대상 폴더·변경 목적·영향·기획 근거·완료 기준을 먼저 적습니다.
3. 변경한 실제 파일과 확인 결과를 같은 기록에 갱신합니다.
4. 상태를 계획 → 진행 → 검증 대기 → 완료로 갱신합니다. 막힌 경우에는 보류 이유와 필요한 결정을 적습니다.
5. 아래 목록에서 기록을 찾을 수 있게 링크와 상태를 유지합니다.

## 우선순위

P0 게임 실행·핵심 기능 필수 / P1 출시 필수 / P2 출시 품질 향상 / P3 출시 이후 추가 가능.

## 작업 목록

| ID | 작업 | 상태 | 담당 위치 |
|---|---|---|---|
| [TASK-0001](TASK-0001-ProjectStructure.md) | 사용자 지정 폴더명과 프로젝트·관리 지침 반영 | 완료 | 프로젝트 전체 문서와 폴더 |
| [TASK-0002](TASK-0002-PrototypeScenes.md) | 로비·대기실·Chapter01 프로토타입 씬 제작 | 완료 | Unity 프로젝트·Core·Features·Levels |
| [TASK-0003](TASK-0003-ChapterPrototypeFlow.md) | Chapter01~07 및 스토리 컷신 프로토타입 흐름 | 완료 | Unity 프로젝트·Core·Features·Levels |
| [TASK-0004](TASK-0004-SourceBaselineIntegration.md) | GitHub 기획 기준과 로컬 Unity 프로토타입 소스 통합 준비 | 검증 대기 | 프로젝트 전체·Unity 프로젝트·게시 상태 |
| [TASK-0005](TASK-0005-FullPlanningPrototype.md) | 통합 기획 플레이어블 프로토타입 재제작 | 사용자 요청 개발 중단·현시점 인계 | Core·Features·Editor·Tests |
| [TASK-0006](TASK-0006-ReferenceSources.md) | 사용자 제공 개발 사이트의 소스 등록과 활용 기준 연결 | 완료(자료·문서) | Docs·AGENTS·ThirdParty 안내 |

[폴더 지도](../FOLDER_MAP.md) · [작업 원칙](../WORKING_PRINCIPLES.md) · [버그 관리](../Bugs/README.md) · [운영 관리](../Operations/README.md)
