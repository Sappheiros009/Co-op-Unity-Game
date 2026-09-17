# TASK-0001 — 폴더명과 프로젝트·관리 지침 반영

- 상태: 완료
- 우선순위: 준비 작업. 게임 기능 우선순위는 미지정.
- 요청일: 2026-09-17
- 담당 위치: 프로젝트 루트, Docs, Assets/Game, Assets/ThirdParty.
- 근거: 사용자 지정 폴더명, 읽기 쉬운 유지보수·버그·운영 구조 요청, 첨부 개발 프롬프트 반영 요청.

## 현재 결정 → 이유 → 영향 → 다음 결정

- 현재 결정: 지정한 8개 이름을 적용하고 모든 폴더에 한글 안내 문서를 둡니다. 원문 프롬프트와 적용 지침·현재 상태를 보존합니다.
- 이유: 사람이 증상과 담당 폴더를 연결해 작업을 지시하고 이후 결정의 근거를 찾게 합니다.
- 영향: 게임 기능의 이름·경로·문서 및 관리 작업의 기록 방식.
- 후속 관리: 확정 게임 규칙은 [기획서](../../Plan.md), 실제 미정 항목은 [현재 상태](../PROJECT_STATE.md)를 따릅니다.

## 반영 범위

| 위치 | 변경 |
|---|---|
| Assets/Game/Features | Monster·MapGeneration·StartandExit·Story 포함 기능별 책임 안내 |
| Assets/Game/Levels/Episode01 | Chapter02_LAVA·Chapter05_Square·Chapter06_FrozenMountain·Chapter07_Hometown 포함 지역 안내 |
| 각 폴더 README | 역할·관련 위치·실제 파일 안내 |
| Docs/FOLDER_MAP.md | 구조와 증상별 수정 위치 |
| Docs/Maintenance·Bugs·Operations·Testing | 작업·버그·장애·배포·검증 목록과 양식 |
| AGENTS.md, Docs/PROJECT_PROMPT.md, WORKING_PRINCIPLES.md, PROJECT_STATE.md, DESIGN_TEMPLATE.md | 사용자 프롬프트·적용 규칙·확정/미정 상태·설계 양식 |

## 최초 폴더 준비 작업의 검증 기록

- 정확한 대소문자와 이전 제안 이름의 폴더 부재: 통과. 사용자 지정 8개 이름을 확인했고 이전 이름의 폴더는 없습니다.
- 모든 폴더의 README 존재: 통과. 40개 폴더에 README가 있습니다.
- 문서의 상대 링크 대상 존재: 통과. Markdown 문서 54개에서 내부 파일 링크 359개를 확인했고 누락은 없습니다.
- 첨부 프롬프트 원문 보존: 통과. 줄바꿈 방식과 끝 공백을 제외한 원문이 일치하며 번호가 붙은 40개 항목을 확인했습니다.
- 게임 실행·Unity 임포트·실제 멀티플레이: 미실행. 이번 작업은 폴더·문서 준비입니다.
- 최초 작업 당시 외부 게시: 미수행. 이후 게시 결과는 [게시 상태](../PUBLISH_STATUS.md)에서 관리합니다.

[작업 목록](README.md) · [현재 상태](../PROJECT_STATE.md)
