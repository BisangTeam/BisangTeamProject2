# 복도 기능 2·3·4·5·10 구현 내역

작업 브랜치는 `Hyunwoo`다. `RealHyunzun`과 `MissionDemo`에 같은 테스트 복도를 배치했다. 기존 임무와 차단기 A·B는 `MissionDemo`에 있다. 스크립트 변경 전 기준 커밋은 `ec40a87`이다. 씬 표의 줄 번호는 복도를 처음 배치한 시점 기준이며, 이후 편집한 씬에서는 오브젝트 이름으로 찾을 수 있다.

손자국은 정면 레이가 닿은 벽의 실제 위치에 찍힌다. 한 타일에 여러 개를 찍을 수 있고, 같은 위치에서 다시 누르면 새 손자국이 위에 포개진다. 체크포인트는 접촉 좌표·면 방향·찍은 순서를 저장한다. 접촉 좌표가 없는 예전 손자국은 원래 타일 면의 중앙에 복구한다.

손자국 겹침을 적용하면서 `RealHyunzun.unity` 9952줄의 비어 있던 `WallMarker2D.hand`에 현재 씬의 `HandModule`을 연결했다. `MissionDemo`의 연결도 확인했다.

## 플레이하면서 확인하는 방법

1. 시작 위치에서 동쪽으로 복도를 따라가면 고정등 아래의 A 저장 지점, 조명이 닿지 않는 구간, 깜빡이는 등 아래의 B 지점을 차례로 지난다. 남쪽 벽의 매달린 화살표는 윗부분이 고정되고 아래가 흔들린다.
2. **F8**을 누르면 왼쪽 위에 심장 혈액 미리보기 슬라이더가 열린다. 60%에서 10%로 내리면 큰 박동에 맞춰 시점이 짧게 움직인다. 다시 60%로 올리면 떨림이 가라앉는다. F8로 닫으면 실제 심장 혈액량을 따른다.
3. 오른쪽 장기 패널의 심장에서 손으로 혈관을 연결해 손 능력을 활성화한다. 가까운 벽을 보고 **M**을 한 번씩 누를 때마다 바라본 지점에 한 장씩 찍힌다. 살짝 옆을 보고 다시 누르면 두 손자국이 겹치는 모습을 볼 수 있다. 세로 높이는 손자국 프리팹의 `Elevation`을 따른다.
4. 임무까지 포함한 확인은 **MissionDemo**에서 한다. A를 지난 뒤 복도 북쪽의 서비스 통로 입구에서 **F**로 통과한다. 차단기 A를 켜고 혈액팩을 심장에 사용한 다음 B를 밟는다. Windows 플레이어를 종료하고 다시 실행하면 B에서 시작한다. 차단기 A는 이미 사용됐고, B만 켜면 다음 임무가 시작된다. 사용한 우회로는 철판으로 닫히고 원래 복도는 계속 걸을 수 있다.

## 새로 만든 런타임 스크립트 12개

파일 전체가 새 코드다. 오른쪽에는 동작을 따라가기 좋은 시작 줄을 적었다.

