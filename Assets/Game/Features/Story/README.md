# Story — 이야기

## 담당 범위

- 이야기 사건, 단서, 대사·자막, 귀향패 진행 조건과 목걸이 관련 사건을 담당합니다.
- 선택에 따른 엔딩 분기와 챕터별 정보 공개를 관리합니다. 실제 지역 배치는 Levels에서 관리합니다.

## 함께 확인할 폴더

- [StartandExit](../StartandExit/README.md): 챕터·스테이지 진행 상태를 받습니다.
- [Items](../Items/README.md): 이야기 아이템의 획득 사실을 확인합니다.
- [Save](../Save/README.md): 개인별로 남겨야 할 이야기 기록을 전달합니다.
- [UI](../UI/README.md): 대사와 선택지를 표시합니다.

## 문제가 생겼을 때

사건 식별자, 발생 조건, 이전 진행 기록, 챕터, 선택값, 중복 발생 여부를 확인합니다. 번역·폰트 문제는 Localization과 UI도 확인합니다.

작업 지시 예시: “Story에서 단서를 얻기 전에 후반 대사가 나오는 조건을 확인하고 사건 순서를 기록해주세요.”

## 구현 파일 안내

현재 프로토타입은 챕터 종료 후 영상 삽입 위치로 이동하는 컷신 placeholder만 제공합니다. 실제 영상·자막·타임라인은 사용자 승인 후 추가합니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeStoryInterludeController.cs` | 챕터별 스토리 영상 placeholder 화면과 대기실 복귀 | `PrototypeStoryInterlude_Chapter0X.unity` 실행 후 Continue 조작 |

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)
