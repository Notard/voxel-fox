using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// 3단계 타일맵 설정: 잔디 타일(팔레트·메시·머티리얼·프리팹), Player 프리팹, Main 씬 배치.
// 여러 번 실행해도 안전하다. 메시와 팔레트는 GUID를 유지한 채 내용만 다시 만든다.
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod MapSetup.Run
public static class MapSetup
{
    public const string TileDir = "Assets/Art/Tiles";
    public const string PalettePath = TileDir + "/Tile_Palette.png";
    public const string MeshPath = TileDir + "/GrassTile_Mesh.asset";
    public const string MaterialPath = TileDir + "/Tile.mat";
    public const string TilePrefabPath = "Assets/Prefabs/GrassTile.prefab";
    public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    public const string ScenePath = "Assets/Scenes/Main.unity";
    const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    // 1복셀 = 0.125m → 타일 16 × 4 × 16 복셀 (2m × 0.5m × 2m)
    const int Voxels = 16;
    const int Layers = 4;
    const float Voxel = MapBuilder.TileSize / Voxels;

    // 팔레트 4×4, 칸 번호 = y * 4 + x
    static readonly Color32[] Palette =
    {
        Hex(0x6FBF47), Hex(0x83CF55), Hex(0x5CA83B), Hex(0x58A03A), // 잔디 3톤, 잔디 옆면
        Hex(0x8D5B3B), Hex(0x9C6A47), Hex(0x7B4D31), Hex(0x5E3D28), // 흙 3톤, 바닥
    };
    const int GrassSide = 3, Bottom = 7;

    [MenuItem("VoxelFox/Map Setup")]
    public static void Run()
    {
        var palette = CreatePalette();
        var material = CreateMaterial(palette);
        var mesh = CreateMesh();
        var tile = CreateTilePrefab(mesh, material);
        var player = CreatePlayerPrefab();
        SetupScene(tile, player);
        AssetDatabase.SaveAssets();
        Debug.Log("[MapSetup] 완료");
    }

    static Color32 Hex(int rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

    static Texture2D CreatePalette()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = i < Palette.Length ? Palette[i] : Palette[Bottom];
        tex.SetPixels32(pixels);
        File.WriteAllBytes(PalettePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(PalettePath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(PalettePath);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Debug.Log($"[MapSetup] 팔레트: {PalettePath}");
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
    }

    static Material CreateMaterial(Texture2D palette)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        mat.SetTexture("_BaseMap", palette);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 보이는 면만 만든다: 윗면은 복셀마다, 옆면은 복셀 층마다, 바닥은 한 장.
    // 원점 = 윗면 중앙 (윗면 y = 0, 바닥 y = -0.5)
    static Mesh CreateMesh()
    {
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        var rng = new System.Random(7); // 고정 시드 → 매번 같은 무늬

        // p, p+u, p+u+v, p+v 순서, 법선 = u × v (Unity 앞면 = 시계 방향)
        void Quad(Vector3 p, Vector3 u, Vector3 v, int color)
        {
            int start = verts.Count;
            var n = Vector3.Cross(u, v).normalized;
            var uv = new Vector2((color % 4 + 0.5f) / 4f, (color / 4 + 0.5f) / 4f);
            foreach (var corner in new[] { p, p + u, p + u + v, p + v })
            {
                verts.Add(corner);
                normals.Add(n);
                uvs.Add(uv);
            }
            tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        int Grass() => rng.NextDouble() switch { < 0.55 => 0, < 0.8 => 1, _ => 2 };
        int Dirt() => rng.NextDouble() switch { < 0.5 => 4, < 0.75 => 5, _ => 6 };
        // 옆면: 맨 윗층은 잔디, 둘째 층은 잔디가 흘러내린 곳이 드문드문, 나머지는 흙
        int Side(int layer) => layer == Layers - 1 ? (rng.NextDouble() < 0.7 ? GrassSide : 2)
            : layer == Layers - 2 && rng.NextDouble() < 0.3 ? GrassSide
            : Dirt();

        float half = MapBuilder.TileSize / 2f;
        float bottom = -MapBuilder.TileHeight;
        var x = Vector3.right * Voxel;
        var y = Vector3.up * Voxel;
        var z = Vector3.forward * Voxel;

        for (int i = 0; i < Voxels; i++)
            for (int j = 0; j < Voxels; j++)
                Quad(new Vector3(-half + i * Voxel, 0, -half + j * Voxel), z, x, Grass());

        for (int i = 0; i < Voxels; i++)
            for (int k = 0; k < Layers; k++)
            {
                float a = -half + i * Voxel, h = bottom + k * Voxel;
                Quad(new Vector3(half, h, a), y, z, Side(k));   // +X
                Quad(new Vector3(-half, h, a), z, y, Side(k));  // -X
                Quad(new Vector3(a, h, half), x, y, Side(k));   // +Z
                Quad(new Vector3(a, h, -half), y, x, Side(k));  // -Z
            }

        Quad(new Vector3(-half, bottom, -half), Vector3.right * 2 * half, Vector3.forward * 2 * half, Bottom);

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }
        mesh.Clear();
        mesh.name = "GrassTile";
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        EditorUtility.SetDirty(mesh);
        Debug.Log($"[MapSetup] 타일 메시: 면 {tris.Count / 6}, 정점 {verts.Count}");
        return mesh;
    }

    static GameObject CreateTilePrefab(Mesh mesh, Material material)
    {
        var go = new GameObject("GrassTile");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0, -MapBuilder.TileHeight / 2f, 0);
        box.size = new Vector3(MapBuilder.TileSize, MapBuilder.TileHeight, MapBuilder.TileSize);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, TilePrefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"[MapSetup] 타일 프리팹: {TilePrefabPath}");
        return prefab;
    }

