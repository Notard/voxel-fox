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

---

## 3-1. 바닥 입체감: 노멀맵 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)**

### 변경 이유
- 사용자 요청이다. 바닥이 색 무늬만 있어서 평평한 판처럼 보였다. 노멀맵으로 복셀의 높낮이와 타일 모서리를 표현해 입체감을 살렸다.

### 바뀐 구조
| 항목 | 이전 | 이후 |
|---|---|---|
| 색 | 4×4 팔레트. 면마다 색 칸 하나를 찍음 | 색 아틀라스 `Tile_Albedo.png` 512×256 (Point 필터). 복셀 1칸 = 16px. 왼쪽 256×256은 윗면, 오른쪽 256×64 네 줄은 옆면 +X/-X/+Z/-Z |
| 입체감 | 없음 | 노멀맵 `Tile_Normal.png` 512×256 (Bilinear, NormalMap 타입), URP Lit `_BumpMap` |
| 메시 | 복셀 면마다 사각형 513장 (정점 2,052) | 면 6장 (정점 24). 무늬와 입체감은 텍스처가 맡는다. 노멀맵을 위해 접선(tangent)도 계산 |

- 팔레트 방식에서는 노멀맵을 쓸 수 없다. 면 하나가 텍스처의 한 점만 찍으므로 노멀맵이 입힐 "표면"이 없기 때문이다. 그래서 텍스처를 실제 그림(아틀라스)으로 바꿨다.
- 복셀 무늬(색 배치, 고정 시드 7)는 이전과 같은 규칙으로 만든다.

### 노멀맵 만드는 방법
1. 복셀마다 색에 따라 높이를 준다. 밝은 잔디는 3px, 보통 잔디는 1.5px, 진한 잔디는 0px이다. 밝은 곳은 솟고 어두운 곳은 패여 보인다. 흙과 옆면 잔디도 같은 방식이다.
2. 높이가 **다른** 이웃 복셀과의 경계에만 폭 3px 경사를 둔다. 같은 높이끼리는 평평하게 이어져서 잔디가 덩어리로 뭉쳐 보인다.
3. 면 바깥은 -3px로 보고 모서리를 깎는다. 타일마다 테두리가 둥글게 보여서 칸이 구분된다.
4. 높이맵의 기울기(중앙 차분)로 접선 공간 노멀을 계산해 PNG로 저장한다.

### 중간에 버린 첫 시도
- 처음에는 **모든 복셀**의 테두리를 똑같이 깎았다. 가까이서 보면 입체감은 강했지만, 욕실 타일처럼 반듯한 격자로 보이고 잔디 느낌이 사라졌다. 그래서 위 2번처럼 높이가 다른 경계에만 턱을 두는 방식으로 바꿨다.

### 검증
- `bash tools/build_map.sh`로 다시 만들었다.
- EditMode **19/19 통과**. `TileMaterial_HasNormalMapAndMeshHasTangents`를 새로 추가했다. 노멀맵이 NormalMap 타입인지, `_NORMALMAP` 키워드가 켜져 있는지(스크립트로 텍스처를 넣으면 저절로 켜지지 않음), 메시에 접선이 있는지 확인한다.
- PlayMode **11/11 통과**. 타일 크기와 콜라이더는 그대로라 이동·낙하 결과도 같다.
- 쿼터뷰와 근접 이미지에서 잔디 덩어리의 턱, 둥근 타일 모서리, 흰 여우를 눈으로 확인했다.

---

## 4단계. 애니메이션 연결 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)** (게임 화면 + 여우 근접 녹화, 30fps)

### 결과 요약
| 항목 | 결과 |
|---|---|
| 파라미터 갱신 | `PlayerController.UpdateAnimator`: 이동한 **뒤의** 실제 속도로 `Speed`(수평 속도), `IsGrounded`, `WalkSpeed`를 넣고, 점프한 프레임에 `Jump` 트리거 |
| 상태 전환 | Idle ⇄ Walk (Speed 0.1 기준) · Any → Jump · Jump → Idle (착지 + 멈춤) · **Jump → Walk (착지 + 이동 중, 새로 추가)** |
| Walk 배속 | 새 파라미터 `WalkSpeed` = 이동 속도 ÷ 0.868 m/s. 3 m/s면 3.46배속 |
| Jump | 도약 직전(프레임 6, offset 0.3)부터 0.7배속으로 튼다. 남은 동작이 체공 시간(0.69초)에 맞는다 |
| Walk 동작 | 다리 흔드는 각도 **±26° → ±40°** (`make_fox.py`의 `WALK_SWING`) |
| Animator 컬링 | Cull Update Transforms → **Always Animate** |
| 에디터 메뉴 | VoxelFox > Play Main Scene (`PlayMain.cs`): Main 씬을 열고 바로 Play |

