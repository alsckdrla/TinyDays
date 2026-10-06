# Tiny Days — 본게임

현재 재개 기준은 [MasterPlan](Docs/MasterPlan.md) v0.147과 [Progress](Docs/Progress.md)다. 최신 작업은 **4-4 닭 돌봄·달걀 생산**이며 `AutonomousLifeStudy`를 사용한다.3-2 전체 흐름은 사용자 확인을 마쳤다. 외형·동작의 미세 보완은 보류하고 기존 개별 생활 검토 `RabbitHomeLifeStudy`와 농가 비교 장면은 보존한다. 상세 [현재 자율 생활 검토](Docs/AutonomousLife147Review.md).

## 자율 생활 검토 실행 (v0.147)

`Logs/AutonomousLifePlayer/TinyDaysAutonomousLife.exe`를 실행한다. 같은 폴더의 Data·DLL·MonoBleedingEdge도 함께 유지한다. 저장이 없으면 기본 하루10분·1배속·Day1 12시로 시작하며 토끼3명이 채집·농사·운반·식사·휴식·밤 귀가를 선택한다. 주민을 선택하면 현재 행동과 이유를 볼 수 있다. **실제 1분마다·정상 종료 시 자동 저장하고, 재실행하면 이어간다. Esc에서 저장·불러오기를 사용할 수 있다. 처음부터는 확인 후 새 농가로 시작한다.**

다른 PC에서는 최신 소스를 받은 뒤 `Assets/Scenes/AutonomousLifeStudy.unity`를 열거나, Unity2022.3.20f1 배치의 `-executeMethod AutonomousLifeBuilder.BuildPlayer`로 기존 장면의 실행 파일만 빌드한다. 출력은 위 경로이며 성공 표식은 `LIFE140_PLAYER_OK`다. 이 명령은 원본 모델과 장면을 재생성하지 않는다. Logs 실행 파일은 Git 제외이므로 PC마다 재빌드한다.

닭3마리와 닭장·마당이 추가됐다. 주민이 공동 식량을 먹이로 가져오고, 닭이 먹은 뒤 반나절에 달걀을 낳으면 최대3개씩 수거한다. 비·눈·밤에는 닭장으로 들어간다. 기존142/144/145/146 저장의 생활을 유지하며147로 이어간다. 모든 수치와 닭 외형/동작은 검토용이며 사용자 확인 대기다.

주민들이 쉼터에서 인사하고, 읽을 수 있는 대사 없이 `…` 표시로 짧게 대화한 뒤 함께 쉰다. 급한 식사·귀가·날씨 대응을 우선하며 혼자 쉬기도 유지한다. 교류 중 저장/이어하기를 지원하고 기존142/144/145 저장을 유지한다. 몸짓과 빈도는 검토용이다.

사계절은 계절당3일(하루10분 기준30분)로 진행한다. `계절`에서 직접 고정하거나 자동 순환한다. 겨울은 성장50%·일찍 휴식·긴 여가를 사용하며 비 구간은 눈으로 표현한다. 이 값과 눈 덮임은 검토용이다. 기존142/144 저장은 생활을 유지하면서 봄·자동으로 확장한다.

맑음·흐림·비 날씨 검토가 추가됐다. `날씨` 버튼에서 고정 날씨/자동 순환을 선택한다. 날씨는 생활과 같은 배속·정지를 사용한다. 비가 오면 작업과 운반을 정리하고 처마 아래에서 쉬며, 긴급 식량 부족에는 생산을 우선한다. 기존 저장도 이어갈 수 있고 처음부터는 맑음·자동으로 시작한다.

주민1은 농사 선호,2는 채집 선호,3은 느긋한 임시 성향이다. 주민을 선택하면 성향·행동·이유가 표시된다. 급한 필요에는 성향보다 공동 식량·휴식·귀가를 우선하며, 나무와 밭 주변에서 여가를 보낸다. 중단된 운반 물품은 바닥 바구니로 보존하고 회수한다. 이 성향과 수치는 최종 직업·성격·밸런스가 아니다.

