# Player — 플레이어

## 다음 작업자용 빠른 안내

- 쉬운 이름: 이동과 카메라.
- 수정 시작점: [PrototypeCapsulePlayer.cs](PrototypeCapsulePlayer.cs).
- 현재 담당: 입력에 따른 이동·점프·등반·시점.
- 이 폴더만으로 처리하지 않는 범위: 출구 점수·영구 저장.
- 함께 확인: Cooperation, Interaction, Online.
- 지시 예시: “벽 타기 위치 튐 → Features/Player → PrototypeCapsulePlayer.cs → Cooperation, Interaction, Online와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

## 담당 범위

- 이동·달리기·점프·앉기와 벽 타기, 1인칭 카메라를 담당합니다.
- 체력·스태미나·다운 상태와 직업별 특기를 관리합니다.

## 함께 확인할 폴더

- [Cooperation](../Cooperation/README.md): 함께 하는 행동과 구조 결과를 받습니다.
- [Items](../Items/README.md): 회복량과 아이템 효과를 적용합니다.
- [Online](../Online/README.md): 다른 참가자와 플레이어 상태를 맞춥니다.

## 문제가 생겼을 때

입력, 지면·벽 판정, 현재 이동 상태, 체력·스태미나를 순서대로 확인합니다. 끌어올리기 도중의 문제는 Cooperation도 확인합니다.

작업 지시 예시: “Player에서 벽 꼭대기에 올라갈 때 위치가 튀는 현상을 확인하고, 재현 조건과 수정 파일을 버그 기록에 남겨주세요.”

## 현재 프로토타입 구현 파일

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeCapsulePlayer.cs` | 7챕터 공통 1인칭 플레이어 이동·시점·출구 접근 흐름 | 각 챕터 실행 후 이동·출구 접근 확인 |
| [PrototypeWaitingRoomWalker.cs](PrototypeWaitingRoomWalker.cs) | 직접 걷는 3D 대기방 전용 이동·카메라·충돌. 챕터 점수·피해 판정과 분리 | [대기방 검증](../../../../Docs/Testing/TEST-0006-WalkableWaitingRoom.md) |
| [PrototypePlayerInput.cs](PrototypePlayerInput.cs) | 이동·시점·협동 조작 의도. 좌표·체력·점수·시뮬레이션 시간은 포함하지 않음 | [서버 입력·물리 제어 검사](../../../../Docs/Testing/TEST-0005-ServerPlayerControl.md) |

현재 플레이어는 로컬 테스트용 단순 조작입니다. 최종 이동·애니메이션·직업·네트워크 소유권은 별도 구현과 검증이 필요합니다.

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeCapsulePlayer.cs`: 이동·스태미나·앉기·벽 타기·낙하 복귀. `PrototypeSlimeBody.cs`: 독립 몸통·손·발·얼굴 파츠. `PrototypeClimbSurface.cs`: 등반 가능 표면.

`PrototypeMovementRules.cs`의 모터 입력을 런타임 키보드 어댑터와 물리 회귀가 공유한다. `PrototypeMotorTests`는 낮은 천장·맨틀·벽 점프·메뉴 중 낙하를 검증한다. 최종 온라인 이동 보정은 별도다.

[PrototypeWaitingRoomWalker](PrototypeWaitingRoomWalker.cs)는 대기방 전용 모터를 로컬/서버/표시 제어로 분리한다. 표시 전용 제어는 CharacterController로 움직이지 않고 서버 위치만 적용한다. 챕터의 체력·점수 판정은 수행하지 않는다. [대기방 서버 검사](../../../../Docs/Testing/TEST-0008-NetworkWaitingRoom.md).

`PrototypeCapsulePlayer`는 독립 참가자 ID와 로컬/서버/표시 전용 제어를 구분한다. 서버 제어는 호스트 키보드·카메라·설정창에 의존하지 않고 `StepServerInput`으로 해당 참가자만 실행한다. 물리·입력 준비 검사는 [TEST-0005](../../../../Docs/Testing/TEST-0005-ServerPlayerControl.md), 후속 실제 2/3/4인 입력 수신·서버 이동·표시 전용 복제는 [TEST-0007](../../../../Docs/Testing/TEST-0007-NetworkWorldReplica.md)에 구분한다. 시점 즉시 반응과 위치 보간은 구현했지만 위치 예측/보정·지연/손실 검증은 남아 있다.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).


## 2026-09-20 Player 이동 코드 검토

검토 대상은 `PrototypeCapsulePlayer.cs`, `PrototypeWaitingRoomWalker.cs`, `PrototypePlayerInput.cs`, `PrototypeMovementRules.cs`, `PrototypeClimbSurface.cs`, `PrototypeSlimeBody.cs`와 기존 입력·서버 제어 계약이다.

### 확인된 구현

- CharacterController·Input System·`PrototypeTuning`·`PrototypeMotorInput` 재사용.
- 이동·앞방향 달리기·점프·앉기·벽 타기·맨틀·1인칭 카메라 구현.
- Local·Server·Replica 제어 분리.
- 낮은 천장 기립 전 캡슐 점유 검사.
- 부모 오브젝트 등반 표면 인식.
- 포커스 상실·일시정지 시 로컬 입력 차단.
- Player 폴더 밖 소스·패키지·프로젝트 설정 수정 없음.

### 미해결 검토 이슈

- **P1**: `PrototypeCapsulePlayer.StepMotor`에 `deltaTime > 0.1f` 상한이 없다. 프레임 급증 시 과도한 이동·중력이 발생할 수 있다.
- **P1**: `PrototypeWaitingRoomWalker.EnableReplicaView`가 Replica 카메라를 활성화한다. 현재 Online 계약과 “Replica 로컬 카메라 금지” 요구가 충돌하므로 07 담당의 소유권 결정이 필요하다.
- **P2**: `CanStand`의 반복 `Physics.OverlapCapsule`이 배열을 할당한다. `Physics.CheckCapsule` 또는 NonAlloc 방식 검토가 필요하다.
- **P2**: `StepMotor`가 `Configure` 이후 호출된다는 전제에 의존해 `ViewCamera`, `Interaction`, `_game` 초기화 누락 시 NullReference 위험이 있다.
- **P2**: 대기방 달리기가 `PrototypeMovementRules.CanSprint`와 별도 조건을 사용하므로 전용 규칙 여부를 명시해야 한다.

### 검증 경계

소스 검토와 `git diff --check`만 확인했다. Unity 컴파일·Windows 빌드·PlayMode·실제 2~4인 네트워크·지연/손실 검증은 실행하지 않았다. 이 기록은 빌드 성공이나 실제 게임 동작 성공을 의미하지 않는다.

### 담당 연계

- **07 Online**: Replica 카메라 소유권 계약 확정.
- **09 Tests/Operations**: deltaTime 경계, 기립 충돌, 벽 타기, 포커스 상실 테스트 추가.