### 설계 메모
- **발 미끄러짐을 막는 방법:** 걷는 동안 디딘 발은 땅에 붙어 있어야 한다. 그러려면 발이 반 주기 동안 몸 기준으로 뒤로 미는 거리(보폭)만큼 몸이 앞으로 가야 한다. `MapSetup`이 Walk 클립을 샘플링해 앞왼다리 발끝의 앞뒤 폭을 재고, "1배속일 때 맞는 이동 속도" = 보폭 × 2 ÷ 클립 길이를 계산해 Player 프리팹에 넣는다. 게임 중에는 실제 속도 ÷ 이 값으로 Walk 배속을 정한다.
- **다리 각도를 40°로 바꾼 이유:** 26°일 때 보폭은 0.197m이고 1배속 이동 속도는 0.59 m/s다. 3 m/s에 맞추려면 5.1배속이 되어 다리가 1초에 7.6번 왕복한다. 너무 정신없어 보인다. 40°로 늘리면 보폭 0.289m, 0.868 m/s, 3.46배속(1초에 약 5번 왕복)이 된다. 짧은 다리로 종종걸음 치는 느낌이다. 이동 속도 3 m/s(계획서)는 그대로 두었다.
- **점프 후 exit time 제거:** 2단계에서는 "점프 직후 한 프레임은 아직 땅에 붙어 있다"는 이유로 Jump → Idle에 exit time 0.5를 두었다. 이제 `IsGrounded`를 이동한 **뒤에** 넣으므로 점프한 프레임부터 false다. exit time 없이 착지하는 순간 넘어간다.
- **착지할 때 움직이고 있으면 Walk로:** 계획서에는 Jump → Idle만 있었다. 걸으면서 점프하면 착지 순간 잠깐 Idle이 끼어 멈칫해 보이므로 Jump → Walk 전환을 더했다.
- **Always Animate:** FBX 기본값은 화면에 안 그려지는 동안 뼈를 움직이지 않는다. 플레이어는 늘 움직여야 하고, 떨어져서 화면 밖에 있을 때도 마찬가지다.

### 검증 (완료 기준)
EditMode **20/20 통과**
- Animator 파라미터 4개(`WalkSpeed` 추가)
- Fox 프리팹 Animator = Always Animate
- Player 프리팹: PlayerController에 Fox Animator가 연결되어 있고, Walk 1배속 속도가 0.87 m/s (±0.05)

PlayMode **17/17 통과**. 새로 추가한 `FoxAnimationTests` 6개:
- 서 있으면 Idle
- 걸으면 Walk이고, 재생 배속 = 이동 속도 ÷ 1배속 속도
- 멈추면 0.3초 안에 Idle
- 점프하면 Jump(공중에서 IsGrounded = false), 착지 후 Idle
- 걸으면서 점프하면 착지 후 바로 Walk
- **발 미끄러짐:** 1초 동안 걸으며 앞왼다리 발끝의 월드 위치를 240fps로 기록한다. 디딤(발이 가장 앞 → 가장 뒤)마다 땅에서 밀린 거리를 잰다. 결과는 보폭 0.301m, 디딤 4번, **미끄러짐 최대 0.013m, 평균 0.010m**. 1배속으로 틀었다면 디딤마다 약 0.7m 미끄러진다.

결과 파일: `logs/04_editmode_results.xml`, `logs/04_playmode_results.xml`

근접 녹화에서 Idle → Walk(대각선 다리 교차) → Jump(다리를 뻗은 공중 자세) → 착지 후 Walk로 이어지는 것을 눈으로 확인했다.

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| 발 미끄러짐 테스트가 `WaitForEndOfFrame` 에러로 실패 | 배치 모드 테스트에서는 WaitForEndOfFrame을 쓸 수 없음 | 애니메이션이 적용된 뒤 도는 `LateUpdate`에서 기록하는 `FootRecorder` 컴포넌트를 붙임 |
| 60fps 측정에서 미끄러짐이 0.05~0.15m로 들쭉날쭉 | 디딤 한 번이 6프레임 정도라 발 끝점을 정확히 못 잡음 | 이 테스트만 240fps로 측정 |
| 끝점을 1초에 16번이나 잡음 (통과는 했지만 잘못된 측정) | 곡선의 작은 흔들림을 끝점으로 잡아 디딤이 쪼개짐 | 앞뒤 ±10프레임 안의 최댓값/최솟값만 끝점으로 인정하고, 보폭의 70% 이상 움직인 구간만 디딤으로 셈 |
| 고친 뒤 보폭이 0으로 나옴 → **다리가 전혀 움직이지 않고 있었음** | Animator 컬링(Cull Update Transforms) 때문에 화면 렌더링이 없는 배치 모드에서 뼈가 갱신되지 않았다. 앞의 "통과"는 발이 몸에 붙은 채 움직인 것을 잘못 잰 결과였다 | Fox 프리팹 Animator를 Always Animate로 바꾸고 테스트 추가. 그 뒤 보폭 0.301m, 미끄러짐 1.3cm로 제대로 측정됨 |

