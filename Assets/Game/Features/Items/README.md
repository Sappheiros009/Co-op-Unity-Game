# Items — 아이템과 소지품

## 다음 작업자용 빠른 안내

- 쉬운 이름: 소지품과 소모품.
- 수정 시작점: [PrototypeInventory.cs](PrototypeInventory.cs).
- 현재 담당: 아이템 보유·획득·소모, PrototypeLure.cs의 유인물.
- 이 폴더만으로 처리하지 않는 범위: 상자 물리·출구 판정.
- 함께 확인: Interaction, Cooperation, Save.
- 지시 예시: “회복품이 중복 소모됨 → Features/Items → PrototypeInventory.cs → Interaction, Cooperation, Save와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

## 담당 범위

- 회복품·열쇠·미끼·이동 보조 도구의 정의와 효과, 획득·소모·전달을 담당합니다.
- 소지품 상태와 필수 아이템을 잃었을 때의 복구 규칙을 관리합니다. 개인 소지품과 공유 가방의 구체적인 방식은 확정된 기획을 따릅니다.

## 함께 확인할 폴더

- [Interaction](../Interaction/README.md): 획득·사용 요청을 받습니다.
- [Player](../Player/README.md): 회복과 상태 효과를 적용합니다.
- [Cooperation](../Cooperation/README.md): 부활 비용과 협동용 도구를 제공합니다.
- [Puzzles](../Puzzles/README.md): 필수 장치에 필요한 아이템을 전달합니다.
- [Online](../Online/README.md): 획득·소모가 참가자마다 다르게 처리되지 않도록 맞춥니다.
- [Save](../Save/README.md): 영구 저장 대상으로 확정된 항목만 전달합니다.

## 문제가 생겼을 때

아이템 식별자, 보유자, 수량, 획득·소모 요청, 필수 아이템 복구 위치를 확인합니다.

작업 지시 예시: “Items에서 퍼즐 필수 아이템이 사라져 진행이 막히는 조건을 재현하고 복구 규칙을 확인해주세요.”

## 구현 파일 안내

현재 프로토타입 코드가 존재한다. 수정 시작점은 위 빠른 안내, 구체 범위는 아래 구현 기록을 따른다. 기획 전체 구현 완료를 의미하지 않는다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeInventory.cs`: 시험 슬롯·획득·원자적 전달·소모. `PrototypeLure.cs`: 소모형 적 유인구. 회복팩·재사용 열쇠·로프는 Interaction에서 사용한다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
