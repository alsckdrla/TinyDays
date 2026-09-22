# 2-7-3 종합 관찰

2026-09-17 / 검증 진행 중

## 이번 확인

- 사용자 승인: v0.52 시계·Day 표시를 확인하고 2-7-3 진행을 요청했다. 캐릭터 보완은 계속 보류한다.
- 자동 검사: `Logs/stage273-regression.log`에서 조명·시계·배속 검사 모두 통과. 이전 캡처 파일 잠금은 재발하지 않았다. 농가 생성기 검증의 1m 줌 하한·패닝 계산·내부/지하 반투명·복원·수동 영역 보존도 포함한다.
- 그림자: `Logs/stage273-shadows.log`의 세 프리셋·설정 복원 검사 통과.
- 실제 Windows 800×600: 시간 패널·시계·Day가 잘리지 않는다. 메뉴 숨김 중 시계 유지, Esc 설정 열기/닫기, 낮음·높음·기본 그림자 선택과 실제 외곽 변화 확인. 기본으로 복원했다.
- 실제 최대화: 시계 중앙 정렬과 메뉴 숨김 유지 확인. 창 복원 직후 캡처 한 장이 일시적으로 확대되어 보였으나 다음 캡처에서는 정상 복원됐다.
- 실제 주민 목록에서 주민 2 포커싱, 앞 나무 반투명, 이동 중 번호 유지, Home으로 번호·포커싱 해제와 전체 구도 복귀를 확인했다. 줌·패닝 입력을 보냈지만 정확한 1m 도달과 구도 오프셋 보존은 이번 캡처만으로 확정하지 않는다.
- 자동 순환에서 23:38 Day 1 → 00:11 Day 2 확인. 밤의 길·주민·창문 불빛과 네 고정 비교 구도를 확인했다.
- 5분 이상 관찰: 기본 5분·1배속을 유지한 동일 실행에서 주민 경과 12초부터 337초까지 중간 화면을 확인했다. 12:56 Day 1 → 일몰 → 00:11 Day 2 → 06:16 → 12:24 → 14:58까지 진행했다. 정지·시간 직접 선택 없이 주민과 조명이 진행했으며 관찰 지점에서 정체·종료·Day 잘림은 없었다. 간헐적 화면 캡처 관찰이므로 순간적인 튐까지 없다고 보증하지 않는다.
- 600×900 실제 창: 시계·Day·시간 패널은 화면 안에 들어온다. 기본 전체 구도에서는 오른쪽 창고와 대지 일부가 잘리고 안내문 글씨가 작다.
- 1280×800 실제 창: 시계·Day·전체 농가 표시 확인 후 검토용으로 열어두었다. 기본 그림자·5분·1배속 상태다.

## 주의 및 남은 확인

- 작은 창의 안내문은 작고, Esc 패널 제목·안내문은 배경과 대비가 낮다. 현재 발견 사항이며 수정·사용자 승인 완료로 표시하지 않는다.
- 세로 창의 전체 구도는 모든 건물을 포함하도록 조정한 뒤 재검증이 필요하다.
- 코드 검토에서 메뉴 표시 중 상단 전체 띠가 입력 차단 영역에 포함됨을 발견했다. 따라서 시계가 카메라 입력을 막지 않는다는 이전 기록은 메뉴 숨김 상태와 구분해야 한다. 실제 입력 재현 및 보완이 필요하다.
- 자유 회전·Q/E·정확한 1m·내부 진입의 실제 입력, Unity Editor Game 창 검증은 별도 확인이 필요하다.
- 자동 검사와 간헐적 실제 화면 관찰은 모든 프레임의 연속 영상 검증이나 장시간 안정성·성능 보증이 아니다.
- 2-7-3 진행 중. 사용자 최종 확인 전 단계 완료로 처리하지 않는다.

## 2026-09-21 — v0.55 배경 바닥 반투명 보완