### 함께 바뀐 것
- `make_fox.py`의 Walk 각도가 바뀌어 `bash tools/build_fox.sh`로 여우를 다시 만들었다. 여우 미리보기 이미지와 fox_preview.html 설명(±40°)도 갱신했다.
- `tools/build_fox.sh`, `tools/build_map.sh`: 로그 이름 앞머리를 `TAG` 환경 변수로 바꿀 수 있게 했다(예: `TAG=04 bash tools/build_map.sh`). 이전 단계 로그를 덮어쓰지 않기 위해서다.
- 미리보기 녹화를 60fps 게임 → 30fps 저장으로 올렸다(Walk 한 주기 0.19초를 약 6장으로 담기 위해). 여우를 따라가는 근접 화면(`close_###.jpg`)도 추가했다.

### 로그 파일
- `logs/04_fox_setup.log`, `logs/04_unity_capture.log`: 여우 재생성
- `logs/04_map_setup.log`: 플레이어 프리팹(보폭 측정값 포함)
- `logs/04_editmode.log`, `logs/04_editmode_results.xml`, `logs/04_playmode.log`, `logs/04_playmode_results.xml`

---

## 4-1. 카메라 추적 + 격자 방향 이동 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)**

### 변경 이유 (사용자 피드백)
Unity에서 직접 플레이해 본 사용자가 두 가지를 요청했다.
1. **"캐릭터 중심으로 카메라가 따라가야 해."** 3단계 카메라는 맵 전체가 보이는 고정 카메라여서 여우가 화면 구석에서 작게 움직였다.
2. **"쿼터뷰인데 화살표를 누르면 대각선으로 움직여."** 카메라가 격자에서 30° 돌아가 있는데, 입력은 "화면 위쪽 = ↑"로 바꾸고 있었다. 화면 위쪽은 타일 격자 기준으로 비스듬한 방향이라, ↑를 누르면 여우가 타일 줄을 비스듬히 가로질렀다.

### 바뀐 것
| 항목 | 이전 (3단계) | 이후 |
|---|---|---|
| 카메라 | 맵 모서리가 모두 들어오는 고정 위치 | 여우 몸통 가운데를 화면 중앙에 두고 따라감. 거리 11m, 따라가는 부드러움 0.15초 |
| 카메라 각도 | 내려다보는 각 45°, 방향 30°, 화각 40° | 그대로 |
| 떨어질 때 | 해당 없음 | 높이 -1.5m 아래로는 따라가지 않음 → 여우가 아래로 사라지는 모습이 보인다 |
| ↑ 입력 | 화면 위쪽 (격자에서 30° 틀어짐) | **북쪽(+Z)**: 화면 위쪽에서 가장 가까운 격자 축 |
| → 입력 | 화면 오른쪽 | **동쪽(+X)** |
| 두 키 동시 | 화면 기준 대각선 | 격자 대각선 (예: ↑+→ = 북동) |

- 격자 축은 카메라 방향(yaw)을 90° 단위로 반올림해 구한다. 30°는 0°가 되므로 ↑ = 북쪽이다. 나중에 카메라 방향을 바꿔도 가장 가까운 축을 따라간다.
- 카메라 각도(30°)는 바꾸지 않았다. 대안으로 카메라를 격자에 정면(0°)으로 맞추면 ↑가 화면 위쪽과 북쪽이 모두 되지만, 타일 옆면이 거의 안 보여 쿼터뷰 느낌이 사라진다(3단계 설계 메모).
- `MapBuilder.HalfExtents`는 고정 카메라 구도 계산에만 쓰여서 지웠다.

