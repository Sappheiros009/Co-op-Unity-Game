# TEST-0007 — 전용 서버 챕터 시작과 클라이언트 상태 복제

기준일: 2026-09-19 · 상태: 단계별 구현·검증 중, 전체 챕터 멀티플레이 완료 아님

## 이번 연결 범위

별도 로컬 서버가 출발 명단을 확정하면 실제 챕터 월드를 생성한다. 각 클라이언트는 자신의 입력 의도만 보내며, 승인된 연결 ID에 바인딩된 참가자를 서버 tick에서 움직인다. 클라이언트가 좌표·체력·아이템·점수를 지정하는 요청은 추가하지 않았다. 방장 슬롯과 실제 참가자 배열의 인덱스가 다를 때도 바인딩으로 방장 역할을 연결한다.

클라이언트는 같은 시드의 정적 맵을 구간마다 한 번 만들고 서버가 보낸 참가자·동적 소품·획득물·목표·정산 상태를 표시한다. 표시용 맵의 게임 스크립트·충돌체·물리 판정은 비활성화한다. 시험 동료 AI를 만들지 않으며 자기 카메라와 AudioListener만 활성화한다. 시점은 즉시 반응하고 위치는 서버 상태 사이를 보간한다. 위치 예측·재조정의 완성이나 네트워크 지연 측정 결과는 아니다.

| 파일 | 책임 |
|---|---|
| [NetworkBootstrap](../../Assets/Game/Features/Online/PrototypeNetworkBootstrap.cs) | 실제 서버 월드와 클라이언트 표시의 실행 진입점 |
| [NetworkWorld](../../Assets/Game/Features/Online/PrototypeNetworkWorld.cs) | 서버 입력 tick·고정 참가자·구간 전환·스냅샷 생성 |
| [NetworkInputBuffer](../../Assets/Game/Features/Online/PrototypeNetworkInputBuffer.cs) | 연결별 입력·순서·수명·한도·단발 입력 소비 |
| [NetworkWorldState](../../Assets/Game/Features/Online/PrototypeNetworkWorldState.cs) | 월드·참가자·소품 상태의 값과 크기 계약 |
| [NetworkReplica](../../Assets/Game/Features/Online/PrototypeNetworkReplica.cs) | 표시 전용 맵·자기 카메라·서버 위치/상태 적용 |
| [NetworkClientWorld](../../Assets/Game/Features/Online/PrototypeNetworkClientWorld.cs) | 로컬 입력·HUD·설정·기억 보기·이야기 동의 화면 |
| [Interaction](../../Assets/Game/Features/Interaction/PrototypeInteraction.cs) | 서버 안내에 키값 대신 행동 ID를 넣고 클라이언트의 현재 키 설정으로 표시 |
| [NetworkReplicaTests](../../Assets/Game/Tests/PrototypeNetworkReplicaTests.cs) | 표시 권한 격리·상태 적용·구간 재생성·방장 바인딩·키/안내 배경 검사 |
| [NetworkQa](../../Assets/Game/Features/Online/PrototypeNetworkQa.cs), [LocalNetwork.ps1](../../LocalNetwork.ps1) | 별도 프로세스의 실제 입력 전송·이동 격리·동일 서버/런·깨끗한 종료·새 PNG 확인 |

## 실제 검사 기록