| 파일 | 기능 | 전체 줄 | 주요 코드 |
|---|---|---:|---|
| [RaycastLight2D.cs](../Assets/Hyunwoo/_Scripts/RaycastLight2D.cs) | 2 | 1–64 | 9줄 밝기·반경·깜빡임 설정 · 16줄 월드 좌표와의 거리로 빛의 세기 계산 · 39줄 같은 씬에서 현재 시야에 닿는 조명을 수집하고 이번 프레임의 깜빡임 세기 계산 · 58줄 인스펙터에서 선택한 조명의 범위 표시 |
| [LowBloodPulse.cs](../Assets/Hyunwoo/_Scripts/LowBloodPulse.cs) | 4 | 1–71 | 9줄 실제 심장·박동 애니메이션과 미리보기 설정 · 24줄 큰 박동 이벤트 구독 · 37줄 혈액 부족 정도와 박동 경과 시간으로 시점 높이 변화량 계산 · 50줄 Input System의 마우스 좌표로 테스트 슬라이더 드래그 처리 · 64줄 미리보기 수치와 슬라이더 표시 |
| [HandModule.cs](../Assets/Hyunwoo/HumanBodySystem/HandModule.cs) | 5 | 1–17 | 8줄 손이 표식을 사용할 수 있는지 공개 · 10–14줄 장기별 혈액 단계의 능력값에 따라 사용 가능 여부 갱신 |
| [WallMarker2D.cs](../Assets/Hyunwoo/_Scripts/WallMarker2D.cs) | 5·10 | 1–146 | 11줄 저장할 벽·접촉 좌표·법선 정의 · 40줄 찍은 순서로 표식 보관 · 63줄 M 키 입력 · 69줄 도달 거리와 첫 충돌 판정 · 90–97줄 실제 접촉점과 법선을 타일맵 좌표로 변환 · 101줄 해당 위치에 새 프리팹 배치 · 111줄 겹친 항목도 전부 저장 · 118줄 순서대로 복구 · 134줄 이전 저장 형식의 벽면 중앙 복구 |
| [CheckpointPoint.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointPoint.cs) | 10 | 1–25 | 8줄 지점 고정 ID · 13줄 플레이어가 진입하면 저장 요청 · 19줄 불러온 위치에서 빠져나간 뒤 같은 지점에 다시 저장할 수 있게 처리 |
| [CheckpointSave.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointSave.cs) | 10 | 1–142 | 29줄 플레이어·장기·장치·저장 지점 수집, 임무의 시작 상태 준비 · 45줄 장치·표식·혈액·혈관·아이템 복구 후 지점 위치와 방향 적용 · 78줄 저장 요청과 재시작 직후 중복 진입 처리 · 88줄 같은 프레임의 아이템·임무 처리가 끝나면 저장 · 96줄 장기별 상태와 씬별 장치·표식을 묶어 파일 기록 · 130줄 지점 저장·불러오기 결과 표시 |
| [CheckpointData.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointData.cs) | 10 | 1–53 | 8줄 파일 버전·마지막 씬/지점·심장·장기·소지품 · 22줄 장기 ID, 혈액량, 혈관 연결 여부 · 30줄 아이템 ID와 수량 · 37줄 장치 ID와 사용 여부 · 44줄 씬별 임무·목표 횟수·장치·벽면 표식 |
| [CheckpointFile.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointFile.cs) | 10 | 1–57 | 9줄 영구 저장 파일 경로 · 11줄 주 파일을 읽고 손상됐으면 백업에서 복구 · 16줄 JSON과 버전·필수 식별자 확인 · 39줄 임시 파일 완성 후 기존 파일 교체, 직전 저장을 .bak에 보관 |
| [PlayerInventory.cs](../Assets/Hyunwoo/_Scripts/Save/PlayerInventory.cs) | 10 | 1–67 | 10줄 아이템 ID·수량·표시할 UI 오브젝트 · 21줄 수량 조회 · 23줄 아이템 추가 · 36줄 한 개 소비하고 마지막 아이템이면 아이콘 숨김 · 45줄 현재 수량 저장 · 53줄 수량과 아이콘 표시 복원 |
| [CheckpointDevice.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointDevice.cs) | 10 | 1–32 | 8줄 장치별 고정 ID와 상태별 오브젝트 · 15줄 일반 장치의 사용 여부 또는 미닫이문의 목표 상태 읽기 · 19줄 저장 상태 적용 및 열림·닫힘 표시 동기화 |
| [CheckpointShortcut2D.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointShortcut2D.cs) | 10 검증 | 1–35 | 14줄 입구 판정과 저장 장치 연결 · 20줄 F 상호작용 확인 · 25줄 입구 안에서 한 번만 출구로 이동시키고 입구를 닫음 |
| [CheckpointLaunch.cs](../Assets/Hyunwoo/_Scripts/Save/CheckpointLaunch.cs) | 10 | 1–18 | 10줄 게임 실행 시 저장된 씬을 열고, 새 게임은 MissionDemo로 시작 |

## 기존 스크립트에서 수정한 20개 파일

수정된 줄은 작업 전 브랜치와 비교한 결과다. 삭제만 이루어진 줄에는 기존 줄 번호를 따로 표기했다.

