# Chapter01_Mine — 광산

## 지역의 역할

관리자의 시선을 피해 이동하고 부모의 흔적과 첫 조각 단서를 찾는 지역입니다.

현재 프로토타입은 3D 광산형 테스트 맵과 캡슐 플레이어·동료·몬스터·출구를 포함합니다. Chapter01 전용 색상 팔레트를 적용하며, 출구 집계 후 `PrototypeStoryInterlude_Chapter01`을 거쳐 대기실로 돌아갑니다.

## 현재 프로토타입

- `PrototypeChapter01.unity`는 3D 원근 카메라를 사용합니다.
- `PrototypeSceneBuilder`가 바닥·벽·장애물·위험 요소·시작 지점·출구와 캡슐 배치를 씬 파일에 저장합니다. `PrototypeMapBuilder`는 Play 시 저장된 오브젝트를 재연결하고, 저장되지 않은 구형 씬에서는 보정 생성합니다.
- 플레이어·동료·몬스터는 모두 기능 검증용 캡슐 placeholder입니다.
- 맵은 고정형 테스트 구성입니다. 정식 랜덤 생성, 실제 네트워크 동기화, 최종 아트·보스·서사 콘텐츠는 아직 구현·검증하지 않았습니다.

## 여기에 둘 파일

- 이 챕터의 씬, 스테이지 구성과 지역 전용 프리팹·설정.
- 고정 방, 연결 통로, 장치·몬스터·아이템의 배치 자료.
- 보스 공간과 배치 자료. 보스 행동 코드는 Monster가 담당합니다.

파일이 생기면 필요한 범위에서 `Stages`, `Rooms`, `Corridors`, `Boss`로 나눕니다. 현재 기획의 일반 5스테이지와 별도 보스를 구분해 기록합니다.

## 함께 확인할 폴더

- [MapGeneration](../../../Features/MapGeneration/README.md): 방과 통로의 조합·진행 가능성 검사.
- [Puzzles](../../../Features/Puzzles/README.md): 장치의 공통 작동 규칙.
- [Monster](../../../Features/Monster/README.md): 일반 몬스터와 보스 행동.
- [Story](../../../Features/Story/README.md): 사건·단서·대사·엔딩 조건.
- [StartandExit](../../../Features/StartandExit/README.md): 스테이지 전환·전멸·재시작.
- [Shared](../../Shared/README.md): 여러 지역이 공유하는 콘텐츠.

## 문제가 생겼을 때

챕터·스테이지, 씬 또는 배치 파일, 생성 조건, 정확한 위치와 재현 동선을 기록합니다. 특정 배치에서만 생기는 문제인지 공통 기능의 문제인지 확인하고 담당 폴더를 함께 적습니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeChapter01.unity` | Chapter01 3D 테스트 씬 | `PrototypeSceneBuilder.BuildScenes` 실행 후 플레이 |
| `PrototypeMapBuilder.cs` | 3D 광산 테스트 맵의 저장 배치 재연결 및 누락 시 보정 생성 | Scene 뷰에서 배치를 확인하고 Chapter01에서 시작점·출구·장애물 실행 확인 |
| `PrototypeChapter01.unity` | Chapter01 저장형 3D 테스트 씬 | 대기실에서 Chapter 01 선택 후 출구 집계 확인 |

[전체 폴더 지도](../../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../../Docs/Bugs/BUG_TEMPLATE.md)
