# TEST-0009 — 서버 완료 결과·로컬 저장·이야기 합의·대기방 복귀

기준일: 2026-09-19 · 상태: 서버 결과·시험 배치 전환 검증 완료, 전체 온라인 코스는 진행 중

## 구현과 데이터 책임

전용 서버가 마지막 보스 구간을 마감하면 런 ID·챕터·고정 시작 인원·팀 점수·마감 시간·배점/빌드 버전을 한 번 확정한다. 클라이언트는 서버에서 받은 값과 런·구간·명단·정산 상태의 일치를 검사한 뒤 자기 로컬 진행 파일에 반영한다. 표시용 `PrototypeSession`의 점수나 로컬 시험 인원으로 결과를 재계산하지 않는다. 클라이언트가 완료 결과를 서버로 제출하는 명령은 없다.

해금·시험 조각·업적은 2~4인 모두 반영한다. 팀 기록은 **시작 4인**인 완료 결과만 저장하며 일부 미탈출·이탈로 시작 인원을 줄이지 않는다. 같은 런의 반복 패킷은 기록·파일 쓰기를 중복하지 않는다. 동일 런의 모순된 결과는 거부한다. 디스크 실패는 메모리에 진행을 유지하고 기존 미저장 안내·명시적 재시도로 처리한다. 손상·미래 버전 파일은 덮어쓰지 않는다.

발견한 기억은 서버 런 기록을 각 참가자가 로컬에 저장하며 전멸해도 보존한다. 전용 서버 프로세스에는 개인 진행을 쓰지 않는다. 이야기 동의는 현재 접속 중인 전원이 대상이고 철회할 수 있다. 접속 종료자는 동의 대상에서만 빠진다. 이야기 후 같은 서버의 새 대기방 세대로 복귀하고 준비를 해제한다. 이는 Steam 인증·클라우드 영구 보관·공개 랭킹의 구현이 아니다. 연결 중 끊겨서 결과를 받지 못한 참가자의 추후 복구는 미구현이다.

| 담당 파일 | 역할 |
|---|---|
| [ChapterCompletion](../../Assets/Game/Features/Save/PrototypeChapterCompletion.cs) | 세션 전역값과 분리된 완료 데이터·범위·동일 결과 검사 |
| [Save](../../Assets/Game/Features/Save/PrototypeSave.cs) | 공통 완료 반영·런별 기록 중복 방지·기억·원본 보호 |
| [NetworkProgress](../../Assets/Game/Features/Online/PrototypeNetworkProgress.cs) | 검증된 서버 결과/기억을 클라이언트 저장에 투영 |
| [NetworkWorld](../../Assets/Game/Features/Online/PrototypeNetworkWorld.cs), [WorldState](../../Assets/Game/Features/Online/PrototypeNetworkWorldState.cs) | 최종 구간 마감에서 결과 동결·명시적 완료 구분자·수신 값 검사 |
| [ClientWorld](../../Assets/Game/Features/Online/PrototypeNetworkClientWorld.cs), [StoryMemory](../../Assets/Game/Features/Story/PrototypeStoryMemory.cs) | 결과 표시 경로 연결·서버의 개인 파일 쓰기 제거 |
| [NetworkProgressTests](../../Assets/Game/Tests/PrototypeNetworkProgressTests.cs) | 인원·충돌 결과·반복·쓰기 잠금·미래 파일·전멸 기억 회귀 |
| [Lifecycle QA](../../Assets/Game/Features/Online/PrototypeNetworkQa.Lifecycle.cs), [실행 스크립트](../../LocalNetwork.ps1) | 별도 프로세스·명시적 서버 시험 배치·결과 파일 대조 |

## 검증 기록