### 검증
- `TAG=04b bash tools/build_map.sh`로 씬을 다시 배치하고 테스트했다.
- EditMode **20/20 통과**. `MainScene_IsWired`는 이제 카메라가 여우를 따라가도록 연결되어 있는지, 45°로 여우를 화면 중앙에 두는지 확인한다.
- PlayMode **19/19 통과**. 새 테스트 3개:
  - `Input_FollowsGridAxes`: ↑ = +Z, → = +X이고, ↑가 화면 안쪽과 45° 이내
  - `UpArrow_MovesStraightNorth`: ↑를 1초 누르면 북쪽으로 3m(±0.1), 옆으로 0m(±0.02) → 사용자가 말한 대각선 이동이 없음
  - `Camera_FollowsPlayerAtCenter`: 시작할 때와 멈춘 뒤에는 여우가 화면 중앙(2% 이내), 걷는 동안에도 15% 이내
- 기존 이동·애니메이션 테스트도 모두 통과했다(발 미끄러짐 최대 1.3cm 그대로).
- 녹화 프레임에서 여우가 늘 화면 중앙에 있고, 구멍에 빠지면 카메라가 멈춘 채 여우가 아래로 사라지는 것을 확인했다.
- 결과 파일: `logs/04b_editmode_results.xml`, `logs/04b_playmode_results.xml`

### 참고: Unity 에디터 시작 시 Search 에러
에디터를 켤 때 Console에 `ArgumentOutOfRangeException (SearchDatabase.GetDefaultSearchDatabase)`가 한 번 나왔다. Unity 내부 검색 색인 기능의 에러이고 게임 코드와는 관계없다. 기본 색인(`UserSettings/Search.index`)이 있는 상태에서도 나서, 색인을 새로 만드는 방법은 효과가 없었다. 대신 에디터를 켤 때 색인을 갱신하는 설정을 껐다(`UserSettings/Search.settings`의 `indexOnEditorStartup = false`). 그 뒤 실행에서는 에러가 나지 않았다. 이 설정은 PC별 설정이라 Git에는 포함되지 않는다.

---

## 4-2. 카메라 거리 1.5배 + 여우 가운데 고정 — ✅ 완료 (2026-09-29)

### 변경 이유 (사용자 피드백)
4-1 결과를 Unity에서 본 사용자가 요청했다: **"카메라를 좀 더 멀리하자. 1.5배 정도 멀리 가고, 여우를 카메라 가운데 배치해야 해."**
- 거리 11m에서는 여우 주변 몇 칸만 보여 맵 전체 모양(구멍 위치)을 파악하기 어려웠다.
- 4-1 카메라는 0.15초 늦게 따라갔다(SmoothDamp). 걷는 동안 여우가 진행 방향으로 화면 폭의 약 6%(0.45m)만큼 밀려나 가운데에서 벗어나 보였다.

### 바뀐 것
| 항목 | 4-1 | 4-2 |
|---|---|---|
| 거리 | 11m | **16.5m** (1.5배). 맵 4×4 전체와 둘레가 한 화면에 보인다 |
| 앞뒤·좌우 따라가기 | 0.15초 지연 | **지연 없음** → 걷는 동안에도 여우가 정확히 가운데 |
| 높이 따라가기 | 0.15초 지연 | 0.2초 지연 (점프 때 화면 전체가 1.2m씩 출렁이지 않게) |

- 점프할 때만 여우가 화면에서 잠깐 위로 올라갔다 내려온다. 이것도 딱 붙이면 점프마다 땅과 배경 전체가 위아래로 움직여 어지럽다.
- 거리 값은 씬에 저장되어 코드 기본값보다 우선한다. 그래서 `MapSetup`이 씬을 배치할 때 16.5m를 직접 넣는다.

### 검증
- `TAG=04c bash tools/build_map.sh`: EditMode **20/20**, PlayMode **20/20 통과**
- `Camera_FollowsPlayerAtCenter` 기준을 강화했다. 걷는 1.5초 동안 매 프레임 여우가 화면 가운데에서 **1% 이내**여야 한다(4-1은 15%).
- 새 테스트 `Camera_Is16_5mFromPlayer`: 카메라와 여우 몸통 가운데의 거리가 16.5m(±0.05)인지 확인한다.
- 녹화 프레임에서 걷는 동안 여우가 가운데에 있고, 점프 때만 살짝 위로 올라가는 것을 확인했다.
- 결과 파일: `logs/04c_editmode_results.xml`, `logs/04c_playmode_results.xml`