- 구현: 베이지색 Backdrop을 유지하고 실제 알파 블렌딩·깊이 쓰기 전환을 지원하도록 수정했다. 농가 밖에서도 Backdrop의 수직 영역 아래라면 25% 반투명으로 전환한다. 지상에서는 원래 재질로 복원한다.
- 정정: v0.54에서 추가했던 Plane 이름 기반 등록·바닥 분류와 전용 검사는 원복했다. 그 검사의 통과는 실제 Backdrop 문제 해결을 의미하지 않았다. 실제 수동 오브젝트는 변경하지 않았다.
- 자동 검사: Logs/backdrop-occlusion.log의 BACKDROP_OCCLUSION_OK, Logs/backdrop-interior.log의 INTERIOR_OCCLUSION_OK 통과. 세 번의 전환·복원, 농가 안팎 지하, 네 시간대·색상 편집, Home, 주민 포커싱 및 기존 내부 가림 회귀를 확인했다.
- GPU 렌더: Docs/Captures/BackdropOcclusion의 Opaque / Transparent / Unobstructed에서 실제 배경 바닥 뒤에 임시 검증 물체를 두고 투과 여부를 비교했다. 불투명 상태는 베이지색, 반투명 상태는 뒤쪽 초록색 물체가 섞여 보이며 원본 물체색과 구별된다. 검증 물체는 저장하지 않았다.
- 보존: 농가 장면·생성 자산·수동 영역은 변경하지 않았다. 기존 잘못된 Plane 전용 검사를 제거한 것 외에 생성기 변경은 없다.
- 실제 입력: 이번 환경에서는 네이티브 게임 창 제어를 사용할 수 없어 실제 독립 실행 창의 지하 진입 조작은 미검증이다. 자동 렌더와 실제 플레이를 구분하며 사용자 확인 대기로 남긴다.
- Windows 빌드: Logs/backdrop-player-build.log의 RESIDENT_REVIEW_PLAYER_OK 및 종료 코드 0 확인. Logs/ResidentSelectionPlayer/TinyDaysReview.exe 갱신 완료.

## 2026-09-21 — v0.56 실제 땅 지하 반투명

- 초록색 땅의 수직 영역 판정을 지표면 높이 판정으로 교체했다. 카메라가 지표면보다 낮으면 위치·포커싱 여부와 관계없이 초록색 땅·흙 받침·배경 바닥이 25% 반투명으로 유지된다. 땅의 하부 면은 제거하지 않았다.
- Logs/ground-below.log: BACKDROP_OCCLUSION_OK. 땅 내부부터 -20m 깊이와 대지 바깥까지, 포커싱 유무, 네 시간대·색 편집·Home 복원을 직접 호출로 검사했다.
- Logs/ground-interior.log: INTERIOR_OCCLUSION_OK. 기존 주민 포커싱·선택, 건물/소품 내부 가림·복원 회귀 통과.
- Docs/Captures/BackdropOcclusion/FarmBelowCenter.png와 FarmBelowOutside.png를 직접 확인했다. 실제 농가 중앙 아래 및 바깥 아래에서 땅 너머 주민과 집·나무가 보인다. 일반 불투명 건물·밭 등 자체에 가려진 부분까지 모두 노출하는 기능은 아니다.
- 농가 장면·수동 편집 영역·생성 자산은 수정하지 않았다. 실제 게임 창 입력은 네이티브 제어 미지원으로 미검증이며 사용자 확인 대기다.
- Windows 빌드: Logs/ground-player-build.log의 RESIDENT_REVIEW_PLAYER_OK 및 종료 코드 0 확인. 독립 실행 파일 갱신 완료.

## 2026-09-21 — v0.57 작은 창 UI 버튼·시간 패널

