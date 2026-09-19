# 개발 참고자료와 사이트 활용 기준

기준일: 2026-09-19 · 상태: 참고자료 등록 및 우선 후보 조사 완료. 에셋 도입·최종 디자인 승인 아님.

## 사용자 제공 원본

- 자료명: [유용한 게임개발 사이트](https://docs.google.com/document/d/1WuDT_G57F4pICnILEmlQFfbsRwjWqCigbKr6gSSigbQ/edit?tab=t.0)
- 문서 ID: `1WuDT_G57F4pICnILEmlQFfbsRwjWqCigbKr6gSSigbQ`
- 확인 범위: Google Docs 연결로 전체 탭 목록과 본문 확인. 현재 1개 탭(`t.0`, `Tab 1`).
- 원본 최종 수정 시각: 2026-09-19 06:27:50 UTC. 이 목록은 확인 당시의 요약이며 원본 수정 사항을 자동 동기화하지 않는다.
- 사용자 결정: 이 문서를 프로젝트 소스로 등록하고, 관련 작업에서 수록 사이트를 적극 활용한다.

여기서 소스는 **개발 참고자료**다. 문서에 소개된 모든 패키지를 설치하거나 에셋을 게임에 넣었다는 뜻이 아니다. 원본 Google 문서는 수정하지 않았다. 확정 게임 규칙은 [Plan.md](../Plan.md), 사용자 선택권은 [작업 원칙](WORKING_PRINCIPLES.md)을 따른다.

## 작업할 때 사용하는 방법

1. 에셋·UI·음향·애니메이션·제작 도구를 검토할 때 이 목록에서 관련 사이트를 먼저 찾는다. 기술 버전·가격·권리는 선택 시점의 공식 문서로 다시 확인한다.
2. 사이트 이름만 제시하지 않고 실제 에셋·팩·튜토리얼의 링크, 필요한 기능, 장단점, 대략적인 작업량·성능 영향을 연결한다. 적합한 자료가 없으면 그 이유와 대안을 남긴다.
3. 캐릭터·몬스터·보스·UI·VFX·SFX·음악의 최종 모습과 소리는 사용자가 고른다. 후보 조사와 시안 제시는 진행하되, 임의 교체·구매·구독·외부 생성 작업은 하지 않는다.
4. 직접 제작할 고유 슬라임·핵심 퍼즐·서사 랜드마크는 참고 자료를 분석해 제작한다. 범용 팩이나 AI 생성 결과를 사용자 요구의 대체품으로 확정하지 않는다.
5. 도입할 파일은 상업 이용, 수정, 게임 배포, 원본 재배포, 출처 표시, 팀 사용 범위를 확인한다. 게임에 포함할 권한과 공개 GitHub에 원본을 올릴 권한은 구분한다.
6. 도입 전에는 정확한 패키지·목적 경로·변경 범위를 확인하고 승인을 받는다. 도입 후에는 실제 원본·버전·라이선스·수정 내용·적용 파일·Unity 검증 결과를 [ThirdParty](../Assets/ThirdParty/README.md)와 담당 기능 README에 연결한다. 알려지지 않은 라이선스를 임의 허용하지 않는다.
7. 생성형 도구에는 제공 권한이 있는 입력만 사용한다. 추출한 타 게임 데이터, 비공개 프로젝트 파일, 인물 영상·음성을 외부에 자동 업로드하지 않는다.

## 분야별 자료 지도

아래는 원문에 등장한 사이트·도구를 묶은 목록이다. 이름만 등록한 항목은 공식 URL·현재 기능·가격·개별 라이선스를 아직 검증하지 않았으며, 도입 승인을 뜻하지 않는다. 중복 소개는 한 번만 묶었다.

| 분야 | 등록 자료 | 우리 프로젝트에서 검토할 용도 | 담당 |
|---|---|---|---|
| 모션·리깅 | ActorCore, DeepMotion, Mixamo(원문 비교 대상), Quaternius | 걷기·달리기·점프 등 기본 모션 참고와 리타게팅. 비인간형 슬라임의 파츠·리그 적합성 별도 검증 | Player, Cooperation, Art |
| 3D·텍스처 생성 보조 | Meshy(원문 비교 대상), Krea.ai, CSM.ai, Tripo AI(원문: 트리포 ai) | 승인된 원본을 이용한 비교 시안·제작 보조. 최종 Blender 원본의 토폴로지·UV·파츠 기준을 대체하지 않음 | Art, Monster, Levels |
| 모델·환경 라이브러리 | Sketchfab, Poly Pizza, [Kenney](https://kenney.nl/assets), [Quaternius](https://quaternius.com/), [KayKit](https://kaylousberg.itch.io/kaykit-dungeon-pack), Synty Studios, BlenderKit(원문: blender kit) | 반복 소품·환경 모듈 후보. 낮은 폴리곤 수를 낮은 완성도로 단정하지 않고 스타일·실루엣·성능을 함께 비교 | Art, Levels, MapGeneration |
| 재질·환경광 | [Poly Haven](https://polyhaven.com/license), [ambientCG](https://docs.ambientcg.com/license/) | 암석·금속·목재 재질과 조명 참고. 실사 재질을 그대로 쓰기보다 판타지 색감과 1K~2K 예산에 맞게 검토 | Art, Levels |
| 효과음 | [Freesound](https://freesound.org/help/faq/), [Pixabay](https://pixabay.com/service/license-summary/), [Zapsplat](https://www.zapsplat.com/license-type/standard-license/), [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | 환경음·발소리·UI 확인음 비교. 위험·구조·출구 신호는 역할이 혼동되지 않는지 청취 검수 | Audio, UI |
| 배경음악 | [Incompetech](https://incompetech.com/music/royalty-free/licenses/), Pixabay Music, Suno AI, Udio | 챕터 분위기 후보와 제작 방식 비교. 생성형 음악은 현재 상업 이용·출력·배포 조건을 먼저 확인 | Audio, Story |
| UI 구조 참고 | [Game UI Database](https://www.gameuidatabase.com/) | 로비·설정·HUD의 정보 배치와 가독성 비교. 다른 게임의 화면·이미지를 게임 에셋으로 복제하지 않음 | UI |
| 아이콘·폰트 | [Game-icons.net](https://game-icons.net/about.html), Flaticon, [Google Fonts](https://fonts.google.com/), [눈느](https://noonnu.cc/) | 핑·상호작용·설정 아이콘 후보, 한국어 글리프·폰트 포함 배포 검수 | UI, Localization |
| 카메라·화면·VFX | DOTween, Cinemachine, URP 후처리, Shader Graph, VFX Graph, Gabriel Aguiar Prod | 표시용 보간·컷신·가벼운 경고 효과 제작 참고. 서버 충돌·타이머·판정과 분리 | UI, Story, Art |
| 모델링·레벨 제작 | Blender, ProBuilder | 파츠 분리·UV·리그 제작, 방·통로 크기와 동선의 그레이박스 검증 | Art, Levels, MapGeneration |
| 오디오 편집 | Audacity | 길이·피크·루프·페이드 정리와 음원 비교 | Audio |
| 2D 제작 | PixelOver(원문: Pixelover.io), Aseprite | 필요한 2D 시안·스프라이트 제작 보조 후보. 게임의 3D/스타일라이즈드 방향을 픽셀풍으로 변경하지 않음 | UI, Art |
| 밸런스 모델링 | [Machinations](https://machinations.io/) | 아이템 공급·구조 자원·협동 정산의 흐름 비교. 미정 배점을 도구 예제로 확정하지 않음 | Items, Cooperation, StartandExit |
| 협업·버전 관리 | GitHub, GitHub Desktop, Unity Version Control | 현재 GitHub 작업 흐름의 생산성 자료. 자동 백업·커밋·푸시나 저장소 이전 권한이 추가되는 것은 아님 | Editor, Maintenance |
| 온라인 비교 자료 | Photon PUN 2, Unity Gaming Services, Steamworks Lobby / Steam Networking, FishNet, Mirror | 비용·접속 방식 비교 시 참고. 현재 PlayFab 운영자 서버 결정과 로컬 Netcode 시험 구성을 임의 교체하지 않음 | Online |
| 정확한 서비스 확인 대기 | 힉스필드, 바르코, 스프라이트쿡, 픽셀랩 | 원문에 공식 URL·버전이 없어 명칭만 보존. 실제 사용할 때 서비스 식별부터 확인 | Art, Story, UI |

## 이번에 공식 페이지까지 확인한 우선 후보

후보 선정은 기술 검토이며 시각·청각 디자인 승인이 아니다. 다운로드·설치·구매는 수행하지 않았다.

| 자료 | 확인한 내용 | 제안 활용과 확인할 문제 |
|---|---|---|
| [KayKit Dungeon Pack](https://kaylousberg.itch.io/kaykit-dungeon-pack) | 배포 페이지에 스타일라이즈드 던전 모듈·소품과 CC0 표기 | Chapter01_Mine의 반복 소품·방 모듈 비교. 광산 테마 적합성, 실제 크기·URP 재질·충돌·LOD는 아직 미검증 |
| [Quaternius Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html) | humanoid 기반 모션·Unity용 구성·CC0 안내. 무료/유료 구성 차이 있음 | 기본 이동 모션 비교. 슬라임 전용 리그에 자동 호환된다고 가정하지 않으며 손·발·몸통 분리와 벽 타기·구조 동작은 별도 제작 검토 |
| [Kenney Interface Sounds](https://kenney.nl/assets/interface-sounds) | UI용 오디오 팩과 CC0 표기 | 로비 버튼·준비 확인음의 청취 후보. 실제 파일 청취·볼륨·중복 재생·사용자 선호 확인 전 미도입 |
| [Game-icons 공식 안내](https://game-icons.net/about.html) | 기본 안내는 CC BY 3.0과 저작자 표시 요구. 전부 CC0가 아님 | 핑·구조·상호작용 아이콘 비교. 실제 선택한 아이콘의 작가·조건·출처 표시를 기록하고 최종 스타일은 사용자 승인 |
| [Poly Haven 라이선스](https://polyhaven.com/license) / [ambientCG 라이선스](https://docs.ambientcg.com/license/) | 두 공급자의 에셋 CC0 안내 확인 | 광산·용암·빙설 재질 제작 참고. 사이트의 모든 글·브랜드·미디어까지 에셋 권리와 같다고 취급하지 않음 |

Game UI Database는 검색 도구의 robots 접근 제한으로 이번에는 화면 본문을 확인하지 못했다. 목록에는 남기되 실제 화면을 검토한 것으로 표시하지 않는다. UI 작업 시 접근 가능한 원본 화면을 먼저 확인한다.

## 원문 설명 중 그대로 적용하지 않는 부분

- Game-icons 전체를 CC0로 기록하지 않는다. [공식 안내](https://game-icons.net/about.html)를 기준으로 선택 파일의 라이선스와 표시 의무를 확인한다.
- Pixabay는 CC0가 아닌 [자체 Content License](https://pixabay.com/service/license-summary/)를 사용한다. 단독 원본 배포 등의 제한이 있으므로 무료 다운로드와 공개 저장소 재배포를 혼동하지 않는다.
- Freesound는 파일마다 CC0·CC BY·CC BY-NC 등이 다르다. [공식 FAQ](https://freesound.org/help/faq/)를 확인하고 상업용 게임 후보에서 비상업용 전용 자료를 자동 채택하지 않는다.
- Zapsplat은 [계정 유형별 조건](https://www.zapsplat.com/license-type/standard-license/)과 출처 표시·단독 재배포·자동 다운로드 제한을 확인해야 한다. Incompetech도 [무료 Creative Commons와 별도 유료 라이선스](https://incompetech.com/music/royalty-free/licenses/)를 구분한다.
- 원문의 “현재 Photon PUN 2 사용 중”은 이 프로젝트 현황이 아니다. 현재 [manifest](../Packages/manifest.json)는 Netcode for GameObjects 2.13.2와 URP 17.6.0을 사용한다. 최종 서비스 방향은 PlayFab 운영자 전용 서버이며, 로컬 패키지 검증과 정식 SDK 확정은 구분한다. 무료 동접·트래픽·요금 수치는 채택 시점에 다시 확인한다.
- 현재 URP에는 통합 후처리가 있다. 구형 Post Processing Stack v2를 추가하는 방식으로 일반화하지 않는다. [Unity 6000.6 공식 안내](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/integration-with-post-processing.html)를 기준으로 기존 렌더 파이프라인에 맞춰 검토한다.
- Cinemachine의 다인 타깃 줌 예시는 1인칭 협동 시점 변경 승인이 아니다. 카메라·후처리·VFX는 멀미·저사양·위험 신호 가독성과 사용자 설정을 우선한다.
- “무료만으로 AAA”, “모든 개발자가 사용”, “이 도구만으로 완성” 등의 표현은 검증 결과로 가져오지 않는다. 유료 필요 여부는 품질·제작 시간·호환성·권리를 비교해 사용자에게 제안한다.

## 실제 적용 기록

이번 적용은 원본 확인, 자료 지도 작성, 우선 후보와 라이선스 설명 교정, 작업 지침 연결까지다. 새 에셋 파일·외부 프로그램·패키지 설치, 생성형 서비스 업로드, 원본 Google Docs 수정, GitHub·Notion 게시는 없다.

이후 채택할 때는 자료명·개별 URL·버전·라이선스 확인일·사용자 승인·원본 경로·수정 파일·성능/런타임 검증을 [유지보수 작업](Maintenance/README.md)에 기록한다. 사이트 참고, 파일 도입, 엔진 임포트 성공, 플레이 검증, 최종 디자인 승인은 서로 다른 상태다.

[기획서](../Plan.md) · [에셋 제작 기준](DESIGN-0002-CharacterAssetProduction.md) · [작업 원칙](WORKING_PRINCIPLES.md) · [등록 작업 기록](Maintenance/TASK-0006-ReferenceSources.md)
