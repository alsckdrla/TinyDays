# Tiny Days — 본게임

작업 기준: [마스터 플랜](Docs/MasterPlan.md) v0.15. 캐릭터 디자인·애니메이션은 임시 사용하고 추가 보완은 보류한다. 현재는 **2-6 농가·임시 생활 장면 사용자 확인 대기**이다. 2-3~2-5는 완료 처리하지 않는다.

## 농가 장면 열기

Unity 2022.3.20f1 메뉴 **Tiny Days → Stage 2-6 → Open farm review**에서 `Assets/Scenes/FarmStudy.unity`를 열고 Play를 누른다. 집 2채·창고·텃밭·쉼터 사이로 임시 토끼 6명이 이동하고 머무른다. 실제 생산·자원·피로 판단은 아직 없다.

하단 검토 버튼은 일시정지/재생, 처음부터, 전체/가까이 보기, 네 고정 구도, 다음 주민, 메뉴 숨김이다. 숨긴 메뉴는 왼쪽 위에서 다시 표시한다. 가까이 보기는 선택한 주민을 따라가며 자유 카메라의 최종 조작이 아니다. 처음부터는 주민들의 서로 다른 시작 시점으로 돌아가고 일시정지 상태를 유지한다.

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
