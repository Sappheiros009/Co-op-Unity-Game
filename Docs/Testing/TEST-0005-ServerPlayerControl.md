# TEST-0005 — 서버 참가자 입력 분리와 물리 제어

기준일: 2026-09-19 · 단계: 로컬 서버 플레이 연결을 위한 입력·실행 경계 검증

## 검증 범위와 실제 결과

새 PlayMode 검사 20/20 통과, 실패·건너뜀 0. Unity 6000.6.1f1에서 실제 생성 맵·CharacterController·Raycast·구조·출구 처리를 실행했다. 전용 입력 버퍼 검사는 네트워크 전달 전의 순수 모델 검사다. 이 결과는 UDP를 통한 챕터 플레이 동기화나 사람이 직접 조작한 완주를 증명하지 않는다.

| 영역 | 확인한 내용 | 근거 |
|---|---|---|
| 고정 참가자 구성 | 2/3/4명 각각 독립 ID·이름·모터를 갖고 시험 동료 AI 없이 생성. 역할·출구 명단도 같은 인원 | [서버 플레이어 검사](../../Assets/Game/Tests/PrototypeServerPlayerTests.cs) |
| 로컬 입력 격리 | 서버용 플레이어는 호스트 키보드를 읽지 않고 자체 카메라·AudioListener·HUD를 활성화하지 않음 | 같은 검사 |
| 이동·행동 주체 | 지정된 참가자만 이동·스태미나 소모. 이탈·출구 진입 후 이동과 아이템 소비 거부 | 같은 검사 |
| 구조·끌어올리기 | 서버 구조는 로컬 설정창 영향 없음. 해당 참가자의 입력 해제는 구조 취소. 실제 플레이어 대상 끌어올리기와 막힌 착지 공간 거부 | 같은 검사 |
| 서버 전환 경계 | 출구 정산 후 서버 조정자에게 한 번만 종료를 알리고 로컬 화면 씬을 강제로 열지 않음 | 같은 검사 |
| 입력 계약 | 조작 의도·시점만 전달. 좌표·체력·점수·시뮬레이션 시간·행동자 ID 필드 없음. 중첩 입력의 JSON 왕복 | [입력 버퍼 검사](../../Assets/Game/Tests/PrototypeNetworkInputTests.cs) |
| 바인딩·재전송 | 연결과 행동자 고정 대응, 알 수 없는 연결·다른 런/구간·중복·역순·과도 순서 점프 거부 | 같은 검사 |
| 입력 수명 | 입력 단절 시 유지 입력 해제, 단발 입력은 서버 tick에서 한 번 소비, tick 전 여러 패킷의 단발 입력 보존 | 같은 검사 |
| 경계 값·요청 제한 | NaN/무한·범위 밖 입력·잘못된 서버 시각 거부, 연결별 요청 한도, 이탈·구간 변경 후 고정 명단 유지 | 같은 검사 |

입력 분리 단계의 PlayMode 회귀는 97/97 통과, 실패·건너뜀 0(153.873초). 같은 단계의 Windows 빌드는 2026-09-19 17:59:59(KST)에 Succeeded, Errors 0으로 완료했다. 이후 작성하는 대기방·실제 통신 연결 변경의 성공 근거로 이 결과를 재사용하지 않는다.

이 실행 파일의 일반 화면 11종 자동 캡처, 별도 서버의 2/3/4인 대기실 회귀와 과도 요청 격리도 통과했다. 로비·대기실·챕터 1의 새 PNG 세 장은 직접 열어 표시 상태를 확인했다. 나머지 캡처는 자동 검사이며 사람의 수동 플레이나 챕터 통신 완료 근거가 아니다. 실행 증거는 `Build/FullPrototype/Captures/result.json`, `Build/FullPrototype/SlimeCoopPrototype.provenance.json`, `Logs/full-prototype-windows-build.log`에 생성했다.

