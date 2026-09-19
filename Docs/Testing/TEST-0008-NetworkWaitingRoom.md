# TEST-0008 — 실제 참가자가 직접 걷는 멀티 대기방

기준일: 2026-09-19 · 상태: 3D 멀티 대기방 구현·회귀 완료, 전체 온라인 코스는 별도 진행 중

## 동작과 책임

일반 실행의 2D 로비와 직접 걷는 3D 대기방을 유지한다. 별도 서버 경로도 고정 시점의 챕터 버튼 목록 대신 같은 임시 3D 공간을 사용한다. 실제 접속자만 슬라임으로 나타나며 각자 자기 1인칭 카메라로 이동한다. 방장은 챕터 선택 지점을 이용하고, 각 참가자는 중앙 준비 장치 가까이에서 준비한다. 전원이 준비하면 방장이 같은 장치에서 출발한다. 챕터를 변경하면 준비를 해제한다. 현재 시험의 초기 선택은 챕터 1이다.

네트워크 입력에는 연결자가 고른 슬롯·좌표·속도·물리 시간·점수가 없다. 서버가 승인된 연결과 참가자를 대응시키고 고정 물리 tick으로 이동한다. 장치 사용은 서버의 거리·시선·가림 검사와 방장 권한 검사를 모두 통과해야 한다. 대기방과 챕터 입력은 별도 메시지이며, 대기방 복귀 때 세대 번호를 바꿔 이전 입력을 거부한다. 접속자 변경만으로 남은 참가자의 입력 순서를 초기화하지 않는다.

| 위치 | 담당 |
|---|---|
| [WaitingRoomLayout](../../Assets/Game/Features/UI/PrototypeWaitingRoomLayout.cs) | 로컬/서버/표시가 공유하는 방·벽·장치 배치. 멀티 경로에는 가짜 동료·로컬 인원 변경 장치를 만들지 않음 |
| [WaitingRoomWalker](../../Assets/Game/Features/Player/PrototypeWaitingRoomWalker.cs) | 로컬/서버/표시 제어 분리, 독립 카메라, 서버 모터와 표시 전용 위치 적용 |
| [NetworkWaitingServer](../../Assets/Game/Features/Online/PrototypeNetworkWaitingServer.cs) | 실제 접속 명단·서버 충돌·장치 접근 검사·대기방 활성 구간 |
| [NetworkWaitingClient](../../Assets/Game/Features/Online/PrototypeNetworkWaitingClient.cs) | 1인칭 입력·실제 참가자 표시·근접 준비창·설정·로컬 기록·로비 이탈 |
| [NetworkWaitingState](../../Assets/Game/Features/Online/PrototypeNetworkWaitingState.cs) | 대기방 세대·입력 순서·서버 위치 계약 |
| [NetworkInputChannel](../../Assets/Game/Features/Online/PrototypeNetworkInputChannel.cs) | 공통 연결별 입력 수명·빈도·단발 소비 |
| [NetworkWaitingTests](../../Assets/Game/Tests/PrototypeNetworkWaitingTests.cs) | 연결별 이동·세대 교체·장치 권한·클라이언트 카메라/물리 격리 |
| [NetworkQa](../../Assets/Game/Features/Online/PrototypeNetworkQa.cs), [LocalNetwork.ps1](../../LocalNetwork.ps1) | 별도 실행 파일들이 입력만 전송해 장치까지 걸어가는 회귀 |

## 검증 기록

