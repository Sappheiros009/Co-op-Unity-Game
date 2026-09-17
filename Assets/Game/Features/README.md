# Features — 기능별 구현

게임 규칙을 기능별로 관리합니다. 폴더명은 담당 범위를 찾기 위한 이름이며, 폴더만으로 코드 참조나 실행 순서가 자동으로 제한되지는 않습니다. 구현 시 연결 규약을 함께 관리합니다.

## 관련 위치

- [Player — 플레이어](Player/README.md)
- [Cooperation — 협동 행동](Cooperation/README.md)
- [Interaction — 상호작용](Interaction/README.md)
- [Items — 아이템과 소지품](Items/README.md)
- [Puzzles — 퍼즐과 장치](Puzzles/README.md)
- [Monster — 몬스터](Monster/README.md)
- [MapGeneration — 맵 생성](MapGeneration/README.md)
- [StartandExit — 시작·종료와 회차 진행](StartandExit/README.md)
- [Story — 이야기](Story/README.md)
- [Online — 온라인 연결](Online/README.md)
- [Save — 저장과 복구](Save/README.md)
- [UI — 화면과 조작 안내](UI/README.md)

## 파일과 작업 안내

새 기능의 책임·Owner·Authority·동기화·영구/임시 상태는 [설계 양식](../../../Docs/DESIGN_TEMPLATE.md)으로 필요한 기능부터 정의합니다. 네트워크 구조는 PlayFab의 운영자 전용 서버이며 패키지·세부 동기화는 구현 시 결정합니다.

현재는 폴더 안내 단계입니다. 실제 파일을 추가할 때 아래 표에 경로·역할·확인 방법을 함께 기록합니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
