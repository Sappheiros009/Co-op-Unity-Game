# Episode01 — 에피소드 1

첫 에피소드의 일곱 챕터를 관리합니다. 공통 코드를 챕터마다 복사하지 않고 Features를 사용하며, 챕터별 차이는 배치와 설정으로 기록합니다. 출시할 챕터와 제작 순서는 기획 결정에 따라 별도로 기록합니다.

## 관련 위치

- [Chapter01_Mine — 광산](Chapter01_Mine/README.md)
- [Chapter02_LAVA — 용암 지대](Chapter02_LAVA/README.md)
- [Chapter03_PollutedZone — 오염 지대](Chapter03_PollutedZone/README.md)
- [Chapter04_ThunderSea — 천둥치는 바다](Chapter04_ThunderSea/README.md)
- [Chapter05_Square — 광장](Chapter05_Square/README.md)
- [Chapter06_FrozenMountain — 얼어붙은 땅](Chapter06_FrozenMountain/README.md)
- [Chapter07_Hometown — 슬라임 고향](Chapter07_Hometown/README.md)
- [StoryInterludes — 챕터 사이 영상씬](StoryInterludes/README.md)

## 현재 프로토타입 흐름

대기실에서 원하는 챕터를 선택하면 해당 3D 챕터로 이동합니다. 출구 집계가 끝나면 해당 챕터의 `StoryInterlude` placeholder를 거친 뒤 `PrototypeWaitingRoom`으로 돌아옵니다. 모든 챕터는 공통 테스트 경로를 사용하고 색상 팔레트만 구분합니다.

## 실제 파일과 확인 방법

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `Chapter01_Mine/PrototypeChapter01.unity` | 광산 3D 테스트 맵, 보라·청색 계열 palette | 대기실에서 Chapter 01 선택 |
| `Chapter02_LAVA/PrototypeChapter02.unity` | 용암 지대 3D 테스트 맵, 적·주황 계열 palette | 대기실에서 Chapter 02 선택 |
| `Chapter03_PollutedZone/PrototypeChapter03.unity` | 오염 지대 3D 테스트 맵, 녹색 계열 palette | 대기실에서 Chapter 03 선택 |
| `Chapter04_ThunderSea/PrototypeChapter04.unity` | 천둥 바다 3D 테스트 맵, 남색·청색 계열 palette | 대기실에서 Chapter 04 선택 |
| `Chapter05_Square/PrototypeChapter05.unity` | 광장 3D 테스트 맵, 석재·금색 계열 palette | 대기실에서 Chapter 05 선택 |
| `Chapter06_FrozenMountain/PrototypeChapter06.unity` | 얼어붙은 산 3D 테스트 맵, 빙청색 계열 palette | 대기실에서 Chapter 06 선택 |
| `Chapter07_Hometown/PrototypeChapter07.unity` | 슬라임 고향 3D 테스트 맵, 초록·자주 계열 palette | 대기실에서 Chapter 07 선택 |
| `StoryInterludes/PrototypeStoryInterlude_Chapter0X.unity` | 챕터별 영상 삽입용 2D placeholder | 챕터 종료 후 Continue 조작 |

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)
