# 할 일 목록 — 복셀 여우 타일 게임

> 상세 내용: [plan.md](plan.md) · 작업이 끝날 때마다 `[ ]` → `[x]`로 체크하고 완료 날짜를 적는다.
> 체크 후 `python tools/make_todo_html.py` 실행 → 진행률 갱신 + [todo.html](todo.html) 재생성.
> 작업별 상세 결과: [WORKLOG.md](WORKLOG.md)

**진행률: 81 / 81**

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
- [x] 복셀 잔디 타일 프리팹 (2m × 0.5m × 2m)
- [x] `MapBuilder.cs`: 문자열 레이아웃으로 타일, 구멍 2개, 시작점 생성
- [x] `PlayerController.cs`: WASD/방향키 이동, 회전, 중력, Space 점프
- [x] `CameraRig.cs`: 고정 쿼터뷰 카메라, 카메라 기준 입력 방향 변환
- [x] 미리보기 만들기: Play 모드 캡처(정지 3장 + 이동 녹화) → [map_preview.html](map_preview.html)
- [x] 걷기, 점프, 구멍 낙하 동작 확인 ✅ 완료 기준

## 3-1. 바닥 입체감: 노멀맵
- [x] 타일 텍스처를 4×4 팔레트에서 색·노멀 아틀라스(512×256, 복셀 1칸 16px)로 변경, 메시 513면 → 6면
- [x] 노멀맵: 높이가 다른 복셀 경계에만 턱, 타일 바깥 모서리는 둥글게
- [x] 미리보기 다시 만들기 → [map_preview.html](map_preview.html)
- [x] 변경 이유 기록 → [WORKLOG.md](WORKLOG.md)
- [x] 재생성 후 EditMode 19/19 · PlayMode 11/11 통과 ✅ 완료 기준

## 4. 애니메이션 연결
- [x] PlayerController에서 Animator 파라미터(Speed, IsGrounded, Jump) 갱신
- [x] 상태 전환 설정: Idle ⇄ Walk, Any → Jump → Idle
- [x] 이동 속도에 맞춰 Walk 재생 속도 조정 (발 미끄러짐 최소화)
- [x] 미리보기에 여우 근접 녹화 추가 → [map_preview.html](map_preview.html)
- [x] Idle / Walk / Jump 전환이 자연스러운지 확인 ✅ 완료 기준

## 4-1. 카메라 추적 + 격자 방향 이동 (사용자 피드백)
- [x] 카메라가 여우를 화면 중앙에 두고 부드럽게 따라가기 (쿼터뷰 45°/30° 유지, 거리 11m)
- [x] 화살표 입력을 가장 가까운 격자 축으로: ↑ 북쪽, → 동쪽 (타일 줄을 따라 곧게)
- [x] 테스트 추가 (↑ 1초 = 북쪽 3m·옆으로 0m, 카메라 중앙 유지, 입력 = 격자 축)
- [x] 변경 이유 기록 → [WORKLOG.md](WORKLOG.md)
- [x] 재배치 후 EditMode 20/20 · PlayMode 19/19 통과 ✅ 완료 기준

## 4-2. 카메라 거리 1.5배 + 여우 가운데 고정 (사용자 피드백)
- [x] 카메라 거리 11m → 16.5m
- [x] 앞뒤·좌우는 지연 없이 따라가 걷는 동안에도 여우가 화면 가운데 (높이만 0.2초 부드럽게)
- [x] 테스트 강화 (걷는 중 화면 가운데 1% 이내, 거리 16.5m)
- [x] 변경 이유 기록 → [WORKLOG.md](WORKLOG.md)
- [x] 재배치 후 EditMode 20/20 · PlayMode 20/20 통과 ✅ 완료 기준
- [x] **되돌림 (2026-09-29):** Game 창 Scale이 5배여서 크게 보였던 것으로 확인 → 4-1 카메라(11m, 0.15초 추적)로 복귀 · EditMode 20/20 · PlayMode 19/19

## 4-3. 점프 거리 늘리기 (사용자 피드백)
- [x] 점프 높이 1.2 → 1.5m, 중력 -20 → -15 (달리며 뛴 거리 2.08m → 2.68m, 구멍 2m)
- [x] Jump 동작 재생 속도를 체공 시간에 맞춤 (0.7 → 0.53배)
- [x] 테스트 추가 (가장자리 0.4m 앞에서 뛰면 구멍을 넘음, 점프 거리 > 구멍 × 1.3)
- [x] 변경 이유 기록 → [WORKLOG.md](WORKLOG.md)
- [x] EditMode 20/20 · PlayMode 21/21 통과 ✅ 완료 기준

