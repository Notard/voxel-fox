using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 1단계 프로젝트 초기 설정. 여러 번 실행해도 안전하다.
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod ProjectSetup.Run
public static class ProjectSetup
{
    const string MainScenePath = "Assets/Scenes/Main.unity";

    static readonly string[] Folders =
    {
        "Assets/Art/Characters/Fox",
        "Assets/Art/Tiles",
        "Assets/Prefabs",
        "Assets/Scenes",
        "Assets/Scripts",
    };

    [MenuItem("VoxelFox/Project Setup")]
    public static void Run()
    {
        CreateFolders();
        ImportTmpEssentials();
        CreateMainScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup] 완료");
    }

    static void CreateFolders()
    {
        foreach (var folder in Folders)
        {
            if (AssetDatabase.IsValidFolder(folder)) continue;
            var parts = folder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            Debug.Log($"[ProjectSetup] 폴더 생성: {folder}");
        }
    }

    // 배치 모드에서는 ImportPackage가 -quit 전에 끝나지 않는다.
    // 그때는 명령줄 -importPackage "<ugui>/Package Resources/TMP Essential Resources.unitypackage"를 쓴다.
    static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("[ProjectSetup] TMP Essential Resources 이미 있음");
            return;
        }
        var ugui = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
        var package = Path.Combine(ugui.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(package, false);
        Debug.Log($"[ProjectSetup] TMP Essential Resources 임포트: {package}");
    }

    static void CreateMainScene()
    {
        if (!File.Exists(MainScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, MainScenePath);
            Debug.Log($"[ProjectSetup] 씬 생성: {MainScenePath}");
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
        Debug.Log("[ProjectSetup] Build Settings 씬 목록 = Main");
    }
}
