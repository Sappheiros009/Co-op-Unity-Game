# Cooperation — 협동 행동

## 다음 작업자용 빠른 안내

- 쉬운 이름: 물체 운반과 협동.
- 수정 시작점: [PrototypeCarryable.cs](PrototypeCarryable.cs).
- 현재 담당: 소형 운반·대형 밀기·필수 상자 복귀.
- 이 폴더만으로 처리하지 않는 범위: 구조 입력 처리는 Interaction/PrototypeInteraction.cs, 이동 보조는 Player.
- 함께 확인: Interaction, Player, Items.
- 지시 예시: “상자 운반 중 막힘 → Features/Cooperation → PrototypeCarryable.cs → Interaction, Player, Items와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

## 담당 범위

- 친구 밟고 점프, 끌어올리기, 구조와 부활 행동을 담당합니다.
- 두 플레이어의 거리·상태·역할에 따른 협동 가능 조건을 판단합니다. 힐러 특기와 회복 아이템을 사용하는 부활 조건도 이곳에서 연결합니다.

## 함께 확인할 폴더

- [Player](../Player/README.md): 체력·다운·이동 상태의 실제 값을 관리합니다.
- [Items](../Items/README.md): 부활에 필요한 회복 아이템과 소모 여부를 확인합니다.
- [Interaction](../Interaction/README.md): 도움을 줄 대상을 전달받습니다.
- [StartandExit](../StartandExit/README.md): 스테이지 전환에 따른 일괄 부활은 이쪽에서 지시합니다.
- [Online](../Online/README.md): 협동 참가자 사이의 행동 상태를 맞춥니다.

## 문제가 생겼을 때

구조자와 대상의 상태, 거리, 필요한 아이템·직업 조건, 중단 원인, 동기화 여부를 확인합니다.

작업 지시 예시: “Cooperation에서 부활 행동이 중간에 끊기는 조건을 확인하고 Player·Items에 미치는 영향까지 기록해주세요.”

## 구현 파일 안내

현재 프로토타입 코드가 존재한다. 수정 시작점은 위 빠른 안내, 구체 범위는 아래 구현 기록을 따른다. 기획 전체 구현 완료를 의미하지 않는다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeCarryable.cs`: 소형 운반·대형 밀기·중요 상자 복구. 구조·끌어올리기 요청은 Interaction, 동료 시험 행동은 MapGeneration/PrototypeTeammate가 연결한다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
