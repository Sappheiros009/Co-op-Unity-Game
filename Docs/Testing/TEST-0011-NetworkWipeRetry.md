# TEST-0011 — 전멸·2D 로비·3D 대기방·재도전

기준일: 2026-09-19 · 상태: 로컬 전멸·재도전 검증 통과, 전체 프로토타입 진행 중

## 구현과 책임

로비는 2D, 슬라임 대기방은 직접 걷는 1인칭 3D를 유지한다. 전멸 시 서버는 런 점수·시간·임시 진행을 초기화하고 준비 상태를 해제한다. 클라이언트는 2D 전멸 결과에서 대기방으로 돌아간 뒤 중앙 준비 장치를 이용해 같은 챕터 1구간부터 새 런을 시작한다. 별도 서버와 참가 연결은 유지한다. 이것은 이야기 장면의 전원 동의와 다른 기존 전멸 복귀 경로다.

서버의 전멸 초기화와 별개로 클라이언트 표시 맵을 만드는 임시 `PrototypeSession` 구간 정보가 남는 누락을 수정했다. 검증된 전멸 상태를 처음 적용할 때 한 번만 초기화한다. 반복 전멸 패킷은 대기방을 다시 닫거나 초기화를 반복하지 않는다. 영구 해금·업적·팀 기록·발견한 기억·사용자 설정은 지우지 않는다.

| 담당 파일 | 책임 |
|---|---|
| [PrototypeGame](../../Assets/Game/Core/Prototype/PrototypeGame.cs), [PrototypeSession](../../Assets/Game/Features/Online/PrototypeSession.cs) | 기존 전멸 판정·임시 상태 초기화 |
| [NetworkWorld](../../Assets/Game/Features/Online/PrototypeNetworkWorld.cs), [NetworkRoom](../../Assets/Game/Features/Online/PrototypeNetworkRoom.cs) | 서버 결과·준비 해제·새 대기방 세대·새 런 |
| [NetworkClientWorld](../../Assets/Game/Features/Online/PrototypeNetworkClientWorld.cs) | 이번 수정: 클라이언트 임시 정보 초기화·기존 2D 결과/복귀 |
| [PrototypeHazard](../../Assets/Game/Features/MapGeneration/PrototypeHazard.cs), [HazardTests](../../Assets/Game/Tests/PrototypeHazardTests.cs) | 기존 위험 볼륨과 서버 소유 발 위치로 피해·이탈·점프·출구 보호 판정 |
| [ServerPlayerTests](../../Assets/Game/Tests/PrototypeServerPlayerTests.cs), [MotorProbe](../../Assets/Game/Tests/PrototypeHazardServerMotorProbe.cs) | 실제 `FixedUpdate → StepServerInput` 피해·다운 회귀 |
| [NetworkReplicaTests](../../Assets/Game/Tests/PrototypeNetworkReplicaTests.cs) | 전멸 초기화·반복 패킷·저장 보존·새 구간 카메라 회귀 |
| [NetworkQa.Wipe](../../Assets/Game/Features/Online/PrototypeNetworkQa.Wipe.cs), [NetworkQa](../../Assets/Game/Features/Online/PrototypeNetworkQa.cs) | 명시적 시험 인자에서만 작동하는 다중 프로세스 검사 |
| [LocalNetwork.ps1](../../LocalNetwork.ps1), [PrototypePipeline.ps1](../../PrototypePipeline.ps1) | `WipeFixture`/`Wipe` 실행·보고서/디스크/종료 검증 |

StartandExit가 회차 규칙을, Online이 서버/클라이언트 전환을, Save가 영구 데이터를 담당한다. 최종 UI·외형·서버 제품 선택은 변경하지 않았다.

