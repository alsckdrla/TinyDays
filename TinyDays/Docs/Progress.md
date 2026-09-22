# Tiny Days 진행 기록

최종 갱신: 2026-09-22
기획 기준: [MasterPlan.md](MasterPlan.md) v0.64
현재 단계: **2-7-3 하루의 빛·카메라·종합 관찰 진행 중**

이 문서는 다른 PC에서 작업을 이어갈 때 먼저 확인하는 최신 체크포인트다. 기획의 확정·미정 기준은 항상 MasterPlan.md를 따른다. 세부 검증 근거는 각 단계 관찰 문서를 확인한다.

## 현재 상태

- 2-7-1 대표 조명과 2-7-2 시간 흐름은 사용자 확인을 마쳤다.
- 2-7-3은 종합 관찰 진행 중이며, 최종 사용자 확인 전에는 완료로 기록하지 않는다.
- 현재 FarmStudy에는 농가 대지, 집 3종, 창고, 밭·쉼터·생활 장식, 임시 토끼 주민 6명이 있다.
- 주민 외형과 이동 애니메이션은 임시 작업물이다. 더 과장되고 사랑스러운 캐릭터 움직임이라는 목표는 유지하되, 보완은 사용자 합의에 따라 보류한다.

## 이번 체크포인트의 구현 범위

- 농가·주택·창고와 앞마당, 작업마당, 밭, 쉼터, 길가 장식의 시각·생활 연출.
- 전체 보기, 줌·패닝·회전·높이 조절, 1m 최소 줌, 주민 선택·포커싱·Home 복귀, 카메라와 주민 사이 장애물의 반투명 처리.
- URP 그림자 낮음·기본·높음 선택과 로컬 설정 복원.
- 일출·낮·일몰·포근한 밤의 조명, 시간대 비교·색상 편집, 1~120분 하루 길이, 0.5/1/2/4배속, 자동 순환과 일시정지.
- 상단 24시간 시계와 Day 표시. 메뉴를 숨겨도 유지하며 자정에 Day가 증가한다.
- 베이지색 Backdrop의 지하 25% 반투명과 지상 복원, 농가 밖 하부 판정 및 전용 셰이더의 실제 블렌딩을 보완했다. 잘못 추가했던 Plane 전용 처리·검사는 원복했다.
- 모든 조작 버튼을 42 기준으로 통일하고, 740px 미만에서 하단 메뉴를 3열·2줄로 전환한다. 시간대 패널은 짧은 창에서 내부 스크롤을 사용한다.
- Q/E와 가운데 드래그는 틸트 없는 세계 수직 카메라 이동을 제공한다. 전체 보기에서는 새 화면 중앙 정적 메시를 목표점으로 재탐색하며, 포커싱 중에는 주민 추적을 유지한다. WASD·화살표 키와 왼쪽 드래그는 화면 기준 평면 이동을 제공한다.
- 우클릭 드래그로 카메라를 회전한다. 카메라 휠은 위로 올리면 멀어지고 아래로 내리면 가까워진다. UI·패널 진입과 앱 포커스 상실 시 높이·회전 드래그 상태를 해제한다.
- 전체 보기 회전은 화면 중앙 광선이 닿는 정적 농가 메시를 목표점으로 사용하고, 주민 포커싱은 주민 몸통을 목표점으로 유지한다. 마우스·키보드 패닝은 수평으로 처리하며 전체 목표점과 포커싱 오프셋은 실제 Spring meadow 메시 범위를 넘지 않는다. Backdrop과 주민은 전체 보기 회전 목표에서 제외한다.

## 검증 상태

