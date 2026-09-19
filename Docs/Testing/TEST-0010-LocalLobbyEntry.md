# TEST-0010 — 일반 2D 로비의 로컬 서버 생성·참가

기준일: 2026-09-19 · 상태: 로컬 메뉴 연결 검증 완료, 전체 프로토타입은 진행 중

## 구현 범위

일반 실행은 2D 로비에서 시작한다. 기존 시험 동료와 시작하는 경로를 유지하고 **같은 PC 멀티플레이**에서 별도 전용 서버 만들기·참가를 제공한다. 연결 승인과 실제 대기방 상태를 받은 뒤에만 기존 로비를 닫고 직접 걷는 1인칭 3D 대기방으로 전환한다. 연결 취소·실패 시 로비와 재시도 버튼을 유지한다.

전용 서버는 Windows 실행 파일의 숨김 별도 프로세스다. 플레이어가 서버를 겸하지 않는다. `127.0.0.1`만 사용하며 계정·방화벽·클라우드 설정을 변경하지 않는다. 생성 클라이언트는 자기 서버의 준비 완료 파일과 서버 식별자를 확인한다. 다른 서버가 같은 포트를 사용하면 그 방에 생성자로 잘못 접속하지 않는다. 방장 이탈은 다른 참가자의 서버를 종료하지 않으며, 모두 나가면 약 5초 후 서버가 종료한다. 아무도 접속하지 않은 생성 서버는 시작 후 30초에 종료한다.

각 창의 이름은 **로컬 시험 저장 프로필**이다. 진행·설정을 이름의 SHA-256으로 구분한 `기존 저장 루트/LocalPlayers/해시`에 보관한다. 같은 이름의 동시 쓰기는 파일 잠금으로 차단한다. 이름 변경은 미저장 진행이 없을 때만 허용하고, 돌아온 로비에서도 마지막 프로필을 유지해 미저장 재시도와 기록 확인을 할 수 있다. 다음 실행에서 같은 이름을 입력하면 같은 시험 기록을 사용한다. 이는 Steam 인증·계정 소유권·보안 증거가 아니다. 기존 단일 사용자 저장 파일을 옮기거나 삭제하지 않는다.

UI는 기존 한글 TMP·Canvas·색상을 재사용한 임시 기능 표현이다. 최종 화면 스타일·아트·음향은 이번 변경으로 확정하지 않는다. 사용자 제공 참고 사이트 Game UI Database 접근은 robots 제한으로 내용을 확인하지 못했으며, 사이트 화면을 검토하거나 채택했다고 주장하지 않는다.

| 담당 파일 | 책임 |
|---|---|
| [LobbyController](../../Assets/Game/Features/UI/PrototypeLobbyController.cs), [LocalNetworkPanel](../../Assets/Game/Features/UI/PrototypeLocalNetworkPanel.cs), [Ui](../../Assets/Game/Features/UI/PrototypeUi.cs) | 2D 진입·이름/포트 입력·취소/재시도 |
| [LocalConnection](../../Assets/Game/Features/Online/PrototypeLocalConnection.cs) | 입력 범위·읽기 쉬운 실패 안내 |
| [LocalServerProcess](../../Assets/Game/Features/Online/PrototypeLocalServerProcess.cs) | 숨김 전용 서버 실행·준비 확인·프로세스 핸들 관리 |
| [NetworkBootstrap](../../Assets/Game/Features/Online/PrototypeNetworkBootstrap.cs) | 승인 전 로비 보존·3D 전환·접속 해제 복귀·빈 서버 종료 |
| [NetworkRoom](../../Assets/Game/Features/Online/PrototypeNetworkRoom.cs), [Transport](../../Assets/Game/Features/Online/PrototypeNetworkTransport.cs) | 통신 버전 v4·선택적 생성 서버 식별 검사·종료 상태 |
| [LocalProfile](../../Assets/Game/Features/Save/PrototypeLocalProfile.cs) | 이름별 시험 저장 경로·동시 쓰기 잠금 |
| [LocalLobbyTests](../../Assets/Game/Tests/PrototypeLocalLobbyTests.cs) | 입력·취소·UI 구조·프로필·다른 서버 거부 검사 |
| [LocalLobbyQa](../../Assets/Game/Features/Online/PrototypeLocalLobbyQa.cs), [TestLocalLobby.ps1](../../TestLocalLobby.ps1) | 일반 로비 버튼을 사용하는 실제 다중 프로세스 검사 |

