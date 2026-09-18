# 개발 상태와 상세 결정 목록

기준일: 2026-09-18

## 현재 기준

확정 게임 규칙은 [Plan.md](../Plan.md), 출구 세부 규칙은 [DESIGN-0001](DESIGN-0001-StageExit.md), 기술 책임은 [ARCHITECTURE](ARCHITECTURE.md)를 따른다. 이 문서는 미정 사항과 실제 진행 상태만 관리한다.

## 실제 구현 상태

| 영역 | 상태 |
|---|---|
| 기획·기능별 폴더·관리 양식 | 작성 |
| 문서·계약 검사기 | 실행 가능 |
| 검사기 자기 테스트 | 실행 결과는 [검증 기록](Testing/TEST-0001-PlanningPipeline.md) 참조 |
| 원격 게시·GitHub Actions | [게시 상태](PUBLISH_STATUS.md)에서 실제 결과 관리 |
| Unity 프로젝트 | GitHub 기획을 바탕으로 로컬에 프로토타입 코드·`Packages`·`ProjectSettings`를 만들고 16개 씬과 메타데이터를 재생성했다. `codex/00-prototype-baseline` 커밋 `660b423`으로 게시했으며 `main`은 변경하지 않음 |
| Unity 6000.6.1f1 | 2026-09-18 현재 소스로 16개 씬 생성·Build Settings 등록·StandaloneWindows64 별도 검증 빌드 성공(`Errors: 0`). 헤드리스 초기 기동 확인, 전체 화면 흐름 수동 회귀는 미실행 |
| 프로토타입 출구 규칙 테스트 | `PrototypeExitScoringTests` PlayMode 4개 통과. 전체 기능·멀티클라이언트 테스트는 미구현 |
| PlayFab·Steam 연동 | 서비스 방향 확정. 서버 개설·실제 연동·운영 검증 미실행 |
| 게임 플레이·멀티플레이·출시 | 캡슐 기반 로컬 프로토타입만 구현. PlayFab·Steam 연동·실제 멀티플레이·출시 미검증·미배포 |

## 로컬 프로토타입 구현과 현재 파일 상태

GitHub `main`은 기획·폴더 골격의 기준 이력이고, 이 작업공간의 Unity 프로토타입은 해당 기획을 바탕으로 AI에게 요청해 제작한 후속 구현이다. 후속 구현은 `codex/00-prototype-baseline` 브랜치의 커밋 `660b423`으로 원격에 게시했으며 `main` 반영과 Notion 갱신은 별도 검토 대상이다.

- `Assets/Game/Core/Prototype`, `Assets/Game/Features`, `Assets/Game/Editor`에 로비·대기실·Chapter01~07·스토리 인터루드 흐름을 생성하고 실행하는 C# 프로토타입 코드가 있습니다.
- 기존 작업 기록과 `Build/SlimeCoopPrototype.provenance.json`은 과거 Unity 6000.6.1f1 Windows 빌드 성공을 증명합니다.
- 2026-09-18 Unity 6000.6.1f1에서 `PrototypeSceneBuilder.BuildScenes`를 실행해 `PrototypeLobby.unity`, `PrototypeWaitingRoom.unity`, `PrototypeChapter01~07.unity`, `PrototypeStoryInterlude_Chapter01~07.unity` 총 16개 씬과 메타데이터를 재생성했습니다.
- `ProjectSettings/EditorBuildSettings.asset`은 로비·대기실·챕터와 스토리 인터루드를 흐름 순서대로 16개 등록합니다. 현재 소스의 별도 Windows 검증 빌드는 `Errors: 0`으로 성공했습니다.
- 프로토타입 흐름은 `Lobby → WaitingRoom → 선택한 Chapter01~07 → 해당 StoryInterlude → WaitingRoom`입니다. 챕터 출구 집계가 끝나면 컷신 placeholder로 자동 이동하고, 컷신에서 계속하기를 누르면 대기실로 돌아옵니다.
- `PrototypeSceneBuilder`는 오브젝트·카메라·조명·placeholder 재질/스프라이트와 16개 씬을 생성하고 Build Settings에 등록합니다. `PrototypeBuild`로 현재 소스의 Windows 검증 빌드를 재현했습니다.
- 캡슐·기본 색상·임시 2D 장식은 기능 확인용 placeholder입니다. 최종 캐릭터·몬스터·UI·VFX·SFX·조명·후처리 스타일은 사용자 승인 전까지 확정하지 않습니다.
- 이 구현은 서버 권한, Steam 인증·제재, PlayFab 운영, 실제 네트워크 동기화 또는 저사양 성능 목표를 충족했다는 의미가 아닙니다. 각 항목은 별도 검증 대상으로 유지합니다.

기존 구현 완료 기록과 빌드 산출물은 당시 결과의 증거로 보존하며, 현재 파일 상태의 새 검증은 [TASK-0004](Maintenance/TASK-0004-SourceBaselineIntegration.md)에 별도로 연결한다. 씬·메타데이터 재생성·컴파일·빌드·헤드리스 초기 기동은 확인했지만 화면 흐름 전체의 수동 회귀 결과로 확대 해석하지 않는다.

## 확정된 운영·사용자 결정

- 프론트엔드는 플레이어가 보고·듣고·느끼는 클라이언트 경험 계층으로 정의한다.
- 캐릭터·몬스터·보스 외형, UI/UX 스타일, VFX·SFX·음악·애니메이션·카메라·조명·후처리·지역 분위기의 최종 결정권은 사용자에게 있다. AI는 후보안과 기술 영향을 제안하며 사용자 승인 전에는 확정·구매·외주·제작 지시로 사용하지 않는다.
- 고품질 아트의 기본 방향은 포토리얼리즘이 아닌 판타지 이세계풍 스타일라이즈드이며, 저사양 PC 성능을 고려한 LOD·텍스처·셰이더·VFX·컬링을 우선한다.
- 자동·낮음·중간·높음·사용자 지정 품질 프리셋은 클라이언트 로컬 표현만 조절하며 서버 판정·충돌·점수·출구·협동 조건·랭킹을 변경하지 않는다.

