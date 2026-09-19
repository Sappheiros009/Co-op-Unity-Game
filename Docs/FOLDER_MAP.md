# 폴더 지도 — 어디에서 무엇을 수정할지 찾기

폴더명은 실제 경로의 대소문자까지 그대로 사용합니다. 아래 링크를 열면 담당 범위와 관련 폴더를 확인할 수 있습니다.

게임 기능 폴더에는 프로토타입 코드와 안내 문서가 함께 있습니다. 루트와 Docs/Testing에는 실행 가능한 기획 검증 스크립트, `.github/workflows`에는 CI 파일을 추가했습니다. 게임 코드·씬·프리팹이 생기면 해당 README에 실제 파일과 확인 방법을 기록합니다.

## 처음 보는 사람의 읽는 순서

1. Plan.md: 어떤 게임인지 확인.
2. 이 문서: 증상·요청에 맞는 첫 담당 선택.
3. 담당 README의 '다음 작업자용 빠른 안내': 실제 파일과 책임 경계 확인.
4. PROJECT_STATE.md: 구현·미정·검사 상태 확인. 작업 완료 여부는 소스·빌드·실행·게시를 각각 구분.

2026-09-20 확인: GitHub main의 코드 기준선은 PR #1 병합 커밋 2c4d6a8이다. 로컬에는 이후 미게시 변경이 있으므로 서로 같은 버전으로 취급하지 않는다. 이번 정리는 문서만 게시하며 로컬 후속 게임 코드를 포함하지 않는다.

## 실제 수정 시작점

아래 경로는 Assets/Game 기준이다. 목표 담당과 현재 물리적 코드 위치가 다른 경우 담당 README의 예외를 먼저 따른다.

| 폴더 | 쉬운 이름 | 첫 파일 |
|---|---|---|
| [Features/Player](../Assets/Game/Features/Player/README.md) | 이동과 카메라 | [PrototypeCapsulePlayer.cs](../Assets/Game/Features/Player/PrototypeCapsulePlayer.cs) |
| [Features/Cooperation](../Assets/Game/Features/Cooperation/README.md) | 물체 운반과 협동 | [PrototypeCarryable.cs](../Assets/Game/Features/Cooperation/PrototypeCarryable.cs) |
| [Features/Interaction](../Assets/Game/Features/Interaction/README.md) | 대상 선택과 행동 요청 | [PrototypeInteraction.cs](../Assets/Game/Features/Interaction/PrototypeInteraction.cs) |
| [Features/Items](../Assets/Game/Features/Items/README.md) | 소지품과 소모품 | [PrototypeInventory.cs](../Assets/Game/Features/Items/PrototypeInventory.cs) |
| [Features/Puzzles](../Assets/Game/Features/Puzzles/README.md) | 장치 성공 조건 | [PrototypeStageObjective.cs](../Assets/Game/Features/Puzzles/PrototypeStageObjective.cs) |
| [Features/Monster](../Assets/Game/Features/Monster/README.md) | 일반 몬스터 행동 | [PrototypeCapsuleMonster.cs](../Assets/Game/Features/Monster/PrototypeCapsuleMonster.cs) |
| [Features/MapGeneration](../Assets/Game/Features/MapGeneration/README.md) | 플레이 공간 조립 | [PrototypeMapBuilder.cs](../Assets/Game/Features/MapGeneration/PrototypeMapBuilder.cs) |
| [Features/StartandExit](../Assets/Game/Features/StartandExit/README.md) | 출구 집계와 팀 점수 | [PrototypeExitScoring.cs](../Assets/Game/Features/StartandExit/PrototypeExitScoring.cs) |
| [Features/Online](../Assets/Game/Features/Online/README.md) | 연결과 서버 상태 전달 | [PrototypeNetworkWorld.cs](../Assets/Game/Features/Online/PrototypeNetworkWorld.cs) |
| [Features/Save](../Assets/Game/Features/Save/README.md) | 진행과 결과 보관 | [PrototypeSave.cs](../Assets/Game/Features/Save/PrototypeSave.cs) |
| [Features/Story](../Assets/Game/Features/Story/README.md) | 기억과 챕터 사이 이야기 | [PrototypeStoryInterludeController.cs](../Assets/Game/Features/Story/PrototypeStoryInterludeController.cs) |
| [Features/UI](../Assets/Game/Features/UI/README.md) | 플레이어가 보는 화면 | [PrototypeLobbyController.cs](../Assets/Game/Features/UI/PrototypeLobbyController.cs) |
| [Core](../Assets/Game/Core/README.md) | 공통 기반과 진행 연결 | [Prototype/PrototypeGame.cs](../Assets/Game/Core/Prototype/PrototypeGame.cs) |
| [Audio](../Assets/Game/Audio/README.md) | 효과음과 소리 | [PrototypeCues.cs](../Assets/Game/Audio/PrototypeCues.cs) |
| [Editor](../Assets/Game/Editor/README.md) | Unity 제작 도구 | [PrototypeSceneBuilder.cs](../Assets/Game/Editor/PrototypeSceneBuilder.cs) |
| [Tests](../Assets/Game/Tests/README.md) | 개발자 검사 코드 | [PrototypeSceneFlowTests.cs](../Assets/Game/Tests/PrototypeSceneFlowTests.cs) |
| [Levels](../Assets/Game/Levels/README.md) | 지역과 씬 | [README.md](../Assets/Game/Levels/README.md) |
| [Art](../Assets/Game/Art/README.md) | 공통 시각 자원 | [README.md](../Assets/Game/Art/README.md) |
| [Localization](../Assets/Game/Localization/README.md) | 언어와 폰트 | [README.md](../Assets/Game/Localization/README.md) |