## 확인된 결과와 실패 이력

- 집중 PlayMode **27/27 통과**, 21:50:32~21:50:34 KST. 로비/프로필 16개와 기존 방 모델 11개다. `Logs/local-lobby-playmode.xml`이 근거다. 최초 실행은 테스트 어셈블리의 Netcode 참조 누락으로 컴파일 실패했으며 참조 추가 후 재실행했다.
- 최초 Windows 빌드는 21:53:20 KST 성공·Errors 0이었다. 이후 QA와 현재 저장 프로필 표시를 보완하여 아래 최종 소스로 재빌드했다. 중간 빌드의 결과를 최종 실행 결과로 합산하지 않는다.
- 없는 서버에 접속하는 시험은 NGO가 `Failed to connect to server.`를 Error로 기록한다. 이 한 문구가 해당 실패 유도 단계에서 발생한 경우만 `expectedTransportErrors`로 따로 보존한다. 그 밖의 오류·예외·검증 실패는 시험을 실패시킨다. 예상 로그를 무시한 채 오류가 전혀 없었다고 표시하지 않는다.
- 첫 실제 접속 검사에서 원격 참가자의 **비활성** 카메라까지 카메라 수에 포함한 QA 조건을 발견했다. 표시용 카메라 구조를 임의로 바꾸지 않고 실제 활성 카메라·오디오 리스너를 검사한다. 실패 실행을 통과 수에 합산하지 않는다.
- 메뉴 왕복이 완료된 중간 실행에서는 게임이 만든 자식 서버의 종료 코드를 PowerShell이 늦게 읽어 null을 반환했다. 서버가 살아 있을 때 검증된 프로세스의 OS 핸들을 확보하도록 검사기를 수정한 뒤 다시 실행하여 종료 코드 0을 확인했다. 기능 성공만으로 해당 실패 실행을 전체 통과 처리하지 않았다.

## 최종 검증 결과

최종 Windows 빌드는 Unity 6000.6.1f1 StandaloneWindows64이며 런타임 DLL SHA-256은 `48355C8A566F46854DD23E6CB10A3FAECA9675F9A79930D1E19716E04841C23B`다. 모든 시각은 2026-09-19 KST다. 소스와 테스트 수정 후 다시 실행했으며 아래 단계들은 각각 실행한 결과이지 한 번의 `Full` 실행 기록이 아니다.

| 검사 | 결과 | 시각·근거 |
|---|---|---|
| Windows 빌드 | Succeeded·Errors 0·종료 0 | 22:01:19~22:02:01, `Logs/full-prototype-windows-build.log`, EXE 옆 provenance |
| 전체 PlayMode | **150/150**, 실패·건너뜀 0, 157.987초 | 22:03:57~22:06:35, `Logs/full-prototype-playmode.xml`와 같은 이름의 `.log` |
| 일반 로비 생성/참가 | **2인·4인 2단계 통과**, 시험 클라이언트 8개·별도 서버 2개 모두 정상 종료 | 22:06:36~22:07:38, `Logs/Network/{2,4}-player-lobby/result.json`과 하위 결과 |
| 기존 Network | **7단계 통과**, 프로세스 보고서 33개 PASSED·오류 0 | 22:08:20~22:10:39, 방 관리 2/3/4인·과도 요청 격리·서버 챕터 입력 복제 2/3/4인 |
| 일반 화면 캡처 | **11종 통과**, 오류 0 | 22:08:21~22:08:46, `Build/FullPrototype/Captures/result.json` 및 PNG |
| 기존 Lifecycle | **2/3/4인 3단계 통과**, 프로세스 보고서 12개 PASSED·오류 0·보고서 쓰기 재시도 0 | 22:11:23~22:14:55, `Logs/Network/{2,3,4}-player-lifecycle/result.json` |
| 문서 검사기 자체 회귀 | **22/22**, 실패 0 | 이번 작업 중 `Docs/Testing/Test-ProjectPipeline.ps1` 재실행 |
| 문서 계약 | **894/894**, 실패 0 | 최종 문서 반영 후 `ProjectPipeline.ps1 -Mode Validate -WriteReport` |

