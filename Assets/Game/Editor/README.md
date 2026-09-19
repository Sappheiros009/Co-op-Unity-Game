# Editor — 제작 보조 도구

Unity 에디터에서 사용하는 제작·검사 도구를 둡니다. `SlimeCoop.Prototype.Editor` 어셈블리는 Unity Editor에서만 컴파일되며 런타임 어셈블리와 분리됩니다. 플레이 중 동작하는 게임 코드는 해당 기능에 둡니다. 도구가 추가되면 실행 위치, 입력, 바뀌는 파일과 결과 확인법을 기록합니다.

## 관련 위치

- [MapGeneration — 맵 규칙](../Features/MapGeneration/README.md)
- [Levels — 검사 대상 콘텐츠](../Levels/README.md)

## 현재 프로토타입 도구

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `PrototypeSceneBuilder.cs` | 로비·대기실·Chapter01~07·스토리 컷신 placeholder 총 16개 씬에 오브젝트·카메라·조명·placeholder 에셋을 저장하고 Build Profile 등록 | Unity Editor에서 `SlimeCoop.Prototype.Editor.PrototypeSceneBuilder.BuildScenes` 실행 후 Scene 뷰에서 확인 |
| `PrototypeBuild.cs` | 등록된 16개 씬을 Windows 실행 파일로 빌드 | Unity Editor에서 `SlimeCoop.Prototype.Editor.PrototypeBuild.BuildWindows` 실행 |

도구 실행 성공은 씬 생성·컴파일·빌드 결과만 의미하며, 실제 멀티플레이·서버 운영·출시 검증을 대신하지 않습니다.

씬 생성은 Editor API로 수행합니다. 생성 후에는 `PrototypeLobby.unity`, `PrototypeWaitingRoom.unity`, `PrototypeChapter01.unity`를 열어 저장된 카메라·조명·기본 지오메트리·캡슐 배치를 직접 확인합니다. Play 모드에서는 저장된 오브젝트를 중복 생성하지 않고 런타임 상태만 재연결합니다.

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