- 최종 Windows 빌드는 Unity 6000.6.1f1, StandaloneWindows64 **성공·Errors 0**이다(20:39:50 KST). `Build/FullPrototype/SlimeCoopPrototype.exe`, 같은 폴더의 `SlimeCoopPrototype.provenance.json`, `Logs/full-prototype-windows-build.log`가 근거다. 런타임 DLL SHA-256: `4624ADBFC07E8398215B48767DFAD2AE844D322E364FCAAF8C13CA80ACFF35BF`. 기존 라이선스 클라이언트 검증 경고는 있었으나 빌드 종료 코드는 0이었다. 경고 자체를 해결했다고 주장하지 않는다.
- 최종 전체 PlayMode: **125/125 통과**, 실패·건너뜀 0, 166.329초(20:41:46~20:44:32 KST, `Logs/full-prototype-playmode.xml`). 이전 117개에 대기방 서버·클라이언트·JSON 회귀 8개가 추가됐다. 이 소스에는 전멸 결과를 닫은 뒤 반복 서버 패킷 때문에 대기방 마우스가 계속 풀리지 않도록 한 수정도 포함한다. 실제 전체 온라인 전멸/복귀 완주는 별도 미검증이다.
- 최종 빌드의 네트워크 파이프라인 **7단계 모두 통과**(20:40:55~20:43:44 KST): 별도 서버 + 2/3/4인 방 관리, 과도 요청 격리, 별도 서버 + 2/3/4인 챕터 시작/이동. `Logs/Network/{2-player,3-player,4-player,guards,2-player-world,3-player-world,4-player-world}`의 `result.json`과 각 프로세스 JSON이 근거다. 실행 식별자·서버/런 일치·정상 종료·새 캡처도 대조했다.
- 방 관리 2/3/4인 서버가 허용한 대기방 입력은 각각 180/240/283개, 대기방 입력 거부는 0이다. 방장의 원거리 준비 요청은 1회 거부했으며 비방장 권한·중복 요청 거부와 함께 총 명령 거부는 3/5/7회였다. 실제 입력으로 챕터 장치와 준비 장치에 도착했다. 통신 검사는 좌표 주입이나 순간이동으로 장치에 배치하지 않는다.
- `World` 2/3/4인 서버의 대기방 입력은 136/162/226개, 챕터 입력은 21/23/22개였다. 실제 장치에서 출발한 뒤 지정 참가자만 2m 이상 전진하고 다른 참가자의 수평 이동은 0.1m 이내인지 기존 이동 격리 회귀를 통과했다. 모든 프로세스 오류는 0이었다. 보고서 파일 교체는 일부 실행에서 총 9회 재시도한 뒤 정상 저장했다. 재시도를 게임 통신 오류로 기록하거나 영구 실패를 숨기지 않는다.
- 최종 4인 `waiting-room.png`와 `waiting-ready.png`를 직접 열어 1인칭 공간·7개 표지·실제 참가자 명단·근접 준비창·준비 4/4·분리된 준비/출발 버튼을 확인했다. 긴 챕터 표지는 이름을 바꾸지 않고 최대 표시 폭을 제한해 겹침을 줄였다. 최종 UI 스타일 확정은 아니다.
- 일반 로컬 경로의 화면 11종 캡처도 오류 0으로 통과했다(20:44:19~20:44:45 KST). `Build/FullPrototype/Captures/01-Lobby.png`와 `02-WaitingRoom.png`를 직접 열어 2D 로비 유지·로컬 대기방 3D·시험 동료와 장치를 확인했다. 보고서의 검증 범위 문구를 갱신한 뒤 같은 빌드로 20:47:21~20:47:47 KST 11종을 다시 생성해 통과했고 새 대기방 PNG도 직접 확인했다. 나머지 9종을 이번에 모두 직접 검수했다고 주장하지 않는다. 공통 `Logs/PrototypePipelineReport.json`은 마지막 실행 모드의 보고서이며 각 네트워크 폴더의 결과를 대신하지 않는다.
- 문서 계약·로컬 링크·스크립트 검사는 `ProjectPipeline.ps1 -Mode Validate -WriteReport`로 통과했다. 문서 통과를 게임·배포 성공으로 대신하지 않는다. 이번 검사에서 Unity 씬 YAML을 수동 수정하거나 사용자 편집기를 강제 종료하지 않았다.