| 파일 | 기능 | 수정된 줄 | 변경 내용 |
|---|---|---|---|
| [RaycastRenderer2D.cs](../Assets/Hyunwoo/_Scripts/RaycastRenderer2D.cs) | 2·4 | 2, 44–48, 71–72, 74 | 프레임마다 조명을 재사용 가능한 목록에 모아 모든 그리기 함수에 전달한다. 심장 반응이 계산한 높이 변화량을 1인칭 시점에 더한다. |
| [RaycastLighting2D.cs](../Assets/Hyunwoo/_Scripts/RaycastLighting2D.cs) | 2 | 1, 12–13, 17–18, 24–34 | 장소의 기본 밝기와 주변 조명 세기를 더해 0~1로 제한한다. 기존 거리별 어둠과 벽면 방향별 밝기는 각 렌더러에서 이 값과 곱한다. |
| [RaycastWallRenderer2D.cs](../Assets/Hyunwoo/_Scripts/RaycastWallRenderer2D.cs) | 2·5 | 40, 55, 96–97 | 40줄에서 화면 열마다 가장 가까운 벽의 충돌 결과를 보관한다. 55줄에서 벽의 조도를 계산하고 96줄에서 실제 맞은 타일맵의 텍스처를 선택한다. |
| [RaycastRenderResources.cs](../Assets/Hyunwoo/_Scripts/RaycastRenderResources.cs) | 5 | 16, 26, 33 | 가로 한 줄당 벽 충돌 결과 하나를 담는 배열을 재사용한다. 렌더 해상도의 가로 크기가 달라지면 버퍼 크기도 다시 맞춘다. |
| [RaycastFloorRenderer2D.cs](../Assets/Hyunwoo/_Scripts/RaycastFloorRenderer2D.cs) | 2·4 | 39, 56–57, 62, 70, 72 (기존 55줄 삭제) | 바닥과 천장의 각 픽셀에 해당하는 월드 좌표에서 조도를 구한다. 바닥은 눈까지의 높이, 천장은 눈에서 천장까지의 거리로 투영한다. 텍스처를 읽을 수 없는 면은 검은색으로 그린다. |
| [RaycastObjectRenderer2D.cs](../Assets/Hyunwoo/_Scripts/RaycastObjectRenderer2D.cs) | 2·3·4·5 | 23, 57–58, 67–68, 70, 72–76, 95, 101, 133–134, 148, 151–157, 161, 164, 169–176 | 벽을 그린 직후 손자국을 찍은 순서대로 포갠다. 해당 벽면이 보이는 열에서만 손자국을 그려 모서리에서 공중에 뜨지 않는다. 빌보드와 일반 장식은 조명과 눈높이를 반영하고 흔들리는 장식은 아래쪽 행일수록 더 크게 움직인다. |
| [RaycastWallDecoration2D.cs](../Assets/Hyunwoo/_Scripts/RaycastWallDecoration2D.cs) | 3·5 | 13–15, 25–37 | 손자국이 붙은 오브젝트, 레이가 맞은 면의 법선과 평면 위치를 확인한다. 일반 장식은 설정한 진폭과 초당 왕복 횟수로 흔들린다. |
| [RaycastView2D.cs](../Assets/Hyunwoo/_Scripts/RaycastView2D.cs) | 4 | 15, 19, 29 | EyeHeight를 추가하고 기본 벽 높이의 절반에 시점 오프셋을 더한다. 시점은 바닥과 천장 사이로 제한한다. |
| [DrawStruct.cs](../Assets/Hyunwoo/_Scripts/DrawStruct.cs) | 4 | 27 | 벽 세로줄의 밑단을 실제 눈높이로 계산해 벽·바닥·빌보드가 같은 시점에서 움직인다. |
| [HeartModule.cs](../Assets/Hyunwoo/HumanBodySystem/HeartModule.cs) | 4·10 | 17, 28 | 최대 저장량의 기본값을 100으로 잡고 BloodGaugeRatio를 공개한다. 심장 부족 반응이 현재 저장량을 최대량으로 나눈 비율을 읽는다. |
| [HeartBeat.cs](../Assets/PSW/JangGi/Heart/HeartBeat.cs) | 4 | 6, 34, 42 | 6줄에 StrongBeat 이벤트를 추가했다. 34줄에서 두 번째 큰 박동이 최고점에 도달할 때 한 번 호출한다. 42줄은 파일 끝 줄바꿈이다. |
| [AbstractViscera.cs](../Assets/Hyunwoo/HumanBodySystem/AbstractViscera.cs) | 5·10 | 51, 68–69, 80, 90–92 (기존 65, 71줄 삭제) | 혈액값 변경 이벤트로 장기 게이지 UI를 갱신해 저장 복구에도 반응한다. 단계가 비어 있는 테스트 장기는 건너뛰고 최고 기준 초과 시 마지막 유효 인덱스를 사용한다. |
| [MouseDragAndMove.cs](../Assets/Hyunwoo/BloodPack/MouseDragAndMove.cs) | 10 | 2, 17–19, 36, 60–64 | 혈액팩을 심장에 놓으면 인벤토리의 blood-pack을 한 개 차감한 뒤 인스펙터의 주입량을 사용한다. 재사용을 위해 아이콘 크기와 위치를 복구한다. 인벤토리가 연결되지 않은 기존 씬은 예전 1회 사용 흐름을 따른다. |
| [MissionDataSO.cs](../Assets/PSW/Quest/MissionDataSO.cs) | 10 | 27–28, 32 | 27–28줄에 저장용 고정 ID를 추가했다. 기존 자산에 ID가 없을 때는 해당 자산의 이름으로 찾는다. 32줄은 파일 끝 줄바꿈이다. |
| [MissionManager.cs](../Assets/PSW/Quest/MissionManager.cs) | 10 | 9–11, 44, 47–62, 71–72 | 시작 임무부터 연결된 목록에서 저장 ID를 찾고, UI나 임무 시작 이벤트를 보내기 전에 목표별 횟수를 복원한다. 모든 임무를 마친 상태도 복구한다. |
| [MissionTrigger2D.cs](../Assets/PSW/Mission/MissionTrigger2D.cs) | 10 | 2, 20, 47–49, 54–55, 66 | 차단기의 개별 사용 상태를 저장 장치에 기록하고, 불러온 뒤 같은 차단기로 횟수를 또 올리는 입력을 차단한다. |
| [RaycastDoor2D.cs](../Assets/Hyunwoo/_Scripts/RaycastDoor2D.cs) | 10 | 26, 89–95 | 움직이고 있는 문은 목표 열림 상태를 기록한다. RestoreOpen이 불러오기 시 문짝을 해당 위치에 즉시 배치한다. |
| [Heart.cs](../Assets/Seokmin/Scipts/Heart.cs) | 5·10 | 15, 24 | 혈관을 복구할 때 쓸 실제 심장 연결점 BloodAnchor를 공개한다. 마우스가 연결된 경우에만 드래그 시작 입력을 읽는다. |
| [institution.cs](../Assets/Seokmin/Scipts/institution.cs) | 5·10 | 17–18, 27, 68, 72–93 | 장기와 연결 여부를 공개한다. 수동 드래그와 세이브 복구가 동일한 AttachLine을 사용해 혈관 끝 위치와 심장의 혈액 분배 구독을 함께 복구한다. 마우스가 없는 프레임에는 입력 처리를 건너뛴다. |
| [LineTest.cs](../Assets/Seokmin/Scipts/LineTest.cs) | 5·10 | 50–51, 58, 61 | 마우스가 연결된 경우에만 놓기와 드래그 위치를 읽도록 수정했다. 마우스 없는 Windows 테스트에서도 이미 연결된 혈관은 계속 갱신된다. |

