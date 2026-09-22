# Tiny Days 진행 기록

최종 갱신: 2026-09-22
기획 기준: [MasterPlan.md](MasterPlan.md) v0.82
현재 단계: **v0.78 코트 원본 사용자 확인 완료 / 자연스러운 출발·발 모으기 정지 사용자 확인 대기 / 2-7-3 미완료**

이 문서는 다른 PC에서 작업을 이어갈 때 먼저 확인하는 최신 체크포인트다. 기획의 확정·미정 기준은 항상 MasterPlan.md를 따른다. 세부 검증 근거는 각 단계 관찰 문서를 확인한다.

## 현재 상태

- **최신 v0.82:** 별도 준비/체중 이동 없이 약 0.45초에 출발한다. 골반 높이 변화와 전환 보정 해제를 완만하게 했다. 정지는 감속·착지 뒤 앞발을 고정하고 뒷발을 1.5cm 들어 0.3초에 옆으로 놓으며 몸도 함께 이동한다. 양발 앞뒤 차이 1cm 이내·실제 접지 확인 후 0.15초 안정하고 완료한다. 자동 정지는 5.25m부터 시작한다. 아래 v0.81 이하 표기는 역사 기록이다.
- 출발 수정 전 CSV·렌더를 `AdultRabbitStartBeforeV082.csv`, `Captures/AdultRabbitMotionBeforeV082`에 보존했다. 0~1.65초 240Hz에서 골반 높이 범위 100.474 → 67.380mm, 최대 상하 속도 1.449137 → 0.444403m/s. `AdultRabbitStartVerification.txt`와 최신 `AdultRabbitStartMotion.csv` 참고. 수치 개선과 자연스러움 승인은 별개다.
- 짧은 정지 후 재출발 검사에서 기존 선형 감속이 지지발 허용 거리를 초과해 뒷발이 벌어지고 몸이 주저앉는 경우를 찾았다. 발 동작 시간을 압축하거나 옷감 보정 한도를 늘리지 않고, 필요할 때 감속 곡선을 바꾸어 이동량을 허용 거리 안으로 제한했다.
- 자동 검사: 발 모으기 8위상·295표본, 발 모으기 중 재출발 24경우 통과. 정지 양발 앞뒤 차이 0(보고 정밀도), 들어 올린 신발 바닥 최대 17.500mm(기본 간격 2.5mm + 들기 15mm). 전환 중 다리 길이 추가 보정 최대 약 1.31%. 정지 806표본의 각 신발 바닥 2.499~2.500mm, 무릎 좌우 이탈 최대 0.083mm. 직접 함수 호출 검사다.
- 48조합(8위상×30/60/120fps×0.5×/1×) 지지발 이동 최대 약 0.001mm, 신발 최저 +2.443mm, 도달 불가 0, 거리 차이 최대 11.042mm. 기존 반복 입력 15경우·시간/초기화/일시정지 회귀 통과. 기존 2,748시점에 새 발 정리 구간 1,472시점을 더한 **코트 4,220시점에서 1mm 초과 0**, 최대 실행용 옷감 보정 41.575mm. 모든 방향·연속 시점 무관통 보증은 아니다.
- Windows v0.82 빌드 성공: `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`. `ADULT_RABBIT_MOTION_OK` / `ADULT_RABBIT_MOTION_PLAYER_OK` 확인. 원본 Blender·FBX·코트·정상 걷기·농가·카메라 입력과 실행용 옷감 보정 방식은 변경하지 않았다. GitHub 업로드 없음.
- 남은 확인: 0.5×/1× 실제 입력에서 출발 첫 세 걸음, 수동/자동 정지의 발 정리, 발 모으기 도중 재출발, 코트 반응의 자연스러움. 자동 검증과 렌더는 실제 Game 입력·동작 품질 승인을 대신하지 않는다.
- 정면·측면 자동 렌더 영상: `Captures/AdultRabbitMotion/NaturalStartStopV082.mp4` (30fps/3.5초, 1.5초에 정지). 실제 화면 녹화가 아니라 직접 호출한 동작의 렌더다. 시작·발 정리·정지 주요 프레임에서 무릎/코트와 최종 양발 정렬을 확인했다. 원시 프레임은 로컬 `Logs/AdultRabbitNaturalSequence`에 있다.
- v0.82 실행 프로세스 시작·응답 및 시작 로그의 오류/예외 없음 확인(`Logs/adult-rabbit-motion-player-v082-startup.log`). 검사용으로 실행한 프로세스는 정리했다. 실제 마우스·버튼 입력 검증은 아니다.