구조 실패는 Cooperation의 기획 책임을 참고하되 실제 입력·거리·시간 처리는 Interaction/PrototypeInteraction.cs부터 본다. UI 오류라도 멀티플레이 HUD는 Online/PrototypeNetworkClientWorld.cs를 함께 본다.

## 확인한 데이터 흐름

- 서버 입력: PrototypeNetworkTransport의 InputReceived → PrototypeNetworkWorld.OnInput → PrototypeNetworkInputBuffer.Submit → FixedUpdate의 Consume → PrototypeCapsulePlayer.StepServerInput. 전달된 입력을 서버가 소비하며 클라이언트 좌표를 최종 판정으로 삼지 않는다.
- 표시: PrototypeNetworkWorld.Capture → BroadcastWorld → 클라이언트 수신 → PrototypeNetworkReplica.Apply → 플레이어·물체 표시. 실제 멀티 HUD는 PrototypeNetworkClientWorld가 담당한다.
- 완료·저장: PrototypeExitScoring.Settle → PrototypeGame의 정산 감지 → PrototypeSession.CompleteStage → ServerStageEnded → PrototypeNetworkWorld.OnStageEnded. 마지막 구간 성공 시 PrototypeChapterCompletion.FromSession으로 결과 생성 → 클라이언트 PrototypeNetworkProgress → PrototypeSave.ApplyCompletion. 로컬 저장이며 클라우드 업로드 완료를 의미하지 않는다.

## 발견한 문제와 처리 경계

| 구분 | 근거 위치 | 영향 | 최소 처리 |
|---|---|---|---|
| 확인된 낡은 안내 | Cooperation·Items·Puzzles 등의 README '폴더만 있음' | 실제 코드 탐색 실패 | 코드 시작점 명시·낡은 문구 교정 |
| 확인된 책임 분산 | Interaction.Rescue, Core/PrototypeGame, Online/PrototypeSession | 폴더 이름만으로 잘못된 파일 수정 | 목표 책임과 현재 구현 위치 병기; 코드 이동 없음 |
| 확인된 연결 설명 오류 | Core README '서버 조정자 연결 전' | 서버 흐름 오해 | NetworkWorld의 이벤트 구독 명시 |
| 확인된 원격 차이 | main 기준선과 로컬 후속 변경 | 미게시 기능을 배포 완료로 오인 | 이번 문서 게시와 게임 코드 게시 분리 |
| 잠재적 이동 위험 | 씬·프리팹·.meta·asmdef·Resources·스크립트 경로 | 참조 손실·동작 변경 | 이동·신설·개명 제안은 승인 후 수행 |
| 잠재적 생성물 덮어쓰기 | Editor/PrototypeSceneBuilder.cs와 Levels | 씬 직접 수정이 재생성 시 소실 가능 | 수동 수정 전 생성 코드·씬 연결 확인; 재생성 미실행 |

