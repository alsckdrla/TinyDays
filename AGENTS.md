# Tiny Days 저장소 작업 재개 규칙

- 본게임 작업 전 `TinyDays/AGENTS.md`, `TinyDays/Docs/MasterPlan.md`, `TinyDays/Docs/Progress.md`와 현재 단계 관찰 문서를 읽는다. 기획 기준은 MasterPlan, 최신 진행 상태와 다음 작업은 Progress를 따른다.
- 구현·검증·합의 또는 미완료 상태가 달라진 작업을 마칠 때는 `TinyDays/Docs/Progress.md`의 최종 갱신일, 현재 상태, 검증 결과, 남은 문제, 다음 작업 순서를 실제 결과에 맞춰 갱신한다. 중단·실패·사용자 확인 대기도 기록한다. 변경 없는 단순 질의에는 기록을 반복하지 않는다.
- 다른 PC에서도 위 규칙을 적용한다. GitHub 업로드 요청 시 코드·필요 자산과 최신 진행 문서를 함께 커밋·푸시하고 원격 반영을 확인한다. 로컬 저장과 GitHub 동기화 완료를 구분해 보고한다.
- GitHub 업데이트마다 `Docs/References` 전체와 최신 MasterPlan·Progress, 변경된 루트/프로젝트 AGENTS.md를 포함해 누락을 검사한다. 현재 상황은 Progress, 기획 기준은 MasterPlan, 작업 규칙은 AGENTS.md에 기록한다. 규칙이 달라졌을 때 AGENTS.md도 갱신하며 변경 없는 규칙을 형식적으로 다시 쓰지 않는다. Logs 실행 파일·캐시와 PC 전용 .lnk는 로컬에 보존한다.
- 프로토타입 작업은 별도 요청 범위에서 해당 프로젝트의 AGENTS.md와 Docs/MasterPlan.md, Docs/Progress.md를 기준으로 진행한다.
- 성인 토끼 기본 모델은 `TinyDays/ArtSource/AdultRabbit - 01.blend`다. 사용자 원본을 생성 결과로 덮어쓰지 않는다. `AdultRabbit*.blend`의 나머지 파일은 파생 작업 파일이며 기본 모델 반영은 `TinyDays/Tools/RebuildCanonicalRabbit.ps1`을 사용한다. 구형 농가·RabbitStudy/RabbitMotion은 비교용 보존 자료이며 신규 제작 기준이 아니다.