- **최신 v0.81:** X자 무릎의 좌우 보정을 제거했다. 전환 골반 균형·높이와 다리 길이, 최초 왼발 선택, 재출발 가속을 보완하고 실제 양쪽 신발 바닥 검사 뒤 정지를 완료한다. 아래 v0.80 이하의 최신/미완료 표기는 해당 버전의 역사 기록이다.
- 사용자 추가 승인에 따라 원본 코트 대신 실행용 복제 메시의 무릎 주변만 밀어내는 보정을 추가했다. 코트 원본·Blender·FBX·생성 스크립트·정상 걷기·카메라·농가는 수정하지 않았다. 일반 옷감 물리나 농가 적용은 아니다.
- X자 자세 수정 전 좌표와 이미지는 `AdultRabbitPostureBeforeV081.txt`, `Captures/AdultRabbitMotionBeforeV081`에 보존했다. 기존 자동 정지 한 장면에서는 양발 모두 +2.5mm여서 한 발 공중 정지는 재현되지 않았다. 이전 검사가 무릎 정렬·양발 개별 접지를 보장하지 않았음을 명시한다.
- 자동 수치: 무릎 좌우 이탈 최대 0.083mm, 무릎 사이 최소 288.859mm, 정지 1,098표본의 양쪽 신발 바닥 +2.499~2.500mm. 코트 전면 검사 2,748시점 중 1mm 초과 0. 접지 48조합·반복 입력 15조합·배속/초기화/일시정지 회귀 통과. 실제 입력 확인이 아닌 직접 함수 호출 검사다.
- 초기 옷감 보정 없는 정상 무릎 시도에서는 2,748시점 중 248시점·최대 46.65mm 관통이 남았다. 실행용 보정 추가 후 해소했으며 정상 무릎을 다시 비틀지 않았다. 원본 보존·실행용 메시 정리 검사를 추가했다. 최신 상세 결과는 `AdultRabbitMotionVerification.txt`를 따른다.
- 실제 Windows 버튼·마우스 입력 및 연속 동작 품질 승인은 아직 남아 있다. 정면·측면 렌더 검토와 자동 검사만으로 사용자 승인을 대신하지 않는다. GitHub 업로드는 하지 않았다.
- 최종 v0.81 자동 검사와 Windows 빌드 성공(`ADULT_RABBIT_MOTION_OK`, `ADULT_RABBIT_MOTION_PLAYER_OK`). 실행 파일은 `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`. 실행용 코트 최대 보정 49.647mm, 초기화 3회·클립 복귀·비활성화 직접 호출에서 원본 메시 참조와 정점/삼각형/재질 보존 통과. 편집 모드 SendMessage의 ShouldRunBehaviour assertion은 콜백 직접 호출 검사로 교체했다.
- 최종 렌더 `Posture0.30*`, `Posture0.65*`, `Posture8.00*`와 `CoatCorrectionMaxFront/Side/Quarter`를 확인했다. X자 교차가 없고, 양발 정지·최대 보정 자세에서 코트 전면의 검은 무릎 돌출이 보이지 않는다. 최대 보정 자세는 밑단 형태 변화가 있으므로 연속 재생의 자연스러움은 사용자 확인 대기다.
- v0.81 프로그램 시작·프로세스 응답 확인, 시작 로그 `Logs/adult-rabbit-motion-player-v081-startup.log`에서 오류/예외 없음. 검사용으로 띄운 프로세스는 정리했다. 실제 화면 입력 확인을 대신하지 않는다.

