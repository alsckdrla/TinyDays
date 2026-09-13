# Tiny Days 작업 규칙

- 파일 수정 전 요청 내용을 정리하고 다음 작업과 권장 모델을 사용자에게 알린다. 실제 모델을 변경하지 않았다면 변경했다고 말하지 않는다.
- 작업 후 `작업: … | 확인: … | 다음: …` 한 줄로 보고한다. 확인할 사항이 없으면 확인 칸은 비워 둔다.
- 현재 범위는 1단계이며, 사용자 시각적 확인 전 2단계로 넘어가지 않는다.
- Docs/PrototypePlan.md, Docs/Decisions.md, Docs/Progress.md를 먼저 확인하고 관련 항목을 갱신한다.
- 생성 전용 Assets/Art/Generated와 GeneratedVillage 외의 수동 작업을 보존한다. Unity/Blender 버전을 임의로 올리지 않는다.
- Blender는 --background --factory-startup --python-exit-code 1로 실행한다. 사용자 애드온/환경설정은 수정하지 않는다.
- 원본 .blend, Unity 소스, 생성 스크립트를 남긴다. 실제 실행 검증과 미검증을 구분하고 오류가 남으면 완료라고 하지 않는다.
- 유료 에셋 구매·외부 연결·업로드·원격 저장소 생성은 별도 사용자 요청 없이 하지 않는다. 로컬 Git 기록만 사용한다.
