# TEST-0006 — 직접 걷는 3D 슬라임 대기방

기준일: 2026-09-19 · 대상: 일반 로컬 프로토타입의 대기방과 확정된 이야기 동의 규칙

## 승인된 동작과 구현 위치

2D 로비는 유지한다. 슬라임 대기방에서는 1인칭으로 직접 이동하고, 챕터 선택 지점에 접근해 선택한 뒤 중앙 준비 장치에서 출발한다. 챕터 선택만으로 장면을 바꾸지 않는다. 큰 고정 챕터·파티 패널은 걷는 화면에서 제거하고 현재 선택·인원·근접 조작 안내를 남긴다. 인원·특기·시험/해금·연습·기록·도입·로비 복귀는 공간 안의 장치로 옮겼다. 재질·색·외형은 임시 프로토타입 표현이다.

| 파일 | 책임 |
|---|---|
| [WaitingRoomController](../../Assets/Game/Features/UI/PrototypeWaitingRoomController.cs) | 3D 공간·장치·소형 HUD, 선택과 준비의 분리, 기존 기능 연결 |
| [WaitingRoomWalker](../../Assets/Game/Features/Player/PrototypeWaitingRoomWalker.cs) | 걷기·달리기·점프·앉기·1인칭 시점·캐릭터 충돌 |
| [WaitingRoomStation](../../Assets/Game/Features/UI/PrototypeWaitingRoomStation.cs) | 장치 종류·챕터·월드 라벨, 거리·바라보는 방향·가림 검사 |
| [SceneBuilder](../../Assets/Game/Editor/PrototypeSceneBuilder.cs) | 대기방만 갱신하는 편집기 명령. Unity API로 씬·재질 저장 |
| [대기방 PlayMode 검사](../../Assets/Game/Tests/PrototypeWaitingRoomTests.cs) | 이동·장치·해금·표지판을 포함한 7개 대기방 검사 |
| [씬 흐름 회귀](../../Assets/Game/Tests/PrototypeSceneFlowTests.cs) | 로비·대기방·챕터·이야기 복귀와 2/3/4인 시험 설정 |
| [별도 프로세스 방 모델](../../Assets/Game/Features/Online/PrototypeNetworkRoom.cs) | 이야기 전원 동의·철회·옛 런 거부·이탈 후 고정 출구 명단 유지 |

## 실제 검사 결과

