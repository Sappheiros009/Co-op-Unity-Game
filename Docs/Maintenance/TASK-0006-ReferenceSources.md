# TASK-0006 — 개발 사이트 소스 등록과 활용 기준

상태: 완료(참고자료 등록·문서 연결 범위) · 작성일: 2026-09-19 · 담당: AI 문서 정리, 사용자 최종 표현 결정

## 요청과 완료 범위

사용자가 제공한 [유용한 게임개발 사이트](https://docs.google.com/document/d/1WuDT_G57F4pICnILEmlQFfbsRwjWqCigbKr6gSSigbQ/edit?tab=t.0)를 프로젝트 참고자료로 등록하고 관련 제작 작업에서 적극 활용한다. 특정 팩의 채택·구매·외형 확정 요청과는 구분한다.

Google Docs 연결로 전체 탭 목록과 유일한 탭 `t.0`의 본문을 확인했다. 분야별 사이트, 실제 에셋 후보, 공식 조건의 확인 여부와 적용 절차는 [REFERENCE_SOURCES](../REFERENCE_SOURCES.md)에 통합했다. 원문을 그대로 게임 사양으로 편입하지 않았다.

## 실제 변경 파일

| 파일 | 변경 내용 |
|---|---|
| [REFERENCE_SOURCES.md](../REFERENCE_SOURCES.md) | 원본 식별·확인 범위, 분야별 자료 지도, 공식 페이지를 확인한 우선 후보, 잘못된 설명 교정, 도입·승인·검증 기준 |
| [Plan.md](../../Plan.md) | 통합 기획서의 자료 진입점과 우선 활용 원칙 |
| [AGENTS.md](../../AGENTS.md) | 이후 관련 작업에서 사용자 제공 자료를 먼저 확인하도록 지침 추가 |
| [WORKING_PRINCIPLES.md](../WORKING_PRINCIPLES.md) | 실제 후보·선정 근거 기록, 공식 조건 확인, 최종 사용자 결정권 유지 |
| [FOLDER_MAP.md](../FOLDER_MAP.md) | 새 문서 위치와 참고자료/실제 도입 파일의 책임 구분 |
| [DESIGN-0002](../DESIGN-0002-CharacterAssetProduction.md) | 에셋 제작·조달 설계와 참고자료 연결 |
| [ThirdParty README](../../Assets/ThirdParty/README.md) | 참고자료 연결과 미도입 상태 명시 |
| [PROJECT_STATE.md](../PROJECT_STATE.md) | 자료 등록 상태와 실제 에셋 도입 상태 분리 |
| [작업 목록](README.md) 및 이 문서 | 변경 범위·검증 결과·제외 범위 추적 |

## 검증

- 원본: 문서 제목·ID·수정 시각·전체 탭과 본문 확인. 원본 Google 문서 수정 없음.
- 사이트: KayKit·Quaternius·Kenney의 특정 팩과 Game-icons·Poly Haven·ambientCG·Pixabay·Freesound·Zapsplat·Incompetech·Unity URP의 공식 설명 확인. 확인 링크와 적용 한계는 참고자료에 기록.
- 접근 제한: Game UI Database 본문은 robots 제한으로 미확인. 이름만 등록한 나머지 도구도 현재 가격·기능·개별 파일의 권리를 확인한 것으로 표시하지 않음.
- 문서·계약 검사: `pwsh -NoProfile -File ./ProjectPipeline.ps1 -Mode Validate -WriteReport` 통과(690/690, 실패 0). 재생성되는 결과 파일은 `Docs/Testing/LatestPipelineReport.json`.
- 변경한 기존 문서의 `git diff --check`에서 공백 오류 없음. Git의 LF/CRLF 변환 안내는 검사 실패가 아님.
- 새 게임 코드·씬·패키지 변경이 없으므로 이번 작업에서 Unity 빌드·플레이 회귀는 실행하지 않음. 문서 검사 통과를 게임 동작 검증으로 해석하지 않음.

## 제외한 변경과 이후 적용

에셋 다운로드·설치·구매, 생성형 서비스 업로드, 원본 Google Docs 수정, GitHub·Notion 게시는 수행하지 않았다. 에셋 조달 검수 기준에 따라 참고·후보·실제 도입·최종 디자인 승인을 나눴다. 외형·UI·소리의 최종 선택은 사용자에게 남긴다.

후속 제작에서는 관련 사이트의 개별 자료를 찾아 기능·비용·성능·권리와 함께 비교하고, 실제 채택 시 승인·적용 파일·검증 근거를 해당 기능 README와 유지보수 기록에 연결한다. 프로토타입 전체 구현의 남은 항목은 [TASK-0005](TASK-0005-FullPlanningPrototype.md)에서 별도로 관리한다.

[작업 목록](README.md) · [개발 참고자료](../REFERENCE_SOURCES.md) · [통합 기획서](../../Plan.md)
