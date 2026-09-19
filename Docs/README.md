# 기획·설계·운영 문서

| 문서 | 용도 |
|---|---|
| [통합 기획서](../Plan.md) | 확정된 게임 경험·규칙·범위 |
| [출구와 팀 점수](DESIGN-0001-StageExit.md) | 출구 판정·집계·전환·랭킹 |
| [캐릭터·게임 에셋 제작 및 조달](DESIGN-0002-CharacterAssetProduction.md) | Blender 파이프라인, 파츠 분리, 에셋 조달·라이선스·프론트엔드 승인 기준. 세부 제작 방식은 사용자 조율 중 |
| [인게임 품질 설정](DESIGN-0003-QualitySettings.md) | 품질 프리셋, 저사양 성능, 게임플레이 공정성, 설정 UI 기준. 실제 최소 사양·렌더 파이프라인은 미정 |
| [시스템 구조](ARCHITECTURE.md) | 서버·클라이언트·기능·저장·운영 책임 |
| [파이프라인](PIPELINE.md) | 문서 검사와 게임 검증·배포 단계 |
| [기계 판독 계약](PROJECT_CONTRACT.json) | 확정 규칙과 미정 항목. 런타임 게임 데이터 아님 |
| [개발 상태](PROJECT_STATE.md) | 실제 구현·검증 상태와 상세 결정 과제 |
| [폴더 지도](FOLDER_MAP.md) | 담당 폴더와 증상별 조사 위치 |
| [명칭 기준](NAMING_DECISIONS.md) | 대소문자를 포함한 확정 이름 |
| [게시 상태](PUBLISH_STATUS.md) | Notion·GitHub 반영 증거 |
| [프로토타입 인계서](PROTOTYPE_HANDOFF.md) | 개발 중단 시점 구현·증거·미완료 범위 |
| [프로토타입 실행 안내](PROTOTYPE_GUIDE.md) | Unity 시작 씬·로컬 실행·같은 PC 멀티·조작 |
| [기획 대비 충족도](PROTOTYPE_COVERAGE.md) | 확정 요구별 구현·검증·남은 작업 구분 |
| [작업 원칙](WORKING_PRINCIPLES.md) | 문서·자료·설계·협업 기준 |
| [개발 원칙 원문](PROJECT_PROMPT.md) | 사용자가 제공한 역할·전체 개발 지침 |
| [설계 양식](DESIGN_TEMPLATE.md) | 새 기능의 책임·데이터·검증 작성 |
| [Maintenance](Maintenance/README.md) | 개선·정리·업데이트 |
| [Bugs](Bugs/README.md) | 재현·원인·수정 |
| [Operations](Operations/README.md) | 장애·배포·제재·복구 |
| [Testing](Testing/README.md) | 실제 검사와 실행 근거 |

확정 문서는 서술형 명세로 유지한다. 미정 사항과 검증 결과는 각 담당 문서에 모으고 연결한다.

`DESIGN-0002`와 `DESIGN-0003`은 확정된 사용자 결정권·성능 원칙과, 사용자 승인 또는 실제 프로파일링이 필요한 세부 선택지를 분리해 기록하는 상세 설계 문서다.