- 최초 집중 검사: **23/23 통과**, 실패 0, 4.267초(21:05:39~21:05:43 KST). 새 결과 저장 9개, 기존 저장 7개, 복제 7개다. 이후 시험 코드의 `IReadOnlyList.Find` 컴파일 오류가 발생해 해당 호출을 `FirstOrDefault`로 수정했다. 실패한 실행을 검사 통과로 합산하지 않는다.
- 수정 후 Windows 빌드: Unity 6000.6.1f1 StandaloneWindows64, **성공·Errors 0**, 21:11:09~21:12:21 KST. `Build/FullPrototype/SlimeCoopPrototype.exe` 및 같은 폴더의 provenance, `Logs/full-prototype-windows-build.log`가 근거다. 런타임 DLL SHA-256은 `0F31A98414670D718F25DE68501A57DB91D3458196870369228747F6235750E4`다. 기존 라이선스 검증 경고는 있었으나 종료 코드는 0이며, 경고 자체를 수정한 것은 아니다.
- 최초 실제 2인 전환 시험은 **통과**했다(21:12:58~21:14:11 KST). 분리된 서버+클라이언트 2개에서 챕터 1의 6구간 정산, 2구간의 한 명 도착/5초 집계, 서버 결과 저장, 동의 누락 대기·철회·재동의, 3D 대기방 복귀와 디스크 재읽기를 확인했다. 이야기 1/2 동의 화면과 실제 2/2 접속·준비 0/2로 돌아온 대기방 PNG를 직접 열었다.
- 최종 전체 PlayMode: **134/134 통과**, 실패·건너뜀 0, 167.619초(21:13:31~21:16:19 KST). `Logs/full-prototype-playmode.xml`과 `Logs/full-prototype-playmode.log`가 근거다. 이전 125개에 결과 저장 9개가 추가됐다.
- 최종 `Lifecycle` 파이프라인: **2/3/4인 3단계 모두 통과**, 21:14:40~21:18:21 KST. 각각 3/4/5개 프로세스(서버 포함), 총 12개의 최종 보고서가 PASSED·오류 0이고 정상 종료했다. `Logs/Network/{2,3,4}-player-lifecycle/result.json`과 각 `server.json`, `client-번호.json`이 근거다. 2/3/4인 팀 시험 점수는 6936/8186/9436, 집계 시간은 약 31.461/31.449/31.479초였다. 서버 결과와 각 클라이언트 저장 파일을 대조했다. 2/3인은 공개 기준의 4인 팀 기록을 만들지 않고 해금·기억만 보존한다.
- 4인에서는 마지막 투표자가 완료 결과를 디스크에 저장한 후 접속을 종료했다. 나머지 3명은 같은 서버의 대기방 세대 2로 복귀하고 준비 0/3으로 초기화했다. 네 참가자의 기록은 시작 4인을 유지한다. 최종 4인 `network-story.png`의 3/4 동의·철회 버튼과 `returned-waiting-room.png`의 접속 3/4·준비 0/3·3D 공간을 직접 열어 확인했다.
- 최종 전환 시험 중 보고서 교체의 일시적 파일 잠금 재시도는 2인 16회, 3인 2회, 4인 0회였다. 모든 최종 파일 저장·정상 종료를 확인했으며 게임 저장 실패나 통신 성공으로 혼동하지 않는다. 지속적인 보고서 쓰기 실패는 기존 제한에 따라 실패 종료한다.
- 같은 빌드의 일반 화면 캡처 **11종 통과·오류 0**, 21:20:06~21:20:33 KST. `Build/FullPrototype/Captures/result.json`이 근거다. 최종 `01-Lobby.png`와 `02-WaitingRoom.png`를 직접 열어 로비 2D·대기방 3D·챕터 지점/준비 장치를 확인했다. 나머지 9종을 모두 직접 검수했다고 주장하지 않는다.
- 기존 `Network` **7단계 모두 통과**, 21:20:06~21:22:40 KST. 방 관리 2/3/4인·과도 요청 격리·챕터 시작/입력 격리 2/3/4인으로, 시나리오별 `Logs/Network/*/result.json`과 프로세스 보고서가 근거다. 새 통신 버전에서도 실제 입력 대기방·권한·고정 명단·방장 이전·시작/이동 경로를 유지했다. `Lifecycle` 3단계와 합계 10개 네트워크 시나리오이지만 한 번의 `Full` 실행으로 표시하지 않는다.
- 문서 계약 검사 **857/857 통과·실패 0**. 검사기 자체 회귀 22개는 이전 단계 기록이며 이번에 다시 실행한 결과가 아니다. 씬 YAML 수동 수정·편집기 강제 종료·사용자 진행 파일 초기화·유료 서비스 생성·원격 게시는 하지 않았다.

## 시험 방식과 재현

```powershell
pwsh -NoProfile -File ./PrototypePipeline.ps1 -Mode Lifecycle
pwsh -NoProfile -File ./LocalNetwork.ps1 -Mode Test -Players 4 -Scenario LifecycleFixture -Capture
```

`LifecycleFixture`는 별도 서버와 실제 2/3/4인 클라이언트를 실행한다. 대기방은 클라이언트 입력으로 실제 장치까지 걷는다. 챕터에서는 **서버 QA가 참가자·상자를 시험 위치에 배치**하고 기존 장치·보스·출구·정산 코드를 통과시킨다. 완료·점수를 직접 덮어쓰거나 클라이언트 RPC로 시험 배치를 호출하지 않는다. 본 검사는 코스 이동·퍼즐 접근의 실제 조작 완주가 아니다.

결과·로그·PNG는 `Logs/Network/{2,3,4}-player-lifecycle`에 남긴다. 각 프로세스의 저장은 해당 폴더 안 `Save-이름/실행ID`로 격리한다. 서버/런 ID·정상 종료·6구간 수신·한 명 출구 집계·실제 `progress.json`과 서버 결과의 일치·새 캡처를 대조한다. 2/3인은 전원 재동의로 복귀하고, 4인은 마지막 투표자가 자기 결과 저장 후 나가며 나머지 3명이 복귀한다. 모든 경우 시작 인원은 결과에서 유지된다. 정상 실행에서는 QA가 작동하지 않으며 시험에는 명시적 QA 역할·실행 ID·분리된 저장 경로가 필요하다.

`PrototypePipeline.ps1 -Mode Full`에도 이 3단계를 연결했다. `Network`의 기존 7단계는 별도로 유지한다. 공통 `Logs/PrototypePipelineReport.json`은 마지막 모드만 표시하므로 각 시나리오 폴더의 `result.json`도 함께 확인한다.

## 남은 범위

7챕터 전체의 다중 클라이언트 실제 입력 완주, 아이템/구조/지역 위험의 완전한 표시 회귀, 전체 온라인 전멸·재도전, 일반 2D 로비의 방 생성/참가 연결, 특기·연습, 지연·손실·재접속·결과 재전달, Steam/PlayFab, 최소 사양 성능, 이번 GitHub/Notion 게시는 남아 있다. 시험 배치·로컬 파일·통신 성공을 출시·보안·서비스·최종 디자인 승인으로 확대하지 않는다.

[실행 안내](../PROTOTYPE_GUIDE.md) · [상태](../PROJECT_STATE.md) · [이전 멀티 대기방](TEST-0008-NetworkWaitingRoom.md)
