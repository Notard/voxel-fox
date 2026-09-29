# 복셀 여우 타일 게임 — 개발 계획서

> 원본 메모: `blueprint.md` · 작성일: 2026-09-29 · 대상 플랫폼: Windows PC

## 0. 개요

복셀 스타일의 **여우** 캐릭터가 구멍 뚫린 4x4 타일맵 위를 걸어 다니며 아이템을 모두 모으면 성공,
구멍이나 가장자리 밖으로 떨어지면 실패하는 짧은 3D 게임.

| 항목 | 결정 |
|---|---|
| 엔진 | Unity **6000.5.7f1** (Unity 6), URP 3D 템플릿 |
| 캐릭터 제작 | **Blender 5.2 + Python 스크립트** → 리그 + 애니메이션 포함 FBX |
| 캐릭터 | 동물 — **여우** (복셀, 4족 보행, 꼬리 있음) |
| 맵 | 4x4 타일, **구멍 타일 2개** 포함 |
| 조작 | WASD / 방향키 이동, Space 점프, R 재시작 |
| 입력 | Unity Input System 패키지 |
| 플랫폼 | Windows (Standalone) |

---

## 1. Unity 프로젝트 생성

- Unity Hub → **6000.5.7f1** → `Universal 3D` 템플릿
- 경로: `C:\sample\VoxelFox`
- 패키지: Input System (Active Input Handling = Input System Package), TextMeshPro
- 폴더 구조

```
Assets/
  Art/Characters/Fox/     ← FBX, 머티리얼
  Art/Tiles/              ← 타일, 아이템 프리팹 재료
  Prefabs/
  Scenes/Main.unity
  Scripts/
```

**완료 기준:** 빈 씬이 Play 모드에서 에러 없이 실행된다.

---

## 2. 3D 복셀 여우 캐릭터 (Blender Python)

### 2-1. 제작 방식
- `C:\sample\Blender\make_fox.py` 스크립트 하나로 **모델 생성 → 리그 → 애니메이션 → FBX 내보내기**까지 자동화
- Blender 백그라운드 실행:
  `blender --background --python make_fox.py`
- 파츠별로 3D 배열(복셀 데이터)을 정의하고, 보이는 면만 메시로 생성 (내부 면 제거)
- 파츠마다 버텍스 그룹을 만들고 해당 본에 **100% 가중치**(강체 스키닝) → 복셀이 찌그러지지 않음

### 2-2. 파츠 & 색상
| 파츠 | 대략 크기(복셀) | 색 |
|---|---|---|
| 몸통 | 6 × 5 × 9 | 주황 `#E8772E` / 배 흰색 |
| 머리 | 6 × 5 × 5 + 주둥이 | 주황, 주둥이 흰색, 코 검정 |
| 귀 ×2 | 2 × 3 × 1 | 주황, 안쪽 어두운 색 |
| 다리 ×4 | 2 × 4 × 2 | 발끝 검정 |
| 꼬리 | 3 × 3 × 6 | 주황, 끝 흰색 |

- 머티리얼: 버텍스 컬러 또는 작은 팔레트 텍스처 1장 (Unity에서 URP Lit/Unlit으로 사용)
- 1 복셀 = 0.0625m 기준 → 여우 키 약 0.6m

### 2-3. 본 구조 (Armature)
```
Root
└─ Body
   ├─ Head
   ├─ Leg_FL   (앞왼)
   ├─ Leg_FR   (앞오)
   ├─ Leg_BL   (뒤왼)
   ├─ Leg_BR   (뒤오)
   └─ Tail
```

### 2-4. 애니메이션 (Action)
| 이름 | 프레임(30fps) | 루프 | 내용 |
|---|---|---|---|
| `Idle` | 0–40 | O | 몸통 살짝 위아래, 꼬리 좌우 흔들기 |
| `Walk` | 0–20 | O | 대각선 다리 쌍(FL+BR / FR+BL) 교차 회전 ±30°, 몸통 바운스 |
| `Jump` | 0–20 | X | 웅크림 → 다리 쭉 폄 → 공중 자세 → 착지 준비 |

### 2-5. FBX 내보내기 설정
- Scale 1.0, **Apply Scalings: FBX All**, Forward `-Z`, Up `Y`
- Armature: Add Leaf Bones **끔**
- Bake Animation 켬, **All Actions** 켬 (NLA 없이 액션별 클립)
- 출력: `Assets/Art/Characters/Fox/Fox.fbx`

### 2-6. Unity 임포트
- Rig: **Generic**, Avatar: Create From This Model
- Animation 탭: `Idle`, `Walk` → Loop Time 체크
- Animator Controller `Fox.controller`
  - 파라미터: `Speed`(float), `IsGrounded`(bool), `Jump`(trigger)
  - Idle ↔ Walk (Speed > 0.1), Any → Jump (trigger), Jump → Idle (IsGrounded)