위험 판정은 `PrototypeHazard.FixedUpdate`에서 서버/로컬 판정 월드의 참가자·몬스터 명단을 순회한다. 발 기준점은 캐릭터 원점 위 0.1m이며 기존 위험 Collider의 `ClosestPoint`로 내부 여부를 판별한다. 영역 밖·위로 점프한 상태·출구 안전 상태에서는 피해를 주지 않는다. 복제 월드에서는 판정하지 않으며 클라이언트가 체력이나 위치를 지정하는 요청을 받지 않는다. 영역 크기·색상·기존 DPS는 바꾸지 않았고, 콜백과 위치 판정을 이중 적용하지 않는다.

## 검사 방법

```powershell
pwsh -NoProfile -File ./PrototypePipeline.ps1 -Mode Wipe
pwsh -NoProfile -File ./LocalNetwork.ps1 -Mode Test -Players 4 -Scenario WipeFixture -Capture
```

별도 서버와 실제 2·3·4개 클라이언트를 `127.0.0.1`로 연결한다. 각 실행 ID·참가자마다 저장 경로를 격리하며 사용자 저장은 사용하지 않는다.

1. 실제 대기방 이동 입력으로 챕터 지점·준비 장치에 접근한다.
2. 1구간은 기존 **서버 시험 배치**로 정산한다. 점수와 발견한 기억을 확인한다.
3. 2구간에서 서버가 참가자를 사망 경로 옆에 배치한다. 이후 **실제 클라이언트 이동 입력**으로 위험 지대에 들어가 피해를 받는다. 마지막 생존자는 다른 전원이 쓰러진 뒤 3초를 기다리므로 역할 부족만으로 자동 전멸하지 않는지 검사한다.
4. 전멸 후 점수·시간·임시 기억·준비 상태 초기화, 완료 기록 미생성, 2D 결과 카메라를 확인한다.
5. 기존 복귀 버튼 콜백으로 3D 대기방에 돌아간다. 이전 대기방 입력 세대와 이전 런 입력을 거부하는지 확인한다.
6. 중앙 준비 장치에 다시 걸어가 새 런을 시작한다. 같은 챕터 1구간, 새 런 ID, 체력·소지품·점수 초기화, 정상 이동을 확인한다.
7. 미리 저장한 별도 챕터의 해금·업적·기록과 이번 발견 기억·설정이 디스크 재로드 후 동일한지 대조한다. 전용 서버는 개인 저장 파일을 만들지 않는다.

`Logs/Network/{2,3,4}-player-wipe`에 결과 JSON·프로세스 로그를 남긴다. 2/4인에는 `wipe-lobby.png`, `wipe-returned-waiting.png`, `wipe-retry-chapter.png`를 생성한다. 검사는 자동 버튼 콜백과 입력 패킷을 이용하며 사람의 네이티브 창 조작 검수는 아니다.

## 최종 로컬 실행 결과

| 검사 | 결과 | 실행 시각·근거 |
|---|---|---|
| 전체 PlayMode | **156/156**, 실패·건너뜀 0, 164.466초 | 23:02:42~23:05:27 KST, `Logs/full-prototype-playmode.xml` |
| Windows 빌드 | 성공, exit 0 | 23:06:16~23:06:59 KST, `Build/FullPrototype/SlimeCoopPrototype.provenance.json` |
| 별도 서버 + 2인 전멸/재도전 | **PASSED**, 3개 프로세스 정상 종료 | 23:13:04~23:13:38, `Logs/Network/2-player-wipe/result.json` |
| 별도 서버 + 3인 전멸/재도전 | **PASSED**, 4개 프로세스 정상 종료 | 23:13:39~23:14:14, `Logs/Network/3-player-wipe/result.json` |
| 별도 서버 + 4인 전멸/재도전 | **PASSED**, 5개 프로세스 정상 종료 | 23:14:15~23:14:54, `Logs/Network/4-player-wipe/result.json` |
| Wipe 파이프라인 | **3/3 단계 PASSED** | 23:14:55 KST, `Logs/PrototypePipelineReport.json` |
| PNG 직접 검수 | **2/4인 각 3장, 총 6장** | 2D 전멸 안내/복귀 버튼, 3D 대기방의 7개 지점/준비 해제, 챕터 1구간·전원 HP 100·팀 점수 0 표시 확인 |
| 기존 4인 완료·이야기·복귀 회귀 | **PASSED**, 별도 서버+4인, 저장 후 마지막 투표자 이탈·남은 3인 복귀 | 23:15:29~23:16:40 KST, `Logs/Network/4-player-lifecycle/result.json`, 실행 ID `460f39c0009a40e0856a578258ec38df` |
| 기존 4인 일반 로비 생성·참가·왕복 회귀 | **PASSED**, 실패/취소/재시도·방장 이전·재참가·빈 서버 정상 종료 | 23:16:43~23:17:13 KST, `Logs/Network/4-player-lobby/result.json`, 실행 ID `adab1cb1a8424c96b2481fe3b5dc4eb7` |
| 문서 계약 | **936/936**, 실패 0 | `ProjectPipeline.ps1 -Mode Validate -WriteReport`, `Docs/Testing/LatestPipelineReport.json` |

