# WASD·화살표 검토 카메라 v0.136

후속 v0.137: 키 속도는 최소 계산 거리1m·중심점 거리×0.5m/s로 변경하고 Q/E에도 적용한다. 아래는 v0.136 당시 검사 기록이며 최신 안내는 `ReviewDistance137.md`를 따른다.

2026-10-04. 생활·동작·외형·농가·집 검토에 적용하며 프로토타입은 제외한다.

- W/↑ 앞으로, S/↓ 뒤로, A/← 왼쪽, D/→ 오른쪽. 카메라의 수평 방향을 따라 카메라와 중심을 함께 이동한다. 회전·높이·줌은 유지한다.
- 대각선은 정규화하고 WASD/화살표 중복은 합산하지 않는다. 수직 구도에서도 방향을 유지하며 줌 거리에 따른 기존 농가 이동 속도를 사용한다.
- 재생 일시정지/배속과 관계없이 실제 시간으로 이동한다. 패널 위에서도 가능하고 텍스트 입력·설정/색상창·포커스 상실에서는 차단한다. 농가 추적 중에는 오프셋을 이동하고 Home으로 기존 전체 보기와 오프셋을 복구한다.
- 좌회전·우패닝·기존 가운데 버튼·Q/E·휠·Home은 유지한다. 기존 원본·클립·생활 동작과 저장 장면은 변경하지 않는다.

## 검사와 실행

자동 검사와 네 Windows 빌드 완료. 최종 로그 `Logs/keyboard136-final3.log`에 `REVIEW_KEYBOARD136_VERIFY_OK`, `REVIEW_KEYBOARD136_COMPLETE_OK`와 종료 코드0을 확인했다. 저장 장면/Blender 원본/FBX16개 해시를 보존했다. 결과 `ReviewKeyboard136Verification.txt`.

`ReviewKeyboardCameraChecks.Execute`는 합성 키 상태와 실제 다섯 저장 장면의 제어 함수를 검사한다. 네 방향·대각선·반대 입력·중복 키·회전/수직 구도·줌 속도·30/60/120fps·일시정지/배속·포커스 상실/복구·Home·농가 따라보기 오프셋·설정/색상창 차단과 기존 마우스/클릭/패널 회귀를 확인했다. 텍스트 입력은 공통 차단 조건을 검사했으며 실제 타이핑은 확인하지 않았다.

직접 OS 키 입력·사용자 조작감은 별도 대기다. 집 검토는 저장 Unity 장면의 함수 검사이며 사람이 Game 창을 조작한 검증은 아니다. **키보드 카메라 이동 사용자 확인 대기**.

Windows 로컬 실행 파일:

- 생활: `Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe`
- 동작: `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`
- 외형: `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe`
- 농가: `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`
- 집: `Assets/Scenes/HouseVillageStudy.unity`를 Unity에서 Play.

새 PC에서는 저장 장면으로 빌드한다. 이번 GitHub 업로드 없음. 자세 사용자 확인 대기와 경로 계산 지연은 이 카메라 변경으로 해결되지 않는다.