### 되돌림 (2026-09-29)
- **이유:** 사용자가 Unity Game 창의 Scale이 **5배**로 되어 있었다는 것을 발견했다. 화면이 확대되어 있어서 카메라가 가깝고 여우가 가운데에서 벗어난 것처럼 보였다. 사용자 요청으로 4-2 변경을 되돌리고 원래 카메라로 다시 확인하기로 했다.
- **방법:** `git revert`로 4-2의 코드·씬·테스트·계획서·미리보기 변경만 되돌렸다(`CameraRig` 거리 11m, 0.15초 부드러운 추적, `MapSetup`의 거리 설정 제거, 테스트 기준 15%로 복귀). 위 4-2 기록과 `logs/04c_*` 결과 파일은 이력으로 남겼다.
- **검증:** `TAG=04d bash tools/build_map.sh` → EditMode **20/20**, PlayMode **19/19 통과**. 씬의 카메라 거리 11m를 확인했다.
- **참고:** Game 창 왼쪽 위의 Scale 슬라이더를 1x로 두어야 실제 게임 화면과 같게 보인다.

---

## 4-3. 점프 거리 늘리기 — ✅ 완료 (2026-09-29)

### 변경 이유 (사용자 피드백)
**"점프 거리가 너무 짧아서 구멍에 잘 빠진다."**
- 기존 값(높이 1.2m, 중력 -20)으로는 체공 시간이 0.69초였다. 3 m/s로 달리며 뛰면 **2.08m**를 간다.
- 구멍 너비가 2m라 가장자리에 딱 붙어서 뛰어야 겨우 넘었다. 조금만 일찍 뛰어도 구멍에 빠졌다.

### 바뀐 것
| 항목 | 이전 | 이후 |
|---|---|---|
| 점프 높이 | 1.2m | **1.5m** |
| 중력 | -20 m/s² | **-15 m/s²** |
| 체공 시간 | 0.69초 | 0.89초 |
| 달리며 뛴 거리 | 2.08m | **2.68m** (구멍 너비의 1.34배) |
| Jump 동작 재생 속도 | 0.7배 | 0.53배. 남은 동작(프레임 6~20, 0.47초)을 체공 0.89초에 맞춤 |

- 이동 속도(3 m/s)는 바꾸지 않았다. 속도를 올리면 Walk 배속과 발 맞춤(4단계)이 함께 바뀌고, 좁은 맵에서 조작이 어려워진다.
- 높이만 올리면(중력 -20 그대로) 1.5m로도 2.3m라 여유가 적다. 그래서 중력도 낮춰 공중에 떠 있는 시간을 늘렸다. 대신 떨어지는 속도도 느려져 조금 더 둥실 뜨는 느낌이 난다.
- `PlayerController.AirTime`(체공 시간 계산)을 추가해 테스트가 값을 직접 쓰지 않고 이 계산을 쓰게 했다.

### 검증
- `FoxSetup`(Jump 재생 속도)과 `TAG=04e bash tools/build_map.sh` 실행: EditMode **20/20**, PlayMode **21/21 통과**
- 새 테스트:
  - `RunningJump_ClearsHole`: (0, 2)에서 동쪽으로 달리다가 구멍 (1, 2) 가장자리 **0.4m 앞**에서 뛰면, 구멍을 넘어 건너편 (2, 2) 타일 위(y 0)에 착지한다. 기존 값이었다면 착지 지점이 구멍 안쪽(약 0.3m)이다.
  - `JumpDistance_IsWellOverHoleWidth`: 이동 속도 × 체공 시간 > 구멍 너비 × 1.3
- 기존 테스트도 모두 통과했다. 점프 최고 높이 1.5m(±0.1), 점프 후 Idle/Walk 전환, 구멍·가장자리 낙하(3초 안에 y < -5), 발 미끄러짐 최대 1.3cm.
- 결과 파일: `logs/04e_editmode_results.xml`, `logs/04e_playmode_results.xml`

---

## 5단계. 아이템 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)** (코인 근접 이미지, 코인 2개를 먹는 이동 녹화 + 카운터)

### 결과 요약
| 항목 | 결과 |
|---|---|
| 코인 모델 | `Prefabs/Coin.prefab`: 복셀 240개(1복셀 0.05m), 지름 0.6m, 두께 0.1m + 가운데 마름모 무늬가 앞뒤로 0.05m씩 도드라짐. 보이는 면만 메시로 만들었다(면 336). 팔레트 4색(테두리·금색·무늬·반짝임), 살짝 스스로 빛남(Emission) |
| `Collectible.cs` | 120°/s로 돌고, ±0.1m 폭으로 초당 0.8번 둥실. 코인마다 위치로 박자를 달리한다. 여우(PlayerController)가 트리거에 닿으면 반짝이를 남기고 `Collected` 이벤트를 보낸 뒤 사라진다 |
| 반짝이 | `Prefabs/CoinBurst.prefab`: 금색 작은 큐브 24개가 사방으로 튀었다가 작아지며 사라지는 파티클. 한 번만 재생하고 스스로 지워진다 |
| 배치 | `MapBuilder`가 레이아웃의 `C` 칸 4곳에 코인을 높이 0.6m로 놓는다 (`Items` 아래) |
| `GameManager.cs` | 코인 수(4)와 먹은 수를 센다. 6단계에서 Playing / GameOver / Clear 상태를 더할 자리다 |
| `GameUI.cs` | 화면 좌상단 반투명 판 위에 `아이템 0 / 4` (금색 글자) |
| 설정 자동화 | `Editor/ItemSetup.cs`(메뉴 VoxelFox > Item Setup). `tools/build_map.sh`의 2단계로 실행 |

