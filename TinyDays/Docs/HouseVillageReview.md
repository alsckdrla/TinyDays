# 집 3종·앞마당 디자인 사용자 확인 대기

2026-09-15 / Unity 2022.3.20f1. 기존 FarmStudy와 Home A를 교체하지 않은 별도 비교 장면이다.

## 보기와 조작

- Unity 메뉴 **Tiny Days → House village → Open comparison** 또는 `Assets/Scenes/HouseVillageStudy.unity`를 연다. Play 후 전체 보기·목조집·노란 회벽집·작은 시골집 버튼으로 선택한다.
- 왼쪽 드래그 화면 패닝, 가운데 드래그 회전, 휠 줌, Q/E 높이, Home 전체 보기. 기존 농가와 같은 감도·화면 패닝 계산을 사용한다.
- 주민이 없는 비교 장면이다. 일반 장애물 가림은 적용하지 않으며 카메라가 집·장식·지하에 들어갈 때만 기존 내부 반투명 예외를 사용한다.
- [전체 비교](Captures/HouseVillage/Overview.png).
- [목조집 확대](<Captures/HouseVillage/Timber Cottage-Oblique.png>): 가파른 청회색 지붕, 판재 벽, 다락창과 낮은 현관 데크. 화분 2·관목 2·상자·통·디딤돌 그룹.
- [노란 회벽집 확대](<Captures/HouseVillage/Ochre Farmhouse-ObliqueRight.png>): 황토빛 노랑 벽, 비대칭 창과 현관, 오른쪽 작은 돌출부. 화단 2·디딤돌·벤치·관목·화분 그룹.
- [작은 시골집 확대](<Captures/HouseVillage/Straw Cottage-Oblique.png>): 아이보리 벽, 짚색의 두꺼운 지붕, 목재 창틀과 굴뚝. 화단 2·관목 2·울타리 2·화분·디딤돌 그룹. 실제 짚 가닥을 촘촘하게 모델링한 지붕은 아니다.
- 각 집의 Front / Side / Rear / Oblique / ObliqueRight / Low / Small 파일을 같은 [촬영 폴더](Captures/HouseVillage/)에 보존한다. Small은 480×360 자동 렌더이며 실제 작은 Game 창 검사와 다르다.

## 제작 예산

| 대상 | 삼각형 | 렌더러 | 고유 재질 | 장식 그룹 |
|---|---:|---:|---:|---:|
| 목조집 본체 | 1,684 | 9 | 9 | — |
| 목조집 앞마당 | 1,044 | 23 | 10 | 7 |
| 노란 회벽집 본체 | 1,204 | 9 | 9 | — |
| 노란 회벽집 앞마당 | 900 | 23 | 10 | 6 |
| 작은 시골집 본체 | 1,132 | 10 | 10 | — |
| 작은 시골집 앞마당 | 972 | 24 | 8 | 8 |

집 각각 ≤2,000, 장식 원형 각각 ≤300, 울타리 구간 72≤100삼각형이다. 앞마당 합계는 여러 원형의 인스턴스 합계로서 원형 예산과 다르다. 공통 화분·화단·관목·디딤돌·울타리는 공유 메시/재질이며 상자·통·벤치는 기존 농가 메시/재질을 읽기 전용으로 재사용했다. 집의 모서리는 선명하게, 화분과 관목은 선택적으로 부드러운 노멀을 적용했다. 저사양 PC 성능을 측정하거나 보장한 수치는 아니다.

## 검증과 남은 확인

- [자동 검사 기록](HouseVillageVerification.txt): 컴파일·집/장식 예산·문/창/다락창의 실제 벽 개구부·출입구 0.9m 폭 장식 간섭 없음·읽을 수 있는 메시·퇴화 삼각형 없음·닫힌 부품 변 연결·반복 메시 공유 통과.
- 직접 호출: 각 집의 빈 실내/지붕 아래, 각 장식 내부 진입 시 그룹 25% 반투명과 원본 재질 복구, 비활성화/재활성화, Home 초기화 통과. UI 입력을 통한 검증은 아니다.
- 두 번 반복 생성 시 ManualEdits의 검사 개체 보존 통과. 기존 농가 장면, FarmStudy 생성 자산 전체와 수동 아트 파일은 실행 전후 SHA-256이 동일했다.
- 자동 렌더에서 판재 읽힘과 화분/화단 간격을 보완했고, 여러 시점에서 큰 지붕 틈·누락 면·막힌 개구부가 없는지 검토했다. 정지 렌더는 연속 조작 중 깜빡임 검사를 대체하지 않는다. 기존 조명 설정의 그림자 톱니는 일부 남아 있다.
- **실제 Game 창 조작·연속 카메라 이동 중 깜빡임·전환 감각·디자인 사용자 확인은 대기**다. 기존 농가에 추가/교체하지 않았으며 주민 생활·AI·새 이동 동선은 만들지 않았다.

## 참고와 재생성

- [House_01](References/House_01.png), [House_02](References/House_02.png)를 원본 그대로 보존했다. 복사 전후 SHA-256 일치:
  - House_01: `33E27F52391450ED9E4C6DD08A9949A9AA19F6F7F5359F24D54156451B5CDC07`
  - House_02: `789F107AF74A01BFE0AAA0E157E224BB363D281F5BEFAD7BA30164C2E7BB276E`
- [원본 생성 코드](../Assets/Editor/HouseVillageAssets.cs), [장면 생성·검사·촬영](../Assets/Editor/HouseVillageBuilder.cs), [공유 메시 도구](../Assets/Editor/FarmLowPolyAssets.cs), [검토 카메라](../Assets/Scripts/HouseVillageReview.cs).
- Unity에서 프로젝트를 닫고 [RebuildHouseVillage.ps1](../Tools/RebuildHouseVillage.ps1)을 실행한다. 생성 자산은 `Assets/Art/Generated/HouseVillage`, 자동 영역은 `GeneratedHouseVillage`다. 비교 장면의 `ManualEdits` 밖 생성 영역은 재생성 시 교체된다. 기존 농가 재생성은 실행하지 않는다.
- C# 절차적 메시가 원본이다. 별도 Blender 원본 제작이나 커밋·푸시는 수행하지 않았다. 향후 실제 농가 배치는 사용자 디자인 확인 후 결정한다.