- 모든 조작 버튼을 42 기준으로 맞췄다. 하단 메뉴는 화면 폭 740px 미만에서 3열·2줄로 전환한다.
- 시간대 패널은 짧은 창에서 버튼을 축소하지 않고 패널 위 마우스 휠로 스크롤한다. 클릭 판정·카메라 입력 차단은 스크롤 위치와 동일한 레이아웃을 사용한다.
- `Logs/ui-v057-layout.log`: `STAGE27_UI_OK` 통과. 42 높이, 740px 줄바꿈 경계, 스크롤 상단·하단·비스크롤 범위를 직접 검사했다. 실제 독립 실행 창의 1280×800·800×600·600×900 및 경계 폭 확인은 사용자 확인 대기다.
- Windows 빌드: `Logs/ui-v057-player-build.log`의 `RESIDENT_REVIEW_PLAYER_OK`와 종료 코드 0을 확인했다. 독립 실행 파일을 갱신했다.

## 2026-09-21 — v0.58 키보드 카메라 이동

- Q/E를 유지하고 WASD·화살표 키로 화면 기준 평면 이동을 추가했다. 대각선 속도는 정규화하며 줌 거리와 같은 비율로 이동 폭이 달라진다.
- 주민 포커싱 중에도 구도 이동량에 더해 추적·가림을 유지한다. 색상 선택창·Esc 설정창·시간 입력과 Home·줌·회전·마우스 패닝의 기존 입력 규칙은 유지한다.
- `Logs/keyboard-camera-v058.log`의 `STAGE27_UI_OK`에서 화면 기준 방향·대각선 정규화·줌 연동, `Logs/keyboard-camera-resident-regression.log`의 `RESIDENT_OCCLUSION_OK`에서 주민 포커싱 회귀, `Logs/keyboard-camera-player-build.log`의 `RESIDENT_REVIEW_PLAYER_OK`를 확인했다. 실제 독립 실행 창 키 입력은 사용자 확인 대기다.

## 2026-09-21 — v0.59 우클릭 드래그 회전

- 가운데 드래그에 더해 우클릭 드래그도 FarmStudy 카메라 회전에 연결했다. 두 버튼을 함께 누른 뒤 하나를 놓아도 다른 하나를 누르는 동안 회전이 유지된다.
- UI 위 우클릭은 회전하지 않으며 Esc·색상 선택창·설정창·앱 포커스 상실·메뉴 조작은 회전을 해제한다. 좌클릭 패닝·주민 선택·포커싱·WASD·Q/E·휠·Home은 유지한다.
- `Logs/right-rotate-v059.log`의 `STAGE27_UI_OK`, `Logs/right-rotate-resident-regression.log`의 `RESIDENT_OCCLUSION_OK`, `Logs/right-rotate-player-build.log`의 `RESIDENT_REVIEW_PLAYER_OK`를 확인했다. 실제 독립 실행 창 우클릭 입력은 사용자 확인 대기다.

## 2026-09-21 — v0.60 카메라 목표점·패닝 경계