- 최종 Windows 빌드: Unity 6000.6.1f1, StandaloneWindows64 성공, `Errors: 0`(2026-09-19 19:15:44 KST). 실행 파일은 `Build/FullPrototype/SlimeCoopPrototype.exe`, 근거는 같은 폴더의 `SlimeCoopPrototype.provenance.json`과 `Logs/full-prototype-windows-build.log`다. 시작 시 라이선스 클라이언트 검증 경고는 기록됐으나 실제 빌드는 종료 코드 0으로 성공했다. 경고 자체를 해결했다고 주장하지 않는다.
- 최종 런타임 DLL `Build/FullPrototype/SlimeCoopPrototype_Data/Managed/SlimeCoop.Prototype.Runtime.dll`의 SHA-256은 `518C5E508CF38BB1E66F5303CC008DB6F84D0D122D91D59254B6AC9A7F5E5C08`이다. Unity EXE 실행기는 코드 변경 없이 재사용될 수 있으므로 EXE 수정 시각만으로 최신 여부를 판단하지 않는다.
- 위 빌드의 11종 렌더 캡처와 QA 전환이 통과했다(19:16:20~19:16:46 KST, `Build/FullPrototype/Captures/result.json`, 캡처 오류 0). 최종 `01-Lobby.png`와 `02-WaitingRoom.png`를 직접 열어 2D 로비 유지, 3D 대기방·소형 HUD, 7개 챕터 표지, 준비 표지의 겹침/줄바꿈 해소, 측면 표지의 뒤집힘 해소를 확인했다. 측면 장치 일부는 시작 시점의 시야 밖에 있으며 고개를 돌리고 접근해 이용한다. 나머지 9종의 이번 PNG를 모두 직접 검수했다고 기록하지 않는다.
- 최종 전체 PlayMode 회귀: 110/110 통과, 실패·건너뜀 0, 157.073초(2026-09-19 19:12:02~19:14:39 KST). 최종 표지판 보강까지 포함한다. 근거: `Logs/full-prototype-playmode.xml`. 기존 97개에 대기방 7개·이야기 동의/연결 바인딩 5개·반복 아이템 재사용 1개가 추가됐다.
- 대기방·기존 씬 흐름·방 모델 집중 검사: 19/19 통과, 실패·건너뜀 0, 34.306초. 근거: `Logs/walkable-waiting-tests.xml`.
- 18:35:54(KST) `PrototypeWaitingRoom.unity`만 다시 생성했다. 근거: `Logs/walkable-waiting-scene.log`의 대기방 생성 완료 메시지. 로비·챕터 씬을 일괄 재생성하지 않았다.
- 표시 보강 전 중간 전체 PlayMode 회귀도 109/109 통과했다(155.703초, 18:51:47 KST). 현재 전체 XML은 위 최종 110개 실행 결과로 갱신됐다.
- 첫 Windows 빌드·11종 렌더는 통과했으나 직접 연 대기방 PNG에서 원거리 챕터 표지가 너무 작고 안내의 배경 대비가 약한 점을 확인했다. 표지 글자/높이와 안내 배경을 보강한 뒤 관련 대기방·씬 흐름 13/13을 재검사해 통과했다(34.872초, 18:57:00 KST). 이는 아래 추가 표시 수정 전의 중간 결과다.
- 후속 캡처에서 중앙 준비 장치의 자동 줄바꿈·챕터 표지와의 겹침, 옆 장치의 뒤집힌 글씨를 발견했다. 준비 표지는 낮추고 두 줄로 짧게 표시하며, 자동 줄바꿈을 끄고 표지만 플레이어를 향해 수평 회전하도록 수정했다. 장치와 충돌체 방향은 유지한다. 첫 관련 검사는 14개 중 13개 통과·표지 방향 1개 실패였고, TMP 추가 과정에서 Transform이 RectTransform으로 교체되는 뒤에 참조를 저장하도록 고쳤다. 세 위치의 방향·줄바꿈·장치 방향 보존을 포함한 최종 관련 14/14가 통과했다(35.180초, 19:08:29 KST, `Logs/waiting-final-ui-tests.xml`).
- 18:58:35 KST 중간 Windows 빌드에서 별도 서버 + 2/3/4인 방 통신과 과도 요청 격리를 재검사해 모두 통과했다(19:03:23~19:04:32 KST). 근거: `Logs/Network/2-player`, `3-player`, `4-player`, `guards`의 서버·클라이언트 보고서. 표지판의 마지막 수정 이전 결과이며 챕터 동기화 검증이 아니다.
- 첫 전체 회귀는 108개 중 107개 통과, 1개 실패였다. `DroppedKeyBelowWorldReturnsToDesignatedSource`에서 Unity의 비어 있는 Rigidbody 참조를 C#의 `??`로 처리해 컴포넌트 생성을 건너뛴 원인이 확인됐다. Unity의 `== null` 검사로 수정하고 30회 줍기·내려놓기 재사용/원위치 보존 검사를 추가했으며 최종 전체 회귀에서 통과했다.
- 확정된 대기방 형태와 이야기 전원 동의 계약을 검사기에 추가했다. 검사기 자체 테스트는 22/22 통과했다. 이것은 게임 검사를 대신하지 않는다.
- 최종 전체 검사의 첫 시도는 테스트 실행 전에 Unity IL Post Processor 보조 프로세스 시작 실패·named pipe 부재로 종료됐다. C# 문법 오류가 확인된 것은 아니며 소스 수정 없이 재실행해 위 110개 검사를 통과했다. 환경 재시도와 기능 실패를 구분한다.
- 후속 전체 109개 검사 중 108개는 통과했고 대기방의 설정창 복귀 후 장치 사용 1개가 간헐적으로 실패했다. 진단 기록은 거리·시야·설정창 조건이 유효한데 `hit=WaitingRoom_LocalSlime`인 자기 충돌체 가림이었다. 가림 검사에서 자신의 하위 충돌체만 제외하고, 재사용 버퍼로 실제 벽 가림은 유지했다. 수정 후 집중 6/6 및 그 안의 12회 반복 가림·설정 복귀 검사가 통과했다(2.927초, `Logs/waiting-interaction-tests.xml`).