입력 분리 단계 Runtime DLL SHA-256: `2295FEBB9DCC8E7923889D4DEA6BF55D926698736F2D08377669EC9565CBF41B`. 후속 빌드가 같은 출력 경로를 갱신할 수 있으므로 시각·해시로 검사 단계를 구분한다. 문서 검사기 자체 테스트는 참고자료가 연결한 `Packages/manifest.json`도 격리 검사본에 포함하도록 수정한 뒤 20/20 통과했다.

## 실행과 증거

```powershell
unity test . --mode PlayMode --filter 'SlimeCoop.Prototype.Tests.PrototypeNetworkInputTests;SlimeCoop.Prototype.Tests.PrototypeServerPlayerTests' --output Logs/server-player-tests.xml --timeout 600 --format json -- -logFile Logs/server-player-tests.log
unity test . --mode PlayMode --output Logs/full-prototype-playmode.xml --timeout 600 --format json -- -logFile Logs/full-prototype-tests.log
```

집중 검사 결과는 `Logs/server-player-tests.xml`의 `total=20, passed=20, failed=0, skipped=0`이며, 실행 시간은 6.662초다. 테스트가 사용하는 저장 위치는 `Temp/PrototypeServerPlayer` 아래의 분리된 시험 경로다. 원격 서비스·일반 사용자 저장 파일은 변경하지 않았다.

첫 실행은 샌드박스의 라이선스 IPC/시스템 정보 접근 제한으로 테스트 결과를 생성하지 못했다. 실행 인자·부모 PID로 확인한 이번 시험 Editor만 종료했고, 정상 사용자 권한으로 재실행한 뒤 위 결과를 얻었다. 라이선스 재설정·사용자 편집기 강제 종료·계정 변경은 하지 않았다. 이 첫 실행은 테스트 실패가 아닌 실행 환경 실패로 구분한다.

## 실제 변경 위치

- [PrototypePlayerInput](../../Assets/Game/Features/Player/PrototypePlayerInput.cs): 입력 의도와 유효 범위.
- [PrototypeCapsulePlayer](../../Assets/Game/Features/Player/PrototypeCapsulePlayer.cs): 로컬/서버/표시 전용 구분, 독립 참가자 설정, 서버 tick 제어.
- [PrototypeMovementRules](../../Assets/Game/Features/Player/PrototypeMovementRules.cs): 입력 직렬화.
- [PrototypeInteraction](../../Assets/Game/Features/Interaction/PrototypeInteraction.cs): 로컬 입력과 서버 호출 분리, 실제 플레이어 끌어올리기.
- [PrototypeGame](../../Assets/Game/Core/Prototype/PrototypeGame.cs), [PrototypeMapBuilder](../../Assets/Game/Features/MapGeneration/PrototypeMapBuilder.cs): 서버용 참가자 구성과 장치/출구 명단, 전환 알림.
- [PrototypeNetworkInputBuffer](../../Assets/Game/Features/Online/PrototypeNetworkInputBuffer.cs): 고정 연결 바인딩, 순서·구간·수명·요청 제한.

## 남은 연결과 한계

입력 버퍼와 서버용 플레이어 구성은 아직 `PrototypeNetworkTransport`의 실제 챕터 통신 경로에 연결하지 않았다. 다음 작업은 서버 고정 tick과 입력 수신을 연결하고, 같은 월드의 참가자·장치·아이템·몬스터·출구·정산·구간 전환을 클라이언트에 표시하는 것이다. 2/3/4개 별도 클라이언트의 실제 조작·지연·끊김 회귀가 남아 있다.

입력 단절 0.25초, 연결당 초당 60개 입력 한도, 서버 호출 한 번의 최대 0.1초는 개발 시험용 방어 기준이며 정식 네트워크 수치 확정이 아니다. 현재 전용 서버 서비스·Steam 인증·PlayFab·공개 랭킹·운영 제재는 미연결이다.

[검증 목록](README.md) · [작업 기록](../Maintenance/TASK-0005-FullPlanningPrototype.md) · [기획 대비 충족도](../PROTOTYPE_COVERAGE.md)
