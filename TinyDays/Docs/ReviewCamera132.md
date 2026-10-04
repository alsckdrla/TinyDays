# v0.132 검토 카메라 — 좌회전·우패닝

후속 사용자 확인(2026-10-04): 카메라 조작 수정 완료를 확인받았다. 아래 확인 대기 표현은 구현 당시 기록이다. 에이전트의 실제 OS 입력 재검증이나 모든 프로그램 개별 입력 확인을 추가로 수행한 것은 아니다.

갱신: 2026-10-04. 최신 공통 버튼 기준은 **왼쪽 드래그 회전·오른쪽 드래그 패닝**이다. 생활/동작/외형/농가/집 검토에 적용했다. 기존 회전 감도·패닝 방향/계산·줌·가운데 버튼 기능·Q/E·Home을 유지했다.

## 입력과 안내

- `FarmStudyReview`의 공통 상수: RotateButton=0,PanButton=1,ClickButton=0,HeightDragButton=2. 각 검토 입력이 동일 기준을 사용한다. 생활 검토의 `CameraDrag(int,Vector2)`는 실제 입력과 자동 검사의 공통 경로다.
- 농가 UI와 주민 선택은 왼쪽 클릭, 같은 주민0.3초 내 더블클릭은 따라보기다.5픽셀 이동은 클릭 후보,6픽셀 이상이면 회전이 시작되며 드래그 후에는 주민을 선택하지 않는다. 오른쪽 패닝은 선택/더블클릭 경로에서 분리했다.
- UI 시작/진입,버튼 해제,포커스 상실,Home/전체 보기에서 드래그를 정리한다. 패널 휠은 카메라 줌으로 전달하지 않는다. 화면 안내와 현재 사용 문서를 갱신했다. 과거 변경 이력의 버튼 설명은 당시 기록이다.

## 검사와 실행 파일

`ReviewCameraBindingChecks.Execute`는 기존 다섯 장면을 읽어 입력/회전·패닝 검사를 수행하고 네 Windows 프로그램을 만든다. 장면 생성·모델/클립 생성은 호출하지 않는다. 다섯 장면 파일의 작업 전후 SHA를 비교한다.

- 회전이 카메라 방향을 바꾸고 패닝은 방향을 유지하며 위치만 이동하는지 확인한다. 집 검토도 기존 Unity 장면에서 직접 호출했다.
- 동작 패널의 세 버튼 UI 시작/진입,해제/포커스/Home과 패널 안팎 휠을 확인한다.
- 농가6픽셀 경계,드래그 후 선택 없음,패닝 중 따라보기·선택 보존,클릭·더블클릭 및 포커스/전체 보기 해제를 확인한다.
- 실제 OS 마우스 입력은 이번에 검증하지 않았다. 자동 호출 검사는 사용자 감도/조작 품질 승인과 다르다. **좌회전·우패닝 카메라 사용자 확인 대기**다.

실행 파일과 옆 Data 폴더는 한 세트다. 현재 코드의 재빌드는 Unity2022.3.20f1 배치 `-executeMethod ReviewCameraBindingChecks.Execute` 또는 각 기존 BuildPlayer 진입점으로 한다. Unity를 닫고 실행한다.

| 검토 | 실행 파일 |
|---|---|
| 생활 | `Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe` |
| 동작 | `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe` |
| 외형 | `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe` |
| 농가 | `Logs/ResidentSelectionPlayer/TinyDaysReview.exe` |
| 집 | Unity `Assets/Scenes/HouseVillageStudy.unity` |

2026-10-04 최종 자동 검사와 네 Windows 빌드가 통과했다. 검사 근거는 `ReviewCamera132Verification.txt`와 `Logs/camera132-final.log`의 `REVIEW_CAMERA132_VERIFY_OK` 및 `REVIEW_CAMERA132_COMPLETE_OK`다. 다섯 저장 장면의 작업 전후 SHA가 같았다. 실제 OS 마우스 입력과 사용자 조작 품질은 확인 대기다. 프로토타입·기존 몸동작·모델·저장 장면은 수정하지 않았다. GitHub 업로드는 하지 않았다.