대기방 검사는 7개 선택 지점·준비 장치·8개 보조 장치, 단일 투시 카메라/AudioListener, 구분된 몸 파츠, 실제 CharacterController 이동과 벽 충돌, 원거리·뒤돌아보기·벽 너머·설정창 중 사용 거부를 확인했다. 잠긴 챕터를 선택하거나 시험 모드를 해제한 뒤 이전 선택으로 출발하는 경로도 검사했다.

검사와 캡처의 일부 시작 배치는 분리된 QA 환경에서 이동시킨 뒤 일반 장치 사용 함수를 호출한다. 이것은 사람이 직접 마우스로 돌아다니며 완주했다는 증거가 아니다. 일반 실행에서는 임의 순간이동 단축키를 제공하지 않는다.

## 재현

```powershell
unity run . --editor-version 6000.6.1f1 --format json --timeout 600 -- -executeMethod SlimeCoop.Prototype.Editor.PrototypeSceneBuilder.RebuildWalkableWaitingRoom -logFile Logs/walkable-waiting-scene.log
unity test . --mode PlayMode --filter 'SlimeCoop.Prototype.Tests.PrototypeWaitingRoomTests;SlimeCoop.Prototype.Tests.PrototypeSceneFlowTests;SlimeCoop.Prototype.Tests.PrototypeNetworkRoomTests' --output Logs/walkable-waiting-tests.xml --timeout 600 --format json -- -logFile Logs/walkable-waiting-tests.log
```

편집기에서는 `Slime Prototype > Rebuild Walkable Waiting Room`으로 대기방만 갱신할 수 있다. 작업 중인 씬의 저장 여부를 먼저 확인한다. 실제 플레이는 2D 로비에서 게임 시작 → 화면 클릭 → 이동 → 챕터 지점 E → 중앙 준비 장치 E 순서다.

## 네트워크와 남은 범위

사용자가 승인한 이야기 동의 기준은 현재 접속 중인 전원이다. 접속 종료자는 이야기 투표에서만 제외하고 출구 점수의 고정 시작 명단은 바꾸지 않는다. 이 규칙은 기획·계약과 서버 방 모델에 추가했으며, 실제 이야기 화면의 다중 클라이언트 종료 회귀는 별도 연결 대상이다.

이 검사 시점의 일반 로컬 실행은 직접 걷는 대기방이며 동료 준비는 시험용 자동 처리다. 별도 서버 실행의 대기실은 기존 고정 시점 화면이고 서버 월드 진입점·클라이언트 표시는 미연결 상태였다. 이후 서버 챕터 연결 결과는 [TEST-0007](TEST-0007-NetworkWorldReplica.md)에 별도 기록한다. 두 실행 경로의 완료 범위를 혼동하지 않는다. Steam/PlayFab·실제 계정·최종 아트·최소 사양 성능·사용자 직접 플레이는 검증하지 않았다.

[프로토타입 안내](../PROTOTYPE_GUIDE.md) · [입력 분리 검사](TEST-0005-ServerPlayerControl.md) · [전체 작업](../Maintenance/TASK-0005-FullPlanningPrototype.md)