| 구분 | 상태 | 근거 |
|---|---|---|
| 자동 검사 | 통과 | `Logs/backdrop-occlusion.log`: 실제 Backdrop GPU 렌더에서 뒤쪽 물체 투과 확인, 0.2초 전환·원래 재질 복원, 농가 안팎 하부 판정, 네 시간대·색 편집·Home 및 주민 포커싱 검사 통과. |
| Windows 독립 실행 창 | 일부 확인 | 1280×800, 800×600, 600×900, 최대화 창에서 시간·시계·메뉴·그림자·주민 포커싱을 확인했고 5분 이상 자동 순환을 관찰했다. |
| 사용자 확인 | 진행 중 | 2-7-1·2-7-2 확인 완료. 2-7-3 종합 관찰과 현재 농가 구성의 최종 확인은 남아 있다. |

v0.57 자동 레이아웃 검사: `Logs/ui-v057-layout.log`의 `STAGE27_UI_OK` 통과. 42 높이, 740px 줄바꿈 경계, 시간 패널 스크롤 상단·하단·비스크롤 상태를 직접 검사했다. 실제 독립 실행 창 입력은 별도 확인 대상이다.

v0.57 Windows 검토 빌드: `Logs/ui-v057-player-build.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과. `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`를 갱신했으며 실제 창 확인은 사용자 대기다.

v0.58 자동 검사·Windows 검토 빌드: `Logs/keyboard-camera-v058.log`의 `STAGE27_UI_OK`, `Logs/keyboard-camera-resident-regression.log`의 `RESIDENT_OCCLUSION_OK`, `Logs/keyboard-camera-player-build.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과. 실제 키 입력 확인은 사용자 대기다.

