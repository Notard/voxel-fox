using System.Linq;
using UnityEditor;
using UnityEngine;

// Blender에서 만든 여우 에셋의 임포트 설정을 자동으로 맞춘다.
// Fox.fbx / Fox_Palette.png를 다시 내보내도 설정이 유지된다.
public class FoxAssetPostprocessor : AssetPostprocessor
{
    const string FoxFolder = "Assets/Art/Characters/Fox/";
    static readonly string[] LoopClips = { "Idle", "Walk" };

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(FoxFolder)) return;
        var importer = (TextureImporter)assetImporter;
        // 팔레트 텍스처: 색 한 칸이 번지지 않게 한다.
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(FoxFolder)) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importBlendShapes = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(FoxFolder)) return;
        var importer = (ModelImporter)assetImporter;
        // Blender 테이크 이름 "Fox|Idle" → 클립 이름 "Idle"
        importer.clipAnimations = importer.defaultClipAnimations.Select(clip =>
        {
            var name = clip.takeName.Split('|').Last();
            clip.name = name;
            clip.loopTime = LoopClips.Contains(name);
            return clip;
        }).ToArray();
    }
}
