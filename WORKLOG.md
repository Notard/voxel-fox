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
