# MapGeneration — 맵 생성

## 담당 범위

- 고정된 방과 랜덤 통로의 연결, 배치 후보 선택과 생성 규칙을 담당합니다.
- 생성 결과가 진행 가능한지 검사하고 실패한 구성의 재생성을 처리합니다. 실제 방·통로 제작물은 Levels에 둡니다.

## 함께 확인할 폴더

- [Puzzles](../Puzzles/README.md): 필수 장치와 통로 조건을 확인합니다.
- [Monster](../Monster/README.md): 몬스터 등장에 사용할 위치를 제공합니다.
- [Items](../Items/README.md): 아이템 배치와 필수 자원 위치를 연결합니다.
- [StartandExit](../StartandExit/README.md): 회차와 스테이지에 맞는 맵 구성을 전달합니다.
- [Online](../Online/README.md): 같은 맵 구성을 참가자들이 사용하도록 맞춥니다.

## 문제가 생겼을 때

챕터·스테이지, 생성 시드, 선택된 방·통로, 연결 검사 결과를 기록합니다. 시드를 사용하지 않는 구현이라면 재현에 필요한 배치 조건을 남깁니다.

작업 지시 예시: “MapGeneration에서 출구까지 갈 수 없는 맵을 재현하고 생성 조건과 검사 결과를 버그 기록에 남겨주세요.”

## 현재 프로토타입 구현 파일

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeMapBuilder.cs` | Chapter01~07의 시드 기반 고정 방·변형 통로·시험 장치와 참가자 생성 | 각 챕터 실행, `PrototypeCourseTests`·`PrototypeServerPlayerTests` |
| `PrototypeTeammate.cs` | 로컬 시험 동료의 이동·보조 행동 | 일반 로컬 챕터 실행. 서버 참가자 구성에서는 생성하지 않음 |

현재 맵은 씬 진입 시 `PrototypeRoomLayout`의 시드와 챕터·구간을 사용해 고정 방과 변형 통로를 생성합니다. 기본 도형·팔레트는 기능 검증용이며, 이 생성 규칙 자체가 실제 네트워크 동기화를 뜻하지는 않습니다. 정식 지역별 에셋·콘텐츠는 별도 결정·검증 후 확장합니다.

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeMapBuilder.cs`: 고정 방·지역 시험 기믹 배치. `PrototypeRoomLayout.cs`: 시드·통로·이동 경유점. `PrototypeTeammate.cs`: 로컬 시험 동료. Hazard·CyclePlatform·MovingRaft·IceSurface: 위험·발판·부유물·빙판.

`PrototypeHazard.cs`는 서버/로컬 판정 월드의 참가자·몬스터 발 위치가 기존 위험 Collider 안에 있는지 고정 틱에서 대조한다. 캐릭터·위험 지대에 Rigidbody를 추가하지 않으며 복제 월드는 판정하지 않는다. 실제 피해·구역 이탈·위로 점프·출구 보호와 네트워크 전멸 재도전 회귀는 [TEST-0011](../../../../Docs/Testing/TEST-0011-NetworkWipeRetry.md)에서 관리한다.

`PrototypeGame.ConfigureServerPlayers`가 지정된 경우 같은 맵에 2~4개의 독립 서버 제어 플레이어를 만들고 시험 동료 AI는 생성하지 않습니다. 명단·역할·출구 인원 준비 검사는 [TEST-0005](../../../../Docs/Testing/TEST-0005-ServerPlayerControl.md), 실제 서버 챕터 시작·입력 이동·표시 전용 복제는 [TEST-0007](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md)에 있습니다. 네트워크 부유물 안내는 시험 AI 배치가 아니라 전원 직접 탑승으로 표시합니다. 발판 위험 색·사용 결과 등 일부 표현과 실제 전체 코스 검증은 남아 있습니다.

## 2026-09-20 구현 상태

PrototypeRoomLayout은 시드 기반 몬스터 등장 후보와 시작점→출구·후보→출구 경로를 검사한다. PrototypeMapBuilder는 후보를 고정 순서로 선택하고 정적 충돌·위험 지대·시작/출구 근접 후보를 제외한다. 최종 절차적 생성과 실제 멀티플레이어 동기화는 미검증이다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).
