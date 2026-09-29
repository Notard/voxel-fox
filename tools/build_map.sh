#!/usr/bin/env bash
# 3단계 타일맵 재생성: Unity 설정 → EditMode 테스트 → PlayMode 테스트 + 미리보기 캡처(preview/map)
#   bash tools/build_map.sh
set -euo pipefail
cd "$(dirname "$0")/.."

UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Unity.exe"
PROJECT='C:\sample\VoxelFox'
TAG="${TAG:-03}"  # 로그 이름 앞머리 (예: TAG=04 bash tools/build_map.sh)
LOGS='C:\sample\logs'
mkdir -p logs preview/map

summary() { grep -oE '<test-run [^>]*' "$1" | grep -oE '(result|total|passed|failed|skipped)="[^"]*"' | tr '\n' ' '; echo; }

echo "[1/3] Unity: 타일·플레이어 프리팹, Main 씬 배치"
"$UNITY" -batchmode -quit -nographics -projectPath "$PROJECT" \
  -executeMethod MapSetup.Run -logFile "$LOGS\\${TAG}_map_setup.log"

echo "[2/3] Unity: EditMode 테스트"
"$UNITY" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform EditMode \
  -testResults "$LOGS\\${TAG}_editmode_results.xml" -logFile "$LOGS\\${TAG}_editmode.log" || true
summary logs/${TAG}_editmode_results.xml

# 미리보기는 Play 모드에서 찍는다 (편집 모드에서는 여우 스키닝이 갱신되지 않음).
# 그래픽 장치가 필요하므로 -nographics를 붙이지 않는다.
echo "[3/3] Unity: PlayMode 테스트 + 미리보기 캡처 (preview/map)"
VOXELFOX_PREVIEW_DIR='C:\sample\preview\map' \
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform PlayMode \
  -testResults "$LOGS\\${TAG}_playmode_results.xml" -logFile "$LOGS\\${TAG}_playmode.log" || true
summary logs/${TAG}_playmode_results.xml
