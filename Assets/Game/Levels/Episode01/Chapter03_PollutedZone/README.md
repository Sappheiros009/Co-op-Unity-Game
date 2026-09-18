# Chapter03_PollutedZone — 오염 지대

## 지역의 역할

정화 장치의 분담 조작과 오염 수위 변화, 안전 게이지와 지속 피해를 다루는 지역입니다.

현재 프로토타입은 공통 3D 테스트 경로에 오염 지대의 녹색 계열 색상 팔레트만 적용합니다. 챕터 종료 후 `PrototypeStoryInterlude_Chapter03`를 거쳐 대기실로 돌아갑니다.

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
| `PrototypeChapter03.unity` | 저장형 오염 지대 3D 테스트 씬 | 대기실에서 Chapter 03 선택 |
| `PrototypeStoryInterlude_Chapter03.unity` | Chapter03 종료 후 영상 삽입용 placeholder | 챕터 종료 후 자동 이동 |

[전체 폴더 지도](../../../../../Docs/FOLDER_MAP.md) · [버그 기록 양식](../../../../../Docs/Bugs/BUG_TEMPLATE.md)