전체 PlayMode 이후 변경한 C#은 독립적으로 갱신되는 방·월드 스냅샷을 같은 상태에서 비교하도록 하는 QA 대기 조건뿐이다. 최종 빌드의 전멸 검사 12개 프로세스 보고서는 모두 `PASSED`, 오류 0, 정상 종료다. 영구 파일 재로드·새 런 ID·동일 서버·구세대 입력 거부까지 검사했다. 라이선스 검증 관련 기존 경고 자체를 수정한 것은 아니다.

기존 경로도 같은 최종 DLL 해시에서 재검사했다. 추가로 일반 2D 로비와 4인 3D 대기방, 이야기 3/4 동의 대기와 이탈 후 3인 3D 복귀 PNG 총 4장을 직접 열었다. 이로써 이번 직접 검수는 전멸 6장과 기존 경로 4장, 총 10장이다. 일반 메뉴 검사는 실제 버튼 콜백이며 네이티브 마우스 클릭이 아니고, 완료 경로는 서버 시험 배치가 포함된 [TEST-0009](TEST-0009-NetworkCompletion.md) 방식이다.

실행 파일은 `Build/FullPrototype/SlimeCoopPrototype.exe`다. 해당 빌드의 `SlimeCoopPrototype_Data/Managed/SlimeCoop.Prototype.Runtime.dll` SHA-256은 `982E40878BA262126F6AEC4CC96F602A5F1C642D492D3196805528DAB1D61D2C`다. 최종 전멸 검사 실행 ID는 2인 `04f036af414240649fcc4c5344e913df`, 3인 `746dfda23a734617b2204cb1a8102c1c`, 4인 `887a6a391e504cb7acb6fe27364edfe2`다. 재실행 시 같은 보고서 이름이 갱신되므로 이 시각·실행 ID·빌드 해시와 함께 대조한다.

## 수정 중 실패 이력