로비 시험의 클라이언트 8개에는 실제 참가 창 6개와 서버 부재를 시험하는 별도 창 2개가 포함된다. 후자의 예상 연결 오류는 각 보고서에서 분리한다. 각 시나리오의 생성자와 참가자는 같은 서버 ID를 유지하며, 생성자 재참가 시 이미 이전한 방장 역할을 가로채지 않는다. 각 참가자의 저장 경로는 분리되고 생성자의 재참가 전후 경로는 동일하다.

최종 4인 로비 시험의 `02-create-menu.png`, `03-waiting-room.png`, `04-rejoined-room.png`, `05-returned-lobby.png`와 실패 안내 PNG를 직접 열었다. 한글 입력/안내·4/4 접속·3D 공간·방장 이전 유지·2D 복귀·현재 시험 저장 이름을 확인했다. 기존 네트워크의 최종 4인 `waiting-ready.png`도 열어 3D 동료·준비 4/4·출발 장치를 확인했다. 일반 11종 캡처 전체를 사람이 검수했다고 주장하지 않는다.

서버 시험 배치 Lifecycle는 이전과 같이 실제 코스 이동 완주가 아니다. 기존 라이선스 검증 경고는 빌드 로그에 남았으며 성공 종료와 별개로 경고 자체를 수정한 것은 아니다. 최종 문서 계약 결과는 `ProjectPipeline.ps1 -Mode Validate -WriteReport` 보고서와 함께 확인한다. 이번 변경은 GitHub·Notion에 게시하지 않았다.

## 재현과 증거 경계

```powershell
pwsh -NoProfile -File ./TestLocalLobby.ps1 -Players 2
pwsh -NoProfile -File ./TestLocalLobby.ps1 -Players 4
pwsh -NoProfile -File ./PrototypePipeline.ps1 -Mode Lobby
```

명시적 QA 인자를 지정한 Windows 플레이어가 평범한 로비에서 시작하여 실제 Button 콜백과 입력 필드를 사용한다. 네트워크 부트스트랩 인자로 로비를 건너뛰지 않는다. 생성 → 별도 창 참가 → 3D 표시 → 생성자 이탈 → 방장 역할 유지/이전 → 같은 서버 재참가 → 전원 이탈 → 빈 서버 종료를 검사한다. 서버/클라이언트는 실제 별도 프로세스이며, 버튼 호출은 자동화 코드가 수행하므로 사람이 네이티브 창에서 마우스로 눌러 검수한 결과는 아니다.

보고서·로그·PNG는 `Logs/Network/{2,4}-player-lobby`에 있다. 보고서의 실행 ID·시각·프로세스 ID·서버 ID·실행 DLL 해시·정상 종료를 함께 확인한다. 캡처는 URP 오프스크린 렌더이며 UI 클릭 가능성·모든 해상도·최소 사양 성능 증거는 아니다. 일반 메뉴에는 QA가 작동하지 않는다.

대기 중인 방에 나갔다 다시 참가하는 검사이며, 런 중 안전 구간 재접속·진행 복원 정책을 구현한 것은 아니다. 7챕터 실제 입력 완주·온라인 전멸 후 연속 재도전·Steam/PlayFab·최종 표현·원격 게시는 별도 범위다.

[실행 안내](../PROTOTYPE_GUIDE.md) · [상태](../PROJECT_STATE.md) · [서버 완료·저장·이야기](TEST-0009-NetworkCompletion.md)