검토 조작은 기존 농가 카메라와 하단 메뉴를 사용한다. `시간대 비교`에서 하루 길이와 배속을 변경할 수 있으며 일시정지는 생활과 시간을 함께 멈춘다. 시간대 고정은 조명 시각만 고정하고 생활은 계속된다. 처음부터는 진행을 초기화하면서 하루 길이·배속·일시정지 여부를 유지한다. 농사/식사 몸짓과 실내 귀가는 임시 표현이며 세부 동작의 최종 승인을 뜻하지 않는다.

생활·동작·외형·농가·집 검토 공통 조작: **왼쪽 드래그 회전 / 오른쪽 드래그 패닝 / WASD·화살표 수평 이동 / Q 하강·E 상승**. W/↑ 전방, S/↓ 후방, A/← 좌측, D/→ 우측이며 카메라 방향을 따른다. 키 이동 속도는 현재 중심점 거리×0.5m/s(최소 계산 거리1m)로 가까우면 느리고 멀면 빠르다. 대각선과 중복 키는 더 빨라지지 않는다. 기존 휠·마우스·Home은 유지한다. 키 이동은 재생 일시정지/배속과 무관하고 패널 위에서도 가능하며, 텍스트 입력·설정/색상창·포커스 상실에는 차단한다. 상세 [거리별 카메라 검토](Docs/ReviewDistance137.md).

## 기존 개별 생활 검토 재개 (v0.138 · 보존 자료)

최신 `main`을 받은 뒤 AGENTS → MasterPlan → Progress → [경로 성능 검토](Docs/RabbitRoute138Review.md)를 읽고 `Assets/Scenes/RabbitHomeLifeStudy.unity`를 사용한다. 기본 원본은 `ArtSource/AdultRabbit - 01.blend`다. 참고 파일28개와 비교/검증 자료는 저장소에 포함하며 사용자 품질 확인 대기는 유지한다.