**완료 기준:** 씬에 놓은 여우가 Idle/Walk/Jump 클립을 정상 재생하고, 복셀 형태가 깨지지 않는다.

---

## 3. 4x4 타일맵 + 이동

### 3-1. 맵 레이아웃 (타일 1칸 = 2m, 두께 0.5m)
```
   x→  0   1   2   3
z=3  [ S ][   ][   ][ C ]
z=2  [   ][ H ][   ][   ]
z=1  [ C ][   ][ H ][ C ]
z=0  [   ][   ][   ][ C ]
```
- `S` 시작 지점 · `H` 구멍(타일 없음) · `C` 아이템 · 빈칸 = 일반 타일
- 맵 주변은 낭떠러지 (벽 없음)
- 타일은 복셀 느낌의 잔디 블록 (위 초록 / 옆 흙색), 체커 패턴으로 살짝 명도 차이
- 레이아웃은 `MapBuilder` 스크립트의 문자열 배열로 정의 → 쉽게 수정 가능

### 3-2. 캐릭터 이동
- `CharacterController` 기반 `PlayerController.cs`
  - 이동 속도 3 m/s, 이동 방향으로 부드럽게 회전
  - 중력 -20 m/s², 점프 높이 약 1.2m
  - 카메라 기준 방향으로 입력 변환
- 카메라: 맵 전체가 보이는 **고정 쿼터뷰** (위에서 약 45°, 맵 중심 바라봄)

**완료 기준:** 여우가 타일 위를 걷고 점프하며, 구멍에 들어가면 아래로 떨어진다.

---

## 4. 애니메이션 연결

- `PlayerController`가 Animator 파라미터 갱신
  - `Speed` = 수평 속도, `IsGrounded` = 접지 여부, Space 입력 시 `Jump` 트리거
- 걷는 속도와 Walk 재생 속도 맞추기 (발 미끄러짐 최소화)

**완료 기준:** 멈춤 → Idle, 이동 → Walk, 점프 → Jump가 자연스럽게 전환된다.

---

## 5. 아이템

- 복셀 **코인**(또는 사과) 4개, 레이아웃의 `C` 위치에 배치
- 제자리 회전 + 위아래 둥실 효과
- Trigger Collider, 여우와 닿으면 획득 → 사라짐 + 간단한 파티클/사운드
- 화면 좌상단 UI: `아이템 0 / 4`

**완료 기준:** 아이템을 먹으면 카운트가 오르고 아이템이 사라진다.

---

## 6. 게임 종료 / 성공

`GameManager.cs` 상태: `Playing` → `GameOver` | `Clear`

| 조건 | 결과 |
|---|---|
| 여우 Y < -5 | **GAME OVER** 패널 표시, 조작 정지 |
| 아이템 4/4 획득 | **CLEAR!** 패널 표시, 조작 정지, (선택) 걸린 시간 표시 |

**완료 기준:** 두 조건 모두에서 해당 패널이 뜨고 더 이상 조작되지 않는다.

---

## 7. 재시작

- 결과 패널의 **[다시 하기]** 버튼, 또는 언제든 **R 키**
- `SceneManager.LoadScene(현재 씬)`으로 전체 초기화

**완료 기준:** 재시작 시 여우 위치, 아이템, UI가 모두 처음 상태로 돌아온다.

---

## 스크립트 목록

| 파일 | 역할 |
|---|---|
| `MapBuilder.cs` | 문자열 레이아웃으로 타일/아이템/시작점 생성 |
| `PlayerController.cs` | 입력, 이동, 점프, 애니메이터 파라미터 |
| `CameraRig.cs` | 고정 쿼터뷰 카메라 위치 설정 |
| `Collectible.cs` | 아이템 회전, 획득 처리 |
| `GameManager.cs` | 아이템 카운트, 낙하 판정, 상태 전환, 재시작 |
| `GameUI.cs` | 카운터, GAME OVER / CLEAR 패널 |

## 진행 순서 요약

1. Unity 프로젝트 생성
2. Blender 스크립트로 복셀 여우 + 리그 + 애니메이션 → FBX
3. 4x4 타일맵(구멍 2개) + 이동
4. Animator 연결 (Idle / Walk / Jump)
5. 아이템 4개 + 카운터 UI
6. 낙하 = 실패, 전부 획득 = 성공
7. 재시작 (버튼 / R 키)

## 나중에 고려 (범위 밖)

- 사운드/BGM, 여러 스테이지, 움직이는 타일, 타이머 랭킹, 게임패드 지원
