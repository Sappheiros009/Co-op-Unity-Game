# StoryInterludes — 챕터 사이 영상씬

챕터 종료 후 다음 챕터로 넘어가기 전에 스토리 영상을 재생할 위치입니다. 현재는 실제 영상 파일 대신 색상 배경·챕터 제목·영상 삽입 안내·대기실 복귀 버튼만 제공하는 2D 프로토타입입니다.

## 흐름

`Chapter0X → PrototypeStoryInterlude_Chapter0X → PrototypeWaitingRoom`

컷신을 끝내거나 `Continue to Slime Waiting Room`을 누르면 대기실로 돌아갑니다. 실제 `VideoPlayer`, Timeline, 자막, 스킵 동의 범위는 영상 자산과 사용자 승인 후 추가합니다.

## 실제 파일과 확인 방법

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeStoryInterludeController.cs` | 챕터 번호별 컷신 placeholder와 대기실 복귀 | 각 컷신 씬 실행 후 Continue 조작 |
| `PrototypeStoryInterlude_Chapter01.unity` ~ `PrototypeStoryInterlude_Chapter07.unity` | 챕터별 2D 컷신 삽입 씬 | 해당 챕터 종료 후 자동 이동 또는 씬 직접 실행 |

현재 placeholder는 최종 스토리 연출·영상 품질·자막 스타일을 확정하지 않습니다.

[Story 기능](../../../Features/Story/README.md) · [Episode01](../README.md) · [전체 폴더 지도](../../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../../Docs/Bugs/BUG_TEMPLATE.md)