### 설계 메모
- **한글 글꼴:** TMP 기본 글꼴(LiberationSans)에는 한글이 없어 `아이템`이 네모로 나온다. 맑은 고딕은 재배포가 허용되지 않아 공개 저장소에 넣을 수 없다. 게임이 Windows 전용이므로 실행할 때 설치된 맑은 고딕(굵게)으로 TMP 글꼴을 만든다(`TMP_FontAsset.CreateFontAsset`). 맑은 고딕이 없으면 Noto Sans KR을 쓰고, 둘 다 없으면 경고를 남기고 기본 글꼴을 쓴다.
- **코인 높이 0.6m, 트리거 반지름 0.4m:** 여우 몸통(0.5m)과 귀 끝(0.84m) 사이에 있어서 걷기만 해도 닿는다. 트리거를 코인(반지름 0.3m)보다 조금 크게 잡아 스치기만 해도 먹힌다.
- **Kinematic Rigidbody:** 코인이 둥실거리며 움직이므로, 트리거 판정이 정확하도록 Kinematic Rigidbody를 붙였다.
- **카운터 연결:** `Collectible.Collected`(정적 이벤트) → `GameManager`가 개수를 세고 `ItemsChanged` 이벤트 → `GameUI`가 글자를 갱신한다. 서로 직접 참조하지 않아서 6·7단계에서 재시작할 때 정리하기 쉽다.

### 검증 (완료 기준)
EditMode **23/23 통과**. 새 `ItemAssetTests` 3개:
- 코인 프리팹: 크기 0.6 × 0.6 × 0.2m, 트리거, Kinematic Rigidbody, Collectible, Point 필터 팔레트
- 반짝이: 반복 없음, 자동 재생, 끝나면 스스로 삭제
- Main 씬: MapBuilder에 코인 프리팹이, GameManager에 맵이 연결되어 있고, 카운터가 좌상단에 붙어 있음

PlayMode **26/26 통과**. 새 `ItemTests` 5개:
- 레이아웃의 코인 칸 4곳에 코인이 높이 0.6m로 있음
- 시작하면 `아이템 0 / 4`이고, 글꼴에 한글·숫자가 모두 있음
- 코인이 돌고, 위아래로 0.1~0.25m 폭으로 움직임
- 동쪽으로 걸어 코인 (3, 3)에 닿으면: 코인이 사라지고 `아이템 1 / 4`, 반짝이가 생겼다가 1.5초 안에 사라짐
- 코인 4개를 모두 먹으면 `아이템 4 / 4`, 코인이 모두 사라짐

미리보기 녹화에서는 여우가 코인 (3, 3), (3, 1)을 먹을 때 카운터가 0 → 1 → 2로 오르고, 서쪽 구멍 (2, 1)에 떨어지는 것까지 확인했다. 결과 파일: `logs/05_editmode_results.xml`, `logs/05_playmode_results.xml`

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| 기존 테스트 `Map_BuildsTilesAndHoles`가 코인 칸 높이를 잘못 잴 수 있음 | 아래로 쏘는 레이캐스트가 코인 트리거에 먼저 맞음 | 레이캐스트에서 트리거를 무시 (`QueryTriggerInteraction.Ignore`) |
| 한글 글꼴 확인 테스트가 숫자를 "없음"으로 판단할 수 있음 | 실행 중에 만든 글꼴은 실제로 쓴 글자만 담고 있음 | `HasCharacters(..., tryAddCharacter: true)`로 확인 |
| 녹화 화면에 카운터(UI)가 안 찍힘 | 화면 오버레이 UI는 카메라 렌더에 포함되지 않음 | 녹화하는 동안만 UI를 카메라 기준(Screen Space Camera)으로 바꾸고, 카메라가 늘 녹화용 텍스처에 그리게 함 |
| 테스트 컴파일 에러 CS0019 | `HasCharacters`의 이 오버로드는 없는 글자를 `uint[]`로 돌려줌 | 타입에 맞게 고침 |

