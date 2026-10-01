# v0.113 코트 사각 주머니 제거

2026-09-30. 누운 자세에서 옷과 분리되어 보이던 흰 조각은 별도 사각 장식이며 Spine 하나에 가중치가 연결되어 있었다.

- 생성 코드에서 장식 생성을 제거하고 기존 외형/동작 Blender 원본에서 해당24정점·28삼각형만 삭제했다. 나머지 정점/가중치/면 속성·다른 메시·바인드·모든 액션은 정확히 보존됐다(`AdultPocketPreservation.txt`). 이전 원본은 `Logs/BeforePocketRemoval`에 보존한다.
- `Tools/remove_adult_pocket.py`는 기존 원본을 위한 일회성 마이그레이션이다. 이후 새 원본 생성은 수정된 `generate_adult_rabbit.py`를 사용한다. 이미 제거된 원본에 마이그레이션을 반복 실행하지 않는다.
- 앞으로 의상 표면에 붙는 장식은 주변 옷감의 가중치와 실행용 흔들림을 함께 따르게 한다. 대표 네 자세에서 분리를 확인하며 상의–다리 관통 미세 보완은 추가하지 않는다.
- 동일 구도의 전후 이미지는 `Captures/PocketRemoval/Before_22.png`, `After_22.png`(바로 누움), `Before_28.png`, `After_28.png`(옆누움)에 저장한다.

Unity 검증과 두 Windows 빌드 종료0 (`Logs/pocket-unity.log`). 전체5,836삼각형/재질6개, 의상 탈착3회·몸 복원·비호환 거부·별도 골격 재바인드 검사 통과. 직접 복귀576조합/32개 요청 검사도 통과했다. 두 구도의 전후 이미지를 직접 확인했고 흰 장식이 제거됐다.

외형 실행 파일 `Logs/AdultRabbitPlayer/TinyDaysAdultRabbit.exe`의 콘텐츠 데이터 갱신16:35:13, 동작 `Logs/AdultRabbitMotionPlayer/TinyDaysAdultRabbitMotion.exe`는16:36:40이다. 동작 실행 패널 자동 검사도 종료0/REVIEW_PANEL_OK (`Logs/pocket-player.log`). 실제 OS 마우스·키보드 조작과 사용자 외형 확인은 아직 대기다. 기존34개 동작의 사용자 품질 승인을 추정하지 않는다. 농가/GitHub 업로드는 하지 않았다.
