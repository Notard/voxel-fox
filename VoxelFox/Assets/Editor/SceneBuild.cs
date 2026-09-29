using UnityEditor;

// Main 씬을 처음부터 다시 조립한다: 맵(3단계) → 아이템(5단계) → UI(5·6단계).
// Unity를 한 번만 띄워 세 설정을 이어서 실행한다 (tools/build_map.sh 1단계).
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod SceneBuild.Run
public static class SceneBuild
{
    [MenuItem("VoxelFox/Build Scene (Map + Items + UI)")]
    public static void Run()
    {
        MapSetup.Run();
        ItemSetup.Run();
        UISetup.Run();
    }
}
