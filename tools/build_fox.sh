#!/usr/bin/env bash
# 복셀 여우 전체 재생성: Blender 모델/FBX → Blender 미리보기 → Unity 설정 → Unity 미리보기 → 테스트
#   bash tools/build_fox.sh
set -euo pipefail
cd "$(dirname "$0")/.."

BLENDER="/c/Program Files/Blender Foundation/Blender 5.2/blender.exe"
UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Unity.exe"
PROJECT='C:\sample\VoxelFox'
LOGS='C:\sample\logs'
mkdir -p logs

echo "[1/5] Blender: 모델·리그·애니메이션 → FBX"
"$BLENDER" --background --factory-startup --python Blender/make_fox.py | grep '\[make_fox\]'

echo "[2/5] Blender: 미리보기 렌더"
"$BLENDER" --background Blender/Fox.blend --python Blender/render_preview.py | grep '\[preview\]'

echo "[3/5] Unity: 임포트 설정·머티리얼·Animator·프리팹"
"$UNITY" -batchmode -quit -nographics -projectPath "$PROJECT" \
  -executeMethod FoxSetup.Run -logFile "$LOGS\\02_fox_setup.log"

echo "[4/5] Unity: 미리보기 캡처"
"$UNITY" -batchmode -quit -projectPath "$PROJECT" \
  -executeMethod FoxPreviewCapture.Capture -logFile "$LOGS\\02_unity_capture.log"

echo "[5/5] Unity: EditMode 테스트"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode \
  -testResults "$LOGS\\02_editmode_results.xml" -logFile "$LOGS\\02_editmode.log"
grep -oE '<test-run [^>]*' logs/02_editmode_results.xml | grep -oE '(result|total|passed|failed)="[^"]*"' | tr '\n' ' '
echo
