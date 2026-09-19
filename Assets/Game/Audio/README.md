# Audio — 음악과 효과음

## 다음 작업자용 빠른 안내

- 쉬운 이름: 효과음과 소리.
- 수정 시작점: [PrototypeCues.cs](PrototypeCues.cs).
- 현재 담당: 프로토타입 기능 알림음·핑 표현.
- 이 폴더만으로 처리하지 않는 범위: 위험 판정·최종 음원 선정.
- 함께 확인: UI, Player, Monster.
- 지시 예시: “핑 효과음 크기 조절 → Audio → PrototypeCues.cs → UI, Player, Monster와 영향 확인”.
- 아래 상세 기획은 목표 책임, 날짜가 있는 구현·검사 기록은 해당 시점의 이력이다. 코드 존재와 정상 동작·서비스 연결 완료를 구분한다.

지역별 음악, 장치·적의 예고음, 슬라임 소리와 오디오 설정을 관리합니다. 지역별 사용처와 원본·이용 조건을 기록합니다. 재생 시점의 게임 규칙은 해당 기능이 담당합니다.

## 관련 위치

- [Levels — 지역별 사용처](../Levels/README.md)
- [Monster — 적의 상태](../Features/Monster/README.md)
- [Puzzles — 장치 작동](../Features/Puzzles/README.md)

## 파일과 작업 안내

현재 수정 시작점은 위 빠른 안내를 따른다. 최종 자원과 실제 실행 결과는 별도로 기록한다.

| 실제 파일 | 역할 | 확인 방법 |
|---|---|---|

[전체 폴더 지도](../../../Docs/FOLDER_MAP.md) · [작업 기록 양식](../../../Docs/Maintenance/TASK_TEMPLATE.md)
