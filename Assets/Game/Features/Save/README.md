# Save — 저장과 복구

## 담당 범위

PlayFab과 연결해 계정 데이터·런 데이터·경쟁 데이터를 분리해 저장한다. 영구 챕터 해금·업적·이야기 진행·설정은 계정에 보존한다. 전멸 시 런 점수와 임시 진행을 초기화하는 규칙은 StartandExit가 담당한다.

스테이지·챕터 완료 시 서버가 승인한 결과를 저장한다. 저장 재시도는 같은 정산 ID로 중복 반영을 막는다. 저장 실패를 성공으로 표시하지 않는다.

## 팀 기록과 버전

협동 기록은 팀 총점만 관리한다. 공개 랭킹은 기록 시작 4인 기준이며 일반 이탈·사망·일부 미탈출 자체로 클리어 팀 기록을 무효화하지 않는다. 개인 이야기 진행과 설정은 개인 점수와 다른 데이터다.

schemaVersion·contentVersion·scoreRuleVersion·gameBuildVersion 및 migrationVersion 변환 이력을 관리한다. 시험 데이터와 복구 지점을 통해 마이그레이션 실패·롤백을 검증한다. 데모·정식 및 다른 점수 규칙의 순위표는 분리한다.

[시스템 구조](../../../../Docs/ARCHITECTURE.md)의 데이터 경계를 따른다. 저장 기간·복원·시즌·보안상 기록 보류의 세부 정책은 [상태 문서](../../../../Docs/PROJECT_STATE.md)에 있다. 현재는 아래 로컬 시험 저장만 구현했으며 PlayFab 저장·공개 랭킹은 미연결이다.

## 함께 확인할 폴더

- [StartandExit](../StartandExit/README.md): 완료·종료 시점과 영구 기록 변경을 받습니다.
- [Story](../Story/README.md): 개인 이야기 진행을 받습니다.
- [Items](../Items/README.md): 저장 대상으로 확정된 아이템 기록을 받습니다.
- [Online](../Online/README.md): 참가자별 저장 소유권과 버전 조건을 연결합니다.
- [UI](../UI/README.md): 저장 상태와 사용자 설정을 표시합니다.

## 문제가 생겼을 때

게임·저장 형식 버전, 저장 소유자, 저장 시점, 불러오기 결과와 복구 결과를 확인합니다. 해금 기록과 회차 데이터를 각각 확인합니다.

작업 지시 예시: “Save에서 재접속 후 해금 챕터가 사라지는 조건을 확인하고 저장 전후 값과 재시작 결과를 남겨주세요.”

## 구현 파일 안내

기획상의 서비스 책임과 로컬 시제품을 구분합니다. 사용자 진행 파일을 지우거나 새 데이터로 초기화하는 자동 복구는 하지 않습니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| [PrototypeSave.cs](PrototypeSave.cs) | 해금·업적·로컬 팀 기록, 원본 보호, 원자적 교체, 미저장 상태·재시도 | [저장 회귀](../../Tests/PrototypePersistenceTests.cs) |
| [PrototypeChapterCompletion.cs](PrototypeChapterCompletion.cs) | 표시용 세션값과 분리된 서버/로컬 완료 데이터 | [결과 저장 검사](../../Tests/PrototypeNetworkProgressTests.cs) |
| [PrototypeLocalProfile.cs](PrototypeLocalProfile.cs) | 같은 PC 여러 창의 이름별 시험 저장·배타 쓰기 잠금·미저장 중 이름 변경 방지 | [로비/프로필 검사](../../Tests/PrototypeLocalLobbyTests.cs) |
| [PrototypeSaveNotice.cs](../UI/PrototypeSaveNotice.cs) | 각 화면의 미저장 안내, 설정창 재시도와 상태 연동 | [설정 UI 회귀](../../Tests/PrototypeSettingsUiTests.cs) |

기능 전용 파일은 이 폴더 안에 둡니다. 파일이 늘어나면 필요한 범위에서 `Scripts`, `Prefabs`, `Data`로 나눕니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../Docs/Bugs/BUG_TEMPLATE.md) · [유지보수 작업 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)

## 로컬 프로토타입 구현 (2026-09-19)

`PrototypeSave.cs`: 로컬 진행·해금·업적·4인 시작 팀 기록·버전·중복 저장·손상 원본 보존. 최초 쓰기 전에 기존 파일을 검사한다. 손상·미래 버전은 덮어쓰기를 차단하고, 일시적인 쓰기 실패는 메모리의 진행을 보존해 재시도한다. 진행 파일과 설정 파일의 오류는 독립적이다. 실제 클라우드·공개 랭킹은 미연결.

실행·미구현 경계: [프로토타입 안내](../../../../Docs/PROTOTYPE_GUIDE.md). 새 검증: [TEST-0003](../../../../Docs/Testing/TEST-0003-FullPrototype.md).

서버 완료 결과와 발견한 기억을 각 클라이언트의 로컬 저장으로 연결한다. [NetworkProgress](../Online/PrototypeNetworkProgress.cs)는 서버 런/점수/명단과 일치한 결과만 반영하고 반복 수신에 중복 저장하지 않는다. 실패는 기존 미저장 상태로 표시한다. 실제 구현·시험 배치 전환 검증과 클라우드 미연결 경계는 [TEST-0009](../../../../Docs/Testing/TEST-0009-NetworkCompletion.md)에 기록한다.

일반 로비에서 여러 창을 연결할 때는 서로 다른 이름을 사용한다. `LocalPlayers/이름 해시`의 파일 잠금으로 같은 시험 프로필의 동시 쓰기를 막고, 로비 복귀 후에도 선택한 저장을 유지한다. 이름은 계정 인증이 아니며 기존 일반 저장을 삭제·이동하지 않는다. 자세한 경로·재검사는 [TEST-0010](../../../../Docs/Testing/TEST-0010-LocalLobbyEntry.md)을 따른다.