- **최신 v0.80:** 승인된 코트와 원본 걷기는 유지하고 전환 코드의 관통을 보완했다. 착지한 하체 기준 유지, 재출발/재정지 시 현재 자세 연결, 전환 무릎 굽힘 방향 제한, 첫 보행 주기 뒤 원본 복귀, 매 프레임 IK 이전 자세 복원을 적용했다. 아래 v0.79 미완료 기록은 과거 이력이다.
- 검사 범위를 317 → **2,748시점**으로 확대했다. 보정 전 상태를 같은 확대 검사로 재현하면 288시점이 1mm 초과·최대 66.72mm이며, 수정 후 검출 0이다. `AdultRabbitMotionBeforeV080.txt`와 `AdultRabbitMotionVerification.txt`를 비교한다. 기존 v0.79의 18.85mm 수치는 이전 좁은 표본 기준이다.
- 접지 48조합·반복 요청 15조합·배속·일시정지·초기화 자동 검사 통과. 지지발 최대 이동 약 0.001mm, 신발 최저 +2.499mm, 도달 불가 0, 프레임률/배속 간 거리 차이 4.792mm. 5회 초기화/재실행과 0초 재평가의 메시 차이는 보고 정밀도에서 0이다. 모두 직접 함수 호출 검사다.
- 렌더 비교: `Captures/AdultRabbitMotionBeforeV080/TransitionRestart*.png` → `Captures/AdultRabbitMotion/TransitionRestart*.png`. 같은 입력 순서·정면/측면/비스듬한 카메라에서 무릎 돌출 해소를 확인했다. 모든 방향·시점의 무관통 보증이나 실제 연속 재생 품질 승인은 아니다.
- Windows 동작 검토 v0.80 빌드 성공: `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe` (`Logs/adult-rabbit-motion-player-build.log`: `ADULT_RABBIT_MOTION_PLAYER_OK`). 실제 버튼·마우스 입력은 미검증이며 사용자 확인 대기다.
- 이 PC에서 v0.80 프로그램 시작·프로세스 응답을 확인했고 `Logs/adult-rabbit-motion-player-v080-startup.log`에 오류/예외가 없었다. 이는 시작 확인이며 실제 화면 조작·연속 동작 승인과 구분한다.
- 두 Blender 원본, 동작 FBX, 두 생성 스크립트, FarmStudy 장면의 SHA-256이 작업 전과 일치한다. 모델·코트·원본 애니메이션·농가를 수정하지 않았다. GitHub 업로드도 하지 않았다.

- v0.79 최신: `AdultRabbitFootTransition`에 체중 이동·출발·걷기·마지막 착지·안정 상태, 기존 뼈 기반 IK와 세계 좌표 지지발 고정을 구현했다. 정상 속도 1.15m/s, 최초 출발 약 0.45초, 정지 약 0.3~0.55초. 현재 발에서 재출발하며 경로 끝 5.55m부터 자동 정지한다. UI에 상태와 지지발을 표시한다.
- 정지 48조합 자동 검사: 지지발 이동 최대 0.726mm / 신발 최저 +2.499mm / 도달 불가 0 / 프레임률·배속 간 거리 차이 최대 4.792mm. 출발 중 정지·재출발·반복 입력 15조합: 지지발 이동 최대 0.884mm / 최저 +2.499mm / 도달 불가 0. 근거 `AdultRabbitTransitionVerification.txt`.
- **남은 문제:** 코트 검사 317시점 중 28시점에서 1mm 초과, 최대 18.85mm(`restart 7/6`). `Captures/AdultRabbitMotion/CoatWorstFront.png`에서도 무릎 돌출을 확인했다. 전환 중 골반·다리 자세와 승인된 코트의 간섭이므로 이번 작업 완료나 사용자 승인 대기로 처리하지 않는다. 기존 v0.78 코트 자체는 사용자 확인 완료다.
- Windows 동작 검토 빌드 갱신 성공: `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`, 로그 `ADULT_RABBIT_MOTION_PLAYER_OK`. 접지·시간·초기화 검사 통과, 코트 검사는 경고 및 INCOMPLETE로 남긴 개발 중 빌드다. 빌드 성공을 전체 계획 완료로 표현하지 않는다.
- 기존 Blender/FBX·코트·정상 걷기·팔 원본 및 농가를 변경하지 않았다. 런타임 전환을 수정했다. 접지 검사 통과와 시각 품질은 분리한다. 실제 Windows 입력과 연속 동작 품질은 미검증이며 GitHub 업로드는 하지 않았다.
- 일시정지·처음 위치·클립 변경·비활성화 정리는 직접 호출 검사다. 편집 모드에서 enabled 변경만으로 OnDisable이 호출된다고 가정한 초기 검사는 실패했고, 콜백 명시 호출로 수정했다. 실제 Play Mode 수명주기 검증을 대신하지 않는다.

