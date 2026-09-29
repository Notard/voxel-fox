using UnityEditor;
using UnityEditor.SceneManagement;

// Main 씬을 열고 바로 Play 모드로 들어간다 (타일은 Play 때 만들어진다).
// 명령줄: Unity.exe -projectPath . -executeMethod PlayMain.Run
public static class PlayMain
{
    [MenuItem("VoxelFox/Play Main Scene")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(MapSetup.ScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
}