### 로그 파일
- `logs/05_map_setup.log`, `logs/05_item_setup.log`, `logs/05_editmode.log`, `logs/05_playmode.log`, 결과 `logs/05_*_results.xml`

---

## 6단계. 게임 종료 / 성공 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)** (GAME OVER / CLEAR! 결과 화면, 이동 녹화 끝의 GAME OVER)

### 결과 요약
| 항목 | 결과 |
|---|---|
| `GameManager.cs` | 상태 `Playing` → `GameOver` 또는 `Clear`. Playing 동안 걸린 시간을 잰다. 여우가 y < -5로 떨어지면 GAME OVER, 코인을 모두 모으면 CLEAR. 결과가 나오면 `StateChanged` 이벤트를 보내고 여우 조작을 끈다 |
| 조작 정지 | `PlayerController.ControlEnabled`: 끄면 이동·점프 입력을 무시한다. 중력은 계속 받는다 |
| 결과 패널 2종 | GAME OVER(붉은 상자, 분홍 제목, "떨어졌어요 · 아이템 n / 4"), CLEAR!(갈색 상자, 금색 제목, "아이템 4개 모두 모음 · 12.3초"). 화면 전체를 살짝 어둡게 덮고, 상자는 화면 위쪽에 둔다 |
| UI 설정 분리 | UI 만드는 코드를 `ItemSetup`에서 `Editor/UISetup.cs`로 옮겼다(카운터 + 결과 패널). `Editor/SceneBuild.cs`가 맵 → 아이템 → UI 설정을 Unity 한 번 실행으로 이어서 한다(`tools/build_map.sh` 1단계) |

### 설계 메모
- **결과 패널을 화면 위쪽에:** 처음에는 화면 가운데에 두었는데, 카메라가 여우를 가운데에 두므로 CLEAR 순간 여우가 패널 뒤에 가려졌다. 상자를 250px 위로 올려 여우가 보이게 했다.
- **GAME OVER 뒤에도 떨어짐:** 조작만 끄고 중력은 그대로 둬서, 여우가 화면 밖으로 떨어지는 모습이 이어진다.
- **CLEAR는 뒤집히지 않음:** 결과가 한 번 나오면 상태가 바뀌지 않는다. CLEAR 뒤에 떨어져도 GAME OVER가 되지 않고, GAME OVER 뒤에 코인에 닿아도 세지 않는다.
- **걸린 시간:** 계획서의 선택 항목이다. Playing 동안만 흐르고, 결과가 나오면 멈춘다. CLEAR 패널에 0.1초 단위로 보여 준다.
- **글꼴:** 결과 패널 글자도 한글이 섞여 있어서, `GameUI`가 UI 아래 모든 글자에 맑은 고딕을 입힌다.

### 검증 (완료 기준)
EditMode **25/25 통과**. `MainScene_HasHiddenResultPanel` 2개 추가: 패널·제목·설명이 연결되어 있고 처음에는 꺼져 있다.

PlayMode **31/31 통과**. 새 `GameFlowTests` 5개:
- 시작하면 Playing, 패널 둘 다 꺼짐, 조작 가능
- 서쪽 맵 밖으로 떨어지면 y < -5에서 GAME OVER: GAME OVER 패널만 켜지고 제목 "GAME OVER", 조작 꺼짐. 입력을 넣어도 옆으로 움직이지 않음
- 코인 4개를 모두 먹으면 CLEAR: CLEAR 패널만 켜지고 제목 "CLEAR!", 설명에 걸린 시간. 이후 이동·점프 입력을 넣어도 움직이지 않고, 걸린 시간도 멈춤
- CLEAR 뒤에 맵 밖으로 떨어져도 GAME OVER로 바뀌지 않음
- Playing 동안 걸린 시간이 실제 시간만큼 흐름 (1초 ± 0.05)

미리보기: 이동 녹화 끝에 구멍에 떨어지면 GAME OVER 패널이 뜨고, 씬을 다시 불러 코인 4개를 먹으면 CLEAR! 패널이 뜨는 것을 캡처해 확인했다. 결과 파일: `logs/06_editmode_results.xml`, `logs/06_playmode_results.xml`

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| `UISetup` 컴파일 에러 CS1503 | 글자 생성 함수에 GameObject를 넘김 (Transform이 필요) | `itemPanel.transform`으로 고침 |
| CLEAR 화면에서 여우가 패널에 가려짐 | 카메라가 여우를 가운데에 두는데 패널도 가운데 | 패널 상자를 화면 위쪽(+250px)으로 옮김 |