- 최신 v0.78: 앞쪽 추가 여유 최대 2cm·허벅지 추종 최대 45%, 무릎 높이로 보정 분포 확장, 중앙 가중치 연속 보간. 안감 동일 보정. 걷기·골격·재질·삼각형 예산 유지.
- 317시점 검사(걷기 96 + 정지/재출발 208 + 출발 13): 기존 최대 19.07mm·24시점 1mm 초과 → 최종 최대 0, 1mm 초과 0. 최초 검사 대상 좌표 오류로 나온 미검출 기록은 무효화했고 기존 자산을 재생성하여 올바른 좌표로 다시 비교했다. 유효한 수정 전 기록은 `AdultRabbitMotionBeforeV078.txt`다.
- 이전 이미지 `Captures/AdultRabbitMotionBeforeV078/CoatWorst*`, 최신 같은 시점 `Captures/AdultRabbitMotion/CoatPreviousWorst*`. 외형/동작 자동 회귀·두 Windows 빌드 통과. 실제 입력과 모든 방향의 메시 교차 검사는 미실시이며 코트 관통 추가 보완 사용자 확인 대기. GitHub 업로드는 하지 않았다.

- 최신 v0.77: 사용자 보고 무릎–코트 관통을 보완했다. 하단 허벅지 추종 최대 38%, 앞쪽 하단 여유 최대 4.5cm, 안감 동일 보정. 허리 위·골격·걷기 궤적은 유지한다.
- 외형/동작 자동 회귀 검사·두 Windows 빌드 통과. `Walk_Side_Passing`, `Walk_Front_Passing`, `Walk_PassingR`, `Moving_StartHalf` 렌더에서 기존 상의 앞면의 무릎 돌출이 보이지 않음을 확인했다. 실제 연속 입력·모든 프레임의 교차 검사는 미실시이며 사용자 확인 대기다. 다음은 걷기·출발·정지 연속 재생에서 코트 확인. GitHub 업로드는 하지 않았다.

- 최신 v0.76: 이동 검토 모드에 기존 대기/걷기 클립 혼합과 0.3초 가속·감속을 구현했다. 1.15m/s, 0.8초당 0.92m 이동, 0.5×와 정지가 이동/애니메이션에 동시에 적용된다. 버튼은 이동 검토/처음 위치·출발·정지이며 약 6m에서 자동 정지한다.
- 자동 검사: 가속/감속, 정상 속도 거리, 정지 후 무이동, 일시정지, 0.5×, 감속 중 재출발, 경로 끝·클립 복귀, 실제 혼합 메시 변화 통과. Windows 빌드 성공. `Moving_StartHalf`, `Moving_Cruise`, `Moving_StopHalf`, `Moving_Stopped` 캡처를 보존했다. 화면 밖에서도 혼합을 평가하도록 Animator AlwaysAnimate를 사용한다.
- 실제 프로그램 버튼 입력은 미실시이며 사용자 확인 대기. 혼합 중 발 고정이나 별도 출발/정지 클립은 없고 전환 구간의 접지 품질은 미완료다. 다음은 실제 출발/정지 느낌 확인과 필요 시 전환 발 디딤 보완이다. 농가 적용·GitHub 업로드는 하지 않았다.

- 최신 v0.75: 사용자가 v0.74까지 확인했다. 걷기의 지지발을 골반 움직임 이후에 재계산하고 실제 신발 정점으로 접지 높이를 맞췄다. 지지 구간 등속·회수 구간 연속 곡선, 몸 상하 극값 ±0.030m로 보완했다. 걷기 0.8초와 승인된 팔 각도·외형·대기·네 발 동작은 유지한다.
- 수정 전 측정: 신발 바닥 최저 -0.00530m, 가상 전진 보상 지지발 이동 0.04089m. 수정 전 결과는 `AdultRabbitMotionBeforeV075.txt`, 캡처는 `Captures/AdultRabbitMotionBeforeV075`에 보존했다. 최신 수치는 `AdultRabbitMotionVerification.txt`를 따른다.
- 수정 후 자동 측정: 최저 바닥 높이 +0.00004m, 지지 구간 최대 높이 0.00410m, 보상 지지발 이동 0.00262m. 팔꿈치 90.01~99.99도와 기존 회귀 검사 통과. Windows 빌드 성공, 측면 Passing·비스듬한 Recoil 렌더 확인. 샘플 사이 모든 시점의 무관통이나 실제 플레이 승인을 뜻하지 않는다.
- 실제 입력·연속 재생 품질은 미검증이며 사용자 확인 대기다. 제자리 클립 검증용 가상 속도 1.15m/s이며 농가 실제 속도 연결은 후속이다. 다음은 접지·리듬 확인 후 출발·정지와 실제 이동 연결이다. GitHub 업로드는 하지 않았다.