- 최초 수정 후 집중 PlayMode는 **8/8 통과**, 2026-09-19 22:36:06~22:36:10 KST, 4.248초였다. `Logs/wipe-focused-playmode.xml`은 후속 집중 검사 22/22 결과로 갱신했다.
- 최초 집중 검사는 7/8이었다. 시험용 Transport와 ClientWorld를 같은 오브젝트에 붙여 `SendMessage("OnWorld")`가 인자 수가 다른 두 함수로 전달되었다. 시험용 Transport를 별도 오브젝트로 분리하고 재실행했다. 오류 로그를 무시하거나 실패를 통과에 합산하지 않았다.
- 최초 2인 실제 전멸 실행(`46a724275bc74da2b0ed6c51a82ac997`, 22:38:37~22:41:39 KST)은 실패했다. 2구간 위험 지대까지 이동했지만 체력이 100으로 유지되어 watchdog이 종료했다. 먼저 위험 지대에 kinematic·중력 없는 바디를 추가해 명시적인 물리 트리거로 구성했지만, 이것만으로 실제 서버 증상은 해결되지 않았다. 강제 체력 변경으로 실패를 우회하지 않았다.
- 물리 수정 후 집중 검사 10/11에서 위험 지대의 체력 감소·다운·이탈 후 피해 중지는 통과했다. 보호 검사 한 건은 Setup에서 이미 받은 첫 피해를 무시하고 100과 비교해 실패했다. 출구 보호 시작 시 체력을 기준으로 추가 피해가 없는지 검사하도록 수정했다.
- 중간 전체 PlayMode 154/154는 22:45:12 KST 통과했지만, 뒤이은 실제 2인 실행(`bc168142e40d4473ac37aa3736962118`, 22:47:16~22:50:18 KST)도 체력이 100으로 남아 실패했다. 단순 CharacterController 검사가 실제 서버의 매 틱 이동을 포함하지 않아 누락을 잡지 못했다.
- 실제 `FixedUpdate → StepServerInput` 재현 검사(`Logs/wipe-motor-reproduction.xml`, 22:50:21~22:50:22 KST)는 체력 100으로 실패했다. 충돌체의 반복 크기 대입을 줄이는 가설도 실제 회귀에서 실패해 해당 이동 수정은 되돌렸다. 이후 `wipe-motor-diagnostic.xml`에서 Stay 17회가 캐릭터가 아닌 바닥 `Room_2_Floor`와의 접촉임을 확인했다. 엔진 내부 원인을 확정했다고 주장하지 않는다.
- 최종 수정은 기존 위험 영역과 서버 참가자의 발 위치를 직접 대조하는 판정이다. 위험/캐릭터 Rigidbody 추가 없이 같은 형상·피해량을 유지한다. 앞선 Rigidbody 추가와 접촉 디버거는 제거했으며 테스트 전용 FixedUpdate 어댑터만 유지한다. 실패 재현 XML을 성공 검사로 덮어쓰지 않는다.
- 해당 위치 판정 수정의 집중 회귀 **22/22**, 22:59:19~22:59:33 KST가 통과했다. 실제 FixedUpdate 서버 모터도 포함한다. 이후 2인 실행(`98e846afb8ec48b39cd8964df99c0291`, 23:02:14~23:02:39 KST)은 실제 전멸·점수 초기화·클라이언트 2D 결과까지 진행했다. 검사기가 최신 월드의 Lobby와 아직 갱신 전인 방의 Reserved를 비교해 `server_wipe_not_reset`으로 중단했으므로 전체 성공으로 집계하지 않았다. 방 스냅샷 갱신 후 준비 상태를 대조하도록 QA를 보완했다.
- 다음 2인 실행(`7d847935698e4486b29701cc39b24238`, 23:07:25~23:07:59 KST)은 Unity 프로세스 3개가 모두 통과·정상 종료했지만 PowerShell 저장 대조가 실패했다. 실제 설정은 Unity float의 `0.12999999523162842`로 저장되어 `0.13`과 정확 비교할 수 없었다. 해당 감도만 유한값·오차 `0.000001` 이내로 검사하도록 수정했다. C#의 변경 전후 전체 저장 JSON 동일성 검사와 다른 필드 검사는 유지했다. 이후 위 2/3/4인 전체 재실행이 통과했다.
- 문서 검사기 자체 회귀의 첫 실행은 Unity 빌드와 동시에 원본을 복사하다 빌드용 임시 `Assets/Resources/PerformanceTestRunInfo.json`이 제거되어 중단됐다. 동시 실행 실패를 성공으로 계산하지 않았으며 빌드 후 재실행은 **22/22** 통과했다.

## 검증 경계

시험 위치 배치를 포함한 전멸 전환 검증이다. 7챕터 실제 입력 완주, 여러 차례 장시간 재도전, 이탈/재접속과 전멸의 동시 경계, 외부 네트워크 지연·손실, Steam/PlayFab, 최소 사양 성능·최종 UI/아트 검수가 아니다. GitHub·Notion 게시도 별도 완료 조건이며 이번 로컬 결과로 대체하지 않는다.

[현재 상태](../PROJECT_STATE.md) · [실행 안내](../PROTOTYPE_GUIDE.md) · [일반 로비 진입](TEST-0010-LocalLobbyEntry.md)