### 중간 실패와 수정 근거

- 대기방·기존 입력·로컬 대기방·방 모델 집중 PlayMode: 36/36 통과, 실패·건너뜀 0, 4.280초. 근거: `Logs/network-waiting-playmode.xml`. 실제 다중 프로세스 검증을 대신하지 않는다.
- 첫 변경 빌드는 성공했으나 실제 2인 방 회귀는 시간 초과로 실패했다(20:26:59~20:27:36 KST). 입력 순서는 서버에서 증가했지만 참가자 좌표가 시작 위치에 머물렀다. 수신과 고정 물리 tick의 시각 기준 차이 및 보고서 파일 교체 시 일시적 잠금을 별도 수정·재검사한다. 이 시도는 통과 기록이 아니다.
- 수신/소비를 같은 단조 실시간 시계로 바꾼 뒤 실제 장치까지 이동·준비는 확인했지만 두 번째 2인 회귀의 출발 전환은 실패했다(20:32:14~20:32:52 KST). 서버는 Reserved가 되었으나 클라이언트가 빈 대기방 DTO를 유효한 대기방처럼 검사해 새 방 상태를 버렸다. Unity JSON의 null 객체 직렬화 결과를 비대기 단계에서 명시적으로 제거하도록 수정하고 직렬화 왕복 회귀를 추가했다.
- JSON 정규화와 긴 챕터 표지 폭 검사까지 포함한 집중 PlayMode는 37/37 통과했다(20:35:24 KST, 5.621초). 첫 통합 변경의 전체 PlayMode는 124/124 통과했다(20:27:27~20:30:04 KST, 156.428초). 최종 전체 결과는 위 125개 실행으로 갱신됐다.
- 보고서 교체의 일시적인 파일 잠금은 프레임 간 제한된 재시도로 처리하고 재시도 수를 JSON에 기록한다. 2초 동안 계속 실패하면 프로세스를 실패 코드로 종료하며, 최종 보고서를 실제 저장하기 전에는 성공 종료하지 않는다.

## 재현

```powershell
unity test . --mode PlayMode --filter 'SlimeCoop.Prototype.Tests.PrototypeNetworkWaitingTests;SlimeCoop.Prototype.Tests.PrototypeNetworkInputTests;SlimeCoop.Prototype.Tests.PrototypeWaitingRoomTests;SlimeCoop.Prototype.Tests.PrototypeNetworkRoomTests' --output Logs/network-waiting-playmode.xml --timeout 600 --format json -- -logFile Logs/network-waiting-playmode.log
pwsh -NoProfile -File ./PrototypePipeline.ps1 -Mode Network
pwsh -NoProfile -File ./LocalNetwork.ps1 -Mode Play -Players 2
```

## 남은 범위

아래는 이 대기방 단계 당시의 남은 범위다. 완료 결과의 로컬 저장·챕터 1 서버 시험 배치·이야기 합의·복귀는 후속 [TEST-0009](TEST-0009-NetworkCompletion.md)에서 구현/검증했으며 전체 실제 입력 코스 완주와는 구분한다.

로컬 자동 검사는 사용자 직접 연속 조작·전체 온라인 코스 완주·최소 사양 성능의 증거가 아니다. 별도 서버는 명시적인 개발 실행 인자로 대기방에 접속하며 일반 2D 로비의 방 생성/참가 버튼은 후속 연결 대상이다. 실제 참가자의 특기 선택·연습방·영구 해금/완료 결과 저장, 전체 코스 장치/구조/출구/이야기 복귀와 재접속, 지연·손실·WAN·Steam/PlayFab, 이번 GitHub·Notion 게시가 남아 있다. 최종 UI·아트·음향은 사용자 승인 대상이다.

[실행 안내](../PROTOTYPE_GUIDE.md) · [현재 상태](../PROJECT_STATE.md) · [이전 서버 챕터 검사](TEST-0007-NetworkWorldReplica.md)
