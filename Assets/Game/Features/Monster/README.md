# Monster — 몬스터

## 담당 범위

- 몬스터 인식·추적·공격·상태 전환과 등장 규칙을 담당합니다.
- 보스의 행동과 전투 단계도 이곳에서 관리합니다. 보스 방과 배경 배치는 Levels의 해당 챕터가 담당합니다.

## 함께 확인할 폴더

- [Player](../Player/README.md): 피해와 플레이어 상태를 확인합니다.
- [Puzzles](../Puzzles/README.md): 장치와 환경에 의한 결과를 받습니다.
- [MapGeneration](../MapGeneration/README.md): 생성된 맵의 등장 후보 위치를 이용합니다.
- [Online](../Online/README.md): 참가자에게 몬스터 상태를 맞춥니다.

## 문제가 생겼을 때

몬스터 종류, 현재 상태, 인식 대상·거리, 등장 위치, 장애물과 동기화 여부를 확인합니다.

작업 지시 예시: “Monster에서 특정 거리 안에 들어가도 추적하지 않는 현상을 확인하고 해당 챕터의 배치 조건을 기록해주세요.”

## 현재 프로토타입 구현 파일

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeCapsuleMonster.cs` | Chapter01에서 캡슐 몬스터의 단순 배회·접촉 위험 표현을 담당 | `PrototypeChapter01.unity` 실행 후 몬스터 배치와 접촉 확인 |

현재 몬스터는 캡슐 placeholder와 로컬 테스트용 단순 동작입니다. 최종 몬스터·보스 외형과 행동·네트워크 판정은 확정된 에셋·규칙에 따라 별도 구현합니다.

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeCapsuleMonster.cs`: 시야·추적·예고 공격·유인·환경 저지. 직접 공격과 몬스터 처치 점수를 제공하지 않는다.

## 2026-09-20 구현 상태

PrototypeCapsuleMonster.cs는 순찰·추적·유인·공격 예고·공격 대기·환경 장치로 저지 상태를 분리한다. 기존 State, AttackWarningSequence, AttackWarning 공개 API를 유지한다. 수치·최종 에셋·서버 연동은 확정·검증되지 않았다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
