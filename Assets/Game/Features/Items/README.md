# Items — 아이템과 소지품

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

현재는 폴더 구조와 안내 문서만 있습니다. 코드·프리팹·설정 파일이 추가되면 아래에 실제 경로를 기록합니다. 존재하지 않는 파일을 구현 완료 항목으로 기록하지 않습니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeInventory.cs`: 시험 슬롯·획득·원자적 전달·소모. `PrototypeLure.cs`: 소모형 적 유인구. 회복팩·재사용 열쇠·로프는 Interaction에서 사용한다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
