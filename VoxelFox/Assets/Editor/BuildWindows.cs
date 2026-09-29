using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 8단계: Windows 실행 파일을 만든다 → VoxelFox/Build/VoxelFox.exe (Git에는 올리지 않음)
// 창 모드 1600×900(크기 조절 가능)으로 시작한다. 실행 중 Alt+Enter로 전체 화면 전환.
// 아이콘: Art/Icon/AppIcon.png (1024×1024, 둥근 사각형 바깥 투명). Codex(ChatGPT 이미지 생성)로 그렸다.
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod BuildWindows.Run
public static class BuildWindows
{
    public const string OutputPath = "Build/VoxelFox.exe"; // 프로젝트 폴더 기준
    public const string IconPath = "Assets/Art/Icon/AppIcon.png";

    [MenuItem("VoxelFox/Build Windows")]
    public static void Run()
    {
        PlayerSettings.productName = "VoxelFox";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.allowFullscreenSwitch = true;
        SetIcon();

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MapSetup.ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });

        var summary = report.summary;
        Debug.Log($"[BuildWindows] {summary.result}: {summary.outputPath} " +
                  $"({summary.totalSize / (1024f * 1024f):0.0}MB, {summary.totalTime.TotalSeconds:0}초, 경고 {summary.totalWarnings}, 에러 {summary.totalErrors})");
        if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    // 모든 플랫폼 공통(기본) 아이콘으로 지정한다. 작은 크기는 Unity가 줄여서 .exe에 넣는다.
    static void SetIcon()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        Debug.Log($"[BuildWindows] 아이콘: {IconPath}");
    }
}