- 최신 v0.74: 두 발 걷기의 뒤쪽 위팔 회전 30% 확대, 팔꿈치 안쪽 각도 90~100도 계산, 눈 하이라이트를 곡면 밀착 패치로 교체했다. 대기·네 발 동작·걷기 주기·발 궤적·얼굴 비율·카메라 코드는 유지했다. 전체 5,864삼각형·착용 4,604삼각형·재질 6개.
- 외형 및 동작 자동 회귀 검사와 두 Windows 빌드를 수행했다. 눈 확대·측면/정면 동작 렌더를 확인했다. 실제 입력·연속 동작의 관통과 깜빡임 확인은 미실시이며 팔 동작·하이라이트 사용자 확인 대기다. 이전 렌더는 `Captures/AdultRabbitBeforeV074`와 `Captures/AdultRabbitMotionBeforeV074`에 보존했다. GitHub 업로드는 하지 않았다.
- 다음: 뒤로 가는 팔의 범위와 굽힘·하이라이트 외형 확인 후 보행 리듬·접지를 이어서 검토한다.

- 최신 v0.73: 주둥이 추가 돌출 50% 축소, 눈·코를 실제 얼굴 표면에 약 절반 매립하고 표면 방향으로 회전, 하이라이트 재배치, 입선 제거. 전체 5,888삼각형·착용 4,628삼각형·재질 6개. `AdultStandard_v2`와 애니메이션 타이밍·카메라 조작은 유지했다.
- 외형 및 동작 재생성·회귀 검사·Windows 빌드 종료 코드 0 확인. 정면·측면·비스듬한 얼굴 확대 렌더를 확인했다. 수정 전은 `Captures/AdultRabbitBeforeV073`, 최신 확대는 `Captures/AdultRabbit/FaceFront.png`, `FaceSide.png`, `FaceQuarter.png`다. 두 검토 프로그램을 갱신했으며 실제 입력은 미실시, 얼굴 외형 사용자 확인 대기다. GitHub 업로드는 하지 않았다.
- 다음: 얼굴 윤곽과 눈·코 돌출의 사용자 확인 후 동작 리듬·접지 검토를 이어간다.

- 최신 v0.72: 동작 검토의 카메라 입력 누락, 걷기 앞뒤 궤적 반전, 다음 속도를 표시하던 배속 버튼을 수정했다. 왼쪽 패닝·오른쪽 회전·가운데 높이·휠 줌·Q/E·Home, 구도 선택, 8포즈 정지, 현재 배속·시간 표시를 제공한다. 팔 위상도 전진 보행과 맞췄다.
- 자동 검증은 양쪽 지지발의 전방→후방 이동, 1×/0.5× 시간 증가량·일시정지·클립 전환, 카메라 함수 호출을 추가했다. 실제 입력 검증과 구분한다. 기존 v0.71의 검증은 이 세 사용자 보고 문제를 검출하지 못했다.
- Windows 실제 화면 검증은 computer-use 캡처가 두 차례 `SetIsBorderRequired failed: 0x80004002`로 실패하여 완료하지 못했다. 실제 마우스/키 입력, 두 배속의 벽시계 기준 반복 시간, 연속 보행 품질은 사용자 확인 대기다. 새 검토 프로그램은 `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`. 농가 적용·GitHub 업로드는 하지 않았다.