실행 파일·캐시·PC 전용 바로가기는 업로드하지 않는다. Unity Editor를 닫은 상태에서 PowerShell로 아래를 실행한다. 프로젝트 경로만 해당 PC의 실제 위치로 바꾸며, 이 명령은 기존 자산을 다시 생성하지 않고 생활 프로그램만 빌드한다.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/2022.3.20f1/Editor/Unity.exe' -batchmode -projectPath 'D:/mc da kim/Codex/TinyDays/TinyDays' -executeMethod RabbitHomeLifeBuilder.BuildPlayer -quit -logFile 'D:/mc da kim/Codex/TinyDays/TinyDays/Logs/home-life-rebuild.log'
```

빌드 로그의 `RABBIT_HOME_PLAYER_OK`와 성공 결과를 확인한다. 출력은 `Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe`이며 실행할 때 같은 폴더의 데이터와 DLL도 함께 유지한다.

아래는 구형 농가 검토의 v0.46 당시 설명이며 현재 단계·기준은 문서 상단의 v0.142를 따른다. 구형 농가는 낮12시·하루5분·1배속으로 시작한다. `시간대 비교` 하단에서 하루 길이1~120분을 입력하거나5·10·20·30분 버튼을 누른다.0.5·1·2·4배속은 주민과 자동 낮밤에 함께 적용한다. 자율 생활 장면의 기본 하루는10분이다.

특정 시간을 고정하거나 `자동 순환`으로 현재 시각부터 재개한다. 색상표 편집은 해당 시간에 고정하며 확인하면 색상을 저장하고 취소/Esc는 편집 전 색으로 돌아간다. 일시정지는 주민과 시간을 함께 멈춘다. 처음부터는 현재 하루 길이·배속·정지 여부를 유지하고 낮 12시 자동 모드로 돌아간다. 캐릭터 보완은 보류 상태다.

## 농가 장면 열기

Unity 2022.3.20f1 메뉴 **Tiny Days → Stage 2-6 → Open farm review**에서 `Assets/Scenes/FarmStudy.unity`를 열고 Play를 누른다. 목조집·노란 회벽집·짚지붕집과 오른쪽 창고, 텃밭·쉼터 사이로 임시 토끼 6명이 이동하고 머무른다. 실제 생산·자원·피로 판단은 아직 없다.

하단 검토 버튼은 일시정지/재생, 처음부터, 전체 보기, 네 구도, 주민 목록, 메뉴 숨김이다. 주민을 클릭하면 선택하고 더블클릭하면 따라본다. 주민 목록을 열면 전체 번호가 표시되며 목록·주민·번호를 한 번 클릭해 따라볼 수 있다. 선택 후 목록을 닫고 선택한 번호만 유지한다. 포커싱 중에도 왼쪽 회전·오른쪽 패닝·WASD/화살표·Q/E·가운데 높이·휠 줌을 유지하며 이동 오프셋을 적용해 주민을 계속 따라간다. 전체 보기/Home은 오프셋·주민 선택·목록·번호를 해제하고 기본 전체 구도로 돌아간다. 메뉴 숨김은 목록·번호도 숨기고 오른쪽 하단의 같은 위치·크기 버튼으로 복원한다.

전체/자유 보기에서는 주민 가림으로 투명화하지 않는다. 따라보는 주민 앞의 장애물과 카메라 내부·지하 진입 조건을 독립적으로 반투명 처리한다. 이번 검증은 `Docs/ResidentOcclusionVerification.txt`와 `Docs/ResidentSelectionObservation.md`에 기록한다.

Esc는 그림자 품질 설정을 열고 닫는다. 낮음(선명한 경계), 기본(부드러운 경계·기본값), 높음(더 부드러운 경계)을 선택할 수 있으며, 모든 품질은 현재 최대 줌아웃까지 그림자를 유지한다. 설정이 열린 동안 카메라와 주민 입력은 차단되고 선택값은 실행 후에도 유지된다.

`Tools/RebuildFarmStudy.ps1`은 기존 캐릭터를 재생성하지 않고 농가만 생성·검사한다. 환경 메시·재질은 `Assets/Art/Generated/FarmStudy`, 생성 소스는 `Assets/Editor/FarmStudyBuilder.cs`다. 장면의 `GeneratedFarmStudy`만 갱신하고 `ManualEdits`를 보존한다. 환경은 Unity에서 생성한 검토 자산이므로 별도 Blender 원본은 없으며 기존 캐릭터 Blender 원본은 그대로 보존한다.

주민 위치·활동은 `FarmLifeDirector`, 모델과 기존 클립 재생은 `FarmResidentVisual`, 검토 화면은 `FarmStudyReview`로 분리했다. 캐릭터 교체 시 모델·Animator·클립 연결과 접지 크기를 다시 확인해야 한다. 현재 경로·대기 시간·주민 크기는 장면 검토값이다.

자동 검사 기록은 `Docs/Stage26Verification.txt`, 실제 화면 기록은 `Docs/Stage26Observation.md`다. `Layout-*.png`는 자동 구도 렌더이며 실제 Game 창 입력 검사와 구분한다.

## 기존 캐릭터 장면 — 보존 자료

이하 기존 검토 기능을 보존한다. 추가 디자인·애니메이션 작업은 재개 합의 전 수행하지 않는다.

## 열기와 확인

현재 동작 검토는 Unity 메뉴 **Tiny Days → Stage 2-3 → Open motion review** 또는 `Assets/Scenes/RabbitMotionStudy.unity`를 열어 Play 모드로 확인한다. **연결 장면 재생**은 두 발 대기 → 걷기 출발/이동/정지 → 네 발 전환/대기 → 깡충 출발/이동/정지 → 두 발 전환/대기 순서로 약 16.4초 동안 한 번 진행하고 마지막 대기를 유지한다.

상단에서 10개 개별 동작도 선택할 수 있다. 개별 선택은 시작 위치·자세를 초기화하며 연결 장면의 연속 이동과 구분한다. 일시정지·처음부터·0.5×/1× 재생·가까이/작게 보기·네 가지 고정 구도를 제공한다. 정지 상태에서 처음부터를 누르면 0초로 돌아가며, **재생**을 눌러 진행한다. 검토 도구이며 본게임 조작 확정이 아니다.

기존 정지 외형 비교는 아래와 같이 확인한다.

1. Unity 2022.3.20f1에서 이 폴더를 열거나 `Tools/OpenUnity.ps1`을 실행한다.
2. `Assets/Scenes/RabbitStudy.unity` 장면을 연다.
3. Game 창에서 두 발·네 발 기본 자세를 비교한다. 두 토끼는 같은 모델의 자세 비교용이며 게임 주민 2명으로 확정한 것이 아니다.
4. 정면·측면·후면·비스듬한 구도는 `Docs/Captures/Stage22`에서 비교한다. 본게임 카메라 조작은 아직 구현하지 않았다.

## 제작 파일

- `ArtSource/RabbitStudy.blend`: 관절·작업복·두 정지 자세와 Blender 확인용 조명.
- `Tools/generate_rabbit.py`: 원본 생성 및 FBX 출력. `Pose_Biped`와 `Pose_Quadruped`는 각각 정지 자세이고 보행 클립이 아니다.
- `Assets/Art/Generated/RabbitStudy.fbx`: 하나의 스킨 메시, Generic 관절 및 두 정지 자세.
- `Assets/Art/Generated/Prefabs`: 자세별 외형 검토 프리팹.
- `Assets/Settings`: URP 14.0.10의 3D Renderer와 파이프라인 설정.
- `Docs/Stage22Verification.txt`: 자동 검사 결과.
- `Docs/Stage22Observation.md`: 실제 Game 창 관찰 결과와 남은 확인.

## 재생성과 수동 작업

**2-3 이동·자세 전환:** `Tools/RebuildRabbitMotion.ps1`은 승인한 `ArtSource/RabbitStudy.blend`를 읽어 별도의 `ArtSource/RabbitMotion.blend`와 `Assets/Art/Generated/RabbitMotion.fbx`를 만들고 동작 장면·검증 기록을 갱신한다. 기존 정지 외형 원본·FBX·장면은 덮어쓰지 않는다. 동작 장면에서 생성 영역은 `GeneratedMotionReview`, 수동 영역은 `ManualEdits`다. `-SkipArt`는 동작 원본 생성을 생략한다. 접지 검사는 Python 3 표준 라이브러리를 사용한다. 기록은 `Docs/Stage23Verification.txt`, `Docs/Stage23ContactVerification.txt`, `Docs/Stage23Observation.md`에서 구분한다. 자세 전환 추가 전 자료는 `Docs/Captures/Stage23/BeforeTransitions`, 탄력 보완 전 자료는 `Docs/Captures/Stage23/BeforeElasticRevision`에 보존했다.

`Tools/RebuildRabbit.ps1`은 Blender 생성 후 Unity 가져오기·장면 생성·검사를 실행한다. `-SkipArt`는 Blender를 건너뛰고, `-SkipBlenderRenders`는 Blender 비교 PNG만 생략한다. 프로젝트가 Unity에 열려 있으면 배치 재생성 전에 직접 닫는다.

자동 생성기는 토끼 원본·Generated 자산 및 장면의 `GeneratedReview`만 관리한다. **수동 자산은 `Assets/Art/Manual`, 수동 장면 편집은 `ManualEdits`에 둔다.** 원본을 직접 다듬으려면 별도 이름으로 복사해 수동 원본으로 보존한다. Generated 프리팹이나 생성 원본의 직접 수정은 재생성 시 교체될 수 있다.

Unity 프로젝트와 .meta, Packages의 manifest/lock, ProjectSettings, 원본·스크립트·문서·검토 이미지를 보존한다. Library·Temp·Logs·UserSettings는 로컬 생성물이다. 기존 TinyDaysPrototype과 저장 파일은 사용하지 않는다.