소스는 Assets/Game, 외부 도입물은 Assets/ThirdParty, 패키지 정의는 Packages, 프로젝트 설정은 ProjectSettings에서 찾는다. Build는 실행 산출물, Library·Temp·Obj는 캐시, Logs는 실행 기록, UserSettings는 로컬 설정이다. 캐시나 빌드 결과를 게임 소스의 수정 시작점으로 삼지 않는다. 폴더를 추가로 세분화하거나 자료를 이동하지 않았다.

이번 작업은 문서 경로·호출 연결의 중간 점검만 수행한다. 빌드·컴파일·게임 실행·자동 테스트·최종 기능 검증은 수행하지 않는다. 문서 정리는 프로토타입 완성 판정이 아니다.

## 전체 구조

```text
프로젝트/
├─ Plan.md
├─ README.md
├─ AGENTS.md
├─ ProjectPipeline.ps1
├─ PrototypePipeline.ps1
├─ TestLocalLobby.ps1
├─ LocalNetwork.ps1
├─ .gitignore
├─ .github/
│  └─ workflows/
│     └─ project-validation.yml
├─ Docs/
│  ├─ FOLDER_MAP.md
│  ├─ NAMING_DECISIONS.md
│  ├─ PROJECT_PROMPT.md
│  ├─ WORKING_PRINCIPLES.md
│  ├─ REFERENCE_SOURCES.md
│  ├─ PROJECT_STATE.md
│  ├─ PROTOTYPE_GUIDE.md
│  ├─ PROTOTYPE_COVERAGE.md
│  ├─ DESIGN_TEMPLATE.md
│  ├─ DESIGN-0001-StageExit.md
│  ├─ DESIGN-0002-CharacterAssetProduction.md
│  ├─ DESIGN-0003-QualitySettings.md
│  ├─ ARCHITECTURE.md
│  ├─ PIPELINE.md
│  ├─ PROJECT_CONTRACT.json
│  ├─ Maintenance/
│  ├─ Bugs/
│  ├─ Operations/
│  └─ Testing/
└─ Assets/
   ├─ Game/
   │  ├─ Core/
   │  ├─ Features/
   │  │  ├─ Player/
   │  │  ├─ Cooperation/
   │  │  ├─ Interaction/
   │  │  ├─ Items/
   │  │  ├─ Puzzles/
   │  │  ├─ Monster/
   │  │  ├─ MapGeneration/
   │  │  ├─ StartandExit/
   │  │  ├─ Story/
   │  │  ├─ Online/
   │  │  ├─ Save/
   │  │  └─ UI/
   │  ├─ Levels/
   │  │  ├─ Shared/
   │  │  ├─ Lobby/
   │  │  ├─ Prologue/
   │  │  ├─ Episode01/
   │  │  │  ├─ Chapter01_Mine/
   │  │  │  ├─ Chapter02_LAVA/
   │  │  │  ├─ Chapter03_PollutedZone/
   │  │  │  ├─ Chapter04_ThunderSea/
   │  │  │  ├─ Chapter05_Square/
   │  │  │  ├─ Chapter06_FrozenMountain/
   │  │  │  ├─ Chapter07_Hometown/
   │  │  │  └─ StoryInterludes/
   │  │  └─ Sandbox/
   │  ├─ Art/
   │  ├─ Audio/
   │  ├─ Localization/
   │  ├─ Editor/
   │  └─ Tests/
   └─ ThirdParty/
```

로컬 작업공간에는 Unity 6000.6.1f1용 `Packages`·`ProjectSettings`, 프로토타입 코드와 16개 씬이 있습니다. 2026-09-18 `Assets/Game/Editor/PrototypeSceneBuilder.cs`로 씬·메타데이터와 Build Settings를 재생성하고 현재 소스의 별도 Windows 빌드 및 헤드리스 초기 기동을 검증했습니다. 기준선 소스는 PR #1으로 GitHub main에 병합되었으며, 이후 로컬 변경의 게시 여부는 별도 확인한다. `Build`, `Library`, `Logs`, `UserSettings`, `_UnityTemplate`은 로컬 산출물·캐시·제작용 작업공간이므로 소스 폴더 지도와 Git 게시 대상에서 제외합니다.

