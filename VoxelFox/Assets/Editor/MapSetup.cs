using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// 3단계 타일맵 설정: 잔디 타일(색·노멀 텍스처, 메시, 머티리얼, 프리팹), Player 프리팹, Main 씬 배치.
// 여러 번 실행해도 안전하다. 메시와 텍스처는 GUID를 유지한 채 내용만 다시 만든다.
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod MapSetup.Run
public static class MapSetup
{
    public const string TileDir = "Assets/Art/Tiles";
    public const string AlbedoPath = TileDir + "/Tile_Albedo.png";
    public const string NormalPath = TileDir + "/Tile_Normal.png";
    public const string MeshPath = TileDir + "/GrassTile_Mesh.asset";
    public const string MaterialPath = TileDir + "/Tile.mat";
    public const string TilePrefabPath = "Assets/Prefabs/GrassTile.prefab";
    public const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    public const string ScenePath = "Assets/Scenes/Main.unity";
    const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    // 1복셀 = 0.125m → 타일 16 × 4 × 16 복셀 (2m × 0.5m × 2m)
    const int Voxels = 16;
    const int Layers = 4;

    // 텍스처 아틀라스 512 × 256, 복셀 한 칸 = 16픽셀
    //   왼쪽 256 × 256: 윗면 (16 × 16 복셀)
    //   오른쪽 256 × 64 네 줄: 옆면 +X, -X, +Z, -Z (16 × 4 복셀)
    const int Px = 16;
    const int AtlasW = 512, AtlasH = 256;
    const int SideX = 256, SideH = Layers * Px;

    // 노멀맵용 높이 (픽셀 단위): 경계 경사 폭, 면 바깥(모서리) 높이
    const float BevelWidth = 3f;
    const float BevelDepth = 3f;

    static readonly Color32[] Palette =
    {
        Hex(0x6FBF47), Hex(0x83CF55), Hex(0x5CA83B), Hex(0x58A03A), // 잔디 3톤, 잔디 옆면
        Hex(0x8D5B3B), Hex(0x9C6A47), Hex(0x7B4D31),                // 흙 3톤
    };
    // 색마다 복셀 높이: 밝은 복셀은 튀어나오고 어두운 복셀은 들어가 보이게 한다.
    static readonly float[] Heights = { 1.5f, 3f, 0f, 2f, 1f, 2f, 0f };
    const int GrassSide = 3;

    [MenuItem("VoxelFox/Map Setup")]
    public static void Run()
    {
        var (albedo, normal) = CreateTextures();
        var material = CreateMaterial(albedo, normal);
        var mesh = CreateMesh();
        var tile = CreateTilePrefab(mesh, material);
        var player = CreatePlayerPrefab();
        SetupScene(tile, player);
        AssetDatabase.SaveAssets();
        Debug.Log("[MapSetup] 완료");
    }