- 최신 v0.71: `References/Anim/ref_walking_ani_01.jpg`의 8포즈 걷기 구조를 기준으로 두 발 대기를 3.6초, 두 발 걷기를 30fps 24프레임·0.8초 반복으로 재작업했다. 걷기는 좌우 Contact/Recoil/Passing/High Point, 굽힌 팔의 반대 흔들림, 골반·몸통 변화와 머리 안정, 귀·목도리·배낭의 약한 후행을 포함한다. 네 발 두 클립과 농가 주민·임시 `RabbitMotion`은 유지했다.
- `ref_ani.mp4`는 104바이트이고 `moov atom not found`로 디코딩할 수 없는 불완전한 조각이다. JPG와 사용자 지정 방향을 기준으로 했으며 정상 MP4가 추가되면 별도 비교한다.
- 자동 검사: 두 발 대기 3.60초·최대 변형 0.045m, 걷기 0.80초·0.460m, 네 발 대기 3.00초·0.063m, 깡충 1.00초·0.168m. 네 클립 반복 경계, 8포즈 순서, Contact/Recoil 지지발 높이 편차 0.03m 미만, Passing 발 들림 0.05m 초과를 통과했다. 수정 전 렌더는 `Captures/AdultRabbitMotionBeforeV071`, 새 포즈·정면·측면 렌더는 `Captures/AdultRabbitMotion`에 보존했다.
- Windows 검토 프로그램 `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`를 갱신했다. 자동 수치와 렌더 검토는 통과했지만 실제 연속 재생의 귀여운 리듬·코트/목도리/배낭 관통과 사용자 승인은 별도 대기다. GitHub 업로드는 하지 않았다.

- 최신 v0.70: 외형 다음 단계로 성인 토끼 전용 두 발 대기·총총걸음·네 발 대기·낮은 깡충 이동 네 반복 클립과 별도 `AdultRabbitMotionStudy`를 제작했다. 대기 3초, 이동 1초의 제자리 변형 검토이며 농가 속도·거리·주민 교체는 미적용이다.
- 자동 검사: 네 클립 수입·반복 경계·유효 정점 변형 통과. 두 발 대기 0.028m, 걷기 0.230m, 네 발 대기 0.045m, 깡충 0.214m 최대 변형. 기존 FarmStudy·RabbitMotion은 참조·교체하지 않았다. 최초 네 발 시안이 웅크린 두 발로 보여 손·발 지면 목표 관절 계산으로 보완한 뒤 렌더를 재확인했다.
- Windows 검토 프로그램 `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe` 빌드 성공. 1280×800 창 시작·프로세스 응답·그래픽/입력 초기화를 확인하고 열어두었다. 실제 버튼 조작과 사용자 동작 확인은 별도 대기다. 코트·목도리·배낭의 깊은 굽힘 가중치와 정확한 접지, 이동 속도는 미완료다. 상세는 [AdultRabbitMotionReview.md](AdultRabbitMotionReview.md)와 [AdultRabbitMotionVerification.txt](AdultRabbitMotionVerification.txt)를 따른다. GitHub 업로드는 하지 않았다.

- 최신 v0.69: 상의를 바지보다 길고 넓게 벌어지는 코트형으로 변경하고 밑단 내부 공간·안감·테두리를 구성했다. 얼굴·목도리·배낭·바지·기존 골격은 유지했다. 수정 전 렌더는 `Captures/AdultRabbitBeforeV069`, 낮은 시점은 `Captures/AdultRabbit/CoatHemLow.png`에 보존했다.
- 전체 5,940삼각형·착용 4,680삼각형·재질 6개. 재생성·자동 탈착/복원/호환/반복 생성 검사와 Windows 빌드 종료 코드 0 확인. 정면·측면·낮은 시점 렌더를 확인했다. `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe` 갱신 완료이며 이번 빌드의 실제 실행·입력 검증 및 외형 승인은 대기다. 코트의 동작 중 관통 보완과 물리 흔들림은 미구현이다. GitHub 업로드는 하지 않았다.

- 최신 v0.68: 목도리 목둘레를 위가 좁고 아래가 넓은 원뿔대 형태로 수정했다. 높이·매듭·앞쪽 끝·기타 외형·골격은 유지한다. 외형 사용자 확인 대기이며 아래 v0.67은 이전 기록이다.
- v0.68 재생성·자동 회귀 검사·Windows 빌드 종료 코드 0 확인. 비스듬한 앞쪽 렌더에서 목둘레 경사를 확인했다. `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe` 갱신 완료. 이번 빌드의 실제 실행·입력 검증은 미실시다.