## 5. 아이템
- [x] 복셀 코인 프리팹 제작
- [x] `Collectible.cs`: 회전 + 둥실 효과, Trigger 획득, 파티클
- [x] MapBuilder로 코인 4개 배치
- [x] `GameUI.cs`: 좌상단 `아이템 0 / 4` 카운터
- [x] 미리보기: 코인 근접 이미지 + 코인 2개를 먹는 이동 녹화(카운터 포함) → [map_preview.html](map_preview.html)
- [x] 획득하면 카운트가 오르고 코인이 사라지는지 확인 ✅ 완료 기준

## 6. 게임 종료 / 성공
- [x] `GameManager.cs`: Playing / GameOver / Clear 상태
- [x] Y < -5이면 GAME OVER
- [x] 아이템 4/4이면 CLEAR
- [x] 결과 패널 UI 2종 + 결과가 나오면 조작 정지
- [x] (선택) CLEAR에 걸린 시간 표시
- [x] 미리보기: GAME OVER / CLEAR 결과 화면 → [map_preview.html](map_preview.html)
- [x] 두 조건 모두 동작하는지 확인 ✅ 완료 기준

## 7. 재시작
- [x] 결과 패널에 [다시 하기] 버튼 추가
- [x] R 키로 언제든 재시작
- [x] 재시작 후 여우, 아이템, UI가 모두 초기 상태로 돌아오는지 확인 ✅ 완료 기준

## 8. 마무리
- [x] Windows 빌드 `VoxelFox/Build/VoxelFox.exe` (창 모드 1600×900, `bash tools/build_windows.sh`)
- [x] 실행 파일 아이콘: Codex(ChatGPT 이미지 생성)로 흰 여우 얼굴 아이콘 → `Art/Icon/AppIcon.png`
- [x] 실행 확인: 로그에 에러 없음, 한글 글꼴 정상
- [x] 실행 파일로 처음부터 끝까지 플레이 테스트 (사용자 확인 2026-09-29) ✅ 완료 기준

---

## 완료 기록
| 날짜 | 단계 | 메모 |
|---|---|---|
| 2026-09-29 | 1. Unity 프로젝트 생성 | 5/5 완료 · PlayMode 스모크 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 1. 추가 정리 | 템플릿 샘플(SampleScene, TutorialInfo, Readme) 삭제 · 재검증 통과 · 이유: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | Git 연결 | GitHub `Notard/voxel-fox` (Public) 생성 · push · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 2. 복셀 여우 캐릭터 | 11/11 완료 · EditMode 9/9 · PlayMode 1/1 통과 · 미리보기 [fox_preview.html](fox_preview.html) · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 2-1. 흰 여우 · 0.9배 | 더 예쁜 여우로 만들기 위해 붉은 여우 → 흰 여우, 크기 0.9배 · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 3. 4x4 타일맵 + 이동 | 6/6 완료 · EditMode 18/18 · PlayMode 11/11 통과 · 미리보기 [map_preview.html](map_preview.html) · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 3-1. 바닥 노멀맵 | 바닥이 평평해 보여서 노멀맵으로 입체감 추가(사용자 요청) · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 4. 애니메이션 연결 | 5/5 완료 · Walk 다리 각도 26°→40° · 발 미끄러짐 최대 1.3cm · EditMode 20/20 · PlayMode 17/17 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 4-1. 카메라 추적 + 격자 이동 | 사용자 피드백: 카메라가 여우를 따라가야 하고, 화살표가 대각선으로 움직임 → 수정 · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 4-2. 카메라 거리 · 가운데 고정 | 사용자 피드백: 카메라를 1.5배 멀리, 여우를 가운데에 → 16.5m, 수평 지연 제거 · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 4-2 되돌림 | Game 창 Scale 5배 때문에 크게 보였던 것 → 4-1 카메라로 복귀 · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 4-3. 점프 거리 | 사용자 피드백: 점프가 짧아 구멍에 잘 빠짐 → 높이 1.5m·중력 -15, 2.08m → 2.68m · 테스트 통과 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 5. 아이템 | 6/6 완료 · 복셀 코인 4개 · 반짝이 · 한글 카운터(맑은 고딕) · EditMode 23/23 · PlayMode 26/26 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 6. 게임 종료 / 성공 | 7/7 완료 · GAME OVER / CLEAR! 패널 · 조작·시간 정지 · EditMode 25/25 · PlayMode 31/31 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 7. 재시작 | 3/3 완료 · [다시 하기] 버튼 · R 키(게임패드 Start) · 씬 다시 불러오기 · EditMode 27/27 · PlayMode 35/35 · 상세: [WORKLOG.md](WORKLOG.md) |
| 2026-09-29 | 8. 마무리 | Windows 빌드 102.7MB · 경고·에러 0 · 아이콘(Codex) · 사용자 플레이 테스트 확인 · **전체 완료** · 상세: [WORKLOG.md](WORKLOG.md) |
