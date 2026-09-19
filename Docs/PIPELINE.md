# 개발·검증·배포 파이프라인

문서 ID: PIPE-0001 · 2026-09-17

## 실행 가능한 기획 검사

PowerShell 7.2 이상에서 실행한다.

```powershell
pwsh -NoProfile -File ./ProjectPipeline.ps1 -Mode Validate -WriteReport
pwsh -NoProfile -File ./Docs/Testing/Test-ProjectPipeline.ps1
pwsh -NoProfile -File ./ProjectPipeline.ps1 -Mode Readiness
```

| 명령 | 범위 | 결과 |
|---|---|---|
| Validate | 필수 경로·명칭, 계약 값과 자료형, 기획서 대화 흔적, 링크, PowerShell 문법, CI 기본 정책 | 0 통과 / 1 실패 |
| 자기 테스트 | 정상 문서와 오류를 넣은 임시 복사본으로 검사기의 탐지 확인 | 0 통과 / 1 실패 |
| Readiness | 미정 항목·Unity 필수 파일·실제 게임 및 출시 검사 존재 여부 | 2 BLOCKED가 현재 기대 결과 |

-WriteReport는 Docs/Testing/LatestPipelineReport.json을 갱신한다. 해당 파일은 Git에서 제외하고 CI 검증 증거로만 보관한다. 외부 URL·앵커·자연어 규칙 전체의 의미, 게임 실행과 실제 핵 탐지 성능을 검사하는 도구가 아니다.

사용자 요청에 따라 확정된 Q&A 원문 블록을 서술형 기획으로 전환했다. 기존 18개 원문 해시 검사는 제거하고 확정 계약 값·자료형, 기획서의 필수 내용과 대화 잔여물 검사를 적용한다. 이 변경은 문서 형식 변경에 따른 검사 기준 갱신이며 새로운 게임 결정을 승인하는 기능은 아니다.

## GitHub Actions

[project-validation.yml](../.github/workflows/project-validation.yml)은 push·pull request·수동 실행에서 Validate와 검사기 자기 테스트를 실행한다. 원격 결과는 [게시 상태](PUBLISH_STATUS.md)와 실제 Actions 실행에서 확인한다.

외부 액션은 전체 commit SHA로 고정한다. contents 읽기 권한만 사용하고 checkout 자격증명을 유지하지 않는다. 외부 PR에 운영 키·Steam 키를 제공하거나 사용자 PC를 runner로 사용하지 않는다.

수동 실행에서 출시 준비 검사를 선택하면 아직 충족되지 않은 조건 때문에 실패한다. 이 워크플로는 게임을 빌드하거나 배포하지 않는다.

## 전체 개발 단계

| 단계 | 산출물 | 통과 기준 |
|---|---|---|
| 기획 | Plan·상세 규칙·미정 목록 | 확정과 미정 구분, 담당·예외·검증 기준 명확 |
| 정적 검사 | 계약·문서·검사기 결과 | Validate·자기 테스트 통과 |
| Unity 준비 | Editor·manifest·lock·메타 | 6000.6.1f1 목표의 공식 배포·호환 확인, 재현 가능한 import |
| 핵심 구현 | 이동·협동·퍼즐·출구 | 실제 씬에서 정상·실패 흐름 검증 |
| 온라인 | PlayFab 전용 서버·클라이언트 | 2·3·4인, 지연·손실·중복·끊김·방장 변경 검증 |
| 데이터·보안 | 저장·팀 기록·제재 | 위조 거부, 중복 정산 방지, 마이그레이션·오탐 복구 |
| 빌드·사전 배포 | 빌드·해시·시험 환경 | 시작·접속·저장·부하·버전 호환 확인 |
| 출시·운영 | 승인 배포·안내·복구 | 알려진 문제·롤백 책임과 실제 결과 기록 |