- 최신 v0.67: 사용자 표시선을 따라 귀 노출 길이 약 30% 축소, 기본 몸·상의 어깨 경사 보완, 목도리 띠·매듭·끝 확대를 적용했다. 머리·얼굴·배낭·기존 농가·애니메이션은 유지하고 `AdultStandard_v2` 관절·바인드 골격도 유지했다. 전체 5,948삼각형·착용 4,688삼각형·8스킨 렌더러·재질 6개다.
- v0.67 자동 검사와 Windows 빌드 종료 코드 0·성공 표식 확인. `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe`를 재빌드하고 1280×900으로 시작해 프로세스 응답·그래픽/입력 초기화를 확인했다. 로그는 `Logs/adult-rabbit-v067-player-start.log`이며 실제 입력은 별도 대기다. 검토 창을 열어두었다.
- 자동 탈착·몸 복원·골격 호환·반복 생성 및 상의/목도리/배낭 8가지 조합 검사를 통과했다. 렌더 확인과 실제 입력은 구분하며 외형 사용자 확인 대기다. 수정 전 캡처는 `Captures/AdultRabbitBeforeV067`에 보존했다. 세부 내용과 남은 확인은 [AdultRabbitAdjustment.md](AdultRabbitAdjustment.md)를 따른다. 아래 v0.66 이하는 역사 기록이다.

- 최신 v0.66: 초안은 사용자 미승인으로 남기고 머리·턱·주둥이·귀·어깨·소매·목수건·배낭을 재제작했다. `AdultStandard_v2` 골격과 새 바인드 정보로 의상을 재생성하고 이전 v1 규격을 거부한다. 전체 5,892삼각형·착용 4,680삼각형·8스킨 렌더러·재질 6개다. 이전 수치는 아래 역사 기록이다.
- v0.66 검토 프로그램 재빌드 종료 코드 0과 `ADULT_RABBIT_UNITY_OK`·`ADULT_RABBIT_PLAYER_OK` 확인. `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe`를 1280×900으로 실행해 프로세스 응답·창 제목·그래픽/입력 초기화를 확인했다. 이번 시작 로그는 `Logs/adult-rabbit-v2-player-start.log`다. 실제 화면 입력은 미검증이며 검토 창을 열어두었다.
- 자동 탈착·몸 복원·v1/다른 체형 거부·별도 v2 골격 재연결·반복 생성 검사 통과. 정면·측면·후면·작은 화면·배낭 확대·정적 자세 렌더를 확인했다. 목수건과 곡면 덮개는 드러나지만 깊은 굽힘의 간섭·정확한 발 접지는 후속이다. 전후 비교는 [AdultRabbitRedesign.md](AdultRabbitRedesign.md). 디자인 승인과 실제 프로그램 입력은 별도 대기이며 GitHub 업로드는 하지 않았다.

- 성인 토끼 전용 Windows 검토 프로그램: `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe`. 최종 자동 검사·빌드 종료 코드 0과 `ADULT_RABBIT_UNITY_OK` / `ADULT_RABBIT_PLAYER_OK`를 확인했다. 1280×900 창 실행·프로세스 응답·그래픽 초기화를 확인했으며 실제 버튼 조작과 화면 디자인 승인은 사용자 확인 대기다. 로그는 `Logs/adult-rabbit-unity.log`, `Logs/adult-rabbit-player-build.log`, `Logs/adult-rabbit-player-start.log`에 보존한다. 실행 파일은 Git 제외 로컬 산출물이다.
- 2026-09-22 성인 토끼를 참고 이미지에 맞춰 별도 `AdultRabbitStudy`에 제작했다. 상의·하의·신발·목수건·배낭은 독립 슬롯이며 같은 성인 공통 골격에 재연결한다. 갓난아기·어린아이·성인은 별도 체형군으로 기록했고 실제 모델은 성인만 제작했다.
- 전체 몸·장비 5,792삼각형, 전체 착용 시 4,580삼각형·8스킨 렌더러·재질 6개. 단색 기반이며 Subdivision·이미지 텍스처를 사용하지 않았다. 자동 검사와 다각도 렌더 검토 결과는 [AdultRabbitReview.md](AdultRabbitReview.md), [AdultRabbitVerification.txt](AdultRabbitVerification.txt)를 따른다.
- 기존 농가와 토끼 애니메이션은 유지했다. 새 모델의 깊은 굽힘에서 의상·배낭 간섭과 네 발 접지 보완, 실제 입력·디자인 사용자 확인은 남아 있다. 원본 `.blend`·FBX·생성 코드·공용 의상 정의를 보존했으며 이번 변경은 아직 GitHub에 올리지 않았다.

- 2026-09-22 사용자 확정: 캐릭터를 포함한 모델링은 로우폴리 또는 Shade Smooth를 적용한 로우폴리로 제작한다. Subdivision·면 수를 늘리는 MeshSmooth는 사용하지 않는다. 성인 토끼 첫 모델 제작은 위 최신 기록을 따르며 저사양 성능 검증은 미실시다.