## 씬에 연결한 오브젝트와 줄 번호

두 씬의 오브젝트 블록을 비교한 표다. [RealHyunzun.unity](../Assets/Hyunwoo/Scene2/RealHyunzun.unity) · [MissionDemo.unity](../Assets/PSW/Mission/Example/MissionDemo.unity)

| 씬에 추가한 요소 | RealHyunzun 줄 | MissionDemo 줄 |
|---|---:|---:|
| 박동·벽 표식·인벤토리·저장 매니저 연결 | 9583–9651 | 7938–8006 |
| 손 아이콘, 손 모듈, 혈관 연결점 | 9652–9765 | 8007–8120 |
| 고정등과 천장 램프 | 9766–9829 | 8121–8184 |
| 깜빡이는 등과 천장 램프 | 9830–9893 | 8185–8248 |
| 흔들리는 방향 표지판 | 9894–9944 | 8249–8299 |
| A 지점 트리거와 벽의 저장 표시 | 9945–10088 | 8300–8443 |
| B 지점 트리거와 벽의 저장 표시 | 10089–10232 | 8444–8587 |
| 우회로 입구, 열린 표시, 닫힌 철판과 충돌체 | 10233–10484 | 8588–8839 |
| 우회로 출구 위치와 방향 | 10485–10515 | 8840–8870 |
| 기존 미닫이문의 저장 상태 | 10516–10532 | 8871–8887 |
| 차단기 A와 B의 개별 상태 | 해당 씬에 없음 | 8888–8921 |

