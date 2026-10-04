# 중심점 거리별 키보드 카메라 속도 v0.137

2026-10-04. 생활·동작·외형·농가·집 검토 공통 기준이다. v0.136의 키보드 최소8m 기준과 고정 Q/E 속도는 대체하며 기존 마우스 감도는 유지한다.

- 속도=max(1m,현재 카메라 중심점 거리)×0.5m/s.2m에서1m/s,8m에서4m/s,20m에서10m/s다. 가까이 확대하면 천천히, 멀리 떨어지면 빠르게 움직인다.
- WASD/화살표는 카메라 기준 수평 이동, Q는 하강·E는 상승이다. 외형에도 Q/E를 제공한다. 농가는 높이 오프셋까지 포함해 계산하고 기존 높이 이동과 추적 오프셋/Home을 보존한다.
- 카메라 중심점을 사용하며 화면 중앙 물체를 속도 계산용으로 탐색하지 않는다. 회전·줌을 키 이동에서 자동 변경하지 않는다. 농가 높이 이동의 기존 중심 재설정 동작은 보존한다.
- 모든 키는 패널 위에서도 가능하고 입력창/설정/색상창/포커스 상실에서는 차단한다. 실제 시간 기준·대각선 정규화·동일 키 중복 방지를 유지한다.

## 검증과 재개

자동 검사와 네 Windows 빌드 완료. `Logs/distance137-final4.log`에 `REVIEW_DISTANCE137_VERIFY_OK`, 네 플레이어 성공 표식과 `REVIEW_DISTANCE137_COMPLETE_OK`, 종료 코드0을 확인했다. 저장 장면/Blender/FBX16개 해시를 보존했다. 결과 `ReviewDistance137Verification.txt`.

`ReviewDistanceCameraChecks.Execute`로 다섯 저장 장면의 거리1/2/4/8/20m 속도, Q/E·농가 높이 오프셋/추적,30/60/120fps·일시정지/배속·입력 차단·Home과 기존 마우스 회귀를 검사했다. 농가의 중심점 재설정 중 프레임 차이는 최대1/120초 하위 계산으로 줄였고 검사용1초 높이 이동은 세fps 모두7.088690m였다. 다른 검토는4m에서 수µm 이내 차이였다. 매 하위 계산에서 가상 카메라 위치만 갱신하며 반투명 처리는 반복하지 않는다.

합성 키 상태/직접 함수 검증이며 실제 OS 키 입력과 GUI 타이핑을 대신하지 않는다. 집은 저장 Unity 장면 함수 검사이며 실제 Game 창 조작은 하지 않았다. **거리별 키보드 이동 속도 사용자 확인 대기**.

생활 프로그램은 `Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe`, 동작은 `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`, 외형은 `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe`, 농가는 `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`다. 집은 `Assets/Scenes/HouseVillageStudy.unity`를 Unity에서 Play한다. 실행 파일/데이터 폴더는 로컬 산출물이며 다른 PC에서는 저장 장면으로 재빌드한다.

기본 모델·원본 애니메이션·생활 흐름·프로토타입은 보존한다. GitHub 업로드 없음. 경로 계산 지연은 별도 미해결 대상이다.
