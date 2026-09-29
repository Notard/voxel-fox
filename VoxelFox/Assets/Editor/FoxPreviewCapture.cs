using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Unity로 임포트한 여우를 URP로 렌더링해 PNG로 저장한다(미리보기 비교용).
// 그래픽 장치가 필요하므로 -nographics 없이 실행한다.
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod FoxPreviewCapture.Capture
public static class FoxPreviewCapture
{
    const int Size = 480;

    static readonly (string clip, float time)[] Shots =
    {
        ("Idle", 0f),
        ("Walk", 0f),
        ("Jump", 8f / 30f),
    };

    [MenuItem("VoxelFox/Capture Fox Preview")]
    public static void Capture()
    {
        var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../preview/unity"));
        Directory.CreateDirectory(outDir);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 0.85f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.98f, 0.96f, 0.94f, 1f);
        // Unity에서 여우는 +Z를 바라본다 → 앞 오른쪽 위에서 비스듬히 본다.
        var center = new Vector3(0, 0.45f, 0.06f);
        cam.transform.position = center + new Vector3(3.6f, 2.4f, 3.8f);
        cam.transform.LookAt(center);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FoxSetup.PrefabPath);
        var fox = Object.Instantiate(prefab);
        // 배치 모드에서는 플레이어 루프가 돌지 않아 스키닝이 갱신되지 않는다.
        // 포즈마다 BakeMesh로 굳힌 메시를 따로 그린다.
        var smr = fox.GetComponentInChildren<SkinnedMeshRenderer>();
        smr.enabled = false;
        var baked = new Mesh();
        var bakedGo = new GameObject("BakedFox", typeof(MeshFilter), typeof(MeshRenderer));
        bakedGo.GetComponent<MeshFilter>().sharedMesh = baked;
        bakedGo.GetComponent<MeshRenderer>().sharedMaterial = smr.sharedMaterial;

        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);

        foreach (var (clipName, time) in Shots)
        {
            var clip = FindClip(clipName);
            clip.SampleAnimation(fox, time);
            smr.BakeMesh(baked, true);
            bakedGo.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            var file = Path.Combine(outDir, $"unity_{clipName}.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Debug.Log($"[FoxPreviewCapture] {file}");
        }

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
    }

    static AnimationClip FindClip(string name)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(FoxSetup.FbxPath))
            if (asset is AnimationClip clip && clip.name == name)
                return clip;
        throw new System.Exception($"클립 없음: {name}");
    }
}
