# 할 일 목록 — 복셀 여우 타일 게임

> 상세 내용: [plan.md](plan.md) · 작업이 끝날 때마다 `[ ]` → `[x]`로 체크하고 완료 날짜를 적는다.
> 체크 후 `python tools/make_todo_html.py` 실행 → 진행률 갱신 + [todo.html](todo.html) 재생성.
> 작업별 상세 결과: [WORKLOG.md](WORKLOG.md)

**진행률: 29 / 52**

---

## 1. Unity 프로젝트 생성
- [x] Unity 6000.5.7f1 · Universal 3D 템플릿으로 `C:\sample\VoxelFox` 프로젝트 생성
- [x] Input System 패키지 설치 및 Active Input Handling 설정
- [x] TextMeshPro 설치 (Essential Resources 임포트)
- [x] 폴더 구조 생성 (`Art`, `Prefabs`, `Scenes`, `Scripts`)
- [x] `Scenes/Main.unity` 생성 후 Play 모드에서 에러 없이 실행 확인 ✅ 완료 기준
- [x] 템플릿 샘플 정리 (`SampleScene`, `TutorialInfo`, `Readme` 삭제)

## 1-1. Git 저장소 연결
- [x] `C:\sample`에서 `git init` (기본 브랜치 `main`)
- [x] `.gitignore` 작성 (Unity 자동 생성 폴더, IDE 파일, `logs/*.log` 제외)
- [x] `.gitattributes` 작성 (줄바꿈 정리, 바이너리는 Git LFS)
- [x] 첫 커밋 (`d5ca66f`)
- [x] GitHub 저장소 `Notard/voxel-fox` (Public) 생성 후 `origin`으로 연결
- [x] `main` push 후 `origin/main`과 동기화 확인 ✅ 완료 기준

## 2. 복셀 여우 캐릭터 (Blender Python)
- [x] `C:\sample\Blender\make_fox.py` 작성: 파츠별 복셀 데이터 정의 (몸통, 머리, 귀, 다리 4개, 꼬리)
- [x] 보이는 면만 메시로 만들기 + 색상(버텍스 컬러 또는 팔레트) 적용
- [x] Armature 생성 (Root, Body, Head, Leg_FL/FR/BL/BR, Tail) + 강체 스키닝
- [x] `Idle` 액션 (0–40, 루프)
- [x] `Walk` 액션 (0–20, 루프, 대각선 다리 교차)
- [x] `Jump` 액션 (0–20)
- [x] FBX 내보내기 → `Assets/Art/Characters/Fox/Fox.fbx`
- [x] Unity 임포트: Rig Generic, Idle/Walk에 Loop Time 체크
- [x] `Fox.controller` Animator Controller 생성 (Speed, IsGrounded, Jump)
- [x] 미리보기 만들기: Blender 렌더(4방향 + 애니메이션 프레임) + Unity 캡처 → [fox_preview.html](fox_preview.html)
- [x] 씬에서 세 클립 재생 확인, 복셀 형태가 깨지지 않는지 확인 ✅ 완료 기준

## 2-1. 디자인 변경: 흰 여우 · 0.9배
- [x] 팔레트 변경: 주황 털 → 흰색 3톤, 배·볼·꼬리 끝 → 크림, 귀 안쪽 → 분홍, 다리 갈색·귀 끝·발끝 검정 → 흰색·연회색
- [x] 크기 0.9배 (`SCALE = 0.9`, 1복셀 0.0625m → 0.05625m)
- [x] 미리보기 다시 만들기 (흰색이 제대로 보이도록 Blender 렌더를 EEVEE + Standard 색 변환으로)
- [x] 계획서, 미리보기 페이지, 테스트 기준값(몸길이 1.24m) 갱신
- [x] 변경 이유 기록 → [WORKLOG.md](WORKLOG.md)
- [x] 재생성 후 EditMode 9/9 · PlayMode 1/1 통과 ✅ 완료 기준

## 3. 4x4 타일맵 + 이동
- [ ] 복셀 잔디 타일 프리팹 (2m × 0.5m × 2m)
- [ ] `MapBuilder.cs`: 문자열 레이아웃으로 타일, 구멍 2개, 시작점 생성
- [ ] `PlayerController.cs`: WASD/방향키 이동, 회전, 중력, Space 점프
- [ ] `CameraRig.cs`: 고정 쿼터뷰 카메라, 카메라 기준 입력 방향 변환
- [ ] 걷기, 점프, 구멍 낙하 동작 확인 ✅ 완료 기준

## 4. 애니메이션 연결
- [ ] PlayerController에서 Animator 파라미터(Speed, IsGrounded, Jump) 갱신
- [ ] 상태 전환 설정: Idle ⇄ Walk, Any → Jump → Idle
- [ ] 이동 속도에 맞춰 Walk 재생 속도 조정 (발 미끄러짐 최소화)
- [ ] Idle / Walk / Jump 전환이 자연스러운지 확인 ✅ 완료 기준

## 5. 아이템
- [ ] 복셀 코인 프리팹 제작
- [ ] `Collectible.cs`: 회전 + 둥실 효과, Trigger 획득, 파티클
- [ ] MapBuilder로 코인 4개 배치
- [ ] `GameUI.cs`: 좌상단 `아이템 0 / 4` 카운터
- [ ] 획득하면 카운트가 오르고 코인이 사라지는지 확인 ✅ 완료 기준

## 6. 게임 종료 / 성공
- [ ] `GameManager.cs`: Playing / GameOver / Clear 상태
- [ ] Y < -5이면 GAME OVER
- [ ] 아이템 4/4이면 CLEAR
- [ ] 결과 패널 UI 2종 + 결과가 나오면 조작 정지
- [ ] 두 조건 모두 동작하는지 확인 ✅ 완료 기준

## 7. 재시작
- [ ] 결과 패널에 [다시 하기] 버튼 추가
- [ ] R 키로 언제든 재시작
- [ ] 재시작 후 여우, 아이템, UI가 모두 초기 상태로 돌아오는지 확인 ✅ 완료 기준

## 8. 마무리
- [ ] Windows 빌드 (`Build/VoxelFox.exe`) 후 처음부터 끝까지 플레이 테스트

---

## 완료 기록
| 날짜 | 단계 | 메모 |
|---|---|---|
| 2026-09-29 | 1. Unity 프로젝트 생성 | 5/5 완료 · PlayMode 스모크 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 1. 추가 정리 | 템플릿 샘플(SampleScene, TutorialInfo, Readme) 삭제 · 재검증 통과 · 이유: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | Git 연결 | GitHub `Notard/voxel-fox` (Public) 생성 · push · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 2. 복셀 여우 캐릭터 | 11/11 완료 · EditMode 9/9 · PlayMode 1/1 통과 · 미리보기 [fox_preview.html](fox_preview.html) · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 2-1. 흰 여우 · 0.9배 | 더 예쁜 여우로 만들기 위해 붉은 여우 → 흰 여우, 크기 0.9배 · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