자동화 위치: [루트 검사기](../ProjectPipeline.ps1), [검사기 자기 테스트](Testing/Test-ProjectPipeline.ps1), [CI 파일](../.github/workflows/project-validation.yml). 실행 방법과 미구현 단계는 [파이프라인](PIPELINE.md), 실제 결과는 [TEST-0001](Testing/TEST-0001-PlanningPipeline.md)을 봅니다. `.github/workflows`는 GitHub의 표준 자동화 경로이며 사용자 지정 게임 폴더명을 바꾸지 않습니다.

## 기능별 담당

아래 폴더는 모두 `Assets/Game/Features` 아래에 있습니다.

| 폴더 | 뜻 | 주된 수정 범위 |
|---|---|---|
| [Player](../Assets/Game/Features/Player/README.md) | 플레이어 | 이동·달리기·점프·앉기와 벽 타기, 1인칭 카메라를 담당합니다. |
| [Cooperation](../Assets/Game/Features/Cooperation/README.md) | 협동 행동 | 친구 밟고 점프, 끌어올리기, 구조와 부활 행동을 담당합니다. |
| [Interaction](../Assets/Game/Features/Interaction/README.md) | 상호작용 | 조준하거나 가까이 있는 대상 중 어떤 대상을 조작할지 정합니다. |
| [Items](../Assets/Game/Features/Items/README.md) | 아이템과 소지품 | 회복품·열쇠·미끼·이동 보조 도구의 정의와 효과, 획득·소모·전달을 담당합니다. |
| [Puzzles](../Assets/Game/Features/Puzzles/README.md) | 퍼즐과 장치 | 문·스위치·동시 조작 장치·정화 장치의 작동 조건과 상태를 담당합니다. |
| [Monster](../Assets/Game/Features/Monster/README.md) | 몬스터 | 몬스터 인식·추적·공격·상태 전환과 등장 규칙을 담당합니다. |
| [MapGeneration](../Assets/Game/Features/MapGeneration/README.md) | 맵 생성 | 고정된 방과 랜덤 통로의 연결, 배치 후보 선택과 생성 규칙을 담당합니다. |
| [StartandExit](../Assets/Game/Features/StartandExit/README.md) | 시작·종료와 회차 진행 | 게임 시작·종료뿐 아니라 챕터 도전, 스테이지 전환, 전멸, 재시작을 담당합니다. |
| [Story](../Assets/Game/Features/Story/README.md) | 이야기 | 이야기 사건, 단서, 대사·자막, 귀향패 진행 조건과 목걸이 관련 사건을 담당합니다. |
| [Online](../Assets/Game/Features/Online/README.md) | 온라인 연결 | 친구 초대·접속·종료·재합류, 운영자 게임 서버와 참가자 상태 동기화. 방장 이탈과 서버 장애를 구분합니다. |
| [Save](../Assets/Game/Features/Save/README.md) | 저장과 복구 | 영구적으로 유지할 챕터 해금, 개인 이야기 기록과 사용자 설정의 저장·불러오기·복구를 담당합니다. |
| [UI](../Assets/Game/Features/UI/README.md) | 화면과 조작 안내 | 로비·준비·설정 화면, 상태창, 소지품, 스태미나, 상호작용 안내를 담당합니다. |

## 역할이 겹쳐 보일 때