로컬 핵심 구현·PlayMode·Windows 빌드는 [TEST-0003](Testing/TEST-0003-FullPrototype.md)의 실제 결과로 관리한다. 온라인·운영·출시 검사는 별개이며, 로컬 프로토타입 검사만으로 출시 준비 차단을 해제하지 않는다.

## 로컬 게임 검증 재현

Unity Editor가 이 프로젝트를 열고 있지 않을 때 프로젝트 루트에서 실행한다. 설치된 Unity CLI와 유효한 로컬 Unity 라이선스가 필요하다. 비밀 키는 필요하지 않다.

```powershell
unity test . --mode PlayMode --output Logs/full-prototype-playmode.xml --timeout 600 --format json -- -logFile Logs/full-prototype-tests.log
unity build . --target StandaloneWindows64 --execute-method SlimeCoop.Prototype.Editor.PrototypeBuild.BuildWindows --output-path Build/FullPrototype/SlimeCoopPrototype.exe --log-file Logs/full-prototype-windows-build.log --allow-dirty-build --no-tail --timeout 900 --format json
```

씬 재생성은 Unity 메뉴의 `Slime Prototype > Rebuild Prototype Scenes`에서 수행한다. 새 체크아웃에는 TMP Essential Resources가 포함된다. 누락된 환경에서는 `PrototypeSceneBuilder.ImportTextResources`를 `-quit` 없이 실행해 완료를 기다려야 한다. 자동 캡처는 `--prototype-capture <절대 시험 출력 폴더>`를 지정한 플레이어에만 활성화되며 정상 플레이에서는 실행되지 않는다. 원격 CI에 Unity 게임 빌드·비밀 키를 자동 추가하지 않는다.

로컬 통합 명령은 [PrototypePipeline.ps1](../PrototypePipeline.ps1)이다. `-Mode Full`은 문서 검사·씬 생성·PlayMode·Windows 빌드·저장 장애를 포함한 11개 화면 캡처·별도 서버의 2/3/4인 방 관리·과도 요청 격리·2/3/4인 서버 챕터 시작/이동 복제·2/3/4인 서버 시험 배치 정산/저장/이야기/복귀·일반 로비의 2/4인 생성/참가/재참가를 순서대로 실행한다. 2/4인 대기실·서버 챕터·전환은 렌더 검사도 수행한다. `Test`, `Build`, `Capture`, `Network`, `Lifecycle`, `Lobby`로 단계별 실행할 수도 있다. 실패 코드·보고서 최신 시각·빈 테스트·캡처 오류와 필수 PNG 각각의 존재/생성 시각을 확인하며, 사람이 직접 PNG를 여는 시각 검수는 자동 성공에 포함하지 않는다. 사용자 편집기를 강제 종료하거나 GitHub·Notion으로 게시하지 않는다.

별도 프로세스의 실행 식별자·서버 ID·접속 슬롯·종료 코드와 거부 횟수는 [LocalNetwork.ps1](../LocalNetwork.ps1)이 대조한다. `-Scenario World`는 서버/런 동일성과 입력한 참가자만 실제 이동했는지도 검사한다. 보고서는 읽기 중 원자적 교체를 허용하는 공유 방식으로 연다. 명령은 `127.0.0.1`의 개발용 통신만 검사하며 클라우드 계정·요금 자원·방화벽을 변경하지 않는다. 방 관리 범위는 [TEST-0004](Testing/TEST-0004-LocalNetwork.md), 서버 챕터 시작/이동은 [TEST-0007](Testing/TEST-0007-NetworkWorldReplica.md)에 명시한다.

후속 네트워크 회귀는 [TEST-0008](Testing/TEST-0008-NetworkWaitingRoom.md)의 직접 걷는 대기방을 사용한다. 실제 입력으로 장치에 접근하고 서버의 원거리 준비 거부도 확인한다. 2/4인 방 검사에는 `waiting-room.png`와 `waiting-ready.png`의 새 생성 여부가 포함된다. 일시적인 보고서 교체 잠금은 제한된 재시도 횟수를 기록하고, 최종 보고서를 저장하지 못하면 성공 종료하지 않는다.

