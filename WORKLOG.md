# 작업 기록 — 복셀 여우 타일 게임

> 단계별로 실제로 한 일, 결과, 검증 방법, 발생한 문제를 남긴다. 체크리스트: [TODO.md](TODO.md)

---

## 1단계. Unity 프로젝트 생성 — ✅ 완료 (2026-09-29)

### 결과 요약
| 항목 | 결과 |
|---|---|
| 프로젝트 경로 | `C:\sample\VoxelFox` |
| 에디터 | Unity **6000.5.7f1** |
| 템플릿 | Universal 3D (`com.unity.template.3d-cross-platform-17.0.14`) |
| 렌더 파이프라인 | URP `17.5.0` |
| 입력 | Input System `1.20.0`, Active Input Handling = **Input System Package (New)** |
| UI / 텍스트 | uGUI `2.5.0` (TextMeshPro 내장), TMP Essential Resources 임포트 완료 → `Assets/TextMesh Pro/` |
| 테스트 | Test Framework `1.7.0` |
| 시작 씬 | `Assets/Scenes/Main.unity` (Build Settings 씬 목록에는 Main 하나만 등록) |

### 만든 폴더 구조
```
Assets/
  Art/Characters/Fox/
  Art/Tiles/
  Prefabs/
  Scenes/Main.unity        ← 새 메인 씬 (카메라 + Directional Light)
  Scripts/
  Editor/ProjectSetup.cs   ← 초기 설정 자동화 (메뉴: VoxelFox > Project Setup)
  Tests/PlayMode/          ← Main 씬 스모크 테스트
  TextMesh Pro/            ← TMP 기본 리소스
```
템플릿에 들어 있던 샘플 파일은 정리했다. 아래 "템플릿 샘플 정리"를 참고.

### 진행 방식
모든 작업은 Unity를 배치 모드(명령줄)로 실행해서 처리했다.
1. `-createProject` + `-cloneFromTemplate`로 Universal 3D 템플릿 프로젝트를 생성했다.
2. `-executeMethod ProjectSetup.Run`으로 폴더를 만들고, Main 씬을 생성하고, Build Settings를 설정했다.
3. `-importPackage`로 TMP Essential Resources를 임포트했다.
4. `-runTests -testPlatform PlayMode`로 스모크 테스트를 실행했다.

### 검증 (완료 기준)
- `MainSceneSmokeTest.MainScene_RunsWithoutErrors` 테스트는 Main 씬을 Play 모드로 로드하고 60프레임을 실행한 뒤 에러 로그가 없는지, Main Camera가 있는지 확인한다.
- 결과: **Passed** (1/1, 실패 0) → `logs/01_playmode_results.xml`

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| 처음 실행에서 `ProjectSetup.cs` 컴파일 에러 | 셸 heredoc으로 파일을 쓰는 과정에서 `'\\'` 문자 리터럴이 깨짐 | 쓰이지 않는 줄이라 삭제 |
| 스크립트로 부른 TMP 임포트가 적용되지 않음 | 배치 모드에서는 `AssetDatabase.ImportPackage`가 `-quit` 전에 끝나지 않음 | 명령줄 옵션 `-importPackage`로 임포트 (스크립트에 주석으로 남김) |

로그에 나오는 `Licensing ... Access token is unavailable` 경고는 Hub에 로그인하지 않은 상태로 배치 실행해서 생기는 것이다. 동작에는 영향이 없었다.

### 템플릿 샘플 정리 (2026-09-29, 사용자 요청)
Universal 3D 템플릿에 기본으로 들어 있는 샘플/안내용 파일을 삭제했다.

| 삭제한 항목 | 원래 용도 | 지운 이유 |
|---|---|---|
| `Assets/Scenes/SampleScene.unity` | 템플릿 예제 씬 | 게임 씬은 `Main.unity` 하나로 정했다. Build Settings에서도 이미 빠져 있어서, 남겨 두면 어느 씬이 진짜인지 헷갈리기만 한다. |
| `Assets/TutorialInfo/` (`Readme.cs`, `ReadmeEditor.cs`, `Icons/URP.png`, `Layout.wlt`) | 프로젝트를 처음 열 때 Readme 안내창과 창 레이아웃을 띄우는 템플릿 전용 에디터 스크립트 | 게임과 관계없는 코드다. 에디터를 열 때마다 안내창이 뜨고, 레이아웃을 바꿀 수 있다. |
| `Assets/Readme.asset` | 위 스크립트가 보여주는 URP 안내 문서 데이터 | `Readme.cs`를 지우면 스크립트 참조가 빠진(Missing Script) 에셋이 되므로 함께 지웠다. |