### 로그 파일
- `logs/06_scene_build.log`, `logs/06_editmode.log`, `logs/06_playmode.log`, 결과 `logs/06_*_results.xml`

---

## 7단계. 재시작 — ✅ 완료 (2026-09-29)

미리보기: **[map_preview.html](map_preview.html)** (GAME OVER 패널의 [다시 하기] 버튼, GAME OVER → 다시 하기 → CLEAR!)

### 결과 요약
| 항목 | 결과 |
|---|---|
| 재시작 방식 | `GameManager.Restart()`: 계획대로 현재 씬을 다시 불러 모든 것을 처음 상태로. 같은 프레임에 여러 번 불려도 한 번만 한다 |
| R 키 | 결과가 나오기 전 플레이 중에도 언제든 누를 수 있다. 게임패드 Start도 같다. `InputSystem_Actions`에 재시작 동작이 없어서 `GameManager`가 직접 만든다 |
| [다시 하기] 버튼 | GAME OVER / CLEAR! 상자 아래쪽 금색 버튼 "다시 하기  (R)". `UISetup`이 버튼 → `GameManager.Restart`를 씬에 저장되는 영구 연결로 잇는다. 결과가 나오면 버튼이 선택되어 Enter(게임패드 A)로도 누를 수 있다 |
| EventSystem | 버튼을 누르려면 필요하다. 이 프로젝트는 Input System 패키지만 켜져 있으므로 `InputSystemUIInputModule`을 쓴다(기본 UI 입력 동작 연결) |
| 결과 상자 | 버튼 자리를 위해 높이 340 → 440, 화면 가운데에서 230px 위 |

### 설계 메모
- **정적 이벤트 정리:** 코인 획득은 정적 이벤트(`Collectible.Collected`)로 알린다. 씬을 다시 불러도 옛 GameManager가 이벤트에 남아 있으면 코인을 두 번 센다. 그래서 `OnDisable`에서 반드시 떼고, 두 번 재시작한 뒤에도 한 번만 세는지 테스트로 확인했다.
- **한글 글꼴:** 실행 중에 만든 맑은 고딕 글꼴은 정적 변수에 두고 재시작 뒤에도 다시 쓴다. 사라졌으면 Unity의 null 검사로 알아채 다시 만든다.

### 검증 (완료 기준)
EditMode **27/27 통과**. `MainScene_RestartButtonCallsGameManagerRestart` 2개 추가: 두 결과 패널의 버튼이 `GameManager.Restart`에 연결되어 있고, `InputSystemUIInputModule`이 붙은 EventSystem이 있다.

PlayMode **35/35 통과**. 새 `RestartTests` 4개. R 키는 가상 키보드 장치를 붙여 실제 키 입력으로 눌렀다.
- 플레이 중(코인 1개 먹은 상태)에 R → 옛 GameManager가 사라지고 처음 상태
- GAME OVER 뒤 [다시 하기] 버튼(글자 "다시 하기  (R)") → 처음 상태
- CLEAR 뒤 R → 처음 상태
- 두 번 재시작한 뒤 코인 하나를 먹으면 `아이템 1 / 4` (두 번 세지 않음)

"처음 상태"는 Playing, 아이템 0 / 4(글자 포함), 걸린 시간 0.5초 미만, 결과 패널 둘 다 꺼짐, 여우가 시작 칸 위, 조작 가능, 코인 4개가 모두 있는 것이다.

미리보기 녹화도 GAME OVER 뒤에 `Restart()`로 다시 시작해 코인 4개를 먹고 CLEAR!까지 간다. 결과 파일: `logs/07_editmode_results.xml`, `logs/07_playmode_results.xml`

### 발생한 문제와 해결
| 문제 | 원인 | 해결 |
|---|---|---|
| R 키 테스트 2개 실패 (버튼 테스트는 통과) | Input System은 기본적으로 Game 창에 포커스가 있을 때만 키보드를 받는데, 배치 모드 테스트에는 포커스가 없다 | 테스트 동안만 입력 설정을 "포커스와 관계없이 받기"로 바꾸고 끝나면 원래 값으로 되돌림. 프로젝트 설정 파일은 바뀌지 않는다 |
| 위 설정을 되돌릴 때 테스트 정리 단계 에러 | 설정 객체를 통째로 바꿨다가, 되돌릴 원래 객체가 씬 전환 중 사라짐 | 객체를 바꾸지 않고 두 값만 바꿨다가 되돌림 |

### 로그 파일
- `logs/07_scene_build.log`, `logs/07_editmode.log`, `logs/07_playmode.log`, 결과 `logs/07_*_results.xml`