## 구현 전에 정할 항목

| 영역 | 남은 결정 | 담당 |
|---|---|---|
| 점수 | 시간 시작점·단위·반올림·공식·상한, 2~4인 배점, 보스·장치 인정·중복 횟수 | StartandExit |
| 출구 경계 | 정확한 5초 경계 tick, 지연 보정, 진입 직후 끊김 인정 | StartandExit, Online |
| 랭킹 | 기록 집계 단위·시즌·표시 명단·완전 동점·오염 기록 처리 | Save, Operations |
| 서버 비용 | 월 예산·동접 목표·최소/최대 서버 수·유휴 정리·로그 보관 | Online, Operations |
| 지역 | Korea Central 우선 후보, Japan East·Southeast Asia 대체 후보의 실제 가용성·견적·지연 확인 | Online |
| 네트워크 | 패키지 조합, tick·대역폭·재접속 유예·명단 확정, 안전 구간 합류 상세 | Online |
| 복구 | 서버 장애 시 런 복원, 저장 실패 재시도·슬롯·보관 수·삭제 요청·롤백 범위 | Save |
| 보호·운영 | 확정 탐지 기준·제재 종류/기간·재심 처리기한·증거 보존·운영자 권한 | Operations |
| Unity | 목표 Editor의 공식 배포·호환성 확인, manifest·lock 고정, 빌드 대상 | Editor |
| 부활 | 힐러 시간, 체력·보호 시간·전환 패널티, 다운·사망 세부 전이 | Player, Cooperation |
| 아이템 | 정확한 소지 슬롯 수, 바닥 전달과 공유 가방의 적용 범위 | Items |
| 난이도 | 이용자 선택형 완화의 범위·랭킹 분리 | StartandExit, Puzzles |
| 서사 | 2인 서사와 3~4인 플레이 연결, 스킵 동의 범위, 분기 엔딩, 후반 공개 순서 | Story |
| 지역 | 광산·용암·오염·고향에서 함께 기재된 기믹의 조합과 우선순위 | Levels |
| 협동 도구·증강 | 챕터 학습과 증강 획득 방식의 적용 범위·도입 시점 | Cooperation, Items |
| 화면·아트 | 플레이어 식별 수단 우선순위, 우클릭 안내, 무음 위험 인지 범위 | UI, Art |
| 제작 자원 | 직접 제작과 유료 자원 범위·예산·라이선스 | Art, Audio, ThirdParty |
| 출시 | 실제 공개 챕터 선정, 랭킹·공개 방·채팅 도입 시점, 가격·판매 방식 | Story, Online, Operations |
| 사양·언어 | 지원 OS·입력 장치·최소 사양·성능 예산·최종 출시 언어 | Player, UI, Localization |
| 에셋 제작·조달 | 기본 스타일라이즈드·저사양 우선 원칙은 확정. 세부 제작 방식·조달 조합·예외 항목·최소 사양은 사용자 승인과 프로파일링 후 결정 | [DESIGN-0002](DESIGN-0002-CharacterAssetProduction.md), Art, Audio, UI, Player, Monster, Levels |
| 품질 설정 | 품질 프리셋의 공정성·로컬 적용 원칙은 확정. 실제 최소 사양·렌더 파이프라인·고급 옵션·자동 추천 지표는 검증 후 결정 | [DESIGN-0003](DESIGN-0003-QualitySettings.md), UI, Core, Art, Levels |
| 프론트엔드 표현 결정권 | 결정권 정책은 확정. 캐릭터·몬스터·보스·UI·VFX·SFX·음악·애니메이션·지역 분위기의 세부 스타일 승인 기록이 필요 | [DESIGN-0002](DESIGN-0002-CharacterAssetProduction.md), [DESIGN-0003](DESIGN-0003-QualitySettings.md), UI, Art, Audio |

서로 다른 선택이 함께 남아 있던 아이템 전달·협동 도구·일부 지역 기믹은 위 목록으로 관리한다. 미선택 항목과 AI 제안을 확정 기획으로 승격하지 않는다.

## 확정 이후 적용 원칙

- 이전의 전원 입장 필수, 개인 협동 점수, 몬스터 처치 점수는 적용하지 않는다.
- PC 보호 제품 동봉과 전화번호 기반 통합 제재는 현재 범위에 포함하지 않는다. 서버 검증과 Steam 계정의 검토된 증거를 사용한다.
- 방장 역할은 이전하지만 게임 서버의 판정권은 이전하지 않는다.
- 도착 인원 보너스·4인 공개 랭킹·동점 시 시간 우선은 기존 구체 확정 규칙을 유지한다. 최신 메모에 후보로 다시 등장한 별도 전원 보너스는 채택하지 않는다.
- 빌드 버전 필드명은 기존 GitHub의 gameBuildVersion을 문서 표준으로 사용한다. 별도 migrationVersion은 변환 이력에 둔다.
- game-planning-choice·game-planning-kit·deliverables·tmp는 외부 메모의 작업 경로이며 이 저장소에 존재하는 관리 구조로 표시하지 않는다. 실제 구조는 [폴더 지도](FOLDER_MAP.md)를 따른다.

[통합 기획서](../Plan.md) · [작업 원칙](WORKING_PRINCIPLES.md)
