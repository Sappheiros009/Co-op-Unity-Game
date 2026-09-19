# 08 UI·아트·사운드·스토리 중간점검

기준일: 2026-09-20  
상태: 조사 완료·구현 보류·최종 표현 승인 대기

## 확인 소스

- Notion: 슬라임 협동 게임 기획서
- GitHub: Sappheiros009/Co-op-Unity-Game main
- 로컬 기준 문서: AGENTS.md, Plan.md, Docs/WORKING_PRINCIPLES.md, Docs/PROJECT_STATE.md, Docs/FOLDER_MAP.md, Docs/ARCHITECTURE.md, Docs/PIPELINE.md, Docs/PROJECT_CONTRACT.json

## 상태 분류

### 확정 기획

- 2D 로비, 1인칭 3D 슬라임 대기방, 챕터 선택·준비 장치 흐름.
- HUD, 상호작용 안내, 스태미나·아이템·출구 상태 표시.
- 챕터 사이 2D 스토리 인터루드 구조.
- 판타지 이세계풍 스타일라이즈드와 저사양 우선 원칙.
- 이야기 계속하기·건너뛰기는 현재 접속 중인 전원 동의.
- 프론트엔드 최종 표현은 사용자 승인 사항.

### 미정·승인 필요

- UI 최종 레이아웃·색상·폰트·아이콘.
- 플레이어 식별 방식과 무음 위험 표시.
- 캐릭터·몬스터 외형.
- 음악·효과음 방향과 음원 조달 방식.
- 최종 대사·스토리 분기·컷신 표현.
- 출시 언어·최소 사양·최종 에셋 제작 범위.

### 실제 구현

- Features/UI: 로비·대기방·HUD·설정·로컬 기록 프로토타입.
- Audio/PrototypeCues.cs: 임시 핑·기능 알림 표현.
- Features/Story: 챕터 기억·인터루드 placeholder.
- Levels/Episode01/StoryInterludes: Chapter01~07 placeholder 씬.
- Art, Localization, Levels/Prologue: README 중심 담당 구조.

### 검증 경계

- 이전 기준 소스의 PlayMode·Windows 빌드·일부 2/3/4인 로컬 검증 기록은 존재한다.
- 현재 로컬 작업트리에는 이후 미커밋 변경이 있어 이전 결과를 현재 상태 검증으로 확대하지 않는다.
- 최종 UI·아트·사운드·컷신·Steam·PlayFab·저사양 성능은 미검증 또는 미구현.

## 소스 차이

- GitHub main 브라우저 확인 최신 표시 커밋: 9f777014.
- 로컬 HEAD: 2c4d6a8.
- 로컬 작업트리: 08 파일을 포함한 다수 미커밋 변경.
- 기존 변경은 덮어쓰지 않았으며, 이 문서 외의 변경은 이번 기록에 포함하지 않는다.

## 담당 경계

직접 수정 대상은 08 담당 폴더로 제한한다.

- Assets/Game/Features/UI
- Assets/Game/Art
- Assets/Game/Audio
- Assets/Game/Localization
- Assets/Game/Features/Story
- Assets/Game/Levels/Prologue
- Assets/Game/Levels/Episode01/StoryInterludes

공통 코드·Online·Levels 외 기능·Docs·Plan.md·Packages·ProjectSettings 변경이 필요하면 담당 채팅에 요구사항으로 인계한다.

## 다음 승인 대상

최종 표현을 확정하기 전에 UI·아트·사운드·스토리별 후보안, 비용·성능·라이선스 영향, 다른 담당 폴더 의존성을 제시한다. 사용자 승인 전에는 placeholder와 구현 명세만 유지한다.

이 문서는 구현 완료, 빌드 성공, 실제 게임 동작 성공, 최종 검증 완료를 의미하지 않는다.
