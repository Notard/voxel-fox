#!/usr/bin/env bash
# Windows 실행 파일 만들기 → VoxelFox/Build/VoxelFox.exe
#   bash tools/build_windows.sh
set -euo pipefail
cd "$(dirname "$0")/.."

UNITY="/c/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Unity.exe"
PROJECT='C:\sample\VoxelFox'
TAG="${TAG:-08}"  # 로그 이름 앞머리
LOGS='C:\sample\logs'
mkdir -p logs

echo "[1/1] Unity: Windows 빌드"
"$UNITY" -batchmode -quit -nographics -projectPath "$PROJECT" \
  -executeMethod BuildWindows.Run -logFile "$LOGS\\${TAG}_build_windows.log"
grep -h "\[BuildWindows\]" "logs/${TAG}_build_windows.log"