- 입력·방 모델·복제 집중 검사는 28/28 통과했다(2026-09-19 19:35:14 KST, 2.692초, `Logs/network-world-core-tests.xml`). 이후 표시 개선은 이 결과에 포함되지 않는다.
- 표시 전용 복제의 첫 6개 검사는 5개 통과·1개 실패였다. 테스트가 적용된 DTO 자체를 수정해 비교 기준도 바꾸던 원인이었으며, 다음 스냅샷을 복사하도록 고친 뒤 6/6 통과했다. 근거: `Logs/network-replica-tests.xml`.
- 첫 서버 월드 연결 Windows 빌드는 19:40:52 KST 성공했다. 이 중간 빌드의 런타임 DLL SHA-256은 `D86EFB23884AFD57D786EDE1406A4FCA790BBDEEAD927A1E4FB4989D3B521F61`이다.
- 위 빌드의 실제 별도 서버 + 2인(19:41:14), 4인(19:41:43), 3인(19:42:06) `World` 검사가 통과했다. 근거는 `Logs/Network/2-player-world`, `3-player-world`, `4-player-world`의 `result.json`과 각 참가자 보고서다. 2/4인 `network-chapter.png`를 직접 열어 자기 1인칭 시점·원격 슬라임·참가자 명단·서버 HUD를 확인했다.
- 각 실행에서 참가자 1만 약 1.6초 동안 전진 입력을 실제 전송했다. 서버와 모든 클라이언트에서 해당 참가자는 2m 이상 전진하고 다른 참가자의 수평 이동은 0.1m 이내였다. 서버 tick이 진행되고 점수와 출구 도착은 0으로 유지됐다. 서버 수신, 동일 서버/런, 전원 정상 종료를 대조했다. 순간이동이나 서버 위치 직접 변경을 통한 통신 검사는 아니다.
- 4인 PNG에서 밝은 캐릭터와 하단 글자가 겹쳐 대비가 떨어져 안내 배경을 추가했다. 서버 안내는 행동 ID로 전달하고 클라이언트가 자신의 변경 키로 해석하도록 보강했다. 네트워크 바다 안내에서는 시험 AI 배치 키를 제거했다. 이 변경 뒤 전체 회귀와 새 빌드/네트워크 회귀를 별도로 확인한다.
- 기존 방 관리 회귀의 첫 시도는 보고서 읽기와 `File.Replace` 사이의 파일 공유 충돌로 실패했다. 검사 스크립트가 `FileShare.ReadWrite | FileShare.Delete`로 읽어 생산자의 원자적 교체를 막지 않게 수정했다. 이를 게임 통신 오류나 검사 통과로 기록하지 않는다.
- 표시 보강 뒤 첫 전체 회귀는 117개 중 116개 통과, 새 HUD 검사 1개 실패였다. 테스트의 `SendMessage("OnWorld")`가 같은 오브젝트의 전송 컴포넌트에 있는 다른 인자 형태의 동명 메서드까지 호출했다. 테스트용 전송 컴포넌트를 별도 오브젝트로 분리했고, 관련 복제·서버 입력·상호작용 23/23이 통과했다(24.139초, 19:50:20 KST, `Logs/network-world-presentation-tests.xml`). 테스트 구성 오류와 실제 게임 판정 오류를 구분한다.
- 표시·키 안내 보강과 테스트 구성 수정까지 포함한 최종 전체 PlayMode 회귀는 **117/117 통과**, 실패·건너뜀 0, 157.428초다(19:53:26~19:56:04 KST, `Logs/full-prototype-playmode.xml`). 대기방 기준선 110개에 표시 전용 복제·바인딩·HUD 검사 7개가 추가됐다. 이것은 실제 네트워크 완주를 대신하지 않는다.
- 최신 Windows 빌드는 Unity 6000.6.1f1 StandaloneWindows64에서 **성공·Errors 0**이다(19:57:24 KST). `Build/FullPrototype/SlimeCoopPrototype.exe`, 같은 폴더의 `SlimeCoopPrototype.provenance.json`, `Logs/full-prototype-windows-build.log`가 근거다. 최종 런타임 DLL SHA-256은 `F7405E47ECF5B81F529559BA026E48F0D4F5EF5F7E5443E74348081A89605B14`다. 시작 시 기존 라이선스 클라이언트 검증 경고가 있었으나 빌드 프로세스는 종료 코드 0으로 성공했다. 경고 자체를 해결했다고 주장하지 않는다.
- 이 최종 빌드의 `PrototypePipeline.ps1 -Mode Network`는 **7단계 모두 통과**했다(19:57:50~19:59:36 KST). 2/3/4인 방 관리의 버전·정원·권한·중복 거부 및 방장 이전, 과도 요청 클라이언트 격리, 2/3/4인 서버 챕터 시작/이동 복제를 포함한다. 보고서 파일 공유 수정 뒤 동일 오류는 재발하지 않았다. `Logs/Network` 각 시나리오의 최종 `result.json`과 서버·클라이언트 JSON이 근거이며, 공통 `PrototypePipelineReport.json`은 이후 Capture 실행 결과로 갱신된다.
- 최종 `World` 2/3/4인 서버는 각각 입력 18/17/16개를 실제 수신·허용했고 오류 0이었다. 각 서버와 클라이언트가 이동 격리를 확인하고 종료 코드 0으로 종료했다. 최종 2/4인 PNG도 직접 열어 본인 1인칭 시점·실제 참가자 명단·원격 슬라임과 안내 대비 보강을 확인했다.
- 동일 빌드의 일반 경로 캡처도 **11종 통과·오류 0**이다(19:59:52~20:00:18 KST, `Build/FullPrototype/Captures/result.json`). 최종 `01-Lobby.png`와 `02-WaitingRoom.png`를 직접 열어 2D 로비 유지·직접 걷는 3D 대기방·챕터/준비 장치·표지판을 재확인했다. 나머지 9종을 이번에 모두 직접 검수했거나 사람이 실제 창에서 완주했다고 기록하지 않는다.
- 문서 계약 검사는 788/788, 검사기 자체 회귀는 22/22 통과했다. 게임 테스트·통신·표시·문서 계약은 서로 다른 검증 범위다.