- 전체 보기에서 가운데·우클릭 드래그를 시작하면 화면 중앙 광선이 닿는 가장 가까운 정적 농가 메시를 목표점으로 사용한다. 주민과 넓은 Backdrop은 제외하며, 대상이 없으면 기존 목표점을 유지한다.
- 주민 포커싱은 주민 몸통 기준점을 유지한다. 마우스·키보드 패닝은 수평으로 바꾸고, 전체 목표점과 포커싱 오프셋은 Spring meadow 실제 메시 경계에서 멈춘다. Q/E와 건물 위 목표의 높이는 바꾸지 않는다.
- 자동 검사: `Logs/camera-targeting-v060.log`의 `CAMERA_TARGETING_OK`에서 정적 메시 목표·Backdrop 제외·지면 경계·수평 마우스 패닝·포커싱 경계를 확인했다. `Logs/camera-targeting-ui-v060.log`와 `Logs/camera-targeting-resident-v060.log`의 기존 UI·주민 가림 회귀도 통과했다.
- Windows 빌드: `Logs/camera-targeting-player-build-v060.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과로 `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`를 갱신했다. 실제 Unity Game 창·독립 실행 창에서 회전 중심의 감각, 지면 경계에서의 패닝, 주민 포커싱 중 구도 유지와 Home 복귀는 사용자 확인 대기다. 2-7-3 완료로 기록하지 않는다.

## 2026-09-21 — v0.61 가운데 드래그 높이·우클릭 회전

- 가운데 드래그의 세로 이동과 Q/E를 카메라 높이 오프셋으로 연결했다. 전체 보기 정적 메시와 주민 포커싱 몸통 목표점은 바꾸지 않으며, 높이 이동 중에도 카메라가 해당 목표점을 계속 바라본다. 우클릭 드래그만 회전에 사용한다.
- Home·전체 보기·주민 포커싱·고정 비교 구도 전환은 높이 오프셋을 초기화한다. 왼쪽 수평 패닝, WASD·화살표, Spring meadow 경계 제한, 가림 반투명과 줌 범위는 유지한다.
- 자동 검사: `Logs/camera-height-ui-v061.log`의 `STAGE27_UI_OK`, `Logs/camera-height-targeting-v061.log`의 `CAMERA_TARGETING_OK`, `Logs/camera-height-resident-v061.log`의 `RESIDENT_OCCLUSION_OK` 통과. 전체/포커싱 목표 유지, 높이 이동 후 목표 바라보기, 기존 UI·가림 회귀를 직접 호출로 확인했다.
- Windows 빌드: `Logs/camera-height-player-build-v061.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과. 실제 Unity Game 창·독립 실행 창에서 가운데 드래그 높이, 우클릭 회전, 포커싱 중 높이·회전·패닝, Home 복귀의 사용자 확인은 대기다. 2-7-3 완료로 기록하지 않는다.

## 2026-09-21 — v0.62 카메라 휠 방향

- FarmStudy 카메라에서 휠을 위로 올리면 멀어지고 아래로 내리면 가까워지도록 반전했다. 최소·최대 줌 범위, 주민 포커싱·가림, 지면 경계와 시간대 패널 내부 스크롤은 유지한다.
- 자동 검사: `Logs/wheel-direction-ui-v062.log`의 `STAGE27_UI_OK`에서 휠 양방향 거리 변화와 기존 UI 제어를 확인했다. Windows 빌드는 `Logs/wheel-direction-player-build-v062.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과. 실제 Unity Game 창·독립 실행 창의 휠 감각과 사용자 확인은 대기다.

## 2026-09-21 — v0.63 엘리베이터식 카메라 높이

- 가운데 드래그와 Q/E를 세계 수직 엘리베이터 이동으로 변경했다. 이동 중 yaw·pitch를 유지해 틸트가 생기지 않으며, 전체 보기에서는 새 화면 중앙 광선이 닿는 정적 농가 메시로 목표점·거리를 갱신한다. 주민과 Backdrop은 재탐색 대상에서 제외한다.
- 주민 포커싱은 해제하지 않고 주민을 계속 따라간다. 고정 시선으로 높이를 움직이므로 주민의 화면상 위치는 조금 달라질 수 있다. 왼쪽 패닝·우클릭 회전·휠·지면 경계·가림·Home은 유지한다.
- 자동 검사: `Logs/elevator-camera-targeting-v063.log`의 `CAMERA_TARGETING_OK`, `Logs/elevator-camera-ui-v063.log`의 `STAGE27_UI_OK`, `Logs/elevator-camera-resident-v063.log`의 `RESIDENT_OCCLUSION_OK` 통과. 수직 이동, 무틸트, 전체 보기 목표 재탐색과 포커싱 유지·기존 회귀를 직접 호출로 확인했다.
- Windows 빌드: `Logs/elevator-camera-player-build-v063.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과. 실제 Unity Game 창·독립 실행 창에서 엘리베이터 감각, 목표점 재탐색, 주민 포커싱 중 이동·회전·Home 복귀의 사용자 확인은 대기다. 2-7-3 완료로 기록하지 않는다.