v0.59 자동 검사·Windows 검토 빌드: `Logs/right-rotate-v059.log`의 `STAGE27_UI_OK`, `Logs/right-rotate-resident-regression.log`의 `RESIDENT_OCCLUSION_OK`, `Logs/right-rotate-player-build.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과. 실제 우클릭 입력은 사용자 대기다.

상세 기록: [Stage273Observation.md](Stage273Observation.md), [Stage27LightingObservation.md](Stage27LightingObservation.md), [Stage27ClockDisplayObservation.md](Stage27ClockDisplayObservation.md), [Stage27PlaybackObservation.md](Stage27PlaybackObservation.md).

## 남은 확인과 보류

- 600×900 세로 창에서 오른쪽 창고와 대지 일부가 잘린다. 전체 구도 조정 후 다시 확인해야 한다.
- 작은 창의 안내문과 Esc 설정 패널의 제목·안내문 대비가 낮다. 수정 여부는 사용자와 합의한다.
- 메뉴 표시 중 상단 입력 차단 영역이 시계를 포함한다. 시계가 카메라 입력을 막지 않는지 실제 입력으로 재현·보완이 필요하다.
- 화면 중앙 목표점 회전, Spring meadow 경계 패닝, 틸트 없는 가운데 드래그·Q/E 높이, 우클릭 회전, 정확한 1m 줌, 물체 내부·지하 진입의 실제 입력과 Unity Editor Game 창 검증이 남아 있다.
- Backdrop 수정의 실제 독립 실행 창 지하·지상 전환 입력과 사용자 확인이 남아 있다. 이전 Plane 검사는 이 문제의 해결 근거가 아니며 해당 처리는 원복했다.
- 이 관찰은 장시간 안정성·성능 보증이 아니다.
- 2-3 추가 동작·특별 행동, 2-4 갈퀴·수레, 2-5 곰·여우는 보류 상태다. 자율 판단·생산·건설 규칙은 아직 3단계 이후 범위다.

## 다음 작업 순서

1. 2-7-3에서 발견한 세로 구도·UI 가독성·상단 입력 차단 문제의 수정 범위를 사용자와 합의한다.
2. 합의한 보완 후 실제 독립 실행 창과 Unity Game 창에서 남은 카메라 입력을 확인한다.
3. 2-7-3 사용자 확인을 받은 뒤, 보류한 캐릭터 작업의 재개 시점 또는 3단계 자율 생활 기반 착수를 별도 합의한다.

## PC 간 이어가기

1. `main` 최신 상태를 pull하고 `git status -sb`가 깨끗한지 확인한다.
2. Unity 2022.3.20f1과 URP 14.0.10, Blender 5.2.1을 사용한다.
3. `TinyDays/Docs/MasterPlan.md` → 이 문서 → `TinyDays/Docs/Stage273Observation.md` 순으로 읽는다.
4. Unity에서 `Assets/Scenes/FarmStudy.unity`를 열어 작업을 재개한다.
5. `Library`, `Temp`, `Logs`, `Builds`, `UserSettings`와 로컬 PlayerPrefs 색상·그림자 설정은 Git 동기화 대상이 아니다.
6. 프로토타입을 검토해야 할 때만 `TinyDaysPrototype/Docs/MasterPlan.md`와 `TinyDaysPrototype/Docs/Progress.md`를 읽고, 별도 요청 없이 수정하지 않는다.

## v0.55 실행 파일

- Windows 검토 빌드 갱신 완료: Logs/ResidentSelectionPlayer/TinyDaysReview.exe. Logs/backdrop-player-build.log에서 RESIDENT_REVIEW_PLAYER_OK 확인.
- 실제 게임 창의 지하·지상 조작은 사용자 확인 대기다. 2-7-3 완료나 실제 입력 검증 완료로 처리하지 않는다.

## v0.56 지하 땅 반투명

- 대지 영역 밖에서도 카메라가 지표면보다 낮으면 실제 땅·흙 받침·배경 바닥을 25% 반투명 처리한다. 땅 아래쪽 면은 보존한다.
- 실제 게임 창 입력과 사용자 확인은 대기이며 2-7-3은 진행 중이다.
- v0.56 자동 검사: Logs/ground-below.log 및 Logs/ground-interior.log 통과. 실제 농가 중앙·대지 밖 하부 렌더에서 주민·주변 오브젝트 투과 확인. 직접 호출·렌더 검사이며 실제 창 입력과 구분한다.
- v0.56 Windows 실행 파일 갱신 완료: Logs/ResidentSelectionPlayer/TinyDaysReview.exe. Logs/ground-player-build.log의 RESIDENT_REVIEW_PLAYER_OK 확인.

## v0.60 카메라 목표점·패닝 경계

- 구현: 전체 보기 회전 시작 시 화면 중앙 광선이 닿는 가장 가까운 정적 농가 메시를 목표점으로 설정한다. 주민 포커싱은 주민 몸통 기준점을 유지한다. 넓은 Backdrop과 주민은 전체 보기 목표에서 제외한다.
- 패닝: 마우스와 키보드 이동은 지면과 평행하게 처리하며, 전체 목표점과 포커싱 오프셋을 실제 Spring meadow 메시 경계 안으로 제한한다. 건물 위 목표와 Q/E의 높이는 보존한다.
- 자동 검사: `Logs/camera-targeting-v060.log`의 `CAMERA_TARGETING_OK`, `Logs/camera-targeting-ui-v060.log`의 `STAGE27_UI_OK`, `Logs/camera-targeting-resident-v060.log`의 `RESIDENT_OCCLUSION_OK` 통과. 직접 편집기 호출 검사이며 실제 입력과 구분한다.
- Windows 빌드: `Logs/camera-targeting-player-build-v060.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과로 `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`를 갱신했다. 실제 입력·사용자 확인은 전체 보기의 지면·건물·소품 회전 중심, 주민 포커싱 추적, 지면 경계 패닝과 Home 복귀가 남아 있다. 2-7-3은 진행 중이다.

## v0.61 가운데 드래그 높이·우클릭 회전

