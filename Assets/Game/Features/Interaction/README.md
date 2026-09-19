# Interaction — 상호작용

## 다음 작업자용 빠른 안내

- 쉬운 이름: 대상 선택과 행동 요청.
- 수정 시작점: [PrototypeInteraction.cs](PrototypeInteraction.cs).
- 현재 담당: 조준·근접 대상·구조 유지·아이템 사용 요청.
- 이 폴더만으로 처리하지 않는 범위: 장치 성공 조건·팀 점수.
- 함께 확인: Cooperation, Items, Puzzles.
- 지시 예시: “동료 대신 상자 선택 → Features/Interaction → PrototypeInteraction.cs → Cooperation, Items, Puzzles와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

## 담당 범위

- 조준하거나 가까이 있는 대상 중 어떤 대상을 조작할지 정합니다.
- 친구 돕기 우선순위와 물건 들기·밀기·사용 요청을 담당합니다. 장치의 성공 조건과 아이템의 효과는 담당 기능에 전달합니다.

## 함께 확인할 폴더

- [Cooperation](../Cooperation/README.md): 친구 돕기 요청을 처리합니다.
- [Puzzles](../Puzzles/README.md): 문과 장치의 작동 조건을 판단합니다.
- [Items](../Items/README.md): 아이템 획득·사용·내려놓기를 처리합니다.
- [UI](../UI/README.md): 대상 이름과 가능한 조작을 표시합니다.

## 문제가 생겼을 때

조준 대상, 탐색 범위, 입력, 대상 우선순위, 최종 요청 수신 기능을 확인합니다.

작업 지시 예시: “Interaction에서 상자와 친구가 겹쳐 보일 때 친구 돕기가 우선되는지 확인해주세요.”

## 구현 파일 안내

아래는 로컬 시험 코드이며 실제 서버 소유권·요청 인증과 구분합니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| [PrototypeInteraction.cs](PrototypeInteraction.cs) | 키 입력 스냅샷, 대상·거리·차폐·소지품 검사, 구조/운반/사용 | [상호작용 회귀](../../Tests/PrototypeInteractionTests.cs) |
| [PrototypeInteraction.cs](PrototypeInteraction.cs)의 서버 제어 경계 | 서버 참가자는 로컬 키보드/설정창과 분리. 실제 플레이어 객체도 끌어올리기 가능 | [서버 참가자 검사](../../Tests/PrototypeServerPlayerTests.cs), [결과](../../../../Docs/Testing/TEST-0005-ServerPlayerControl.md) |
| [PrototypeInteractable.cs](PrototypeInteractable.cs) | 획득·장치 사용·필수 아이템 복구 위치 | [구간 회귀](../../Tests/PrototypeCourseTests.cs) |

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeInteraction.cs`: 대상 안내·동료 우선 구조·아이템 사용. `PrototypeInteractable.cs`: 획득·장치·기록 상호작용.

서버 참가자의 근접 안내에는 실제 키 대신 `{key:Interact}`·`{key:Drop}` 행동 ID를 넣는다. 클라이언트는 [PrototypeNetworkClientWorld](../Online/PrototypeNetworkClientWorld.cs)에서 자기 키 설정으로 표시한다. 로컬 실행의 안내는 기존 현재 키 표시를 유지한다. [연결·검증 기록](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md).

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
