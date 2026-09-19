# Story — 이야기

## 다음 작업자용 빠른 안내

- 쉬운 이름: 기억과 챕터 사이 이야기.
- 수정 시작점: [PrototypeStoryInterludeController.cs](PrototypeStoryInterludeController.cs).
- 현재 담당: 이야기 화면 전환, PrototypeStoryMemory.cs의 기억.
- 이 폴더만으로 처리하지 않는 범위: 최종 영상·대사 확정·온라인 전원 동의 판정.
- 함께 확인: UI, Online, Save, Localization.
- 지시 예시: “스토리 뒤 대기방 복귀 실패 → Features/Story → PrototypeStoryInterludeController.cs → UI, Online, Save, Localization와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

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

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeStoryMemory.cs`: 목걸이 상대 방향 계산, 가까운 기억 발견, 영구 다시 읽기용 임시 콘티. 최종 대사와 다섯 조각의 지역 배치는 승인 대상이다.

`PrototypeStoryInterludeController.cs`: 7개 인터루드의 글 콘티·승인 VideoClip 연결·자막·재생 실패 대체 화면·대기실 복귀. 최종 대사·영상은 미확정.

멀티플레이의 계속하기·건너뛰기는 현재 접속 중인 전원이 동의하면 함께 대기실로 이동한다. 접속 종료자는 이 투표에서만 제외하며 출구 점수의 고정 시작 명단은 유지한다. 확정된 동의 규칙과 실제 네트워크 구현 검증은 구분한다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