- 함께 수정: `ProjectSettings.asset`의 `templateDefaultScene`이 `SampleScene`을 가리키고 있어서 `Assets/Scenes/Main.unity`로 바꿨다. 프로젝트를 처음 열면 Main 씬이 열린다.
- 삭제 전 확인: 다른 씬, 에셋, 설정에서 이 파일들을 참조하는 곳이 없었다. 위 `templateDefaultScene` 한 곳만 있었다.
- 삭제 후 검증: PlayMode 스모크 테스트를 다시 실행해서 **Passed (1/1)**, 컴파일 에러와 Missing Script 경고 없음 → `logs/01b_cleanup_playmode_results.xml`
- `InputSystem_Actions.inputactions`는 3단계 이동 입력에 쓸 예정이라 남겨 두었다.

### 로그 파일
- `logs/01_create_project.log`: 프로젝트 생성
- `logs/01_setup.log`: 초기 설정
- `logs/01_tmp_import.log`: TMP 임포트
- `logs/01_playmode.log`, `logs/01_playmode_results.xml`: 스모크 테스트
- `logs/01b_cleanup_playmode.log`, `logs/01b_cleanup_playmode_results.xml`: 샘플 정리 후 재검증

### 다음 단계
2단계: Blender Python으로 복셀 여우 모델, 리그, 애니메이션을 만들고 FBX로 내보낸다.

---

## Git 저장소 연결 — ✅ 완료 (2026-09-29)