`-Mode Lifecycle` 또는 `LocalNetwork.ps1 -Scenario LifecycleFixture -Mode Test -Players 4`는 [TEST-0009](Testing/TEST-0009-NetworkCompletion.md)의 별도 전환 검사를 실행한다. 서버에서 참가자·상자를 시험 위치에 배치하므로 코스의 실제 입력 완주 증거가 아니다. 마지막 구간 정산 데이터·개인 저장 파일·동의 철회·접속 종료자 제외·새 대기방 세대를 대조하며, 2/4인은 `network-story.png`와 `returned-waiting-room.png`도 생성한다. 이 모드의 저장은 프로세스와 실행 ID별로 격리한다.

`-Mode Lobby` 또는 [TestLocalLobby.ps1](../TestLocalLobby.ps1)의 `-Players 2`/`4`는 일반 2D 로비의 버튼으로 전용 서버 생성·참가·취소/재시도·3D 표시·이탈/재참가·빈 서버 종료를 검사한다. 실행 ID·참가 프로세스·동일 서버·정상 종료·새 PNG를 확인하고 결과를 `Logs/Network/인원-player-lobby`에 남긴다. 실행 인자로 네트워크 화면에 바로 진입하는 기존 검사와 구분한다. [TEST-0010](Testing/TEST-0010-LocalLobbyEntry.md)의 실패 유도 로그 분류와 사람의 창 조작 미검증 경계를 따른다.

`-Mode Wipe` 또는 `LocalNetwork.ps1 -Scenario WipeFixture -Mode Test -Players 4 -Capture`는 [TEST-0011](Testing/TEST-0011-NetworkWipeRetry.md)의 전멸·2D 결과·3D 대기방·같은 챕터 새 런 검사를 실행한다. 1구간 시험 배치와 2구간 위험 지대 옆 배치 후 실제 클라이언트 이동을 사용한다. 마지막 생존자 대기·기존 입력 거부·영구 파일 보존을 확인한다. 2/4인 PNG 3장과 실행별 격리 저장을 함께 검증한다. `Full`은 기존 단계 이후 `Wipe` 2/3/4인도 수행한다.

## 필수 회귀 기준

- 한 명 출구 도착으로 클리어, 최대 5초, 시작 전원 도착 시 조기 마감.
- 사망·이탈로 시작 명단이나 협동 역할 수를 축소하지 않음.
- 실제 도착 인원 보너스, 몬스터 처치 점수 없음, 팀 총점·4인 출발 랭킹 기준.
- 전멸 후 로비·해당 챕터 처음부터 재시작, 영구 해금 보존.
- 방장 이전 중 서버 상태·타이머·점수 유지. 서버 장애는 별도 검증.
- 단발 요청 재전송은 중복 적용하지 않고, 반복 행동은 유효 요청별 검증.
- 서버가 거짓 이동·아이템·장치·도착·점수를 거부.
- 정상 지연·장애·신고만으로 핵 확정 금지, 증거 검토·이의제기·복구.
- 저장 변환 실패와 롤백에서 정상 데이터 보존.

## 운영 절차

사건 접수 → 영향 버전·범위 확인 → 증거 보존 → 조치 → 수정 → 재현·회귀 → 시험 배포 → 승인 배포 → 결과 확인 → 종료.

릴리스에 commit, Editor·패키지·콘텐츠·점수·저장 버전, 빌드 해시, 테스트 결과, 알려진 문제, 복구 조건을 연결한다. 탐지 규칙도 버전·오탐 테스트·되돌리기 경로를 관리한다.

[실제 검증 기록](Testing/TEST-0001-PlanningPipeline.md) · [운영](Operations/README.md) · [구조 설계](ARCHITECTURE.md)