    // Player (CharacterController + PlayerController) 아래에 Fox 프리팹을 둔다.
    static GameObject CreatePlayerPrefab()
    {
        var root = new GameObject("Player");
        var cc = root.AddComponent<CharacterController>();
        // 여우 몸통 높이 약 0.5m, 귀 끝 0.84m. 반지름은 몸통 폭 정도로 작게 잡아
        // 구멍 가장자리에 걸려 떠 있는 일이 없게 한다.
        cc.height = 0.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.4f, 0);
        cc.skinWidth = 0.02f;
        cc.stepOffset = 0.25f;
        cc.slopeLimit = 45f;
        cc.minMoveDistance = 0f;

        var controller = root.AddComponent<PlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("actions").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        var fox = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(FoxSetup.PrefabPath));
        fox.transform.SetParent(root.transform, false);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[MapSetup] 플레이어 프리팹: {PlayerPrefabPath}");
        return prefab;
    }

    static void SetupScene(GameObject tilePrefab, GameObject playerPrefab)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // GetComponent는 에디터에서 C# null이 아닌 "빈 객체"를 돌려주므로 ?? 대신 TryGetComponent를 쓴다.
        var mapGo = GameObject.Find("Map");
        if (mapGo == null) mapGo = new GameObject("Map");
        if (!mapGo.TryGetComponent(out MapBuilder map)) map = mapGo.AddComponent<MapBuilder>();
        mapGo.transform.position = Vector3.zero;

        var old = GameObject.Find("Player");
        if (old != null) Object.DestroyImmediate(old);
        var player = ((GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene))
            .GetComponent<PlayerController>();

        var cam = Object.FindAnyObjectByType<Camera>();
        cam.fieldOfView = 40f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.62f, 0.80f, 0.93f); // 맑은 하늘색
        if (!cam.TryGetComponent(out CameraRig rig)) rig = cam.gameObject.AddComponent<CameraRig>();

        // 해는 카메라(남남서, yaw 30) 뒤 왼쪽에서 비춘다 → 카메라에 보이는 면이 밝고 그림자는 앞쪽으로 진다.
        // 처음 값(50, -30)은 남동쪽에서 비춰서 카메라가 여우의 그늘진 면만 봤다.
        var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .First(l => l.type == LightType.Directional);
        sun.transform.rotation = Quaternion.Euler(50, 60, 0);
        sun.intensity = 1.2f;
        // 그늘진 면이 기본 하늘의 갈색 '땅' 빛을 받지 않도록 중립색 3단 환경광을 쓴다.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.84f, 0.92f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.70f);
        RenderSettings.ambientGroundColor = new Color(0.42f, 0.42f, 0.42f);

        Assign(map, "tilePrefab", tilePrefab);
        Assign(map, "player", player);
        Assign(rig, "map", map);
        Assign(player, "cameraRig", rig);

        // 에디터에서도 시작 위치와 카메라 구도가 보이게 해 둔다. 타일은 Play 때 만든다.
        map.PlaceAtStart(player);
        rig.Apply();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MapSetup] 씬 배치: {ScenePath}");
    }

    static void Assign(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