| 항목 | 내용 |
|---|---|
| 원격 저장소 | [github.com/Notard/voxel-fox](https://github.com/Notard/voxel-fox) (**Public**) |
| 저장소 루트 | `C:\sample` (문서 + `VoxelFox/` Unity 프로젝트 + `tools/`) |
| 기본 브랜치 | `main` → `origin/main` 추적 |
| 첫 커밋 | `d5ca66f` 1단계: Unity 프로젝트 생성 및 계획 문서 |

### 설정
- `.gitignore`: Unity 자동 생성 폴더(`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `Build`), IDE 파일(`*.csproj`, `*.sln`, `.vs`), Blender 백업(`*.blend1`), 실행 로그 `logs/*.log`는 제외했다.
  - 로그를 뺀 이유: 로컬 경로와 라이선스 정보가 들어 있고 용량도 크다(프로젝트 생성 로그만 2MB). 테스트 결과 `*.xml`만 저장소에 남긴다.
- `.gitattributes`: 줄바꿈은 자동 정리하고, Unity YAML 에셋은 LF로 통일했다. 바이너리(`fbx`, `blend`, `png`, `ttf`, `wav` 등)는 **Git LFS**로 관리한다.
- 현재 LFS 파일: `LiberationSans.ttf` (TMP 기본 폰트, 350KB)

### 공개 저장소 주의
- 커밋 작성자 이메일(git 전역 설정)이 공개된다.
- `logs/*_results.xml` 안에 Windows 사용자 폴더 경로가 들어 있다.

---

## 2단계. 복셀 여우 캐릭터 (Blender Python) — ✅ 완료 (2026-09-29)

미리보기: **[fox_preview.html](fox_preview.html)** (애니메이션 플레이어, 4방향 이미지, Unity 임포트 결과)

### 결과 요약
| 항목 | 결과 |
|---|---|
| 생성 스크립트 | `Blender/make_fox.py`: 복셀 데이터 → 메시 → 팔레트 머티리얼 → 리그 → 액션 3개 → FBX, `.blend` 저장 |
| 모델 | 복셀 612개, 면 780, 정점 3,120. 코~꼬리 약 1.4m, 귀 끝까지 높이 약 0.94m (1복셀 = 0.0625m) |
| 색상 | 버텍스 컬러 대신 **팔레트 텍스처**(`Fox_Palette.png` 8×8)를 쓰고, 면마다 UV로 색 칸을 지정했다. URP Lit을 그대로 쓸 수 있다. |
| 리그 | Root › Body › Head / Leg_FL·FR·BL·BR / Tail. 모든 본이 위를 향하게 해서 회전축을 직관적으로 맞췄다. |
| 스키닝 | 파츠마다 본 하나에 가중치 100% (강체) |
| 애니메이션 | Idle 0–40 루프 · Walk 0–20 루프 · Jump 0–20 (웅크림 4 → 도약 8 → 공중 13 → 기본 20) |
| Unity 에셋 | `Fox.fbx` (Generic, 클립 이름 Idle/Walk/Jump, Idle·Walk Loop), `Fox.mat` (URP Lit, Point 필터 팔레트), `Fox.controller`, `Prefabs/Fox.prefab` |
| 미리보기 | `Blender/render_preview.py`(Workbench, 정지 4장 + 애니메이션 41프레임), `FoxPreviewCapture.cs`(Unity URP 렌더 3장) |

### 설계 메모
- **면 제거 규칙:** 같은 파츠 안에서 맞닿은 면만 지웠다. 파츠 경계(예: 다리 윗면과 몸통 아랫면)의 면은 남겨서 관절이 회전해도 구멍이 보이지 않는다.
- **Animator Controller:**
  - 파라미터: `Speed`(float), `IsGrounded`(bool, 기본값 true), `Jump`(trigger)
  - 전환: Idle → Walk (Speed > 0.1), Walk → Idle (Speed < 0.1), Any State → Jump (Jump 트리거)
  - Jump → Idle: IsGrounded 조건에 exit time 0.5를 더했다. 점프 직후 한 프레임은 아직 땅에 붙어 있어서, 조건만 두면 바로 Idle로 돌아가기 때문이다. 4단계에서 실제 이동과 맞추며 다시 조정한다.
- **임포트 설정 자동화:** `FoxAssetPostprocessor.cs`가 FBX와 팔레트의 임포트 설정을 맡는다. Blender에서 다시 내보내도 설정이 유지된다.
- **전체 재생성:** `bash tools/build_fox.sh` (Blender 생성 → 미리보기 → Unity 설정 → 캡처 → 테스트)

### 검증 (완료 기준)
EditMode `FoxAssetTests` **9/9 통과**:
- 클립 3개의 길이와 루프 설정 (Idle 1.33s 루프, Walk 0.67s 루프, Jump 0.67s 1회)
- 본 8개가 모두 있는지
- 모든 정점의 가중치가 1인지 (복셀이 찌그러지지 않음)
- 여우가 +Z를 바라보고 서 있는지, 크기가 맞는지
- Walk에서 좌우 앞다리가 반대로 40° 넘게 움직이는지
- Animator 파라미터, 상태, 기본 상태
- 프리팹 머티리얼과 Point 필터

PlayMode 스모크 테스트 **1/1 통과** (회귀 확인). 결과 파일: `logs/02_editmode_results.xml`, `logs/02_playmode_results.xml`

Unity 렌더 캡처로 방향(+Z 정면), 색, Walk/Jump 포즈가 Blender 결과와 같은 것을 눈으로 확인했다.

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| Unity 캡처에서 Jump 포즈가 Idle과 똑같이 나옴 | 배치 모드에서는 플레이어 루프가 돌지 않아 SkinnedMeshRenderer 스키닝이 갱신되지 않음 | 포즈마다 `BakeMesh`로 메시를 굳혀 따로 렌더 |
| `file://`로 연 미리보기 페이지를 브라우저 창에서 조작할 수 없음 | 로컬 파일은 정적 스냅샷으로만 열림 | 로컬 서버(`python -m http.server`)로 띄워 재생, 탭, 스크롤을 확인 |

### 다음 단계에서 고려할 점
- 여우 몸길이(1.4m)가 타일 한 칸(2m)의 70% 정도로 크다. 3단계에서 맵에 올려 보고, 필요하면 프리팹 스케일(예: 0.6~0.7)이나 타일 크기를 조정한다.

---

## 2-1. 디자인 변경: 흰 여우 · 0.9배 — ✅ 완료 (2026-09-29)

미리보기: **[fox_preview.html](fox_preview.html)**

### 변경 이유
- **흰 여우로:** 더 예쁜 여우로 만들기 위해서다(사용자 요청). 붉은 여우의 주황색을 흰색으로 바꿨다.
- **크기 0.9배:** 사용자 요청이다. 2단계에서 적어 둔 "몸길이 1.4m가 타일(2m)에 비해 크다"는 우려도 함께 줄어든다(1.4m → 1.24m, 타일의 62%).

### 색상 변경 내역
| 부위 | 이전 (붉은 여우) | 이후 (흰 여우) | 메모 |
|---|---|---|---|
| 털 (몸통·머리·꼬리·귀·다리) | 주황 3톤 `#E8772E` `#F08A3E` `#CF6322` | 흰색 3톤 `#F5F6F8` `#FFFFFF` `#E2E6EC` | 푸른빛이 도는 그늘 톤을 섞어 눈처럼 보이게 했다 |
| 배·가슴·볼·주둥이·꼬리 끝 | 흰색 `#FFF4E6` `#EEE0CE` | 크림 `#FFF3E4` `#F3E6D6` | 털이 흰색이 되어서, 따뜻한 크림색으로 살짝 구분했다 |
| 다리 | 갈색 `#5A3322` | 털과 같은 흰색 | 갈색 다리는 붉은 여우의 특징이라 흰 여우에는 맞지 않는다 |
| 귀 끝·발끝 | 검정 `#2B2420` | 연회색 `#BFC4CE` | 흰 몸에 검정은 너무 튀어서 부드러운 회색으로 바꿨다 |
| 귀 안쪽 | 어두운 갈색 `#7A3E2A` | 분홍 `#F2B5C1` | 더 귀여운 인상을 준다 |
| 눈·코 | 검정 | 검정 (그대로) | 흰 얼굴에서 가장 중요한 포인트 |

모양(복셀 배치), 본, 애니메이션은 바꾸지 않았다. 팔레트 인덱스만 바꿨으므로 UV와 텍스처 크기도 그대로다.

### 크기 변경
| 항목 | 이전 | 이후 |
|---|---|---|
| 1복셀 | 0.0625m | **0.05625m** (`make_fox.py`의 `SCALE = 0.9`) |
| 코~꼬리 | 약 1.38m | 약 1.24m |
| 귀 끝 높이 | 약 0.94m | 약 0.84m |

- 크기는 Unity 프리팹 스케일이 아니라 **모델 자체(FBX)**에 반영했다. 나중에 CharacterController 크기를 맞출 때 스케일 1 기준으로 계산할 수 있다.
- 본 위치와 애니메이션 이동값(몸통 바운스)도 복셀 단위로 계산하므로 함께 0.9배가 된다.

### 함께 바꾼 것
- **미리보기 렌더:** 흰 여우가 Blender Workbench 스튜디오 조명과 기본 색 변환(AgX)에서 회색으로 보였다. 그래서 **EEVEE + 태양광 + Standard 색 변환**으로 바꿨다. 이제 Unity 렌더와 비슷한 밝기로 나온다.
- **미리보기 배경:** 흰 여우가 잘 보이도록 푸른 회색으로 바꿨다(`fox_preview.html`, `FoxPreviewCapture.cs`).
- **카메라:** 미리보기 카메라 구도를 0.9배에 맞췄다. 그래서 미리보기 이미지에서는 여우가 이전과 비슷한 크기로 보인다. 실제 크기 차이는 위 표의 수치로 확인한다.
- **테스트:** 몸길이 기준을 "1.2~1.6m 범위"에서 "1.24m ± 0.03"으로 좁혔다. 머리 높이 기준은 0.4m에서 0.35m로 낮췄다.
- **문서:** plan.md와 plan.html의 색상 표와 복셀 크기, fox_preview.html의 모델 정보와 팔레트

### 검증
- `bash tools/build_fox.sh`로 전체를 다시 생성했다.
- EditMode **9/9 통과**, PlayMode **1/1 통과** → `logs/02b_editmode_results.xml`, `logs/02b_playmode_results.xml`
- Blender 렌더와 Unity 렌더 모두 흰 털, 분홍 귀, 크림색 볼, 검은 눈·코가 나오는 것을 눈으로 확인했다.

---

## 3단계. 4x4 타일맵 + 이동 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)** (이동 녹화 플레이어, 쿼터뷰·위에서·근접 이미지)

### 결과 요약
| 항목 | 결과 |
|---|---|
| 잔디 타일 | `Prefabs/GrassTile.prefab`: 2m × 0.5m × 2m, 복셀 16×4×16 (1복셀 0.125m). 보이는 면만 메시로 만들었다(면 513, 정점 2,052). 윗면 원점, BoxCollider 포함 |
| 타일 색 | 팔레트 텍스처 `Art/Tiles/Tile_Palette.png`(4×4, Point 필터). 여우와 같은 방식이다. 윗면은 잔디 3톤, 옆면은 윗층 잔디 + 흘러내린 잔디 + 흙 3톤 |
| `MapBuilder.cs` | 문자열 레이아웃(`S` 시작 · `H` 구멍 · `C` 아이템 · `.` 타일)으로 타일 14개를 만들고 여우를 시작 칸에 세운다. 레이아웃 해석은 `MapLayout.cs`에 따로 두었다 |
| `PlayerController.cs` | CharacterController 기반. 3 m/s, 이동 방향으로 회전(720°/s), 중력 -20, 점프 높이 1.2m |
| `CameraRig.cs` | 고정 쿼터뷰(내려다보는 각 45°, 방향 30°, 화각 40°). 맵 모서리가 화면에 딱 들어오는 거리를 계산하고, 입력을 카메라 기준 방향으로 바꾼다 |
| `Prefabs/Player.prefab` | Player(CharacterController 높이 0.8 · 반지름 0.3 + PlayerController) 아래에 `Fox.prefab` |
| 입력 | 템플릿의 `InputSystem_Actions`에서 Player/Move, Player/Jump를 쓴다. WASD, 방향키, 게임패드가 이미 연결되어 있다 |
| 설정 자동화 | `Editor/MapSetup.cs`(메뉴 VoxelFox > Map Setup). 전체 재생성: `bash tools/build_map.sh` |

### 설계 메모
- **타일은 Play 때 만든다:** 타일을 씬에 저장하지 않고 `MapBuilder.Awake`에서 레이아웃 문자열로 만든다. 그래서 레이아웃 문자열만 고치면 맵이 바뀐다. 에디터 씬 뷰에서는 기즈모(초록 타일, 빨간 X 구멍, 노란 아이템, 파란 시작점)로 배치를 확인할 수 있다.
- **같은 타일이 반복돼 보이지 않게:** 칸마다 타일을 90° 단위로 돌리고, 체커 패턴으로 한 칸 걸러 밝기를 0.9배로 낮췄다(계획서의 "체커 패턴 명도 차이").
- **카메라 방향 30°:** 계획서에는 "위에서 약 45°"만 정해져 있었다. 정면(0°)에서 보면 타일 옆면이 거의 안 보이고, 대각선(45°)이면 맵이 마름모가 되어 줄과 칸이 헷갈린다. 그 중간인 30°로 정했다. `CameraRig`의 `yaw` 값으로 바꿀 수 있다.
- **입력은 카메라 기준:** W/↑는 화면 안쪽, D/→는 화면 오른쪽이다. 카메라가 30° 돌아가 있으므로 월드 축과는 조금 다르다.
- **충돌 캡슐 반지름 0.3m:** 여우 몸통 폭 정도로 작게 잡았다. 크게 잡으면 구멍(2m) 가장자리에 걸려 떠 있게 된다. 대신 코와 꼬리는 캡슐 밖으로 나온다.
- **조명 변경 (Main 씬):** 해를 `(50, -30)`에서 `(50, 60)`으로 옮겨 카메라 뒤 왼쪽에서 비추게 했다. 처음 방향은 남동쪽에서 비춰서, 카메라에 보이는 여우와 타일 면이 모두 그늘이었다. 환경광도 기본 하늘(땅 쪽이 갈색)에서 중립색 3단 환경광으로 바꿨다.
- **테스트용 입력:** `PlayerController.readDeviceInput`을 끄면 `MoveInput`과 `RequestJump()`로 조종할 수 있다. 테스트와 미리보기 녹화에서 이 방법을 쓴다.
- **어셈블리 정의 추가:** 테스트에서 게임 스크립트를 참조하려고 `Assets/Scripts/VoxelFox.asmdef`를 만들었다.

### 아직 하지 않은 것 (다음 단계)
- 걷거나 점프해도 여우는 계속 Idle 동작이다. Animator 파라미터 연결은 4단계에서 한다.
- 떨어지면 끝없이 떨어진다. `y < -5`에서 GAME OVER 처리는 6단계에서 한다.
- 레이아웃의 `C` 칸은 좌표만 기록해 둔다. 코인은 5단계에서 놓는다.

### 검증 (완료 기준)
EditMode **18/18 통과**. 기존 여우 테스트 9개에 `MapAssetTests` 9개를 더했다.
- 레이아웃: 타일 14 · 구멍 (1,2)(2,1) · 코인 4 · 시작 (0,3). 줄 길이가 다르거나, 모르는 문자가 있거나, 시작점이 0개 또는 2개면 에러
- 타일 프리팹: 메시와 콜라이더가 2 × 0.5 × 2이고 윗면이 y 0
- 플레이어 프리팹: CharacterController 바닥이 발바닥과 맞고, Fox 모델이 들어 있음
- Main 씬: MapBuilder, CameraRig, Player가 연결되어 있고, 카메라가 45°로 맵 중심을 바라봄

PlayMode **11/11 통과**. 스모크 테스트 1개, `PlayerMovementTests` 9개, 미리보기 녹화 1개. `Time.captureFramerate`로 1/60초 간격을 고정해 결과가 매번 같다.
- 타일 14칸에는 바닥(y 0)이 있고 구멍 2칸에는 없음 (레이캐스트)
- 여우가 시작 칸 위에 서 있음
- 1초 걸으면 3m (±0.1) 이동, 옆으로 새지 않고 이동 방향을 바라봄
- 첫 줄 4칸을 끝까지 걸어도 타일 이음새에서 걸리거나 빠지지 않음
- 점프 최고 높이 1.2m (±0.1) 후 착지. 공중에서 다시 눌러도 더 높이 뛰지 않음
- (1,3)에서 남쪽으로 걸으면 구멍 (1,2)에 빠져 3초 안에 y < -5
- 시작 칸에서 서쪽(맵 밖)으로 걸어도 떨어짐
- 입력 위/오른쪽이 화면 안쪽/오른쪽과 일치

결과 파일: `logs/03_editmode_results.xml`, `logs/03_playmode_results.xml`

미리보기(쿼터뷰, 위, 근접, 녹화 56프레임)에서 흰 여우, 구멍 2개 위치, 점프 그림자, 구멍 (2,1)로 떨어지는 모습을 눈으로 확인했다.

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| EditMode 테스트 컴파일 에러 CS0182 | `[TestCase]` 속성 인자로 `string[]`을 바로 넘길 수 없음 | 레이아웃을 `"S../.."`처럼 `/`로 이은 문자열로 넘기고 테스트 안에서 나눔 |
| 쿼터뷰 캡처에서 흰 여우가 갈색으로 보임 | ① 해가 카메라 반대쪽에 있어서 보이는 면이 모두 그늘이었고, 그늘은 기본 하늘의 갈색 땅 빛을 받음 ② 편집 모드 캡처에서 BakeMesh로 굳힌 여우가 제대로 그려지지 않음 | ① 해 방향과 환경광 변경(위 설계 메모). 이것만으로는 그대로였다. ② 편집 모드 캡처 스크립트를 없애고, 실제 게임처럼 Play 모드에서 찍도록 옮김(`MovementPreviewRecorder`). 흰색으로 정상 표시 |
| 처음 카메라 구도에서 맵이 화면 절반 정도로 작음 | 맵을 둘러싼 구가 화각에 들어가게 거리를 잡아서, 45°로 눌린 세로 방향에 여백이 많이 남음 | 맵 모서리 8곳(타일 바닥 ~ 높이 1m)을 카메라 좌표로 바꿔 각각 들어가는 최소 거리를 계산 |

### 로그 파일
- `logs/03_map_setup.log`: 타일·플레이어 프리팹, 씬 배치
- `logs/03_editmode.log`, `logs/03_editmode_results.xml`: EditMode 테스트
- `logs/03_playmode.log`, `logs/03_playmode_results.xml`: PlayMode 테스트 + 미리보기 캡처