- 구현: 가운데 드래그 세로 이동과 Q/E는 카메라 높이 오프셋만 바꾸고, 전체 보기의 정적 메시 또는 주민 포커싱의 몸통 목표점은 유지한다. 우클릭 드래그만 회전하며, 높이 이동 중에도 카메라는 목표점을 바라본다.
- 초기화: Home·전체 보기·주민 포커싱·고정 비교 구도 전환에서 높이 오프셋을 초기화한다. 왼쪽 수평 패닝·WASD/화살표·지면 경계·가림 반투명·1m 줌은 유지한다.
- 자동 검사: `Logs/camera-height-ui-v061.log`의 `STAGE27_UI_OK`, `Logs/camera-height-targeting-v061.log`의 `CAMERA_TARGETING_OK`, `Logs/camera-height-resident-v061.log`의 `RESIDENT_OCCLUSION_OK` 통과. 직접 편집기 호출 검사이며 실제 입력과 구분한다.
- Windows 빌드: `Logs/camera-height-player-build-v061.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과로 `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`를 갱신했다. 실제 입력·사용자 확인은 2-7-3에 남아 있다.

## v0.62 카메라 휠 방향

- 구현: FarmStudy 카메라의 휠을 위로 올리면 줌아웃, 아래로 내리면 줌인하도록 반전했다. 최소 1m·최대 거리·포커싱·가림·지면 경계와 시간대 패널 내부 스크롤은 유지한다.
- 자동 검사: `Logs/wheel-direction-ui-v062.log`의 `STAGE27_UI_OK`에서 양방향 거리 변화와 기존 UI 제어를 확인했다.
- Windows 빌드: `Logs/wheel-direction-player-build-v062.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과로 `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`를 갱신했다. 실제 독립 실행 창의 휠 입력은 2-7-3 사용자 확인 대기다.

## v0.63 엘리베이터식 높이 이동

- 구현: 가운데 드래그와 Q/E는 세계 수직으로만 카메라를 이동하고 yaw·pitch는 유지한다. 전체 보기에서는 이동 뒤 화면 중앙 광선으로 가장 가까운 정적 농가 메시를 재탐색해 목표점과 거리를 갱신한다. 유효한 메시 또는 거리 범위를 찾지 못하면 마지막 구도를 유지한다.
- 포커싱: 주민 포커싱은 계속 유지하고 주민을 따라간다. 시선은 고정되므로 높이 이동 중 주민의 화면상 위치는 조금 달라질 수 있다.
- 자동 검사: `Logs/elevator-camera-targeting-v063.log`의 `CAMERA_TARGETING_OK`, `Logs/elevator-camera-ui-v063.log`의 `STAGE27_UI_OK`, `Logs/elevator-camera-resident-v063.log`의 `RESIDENT_OCCLUSION_OK` 통과. 직접 편집기 호출 검사이며 실제 입력과 구분한다.
- Windows 빌드: `Logs/elevator-camera-player-build-v063.log`의 `RESIDENT_REVIEW_PLAYER_OK` 통과로 `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`를 갱신했다. 실제 입력·사용자 확인은 2-7-3에 남아 있다.

## v0.64 장기 확장 구상·PC 이동 체크포인트

- 마스터 플랜 6.1~6.7에 비주얼·작은 개입·관심 주민 생애·지역 생활 연속성·역사 DLC·웹 체험판을 기록했다. 사용자는 비주얼 내용도 장기 확장 방향으로만 기록하도록 확인했다.
- 현재 본편 아트와 1.0 범위는 변경하지 않았다. 2-7-3 진행·실제 입력 및 사용자 확인 대기·캐릭터 보완 보류를 유지한다. 이번 변경은 문서 반영이며 게임 기능 구현이나 역사적 사실 검증이 아니다.
- 이 체크포인트는 v0.55~v0.63 코드·검사 자료와 v0.64 문서를 함께 동기화하는 범위다. 프로토타입은 기존 추적 상태를 유지한다.
- 실행 파일·Unity 캐시·로그는 로컬 자료이므로 다른 PC에서는 Unity 2022.3.20f1로 검토 빌드를 다시 만든다. 루트의 개인 Windows 바로가기는 업로드하지 않는다.
