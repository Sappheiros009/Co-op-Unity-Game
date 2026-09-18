# Lobby — 슬라임 휴식공간

친구와 준비하고 조작·협동을 연습하는 공간을 둡니다. 초대·접속은 Online, 준비 화면은 UI, 출발 조건은 StartandExit가 담당합니다.

## 현재 프로토타입

- `PrototypeLobby`는 2D 표현입니다. 직교 카메라와 `SpriteRenderer`로 배경·패널·장식을 만들고, 현재 조작 가능한 메뉴 오버레이를 표시합니다.
- `PrototypeWaitingRoom`은 3D 표현입니다. 원근 카메라, 바닥·벽, 캡슐 슬라임과 챕터 시작 조작을 포함합니다.
- 두 씬의 장식과 캡슐은 기능 확인용 placeholder이며, 최종 프론트엔드 스타일과 에셋은 사용자 결정 후 교체합니다.

## 관련 위치

- [Online — 접속](../../Features/Online/README.md)
- [UI — 로비 화면](../../Features/UI/README.md)
- [StartandExit — 출발](../../Features/StartandExit/README.md)
- [Cooperation — 협동 연습](../../Features/Cooperation/README.md)

## 실제 파일과 확인 방법

| Unity 프로젝트 경로 | 역할 | 확인 방법 |
|---|---|---|
| `Assets/Game/Levels/Lobby/PrototypeLobby.unity` | 2D 로비 씬. 메뉴에서 슬라임 대기실로 이동 | `PrototypeSceneBuilder.BuildScenes` 실행 후 씬 목록과 실행 화면 확인 |
| `Assets/Game/Levels/Lobby/PrototypeWaitingRoom.unity` | 3D 슬라임 대기실. 챕터 1 시작 | 같은 빌드 순서로 씬 전환 및 3D 배치 확인 |

씬 생성 도구가 카메라·조명·배치·placeholder 표현을 씬 파일에 저장하므로 Unity Editor의 Scene 뷰에서 Play 없이 확인할 수 있습니다. 각 컨트롤러의 런타임 초기화는 저장된 표현을 먼저 재사용하고, 구형·누락 씬에만 보정 생성합니다. 최종 제작 에셋을 넣을 때는 씬·프리팹·UI 구조를 다시 검토합니다.

[전체 폴더 지도](../../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../../Docs/Maintenance/TASK_TEMPLATE.md)
