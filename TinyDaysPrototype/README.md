# Tiny Days Prototype

Unity 2022.3.20f1 · Blender 5.2.1 · Windows · 6단계 실행·저장·안정성 진행 중

작업의 장기 기준과 단계 현황은 [마스터 플랜](Docs/MasterPlan.md)에서 확인한다.

## 실행

1. Unity Hub → Projects → Add → Add project from disk → 이 `TinyDaysPrototype` 폴더 선택.
2. 2022.3.20f1로 열고 가져오기가 끝날 때까지 기다린다.
3. Project 창 → Assets → Scenes → **TinyDays** 더블 클릭.
4. 상단 ▶ Play 버튼 → Game 창을 클릭한다. 토끼 주민 6명이 밭 일, 수확 상자 운반, 건설, 벤치 휴식, 풍경 감상을 선택하며 움직인다. 창고가 수확 상자로 30초 이상 막히면 주민이 동쪽 후보지에 작은 목조 헛간을 짓고 식량 저장 용량이 24에서 36으로 늘어난다.
5. WASD 또는 방향키: 이동 / 마우스 휠: 확대·축소 / Home: 기본 구도 / 1: 낮 / 2: 일몰 / 3: 밤 / F: 주민 판단 진단 / Tab: 하단 정보 숨김 / Space: 일시정지·재개 / [: 절반 / ]: 2배 / Esc: 설정·저장 메뉴. 하단 바의 `−`·`+` 버튼으로 1×~16×를 조절하며 현재 속도가 표시된다. Esc 메뉴에서 로컬 저장·불러오기와 환경음 볼륨을 설정한다. 시간은 180초 주기로 계속 진행한다.

`F` 진단은 기본적으로 숨겨져 있으며, 주민별 현재 행동·선택 이유·피로·예약 장소를 표시한다. 실제 오디오와 발 애니메이션은 아직 넣지 않았다.

`Tools/OpenUnity.ps1`로 프로젝트를 열 수도 있다. PowerShell에서 스크립트 실행이 차단되면 Unity Hub 방법을 사용한다.

## 모델·자동화

- Blender → File → Open → `ArtSource/TinyDays_ArtLibrary.blend`: 제작 모델 원본 라이브러리.
- Unity → Tiny Days → Rebuild generated village: 생성 영역 재구성. **ManualEdits 아래에 수동 배치를 보관한다.**
- Unity를 닫고 PowerShell에서 `./Tools/Rebuild.ps1 -Verify`: Blender 재생성, Unity 구성, Play Mode 자동 검증 및 비교 캡처.
- 로그: `Logs/`; 검증 결과: `Docs/Verification.txt`; 비교 화면: `Docs/Captures/`. `Motion_08.png`, `Motion_34.png`, `Motion_60.png`은 이동 풍경의 시간대별 캡처다.

단계 상태는 `Docs/PrototypePlan.md`, 결정은 `Docs/Decisions.md`, 실제 검증 및 다음 작업은 `Docs/Progress.md`에서 확인한다.

## 성향별 이동과 검증

주민은 흙길을 우선하며 빈 잔디도 이용해 장애물을 피한다. 느긋함은 양보 대기, 부지런함은 진행 가능한 우회, 길 선호도는 흙길과 잔디의 선택에 영향을 준다. F키에서 성향과 이동 이유·재탐색 횟수를 확인한다.

모델 변경 없이 Unity만 재생성·검증하려면 Unity를 닫고 `./Tools/Rebuild.ps1 -Verify -SkipArt`를 실행한다. 전체 생성은 기존 명령을 그대로 사용한다. 상세 설정 및 한계는 `Docs/Navigation.md`, 시나리오 결과는 `Docs/NavigationScenarios.txt`에 있다.
