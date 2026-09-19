# Levels — 지역과 스테이지

## 다음 작업자용 빠른 안내

- 쉬운 이름: 지역과 씬.
- 수정 시작점: [README.md](README.md).
- 현재 담당: 로비·챕터·스토리 씬과 지역 자원.
- 이 폴더만으로 처리하지 않는 범위: 현재 런타임 배치는 MapGeneration/PrototypeMapBuilder.cs; 폴더 존재가 완성 증거는 아님.
- 함께 확인: MapGeneration, Editor, Story.
- 지시 예시: “광산 배치 수정 → Levels → README.md → MapGeneration, Editor, Story와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

로비·도입부·챕터의 실제 콘텐츠를 관리합니다. 공통 생성 규칙은 MapGeneration, 게임 기능은 Features에서 관리합니다. 폴더가 준비된 것과 챕터 제작 완료·출시 포함 여부는 구분합니다.

## 관련 위치

- [Shared — 공통 구성물](Shared/README.md)
- [Lobby — 휴식공간](Lobby/README.md)
- [Prologue — 도입부](Prologue/README.md)
- [Episode01 — 첫 에피소드](Episode01/README.md)
- [Sandbox — 시험 공간](Sandbox/README.md)

## 현재 프로토타입 파일

| Unity 프로젝트 경로 | 역할 | 확인 방법 |
|---|---|---|
| `Assets/Game/Levels/Lobby/PrototypeLobby.unity` | 저장된 카메라·SpriteRenderer 기반 2D 로비 | `PrototypeSceneBuilder.BuildScenes` 실행 후 Scene 뷰와 플레이 확인 |
| `Assets/Game/Levels/Lobby/PrototypeWaitingRoom.unity` | 저장된 카메라·조명·기하·캡슐 기반 3D 슬라임 대기실 | Scene 뷰 또는 로비에서 대기실로 전환 |
| `Assets/Game/Levels/Episode01/Chapter01_Mine/PrototypeChapter01.unity` | 저장된 카메라·조명·기하·캡슐 기반 3D Chapter01 광산 테스트 맵 | Scene 뷰 또는 대기실에서 Chapter01 시작 |

씬·배치는 현재 프로토타입 범위에서 씬 파일에 저장되어 Editor의 Scene 뷰에서 Play 없이 확인할 수 있습니다. Play 시에는 저장된 배치를 재연결하며, 정식 지역 제작물과 출시 챕터 포함 여부는 별도 결정합니다.

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