    static Color32 Hex(int rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

    // 복셀마다 색과 높이를 칠한 뒤, 높이맵의 기울기로 노멀맵을 만든다.
    static (Texture2D albedo, Texture2D normal) CreateTextures()
    {
        var colors = new Color32[AtlasW * AtlasH];
        var height = new float[AtlasW * AtlasH];
        var rng = new System.Random(7); // 고정 시드 → 매번 같은 무늬

        int Grass() => rng.NextDouble() switch { < 0.55 => 0, < 0.8 => 1, _ => 2 };
        int Dirt() => rng.NextDouble() switch { < 0.5 => 4, < 0.75 => 5, _ => 6 };
        // 옆면: 맨 윗층은 잔디, 둘째 층은 잔디가 흘러내린 곳이 드문드문, 나머지는 흙
        int Side(int layer) => layer == Layers - 1 ? (rng.NextDouble() < 0.7 ? GrassSide : 2)
            : layer == Layers - 2 && rng.NextDouble() < 0.3 ? GrassSide
            : Dirt();

        // 한 면(복셀 w × h개)을 칠한다. 높이가 다른 이웃과의 경계에만 경사를 두고,
        // 같은 높이끼리는 평평하게 이어 붙인다. 면 바깥은 낮은 것으로 보고 모서리를 둥글게 깎는다.
        void Face(int x0, int y0, int w, int h, int[,] voxel)
        {
            float VH(int vx, int vy) =>
                vx < 0 || vy < 0 || vx >= w || vy >= h ? -BevelDepth : Heights[voxel[vx, vy]];
            for (int vx = 0; vx < w; vx++)
                for (int vy = 0; vy < h; vy++)
                {
                    float own = VH(vx, vy);
                    for (int py = 0; py < Px; py++)
                        for (int px = 0; px < Px; px++)
                        {
                            float hgt = own;
                            // 이웃 쪽 가장자리로 갈수록 두 높이의 중간값에 가까워진다.
                            void Blend(float neighbor, float dist) =>
                                hgt += (neighbor - own) * 0.5f * Mathf.Clamp01(1f - dist / BevelWidth);
                            Blend(VH(vx - 1, vy), px + 0.5f);
                            Blend(VH(vx + 1, vy), Px - px - 0.5f);
                            Blend(VH(vx, vy - 1), py + 0.5f);
                            Blend(VH(vx, vy + 1), Px - py - 0.5f);
                            int i = (y0 + vy * Px + py) * AtlasW + x0 + vx * Px + px;
                            colors[i] = Palette[voxel[vx, vy]];
                            height[i] = hgt;
                        }
                }
        }

        var top = new int[Voxels, Voxels];
        for (int i = 0; i < Voxels; i++)
            for (int j = 0; j < Voxels; j++)
                top[i, j] = Grass();
        Face(0, 0, Voxels, Voxels, top);
        for (int side = 0; side < 4; side++)
        {
            var wall = new int[Voxels, Layers];
            for (int i = 0; i < Voxels; i++)
                for (int layer = 0; layer < Layers; layer++)
                    wall[i, layer] = Side(layer);
            Face(SideX, side * SideH, Voxels, Layers, wall);
        }

        // 접선 공간 노멀: x = 텍스처 u 방향, y = v 방향 (Texture2D는 행 0이 v 0)
        float H(int x, int y) => height[Mathf.Clamp(y, 0, AtlasH - 1) * AtlasW + Mathf.Clamp(x, 0, AtlasW - 1)];
        var normals = new Color[AtlasW * AtlasH];
        for (int y = 0; y < AtlasH; y++)
            for (int x = 0; x < AtlasW; x++)
            {
                var n = new Vector3(
                    -(H(x + 1, y) - H(x - 1, y)) / 2f,
                    -(H(x, y + 1) - H(x, y - 1)) / 2f,
                    1f).normalized;
                normals[y * AtlasW + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }

        var albedo = new Texture2D(AtlasW, AtlasH, TextureFormat.RGBA32, false);
        albedo.SetPixels32(colors);
        var normal = new Texture2D(AtlasW, AtlasH, TextureFormat.RGBA32, false, true);
        normal.SetPixels(normals);
        File.WriteAllBytes(AlbedoPath, albedo.EncodeToPNG());
        File.WriteAllBytes(NormalPath, normal.EncodeToPNG());
        Object.DestroyImmediate(albedo);
        Object.DestroyImmediate(normal);
        AssetDatabase.ImportAsset(AlbedoPath);
        AssetDatabase.ImportAsset(NormalPath);

        // 색: 복셀 경계가 번지지 않게 Point 필터
        var albedoImporter = (TextureImporter)AssetImporter.GetAtPath(AlbedoPath);
        albedoImporter.textureType = TextureImporterType.Default;
        albedoImporter.filterMode = FilterMode.Point;
        albedoImporter.wrapMode = TextureWrapMode.Clamp;
        albedoImporter.textureCompression = TextureImporterCompression.Uncompressed;
        albedoImporter.SaveAndReimport();
        // 노멀: 경사가 부드럽게 이어지도록 Bilinear
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
        normalImporter.textureType = TextureImporterType.NormalMap;
        normalImporter.filterMode = FilterMode.Bilinear;
        normalImporter.wrapMode = TextureWrapMode.Clamp;
        normalImporter.SaveAndReimport();

        Debug.Log($"[MapSetup] 텍스처: {AlbedoPath}, {NormalPath}");
        return (AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
    }

    static Material CreateMaterial(Texture2D albedo, Texture2D normal)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        mat.SetTexture("_BaseMap", albedo);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_BumpMap", normal);
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP"); // 스크립트로 텍스처를 넣으면 키워드가 저절로 켜지지 않는다
        mat.SetFloat("_Smoothness", 0.1f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 면 6장(윗면, 옆면 4, 바닥). 복셀 무늬와 입체감은 텍스처가 맡는다.
    // 원점 = 윗면 중앙 (윗면 y = 0, 바닥 y = -0.5)
    static Mesh CreateMesh()
    {
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        float half = MapBuilder.TileSize / 2f;
        float bottom = -MapBuilder.TileHeight;

        // 모서리 4개는 시계 방향(Unity 앞면). uv는 모서리 위치로 계산한다.
        void Quad(Vector3[] corners, System.Func<Vector3, Vector2> uv)
        {
            int start = verts.Count;
            var n = Vector3.Cross(corners[1] - corners[0], corners[3] - corners[0]).normalized;
            foreach (var c in corners)
            {
                verts.Add(c);
                normals.Add(n);
                uvs.Add(uv(c));
            }
            tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        float U(float s) => (s + half) / (2f * half); // -1..1 → 0..1
        Vector2 Atlas(float px, float py) => new(px / AtlasW, py / AtlasH);
        Vector2 SideUV(int side, float s, float y) =>
            Atlas(SideX + U(s) * Voxels * Px, side * SideH + (y - bottom) / MapBuilder.TileHeight * SideH);
        Vector3 V(float x, float y, float z) => new(x, y, z);

        Quad(new[] { V(-half, 0, -half), V(-half, 0, half), V(half, 0, half), V(half, 0, -half) },
            c => Atlas(U(c.x) * Voxels * Px, U(c.z) * Voxels * Px));
        Quad(new[] { V(half, bottom, -half), V(half, 0, -half), V(half, 0, half), V(half, bottom, half) },
            c => SideUV(0, c.z, c.y)); // +X
        Quad(new[] { V(-half, bottom, -half), V(-half, bottom, half), V(-half, 0, half), V(-half, 0, -half) },
            c => SideUV(1, c.z, c.y)); // -X
        Quad(new[] { V(-half, bottom, half), V(half, bottom, half), V(half, 0, half), V(-half, 0, half) },
            c => SideUV(2, c.x, c.y)); // +Z
        Quad(new[] { V(-half, bottom, -half), V(-half, 0, -half), V(half, 0, -half), V(half, bottom, -half) },
            c => SideUV(3, c.x, c.y)); // -Z
        // 바닥은 거의 보이지 않으므로 +X 옆면 맨 아래 흙 줄을 늘여 쓴다.
        Quad(new[] { V(-half, bottom, -half), V(half, bottom, -half), V(half, bottom, half), V(-half, bottom, half) },
            c => Atlas(SideX + U(c.x) * Voxels * Px, U(c.z) * Px));

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
        mesh.RecalculateTangents(); // 노멀맵에 필요
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

        var fox = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(FoxSetup.PrefabPath));
        fox.transform.SetParent(root.transform, false);

        var controller = root.AddComponent<PlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("actions").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        so.FindProperty("animator").objectReferenceValue = fox.GetComponent<Animator>();
        so.FindProperty("walkCycleSpeed").floatValue = MeasureWalkCycleSpeed();
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[MapSetup] 플레이어 프리팹: {PlayerPrefabPath}");
        return prefab;
    }

    // Walk 클립을 1배속으로 틀 때 발이 땅에 붙어 있으려면 몸이 얼마나 빨리 가야 하는지 잰다.
    // 앞왼다리 발끝이 한 주기 동안 앞뒤로 움직인 폭(보폭)을 재면, 디딘 발이 반 주기 동안 뒤로 미는 거리가 된다.
    // → 한 주기에 몸이 가는 거리 = 보폭 × 2, 속도 = 보폭 × 2 ÷ 클립 길이
    static float MeasureWalkCycleSpeed()
    {
        var fox = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FoxSetup.PrefabPath));
        var walk = AssetDatabase.LoadAllAssetRepresentationsAtPath(FoxSetup.FbxPath)
            .OfType<AnimationClip>().First(c => c.name == "Walk");
        var leg = fox.GetComponentsInChildren<Transform>().First(t => t.name == "Leg_FL");

        walk.SampleAnimation(fox, 0);
        float legLength = leg.position.y; // 다리 본은 엉덩이에서 위를 향하고, 발바닥이 y 0
        float min = float.MaxValue, max = float.MinValue;
        const int Samples = 60;
        for (int i = 0; i < Samples; i++)
        {
            walk.SampleAnimation(fox, walk.length * i / Samples);
            float footZ = (leg.position - leg.up * legLength).z; // 여우는 +Z를 바라본다
            min = Mathf.Min(min, footZ);
            max = Mathf.Max(max, footZ);
        }
        Object.DestroyImmediate(fox);

        float speed = (max - min) * 2f / walk.length;
        Debug.Log($"[MapSetup] Walk 보폭 {max - min:0.000}m, 1배속 이동 속도 {speed:0.000}m/s (다리 길이 {legLength:0.000}m)");
        return speed;
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
        Assign(rig, "target", player.transform);
        Assign(player, "cameraRig", rig);

        // 에디터에서도 시작 위치와 카메라 구도가 보이게 해 둔다. 타일은 Play 때 만든다.
        map.PlaceAtStart(player);
        rig.SnapToTarget();

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