| 기존 씬·설정 파일 | 수정된 줄 | 연결 내용 |
|---|---|---|
| [RealHyunzun.unity](../Assets/Hyunwoo/Scene2/RealHyunzun.unity) | 205, 626–632, 690, 7159, 8841–8843, 9050, 9096–9097, 9583–10532 | 환경과 플레이어 계층에 자식/컴포넌트 참조 추가, 혈액팩 인벤토리 연결, 환경 밝기와 시점 반응 지정. |
| [MissionDemo.unity](../Assets/PSW/Mission/Example/MissionDemo.unity) | 130, 576–582, 2924, 3661–3670, 4608, 4662–4663, 6218–6220, 6425, 6471–6472, 7194, 7283, 7350, 7439, 7938–8921 | 같은 복도 구성과 차단기별 참조 추가. 테스트 씬의 눈 능력 단계, 심장 최대량·초깃값도 지정. |
| [SampleScene.unity](../Assets/Scenes/SampleScene.unity) | 134, 354–366 | 첫 씬 카메라에 CheckpointLaunch를 연결해 이어하기 또는 새 게임의 씬 선택. |
| [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) | 17–19 | RealHyunzun을 마지막에 추가해 기존 빌드 씬 인덱스를 보존. |

기존 임무 자산 네 개는 모두 **15줄**에 임무 고정 ID를 추가했다. 파일명을 바꿔도 ID가 유지된다. [01_EnterHall.asset](../Assets/PSW/Mission/Example/01_EnterHall.asset) · [02_Crossing.asset](../Assets/PSW/Mission/Example/02_Crossing.asset) · [03_RestorePower.asset](../Assets/PSW/Mission/Example/03_RestorePower.asset) · [04_Escape.asset](../Assets/PSW/Mission/Example/04_Escape.asset)

## 추가한 프리팹과 이미지

| 파일 | 용도 |
|---|---|
| [WallMark.prefab](../Assets/Hyunwoo/Raycasting2D/WallMark.prefab) | 벽 한 면에만 보이는 표식. 너비·높이 0.31, 바닥에서 높이 0.32. 교체할 텍스처와 크기를 프리팹에서 지정한다. |
| [WallHandprint.png](../Assets/Hyunwoo/Raycasting2D/Textures/WallHandprint.png) | 벽에 찍는 손자국과 장기 패널에 표시하는 손 아이콘. |
| [HangingArrow.png](../Assets/Hyunwoo/Raycasting2D/Textures/HangingArrow.png) | 흔들림 테스트용 방향 표지판과 사용 가능한 우회로의 입구 표시. |
| [CheckpointSign.png](../Assets/Hyunwoo/Raycasting2D/Textures/CheckpointSign.png) | 복도 벽에 붙는 저장 지점 표시. |

새 스크립트 12개, 프리팹 1개, PNG 3개, Save 폴더 1개에 대응하는 .meta 17개도 함께 추가했다. PNG는 32×32 RGBA이며, 소프트웨어 렌더러가 픽셀을 읽도록 Read/Write를 켰다.

## 인스펙터 기본값