- 2026-09-22 이 PC의 Windows 64비트 검토 실행 파일 생성 완료: `Logs/ResidentSelectionPlayer/TinyDaysReview.exe`. 빌드 종료 코드 0과 `Logs/local-review-player-build-20260922.log`의 `RESIDENT_REVIEW_PLAYER_OK`를 확인했다. 데이터 폴더·UnityPlayer.dll·MonoBleedingEdge가 함께 생성됐다.
- 실행 확인: 1280×800 창으로 시작해 `Tiny Days` 창 생성·프로세스 응답 및 그래픽·입력 초기화 로그를 확인했다(`Logs/local-review-player-start-20260922.log`). 화면의 시각적 확인과 실제 사용자 조작 검증은 미실시이며, 검토 창은 열어두었다. 실행 파일은 로컬 산출물이고 이 기록은 아직 GitHub에 업로드하지 않았다.

- 2-7-1 대표 조명과 2-7-2 시간 흐름은 사용자 확인을 마쳤다.
- 2-7-3은 종합 관찰 진행 중이며, 최종 사용자 확인 전에는 완료로 기록하지 않는다.
- 현재 FarmStudy에는 농가 대지, 집 3종, 창고, 밭·쉼터·생활 장식, 임시 토끼 주민 6명이 있다.
- 농가 주민 외형과 이동 애니메이션은 임시 작업물이다. 2026-09-22 사용자 합의로 캐릭터 디자인을 먼저 재개하고 디자인 확인 뒤 애니메이션을 조정한다. 아래 과거 보류 기록보다 이 합의가 우선하며 새 성인 토끼는 별도 검토 장면에서 먼저 확인한다.

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

1. v0.82 Windows 동작 검토에서 출발 첫 세 걸음, 뒷발을 낮게 들어 나란히 모으는 수동/자동 정지, 발 모으기 중 재출발을 0.5×/1×로 확인한다. 코트와 양발 정렬·몸의 안정도 함께 확인한다.
2. 자연스러운 출발·발 모으기 정지 승인 뒤 방향 전환을 계획한다. 농가 적용은 이후 별도 합의하며 현재 농가와 승인된 원본 자산은 유지한다.
3. 후순위인 세로 구도·UI 가독성·상단 입력 차단과 남은 실제 카메라 검증은 유지한다. 2-7-3 완료 및 3단계 착수는 별도 사용자 확인으로 결정한다.

## PC 간 이어가기

- 2026-09-22 v0.82 전달 체크포인트: 성인 토끼 Blender/FBX·생성 코드·외형/동작 검토 장면·검증 결과·전후 이미지·정면/측면 영상과 최신 문서를 GitHub 반영 대상으로 묶었다. 다른 PC에서는 이 체크포인트를 포함한 최신 `main`을 받은 뒤 이어간다. 실제 원격 동기화 여부는 로컬 HEAD와 `origin/main`으로 확인한다.
- `Logs`의 Windows 실행 파일·데이터·캐시는 Git 제외다. 다른 PC에서 `Tools/RebuildAdultRabbitMotion.ps1 -SkipArt`로 다시 생성한다(Unity 2022.3.20f1 및 Windows 빌드 지원 필요, 같은 프로젝트의 Unity 편집기는 먼저 저장 후 닫기). PC 전용 `.lnk` 바로가기도 이번 전달에서 제외했다. 동작 품질의 사용자 확인 대기는 그대로 유지한다.

- 모든 PC에서 상태가 변경된 작업을 마칠 때 이 문서의 최종 갱신일·현재 상태·검증 결과·남은 문제·다음 작업을 갱신한다. 중단·실패·사용자 확인 대기도 포함한다. 기획 변경은 MasterPlan에도 반영한다.
- 2026-09-22 이 PC에서 원격 커밋 `926c734` 다운로드 및 작업 트리 정리를 확인했다. 저장소 루트와 본게임 AGENTS.md에 진행 기록 갱신 의무를 명시했다. 이번 작업은 문서 운영 규칙 설정이며 현재 2-7-3 진행 상태는 유지한다.
- 위 운영 규칙 변경은 현재 로컬 저장 상태다. 다음 GitHub 업로드 때 함께 반영하고, 다른 PC에서는 최신 main을 받아 적용한다. 자동 업로드는 설정하지 않았다.

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
