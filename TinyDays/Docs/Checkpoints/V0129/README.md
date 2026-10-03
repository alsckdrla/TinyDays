# v0.129 다른 PC 재개 체크포인트

2026-10-03. 이 체크포인트는 **미완료 소스 저장**이며 완성 빌드가 아니다.

## 읽을 순서와 실행 환경

루트/프로젝트 AGENTS → Docs/MasterPlan → Docs/Progress → Docs/RabbitNaturalWalkReview 순으로 읽는다. Unity 2022.3.20f1에서 `Assets/Scenes/RabbitHomeLifeStudy.unity`를 사용한다. 원본은 `ArtSource/AdultRabbit - 01.blend`이며 생성 결과로 덮어쓰지 않는다. 현재 소스를 빌드하면 기존 v0.128 프로그램과 같지 않다.

현재 PC의 `Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe`는 v0.128이며 Git 대상이 아니다. 다른 PC로 실행 파일이 내려받아지지 않는다. 이번 저장 작업에서는 Unity/Blender 생성, 테스트 재실행, Windows 빌드를 하지 않는다.

## 먼저 해결할 것

1. 착지 경계 골반 높이 튐 약28.5mm/240Hz 표본, 지지 무릎 추가 굽힘 약10.213도. 골반 추가 하강 약8mm만으로 완료 처리하지 않는다.
2. 새 접근 방향에서 벤치로 출발할 때 보관 소품과 신발이 겹친다.
3. 반대편에서 내려놓으며 양손을 붙인 채 소품을180도 돌리면 도달 초과/팔 교차가 생긴다. 바닥에 놓고 손을 떼는 동안 원래 방향으로 자동 정리하는 안은 **사용자 답변 대기**다. 업로드 승인은 동작 변경 승인과 다르다.
4. 이후8방향 집기/장애물/일반·운반 회전/중단·재개/전체 생활 흐름 회귀와 시각 확인, Windows 빌드를 진행한다. 기존 PASS 파일은 당시 코드의 기록일 뿐 최신 소스 완료 근거가 아니다.

## 보존 자료와 재검사

- `BeforeV129/`: v0.129 작업 전 생활 보행 관련4개 소스와 당시 진단 CSV의 원본 복사본. 현재 소스를 자동 덮어쓰는 용도가 아니며 완전한 v0.128 저장소 스냅샷도 아니다.
- `SelectedDiagnostics.txt`: Unity 전체 로그에서 관련 결과/실패 행만 발췌했다. 환경·라이선스 출력과 캐시는 제외했다. 각 파일은 서로 다른 실험 단계이므로 최신 전체 회귀로 합치지 않는다.
- 수정 전1× 영상은 `Docs/Captures/RabbitNaturalWalk/BeforeV128`에 있다.
- 진단 진입점: `RabbitNaturalWalkChecks.Diagnose`, 집기 검사: `RabbitNaturalWalkChecks.Pickup`. Diagnose의 Finished는 흐름 종료 표시이며 자세 PASS가 아니다. 마지막 실험 CSV를 walk11 결과로 오인하지 않는다.
- `Tools/RebuildRabbitWalkingTurn.ps1`은 기존 회귀·렌더·Windows 빌드 도구다. 새 집기/착지 검사를 해결하고 통합한 뒤 사용한다. 이 스크립트만 통과해도 v0.129 전체 완료가 되는 것은 아니다. Unity를 닫고 실행하며 해당 PC의 Unity/Blender 설치 경로를 확인한다.

GitHub 푸시 성공과 SHA는 실행 후 별도 확인한다. 본 체크포인트는 소스/자료 보존을 설명하며 동작 품질 승인·실제 프로그램 입력 검증을 주장하지 않는다.