| 오브젝트 | 설정 |
|---|---|
| Raycast Renderer | Ambient Brightness 0.34. 조명 반경 안에서 표면 좌표를 기준으로 밝아진다. 눈 능력은 여기에 거리별 어둠을 곱한다. |
| Corridor Light - Steady / Flicker | 밝기 1.04, 반경 2.45. 두 번째 등만 Flicker가 켜져 있고 Flicker Speed는 4다. |
| Hanging Corridor Sign | Sway Amplitude 0.055 월드 유닛, Sway Cycles Per Second 0.65. Width/Height는 장식 이미지의 게임 공간 크기다. |
| Low Blood Pulse | Threshold 0.30, Maximum Height 0.026 월드 유닛, Pulse Duration 0.26초. 테스트 슬라이더는 심장의 실제 저장량에 영향을 주지 않는다. |
| Hand / Map Player | `RealHyunzun`은 손 혈액이 0을 넘으면, `MissionDemo`는 20을 넘으면 사용 가능하다. M 키, 벽까지 최대 1.6 유닛. `WallMarker2D`의 `Walls`가 기본 맵이며 저장 ID는 `main`이다. 다른 벽 타일맵은 `Additional Walls`에 고유 ID와 Tilemap을 지정한다. 각 벽은 Wall Mask에 포함된 레이어에서 Tilemap과 2D Collider를 같은 오브젝트에 둔다. |
| Checkpoint A / B | A는 (3.6, 1.5), B는 (10.2, 1.5). 두 지점 모두 동쪽을 바라보며 재시작한다. |
| Service Hatch - F | 입구 (6.5, 2.4), 출구 (8.35, 1.5). 다른 통로와 장치에도 고정 ID를 가진 CheckpointDevice를 연결할 수 있다. |

## 저장 파일과 검증

Windows의 정식 저장 위치는 Application.persistentDataPath 아래의 checkpoint-v1.json이다. 같은 폴더의 .bak은 직전 저장본이다. 혈액과 소지품은 플레이어 공통으로, 임무·장치·표식은 씬별로 기록한다. 이후 진행은 다음 저장 지점에서 반영된다.

손자국을 겹쳐 찍도록 변경한 뒤 원본 C#을 에디터용과 Windows용으로 컴파일했다. Unity 6000.3.22f1 Windows 검증 플레이어의 별도 타일맵에서 설치·렌더링·저장 14개와 프로세스 종료 후 복구 8개 검사를 통과했다. 두 번 누른 같은 위치의 자국, 비스듬히 짚은 위치, 겹침 순서, 모서리 잘림, 이어진 타일의 경계, 이전 저장 파일을 확인했다. 기존 도달 불가 코드 경고는 MonsterChase2D.cs 75줄과 HeartRateWave.cs 56줄에 있다.

아래는 겹침 기능을 추가하기 전 복도 전체의 검증 기록이다. 기존에 빌드된 두 씬에서 저장 30개, 재실행 16개, RealHyunzun 16개를 통과했다. 별도 에디터에서 씬을 다시 패키징하는 전체 Windows 빌드는 라이선스 연결 오류로 완료하지 못했다.

- **조명과 장식**. 두 씬에서 고정등·암부의 밝기와 깜빡임 변화, 눈 혈액량에 따른 거리 밝기를 확인했다. 가까운 위치와 먼 위치의 렌더 픽셀을 각각 비교했고 장식 이미지를 교체한 뒤에도 흔들렸다.
- **심장과 손**. F8 키, 마우스 슬라이더 드래그, 부족 시 박동·회복 후 높이 0을 확인했다. 심장→손 드래그로 혈관이 연결됐고, 손이 비활성이면 설치가 거절됐다.
- **여러 타일맵과 이전 저장 파일**. 서로 다른 두 타일맵의 같은 셀·같은 면에 남긴 표식이 재실행 후 각각 원래 타일맵으로 돌아왔다. 타일맵 ID가 없는 기존 v1 저장 파일과 맵마다 다른 벽 텍스처도 확인했다.
- **체크포인트**. MissionDemo에서 A 트리거를 밟고, 우회로 통과와 F 키 차단기 조작, 혈액팩 사용 후 B를 밟았다. 첫 프로세스를 종료한 다음 두 번째 프로세스에서 B의 위치·방향, 심장/장기 혈액, 혈관 연결, 아이템 수량, 벽면 표식, 문 상태와 임무 1/2가 복구됐다.
- **불러온 이후 플레이**. 쓴 우회로 재사용과 차단기 A의 중복 집계가 차단됐고, 남은 차단기 B를 켜 다음 임무로 넘어갔다. 손상된 테스트 주 파일을 백업으로 복구하는 경우도 확인했다.