| 관계 | 책임을 나누는 기준 |
|---|---|
| Player / Cooperation | 체력·이동 상태는 Player, 다른 플레이어를 돕는 행동과 성립 조건은 Cooperation |
| Interaction / Puzzles | 조작 대상 선택과 입력 전달은 Interaction, 장치 성공 조건과 상태는 Puzzles |
| Items / Cooperation | 아이템 보유·소모는 Items, 아이템을 사용하는 구조 행동은 Cooperation |
| Monster / Levels | 몬스터·보스 행동은 Monster, 실제 위치·보스 공간은 해당 Levels 챕터 |
| MapGeneration / Levels | 방·통로를 조합하고 검사하는 규칙은 MapGeneration, 조합할 콘텐츠는 Levels |
| StartandExit / Save | 회차 진행·전멸·팀 점수 산출은 StartandExit, 검증된 팀 결과·영구 해금·이야기/설정 저장은 Save. 개인 점수·개인 랭킹 기록 없음 |
| Story / Localization / UI | 사건 조건은 Story, 언어별 문장은 Localization, 화면 배치는 UI |
| Online / 각 기능 | 연결·전달·상태 동기화는 Online, 체력·아이템·장치 규칙은 담당 기능 |
| Core / 각 기능 | 여러 기능이 공유하는 기반만 Core, 소유자가 명확한 게임 규칙은 해당 기능 |

`StartandExit`라는 이름은 시작·종료뿐 아니라 기존 회차 진행 범위 전체를 포함합니다.

## 지역 폴더

`Assets/Game/Levels`의 [Shared](../Assets/Game/Levels/Shared/README.md), [Lobby](../Assets/Game/Levels/Lobby/README.md), [Prologue](../Assets/Game/Levels/Prologue/README.md), [Sandbox](../Assets/Game/Levels/Sandbox/README.md)를 공통·로비·도입·시험 공간으로 사용합니다.

`Assets/Game/Levels/Episode01`의 챕터는 다음과 같습니다.

| 폴더 | 기획서 지역 |
|---|---|
| [Chapter01_Mine](../Assets/Game/Levels/Episode01/Chapter01_Mine/README.md) | 광산 |
| [Chapter02_LAVA](../Assets/Game/Levels/Episode01/Chapter02_LAVA/README.md) | 용암 지대 |
| [Chapter03_PollutedZone](../Assets/Game/Levels/Episode01/Chapter03_PollutedZone/README.md) | 오염 지대 |
| [Chapter04_ThunderSea](../Assets/Game/Levels/Episode01/Chapter04_ThunderSea/README.md) | 천둥치는 바다 |
| [Chapter05_Square](../Assets/Game/Levels/Episode01/Chapter05_Square/README.md) | 광장 |
| [Chapter06_FrozenMountain](../Assets/Game/Levels/Episode01/Chapter06_FrozenMountain/README.md) | 얼어붙은 산 |
| [Chapter07_Hometown](../Assets/Game/Levels/Episode01/Chapter07_Hometown/README.md) | 슬라임 고향 |

챕터 사이 영상씬은 [StoryInterludes](../Assets/Game/Levels/Episode01/StoryInterludes/README.md)에서 관리합니다. 현재는 실제 영상 대신 챕터별 placeholder 씬만 있습니다.

챕터 폴더를 준비한 것은 제작 완료나 출시 범위 확정을 뜻하지 않습니다. 새 하위 폴더가 필요하면 기존 위치·제안 위치·이유·참조 영향을 제시하고 사용자 승인 후 추가한다.

## 공통 자원과 도구

| 위치 | 담당 |
|---|---|
| [Assets](../Assets/README.md) | Unity 에셋 |
| [Assets/Game](../Assets/Game/README.md) | 게임 제작 파일 |
| [Assets/Game/Core](../Assets/Game/Core/README.md) | 초기화와 공통 기반 |
| [Assets/Game/Art](../Assets/Game/Art/README.md) | 공통 시각 자원 |
| [Assets/Game/Audio](../Assets/Game/Audio/README.md) | 음악과 효과음 |
| [Assets/Game/Localization](../Assets/Game/Localization/README.md) | 언어와 폰트 |
| [Assets/Game/Editor](../Assets/Game/Editor/README.md) | 제작 보조 도구 |
| [Assets/Game/Tests](../Assets/Game/Tests/README.md) | 구현 검증 |
| [Assets/ThirdParty](../Assets/ThirdParty/README.md) | 외부 에셋과 도구 |

에셋·도구 후보를 찾는 출처와 사용 기준은 [개발 참고자료](REFERENCE_SOURCES.md), 실제 도입 파일과 검증 결과는 ThirdParty 및 담당 기능 README에서 관리합니다. 사이트 등록과 파일 도입은 구분합니다.

