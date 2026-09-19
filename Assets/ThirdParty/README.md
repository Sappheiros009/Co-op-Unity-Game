# ThirdParty — 외부 에셋과 도구

외부에서 가져온 코드·에셋·도구를 관리합니다. 이름·버전·출처·이용 조건·수정 여부를 기록합니다. Unity 패키지나 공급자가 설치 위치를 요구하면 그 위치를 유지하고 이 문서에서 연결합니다.

## 관련 위치

- [Maintenance — 도구·의존성 변경 기록](../../Docs/Maintenance/README.md)
- [개발 참고자료 — 사용자 제공 사이트와 후보 검수](../../Docs/REFERENCE_SOURCES.md)

## 파일과 작업 안내

현재 프로젝트가 사용하는 외부 포함물을 아래에 기록합니다. 아래 설치된 Unity 기본 리소스와, 참고자료에 등록만 한 신규 에셋 후보를 구분합니다. 게시 전 2026-09-20에 로컬 패키지 고지·폰트 고지 및 공식 조건을 대조했습니다. [동봉 고지](NOTICES.md)는 프로젝트 전체의 라이선스를 새로 부여하는 문서가 아닙니다.

2026-09-19 개발 사이트 목록과 우선 후보를 참고자료에 등록했습니다. 해당 목록의 새 에셋을 다운로드·설치한 것은 아니며, 도입 시 개별 파일의 라이선스·원본 재배포 권한·사용자 승인·적용 위치·Unity 검증 결과를 기록합니다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|
| `Assets/TextMesh Pro/Shaders`, `Resources` | 설치된 `com.unity.ugui` 2.6.0의 TMP Essential Resources 기반 텍스트 렌더링 | 원본 패키지 `LICENSE.md`의 Unity Companion 고지를 [NOTICES](NOTICES.md)에 보존. 별도 독립 에셋으로 판매하지 않음 |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`, `Resources/Fonts & Materials` | TMP 기본 폰트·SDF 리소스 | `Fonts/LiberationSans - OFL.txt`의 저작권·SIL OFL 1.1 전문을 원래 위치에 함께 게시 |
| `Packages/manifest.json`, `Packages/packages-lock.json` | Unity·URP·Netcode·Transport 등의 의존성/버전 | 패키지 원본은 `Library/PackageCache`에서 복사하지 않고 Unity 패키지 관리자로 복원. 각 공급자 라이선스 유지 |

새 유료 에셋·제3자 게임 모델·텍스처·음원은 이번 게시에 추가하지 않습니다. 한글 OS 폰트는 실행 환경에서 읽으며 Windows 폰트 파일을 저장소에 동봉하지 않습니다. PEAK 추출 폴더는 참고 경로이며 게시 대상이 아닙니다.

[전체 폴더 지도](../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../Docs/Maintenance/TASK_TEMPLATE.md)
