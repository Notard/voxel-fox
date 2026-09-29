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