## 증상으로 담당 찾기

| 증상 | 먼저 볼 폴더 | 함께 볼 폴더 | 기록할 단서 |
|---|---|---|---|
| 벽 타기·점프 위치가 튐 | Player | Cooperation, Online | 벽·입력·이동 상태, 로컬/온라인 차이 |
| 친구를 끌어올리거나 살릴 수 없음 | Cooperation | Player, Items, Online | 양쪽 상태·거리·직업·소지품 |
| 상자 대신 엉뚱한 대상을 조작함 | Interaction | UI, Cooperation | 조준·주변 대상·입력·우선순위 |
| 장치를 작동해도 문이 안 열림 | Puzzles | Interaction, Online, 해당 Levels | 조건·참가 인원·장치 상태 |
| 출구로 갈 수 없는 맵이 나옴 | MapGeneration | 해당 Levels | 시드 또는 생성 조건·방·통로·검사 결과 |
| 몬스터가 인식·추적하지 않음 | Monster | Online, 해당 Levels | 종류·상태·거리·배치 |
| 중요한 아이템이 사라짐 | Items | Interaction, Puzzles, Online | 아이템·보유자·획득/소모 순서 |
| 전멸 후 챕터·점수가 잘못 초기화됨 | StartandExit | Save | 현재 챕터·해금 값·회차 점수의 전후 값 |
| 혼자 입장하면 클리어되지 않거나 협동 점수가 잘못 나옴 | StartandExit | Player, Online, UI | 첫 진입·5초 마감·정산 시점, 실제 도착자·탈락자 명단, 규칙·항목별 점수 |
| 다시 접속하면 진행도가 사라짐 | Save | StartandExit, Online | 저장 소유자·저장 시점·형식 버전 |
| 방장 이탈/서버 장애 후 진행이 끊김 | Online | StartandExit, Save | 두 사건 구분·접속 역할·서버 세션·종료 순서·팀 결과 |
| 대사·엔딩이 잘못 나옴 | Story | Save, 해당 Levels | 사건·선택·개인 진행·챕터 |
| 한글 누락·버튼 글자 잘림 | Localization | UI | 언어·폰트·해상도·화면 |
| 특정 지역의 발판·장치 위치만 문제 | 해당 Levels 챕터 | Puzzles, Player, MapGeneration | 씬·배치 파일·위치·재현 동선 |
| 정상 이용자가 핵으로 의심되거나 추방됨 | Online | 해당 기능, UI, Operations | 인증/도전 ID·시각·빌드/탐지 버전·지연·근거·조치 범위 |
| 점수 위조·중복 랭킹 등록이 의심됨 | StartandExit | Save, Online, Operations | 원본 완료 근거·도전 ID·마감 시각·항목별 점수·중복 처리·기록 상태 |
| 버그 악용과 게임 오류가 구분되지 않음 | 해당 기능 | Bugs, Testing, Operations | 재현 조건·정상 예외·규칙 버전·고의성 근거와 미확인 항목 |

온라인에서만 재현되는 경우 Online을 함께 확인합니다. 첫 담당 폴더는 조사 시작점이며 원인 확정으로 표시하지 않습니다.

보안 조사의 기준은 [시스템 구조](ARCHITECTURE.md)와 [운영 지침](Operations/README.md)입니다. Online과 각 기능이 서버 검증을 담당하고 Operations가 증거·제재·복구를 관리합니다. 실제 사건 발생을 뜻하지 않습니다.

## 사람이 지시할 때 적을 내용

```text
대상 폴더:
함께 확인할 폴더:
관련 작업/버그/운영 기록:
현재 증상 또는 바꾸려는 내용:
재현 조건:
원하는 결과와 기획 근거:
수정할 실제 파일: 조사 후 기입
영향받는 기능:
완료 확인 방법:
확인 결과와 아직 미검증인 범위:
```

작업 종류에 따라 [유지보수](Maintenance/README.md), [버그](Bugs/README.md), [운영 문제](Operations/README.md) 기록을 만들고 [검증 결과](Testing/README.md)를 연결합니다.

[시작 안내](../README.md) · [폴더명 결정 기록](NAMING_DECISIONS.md)
