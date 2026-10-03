# 새 기본 성인 토끼 모델 v0.124

최종 갱신: 2026-10-02

## 기준과 재개

- 기본 원본은 `ArtSource/AdultRabbit - 01.blend`. 사용자가 수정한 납작한 얼굴 아래쪽과 동그란 91.8mm 눈을 유지한다. 새 원본에는 액션이 없으며 기존 작업 파일의 애니메이션을 재사용한다.
- `Tools/RebuildCanonicalRabbit.ps1`은 메시 반영 → Unity 검사 → 외형/동작/생활 Windows 빌드 순서다. 아트 반영을 마쳤으면 `-SkipArt`로 검사/빌드만 실행한다. 기존 동작을 다시 작성하는 `animate_adult_rabbit.py`와 달리 메시 반영 도구는 액션을 생성하지 않는다.
- `AdultRabbit.blend`는 기본 외형의 파생 호환 파일, Motion/Home/Water/Bench는 각 동작의 파생 작업 파일이다. 기존 FBX 경로와 `.meta`를 유지한다. 소품과 수동 장면 영역은 보존한다.
- 백업: `ArtSource/Backups/CanonicalRabbit20261002`. 사용자 원본과 파생 Blender/FBX를 첫 실행 전에 보존하며 재실행은 백업을 덮어쓰지 않는다.

## 검증 구분

- `CanonicalRabbitVerification.json`은 원본 SHA256 보존, 각 작업 파일의 액션 곡선/키 핸들/길이와 바인드 보존, 메시 가중치/토폴로지 동일 및 반복 메시 반영을 검사한다.
- Unity에서는 외형 예산/탈착, 기존 동작, 눈 감기, 출입/물 주기/벤치와 접지를 재검사한다. 렌더와 프로그램 빌드는 실제 OS 입력 또는 사용자 품질 승인과 구분한다.
- 원본 수정에 따른 얼굴 실루엣 변화는 의도된 변경이다. 상의–다리 미세 보완, 옛 농가·구형 검토 장면 교체와 GitHub 업로드는 하지 않는다.
- 최종 실행 결과는 Progress를 따른다. **새 기본 토끼 모델 적용 사용자 확인 대기**로 전달한다.

## 최종 결과

- 원본 SHA 보존/12개 모듈 가중치·바인드/반복 반영 통과. `verify_canonical_rabbit.py`로 저장된5개 파일을 재열어 메시와 `Basis`/`SleepEyesClosed`의 기본 원본 일치를 확인했다. 새 원본 최고점2.172411m를 `CanonicalSource.audit.json`에서 읽어 Unity 높이 검사에 사용한다.
- 파생 Blender에서 비활성 작업 클립이 저장되지 않던 기존 문제를 발견해, 첫 백업 FBX의 해당 본 애니메이션을 복구했다. 얼굴용 클립과 본 클립을 구분하며, 모든 작업 클립을 fake-user로 보존한다. 기존36개 액션은 변경되지 않았고 Motion36/Home37/Water39/Bench39개를 유지한다. `restore_home_gesture.py`/`restore_work_clips.py`는 이 체크포인트의 일회성 복구 도구다.
- FBX5개 GUID 유지, Unity Head5개 일치/눈 감기 유지, 외형5836삼각형/6재질·탈착·기존 동작·출입/물 주기/벤치 회귀 성공. 열린 PNG로 인한 캡처 저장 실패는 이전 이미지를 보존하는 별도 이름 저장으로 해결했다.
- 외형/동작 빌드 후 생활만 `CanonicalRabbitUpdate.ResumeLife`로 재개하여 최종 빌드 성공. `Logs/canonical-life-final.log`의 `CANONICAL_RABBIT_LIFE_OK`, 그래픽 플레이어4개 종료0와 성공 표식 확인. 직접 마우스/키보드 입력과 사용자 품질은 미확인이다.
- 대표 포즈: `Captures/CanonicalRabbit/Clip0.png` 등8장. 실제 생활 플레이어: `Captures/CanonicalRabbit/PlayerBench.png`. 벤치 연속 프레임도 새 모델로 갱신했다. 기존 압축 영상은 과거 버전의 자료이며 이번 최신 모습의 근거는 새 렌더/실행 파일이다.
