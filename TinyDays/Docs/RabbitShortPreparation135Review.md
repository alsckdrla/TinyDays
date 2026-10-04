# v0.135 짧은 대각선 디딤·벤치 준비

갱신: 2026-10-04. 구현·최종 검사·렌더·Windows 빌드 완료. **짧은 대각선 디딤·벤치 준비 사용자 확인 대기**.

## 수정

- v0.134는 반복9cm 뒤걸음을 없앴지만 첫발부터30cm 끝점을 향해 뻗었다. 물뿌리개 대각선 회피도 같은 공통 계산을 사용했다. 이번에는 준비에만 단계별 착지를 적용한다.
- 이동15cm 이하·회전45도 이하이면 두 걸음0.8초, 그보다 크면 세 걸음1.2초다. 첫발 중간 착지→반대 발 도착→첫발 정리이며 몸은 첫0.8초 동안 이동하고 마지막에 안정한다. 횟수를 계속 늘리거나 준비를 재시작하지 않는다.
- 기존 `BeginExactPair`는 유지해 집기 준비에 변경이 전파되지 않게 한다. 새 `BeginPreparation`은 걸음 수/시간과 중간 착지를 관리한다. 회피 후보는 실제 준비 포즈의 신체/신발 피부 정점을 표본 검사한 뒤 선택한다. 지지발 고정·목/관절 길이·메시/골격은 유지한다.
- 취소는 진행 중인 안전 준비를 마친 뒤 대기하고 재개는 현재 자세에서 이어간다. 이미 준비점이면 걸음을 생략한다. 원본 걷기·착석/기립·카메라·호흡/깜빡임은 보존한다.

## 검사와 전달

- `RabbitShortPreparationChecks.Finish`:240/30/60/120fps×두 배속 개별/연결 생활, 여섯 준비 시점 일시정지/취소/재착석, 두/세 걸음 선택 경계와 첫발 중간 착지 검사. 기존 벤치·물 주기·출입 접지 회귀와 회피32배치·한쪽/양쪽 차단을 검사한다.
- 접지 이동3.5mm·간격5mm·침범0.5mm 이하, 회피 장애물 여유5cm 기준을 유지한다.240Hz 위치 기록은 `RabbitShortPreparation135Motion240.csv`, 결과는 `RabbitBenchArrival135Verification.txt` 및 `Logs/short135-final.log`다.
- 최종 검사 `BENCH135_COMPLETE_OK`:16조건과 여섯 준비 위상 취소/재착석 완료. 고정발 이동0.163mm·지지 간격2.554mm·최저 신발 높이2.399mm·도달 초과0이다.32배치19완료·13안전 대기, 한쪽/양쪽 차단과5개 회피 중단·재개, 기존 벤치/물 주기/출입·보행 회귀 통과. 원본/FBX/저장 장면 해시를 보존했다.
- 동일 초기 자세에서 기존 끝점 목표/새 중간 목표를 비교한240Hz 측정은 첫발 이동 벤치300.396→150.198mm, 회피286.493→178.978mm였다(`RabbitShortPreparation135Metrics.txt`, `RabbitShortPreparation135Comparison240.csv`, `Logs/short135-metrics-final.log`). 이 측정은 보정 알고리즘 비교이며 실제 OS 입력 녹화가 아니다. 벤치 최대 무릎 굽힘은65.3→75.6도로 오히려 증가했으므로 전체 자세가 자연스럽다는 승인은 하지 않는다. 골반/머리 높이 범위는 비교 계산에서 유지됐다.
- 새 렌더/전후 영상은 `Captures/RabbitShortPreparation135`에 보존한다. CanBeforeAfter45/BenchBeforeAfter45는 왼쪽 이전·오른쪽 수정이다. 각각 CanAfter0/45/90, BenchAfter0/45/90의1×영상을 제공한다. 실제 플레이어 자동 실행도 `WATER_BENCH_LIFE_PLAYER_OK`로 완료했으며 직접 버튼 입력은 미검증이다.
- 이전 비교 기준은 v0.133 회피 `Captures/RabbitCanAvoidance133/After45.mp4`와 v0.134 벤치 `Captures/RabbitBenchArrival134/After45.mp4`다. 새 세 구도1×렌더는 `Logs/BenchArrival135`에 만든다. 자동 렌더는 실제 OS 입력 녹화가 아니다.
- 생활 프로그램은 `Logs/RabbitHomeLifePlayer/TinyDaysRabbitHomeLife.exe`이며 옆 Data 폴더가 필요하다. 직접 버튼 입력·사용자 품질 승인 여부는 별도 기록한다.
- Mixamo 도입/새 생활 동작/GitHub 업로드는 제외한다. 실제 준비 피부 정점 검사는240Hz 자세 계산·60Hz 장애물 표본을 사용하며 완전한 연속 충돌 보장은 아니다. 검사 추가 후 경로 계산은 해당 PC 실행 파일에서 약2.544초로 늘었다. 별도 개선 대상이며 이번에 해결했다고 주장하지 않는다. 다른 PC에서는 AGENTS→MasterPlan→Progress→이 문서를 읽고 `RabbitHomeLifeStudy`에서 재개한다.