## 재현

```powershell
unity test . --mode PlayMode --output Logs/full-prototype-playmode.xml --timeout 600 --format json -- -logFile Logs/full-prototype-tests.log
unity build . --target StandaloneWindows64 --execute-method SlimeCoop.Prototype.Editor.PrototypeBuild.BuildWindows --output-path Build/FullPrototype/SlimeCoopPrototype.exe --log-file Logs/full-prototype-windows-build.log --allow-dirty-build --no-tail --timeout 900 --format json
pwsh -NoProfile -File ./LocalNetwork.ps1 -Mode Test -Players 2 -Scenario World -Capture
pwsh -NoProfile -File ./LocalNetwork.ps1 -Mode Test -Players 3 -Scenario World
pwsh -NoProfile -File ./LocalNetwork.ps1 -Mode Test -Players 4 -Scenario World -Capture
```

`PrototypePipeline.ps1 -Mode Network`는 기존 2/3/4인 방 관리·과도 요청 격리에 더해 위 `World` 검사를 수행한다. `Full`에도 같은 검사가 포함된다. 파이프라인 성공만으로 PNG 직접 검수·전체 코스 완주를 통과 처리하지 않는다.

## 남은 범위와 지시 위치

- **Online·UI**: 이 검사 당시의 별도 서버 대기실은 고정 카메라였다. 후속 직접 걷는 대기방·장치·준비 검사와 최신 빌드는 [TEST-0008](TEST-0008-NetworkWaitingRoom.md)에 구분한다. 로비 2D 유지 결정은 바뀌지 않았다.
- **Online·MapGeneration·Items**: 용암 발판의 위험 색·유인구·기억 발견·일부 장치의 사용 표현은 추가 복제 대상이다. 현재 DTO 필드가 있다는 것만으로 표시 완료라고 보지 않는다.
- **Online·Cooperation·StartandExit·Story**: 실제 클라이언트의 아이템 획득/소모·구조·장치·출구·5초·부활·전멸·다음 구간·보스·이야기 전원 동의를 연결된 경로에서 끝까지 검증해야 한다. 화면 및 전환 코드 존재와 별도 프로세스 완주 증거를 구분한다.
- **Save**: 서버 완료 결과를 각 클라이언트 영구 진행/기록으로 저장하는 연결은 미완료다. 표시용 `PrototypeSession`의 점수로 일반 저장 함수를 호출하지 않는다.
- **Player·Online**: 위치 예측/보정, 지연·손실·장시간 플레이·재접속·안전 구간 합류는 후속 범위다.
- **게시·운영**: 이번 변경의 GitHub·Notion 게시, Steam/PlayFab·실제 계정·최소 사양 성능은 미검증이다. 사용자 요청에 따른 게시·재확인은 전체 프로토타입 완료 후 수행한다.

[실행 안내](../PROTOTYPE_GUIDE.md) · [작업](../Maintenance/TASK-0005-FullPlanningPrototype.md) · [현재 상태](../PROJECT_STATE.md)
