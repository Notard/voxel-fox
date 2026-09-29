using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 5단계 아이템 설정: 복셀 코인(팔레트·메시·머티리얼·프리팹), 획득 반짝이 프리팹,
// Main 씬에 GameManager와 UI(아이템 카운터) 배치. 여러 번 실행해도 안전하다.
// MapSetup 다음에 실행한다 (tools/build_map.sh).
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod ItemSetup.Run
public static class ItemSetup
{
    public const string ItemDir = "Assets/Art/Items";
    public const string PalettePath = ItemDir + "/Coin_Palette.png";
    public const string MeshPath = ItemDir + "/Coin_Mesh.asset";
    public const string MaterialPath = ItemDir + "/Coin.mat";
    public const string SparkleMaterialPath = ItemDir + "/CoinSparkle.mat";
    public const string CoinPrefabPath = "Assets/Prefabs/Coin.prefab";
    public const string BurstPrefabPath = "Assets/Prefabs/CoinBurst.prefab";

    // 1복셀 = 0.05m. 지름 12복셀(0.6m), 두께 2복셀 + 가운데 무늬 앞뒤로 1복셀씩 도드라짐
    const float Voxel = 0.05f;
    const int Size = 12;

    // 팔레트 4×4, 칸 번호 = y * 4 + x
    static readonly Color32[] Palette =
    {
        Hex(0xC8871A), Hex(0xF5BE2E), Hex(0xFFE27A), Hex(0xFFF6C8), // 테두리, 금색, 무늬, 반짝임
    };
    const int Rim = 0, Gold = 1, Emblem = 2, Shine = 3;

    [MenuItem("VoxelFox/Item Setup")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(ItemDir)) AssetDatabase.CreateFolder("Assets/Art", "Items");
        var palette = CreatePalette();
        var material = CreateMaterial(palette);
        var mesh = CreateMesh();
        var burst = CreateBurstPrefab();
        var coin = CreateCoinPrefab(mesh, material, burst);
        SetupScene(coin);
        AssetDatabase.SaveAssets();
        Debug.Log("[ItemSetup] 완료");
    }

    static Color32 Hex(int rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

    static Texture2D CreatePalette()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Palette[Mathf.Min(i, Palette.Length - 1)];
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
        mat.SetFloat("_Smoothness", 0.45f);
        mat.SetFloat("_Metallic", 0f);
        // 그늘진 면도 금색이 살도록 살짝 스스로 빛나게 한다.
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.25f, 0.17f, 0.02f));
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 복셀 배치 → 보이는 면만 메시로 (여우·타일과 같은 방식). 동전 면이 ±Z를 향한다.
    static Mesh CreateMesh()
    {
        var voxels = new Dictionary<Vector3Int, int>();
        float c = (Size - 1) / 2f;
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            {
                float dx = x - c, dy = y - c;
                float r2 = dx * dx + dy * dy;
                if (r2 > 31f) continue; // 둥근 테두리
                bool rim = r2 > 19f;
                bool emblem = Mathf.Abs(dx) + Mathf.Abs(dy) <= 3f; // 가운데 마름모 무늬
                bool shine = !rim && !emblem && dx < -1.5f && dy > 1.5f && r2 < 15f; // 왼쪽 위 반짝임
                int color = rim ? Rim : emblem ? Emblem : shine ? Shine : Gold;
                voxels[new Vector3Int(x, y, 0)] = color;
                voxels[new Vector3Int(x, y, 1)] = color;
                if (emblem)
                {
                    voxels[new Vector3Int(x, y, -1)] = Emblem;
                    voxels[new Vector3Int(x, y, 2)] = Emblem;
                }
            }

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        var offset = new Vector3(c + 0.5f, c + 0.5f, 1f) * Voxel; // 가운데가 원점
        // (법선, u, v): u × v = 법선 → 시계 방향 = Unity 앞면
        var faces = new[]
        {
            (Vector3Int.right, Vector3.up, Vector3.forward),
            (Vector3Int.left, Vector3.forward, Vector3.up),
            (Vector3Int.up, Vector3.forward, Vector3.right),
            (Vector3Int.down, Vector3.right, Vector3.forward),
            (new Vector3Int(0, 0, 1), Vector3.right, Vector3.up),
            (new Vector3Int(0, 0, -1), Vector3.up, Vector3.right),
        };
        foreach (var (pos, color) in voxels)
            foreach (var (n, u, v) in faces)
            {
                if (voxels.ContainsKey(pos + n)) continue;
                var corner = (Vector3)pos * Voxel - offset + Vector3.Max((Vector3)n, Vector3.zero) * Voxel;
                var uv = new Vector2((color % 4 + 0.5f) / 4f, (color / 4 + 0.5f) / 4f);
                int start = verts.Count;
                foreach (var p in new[] { corner, corner + u * Voxel, corner + (u + v) * Voxel, corner + v * Voxel })
                {
                    verts.Add(p);
                    normals.Add(n);
                    uvs.Add(uv);
                }
                tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }
        mesh.Clear();
        mesh.name = "Coin";
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        Debug.Log($"[ItemSetup] 코인 메시: 복셀 {voxels.Count}, 면 {tris.Count / 6}");
        return mesh;
    }

    // 획득 반짝이: 금색 작은 복셀 큐브가 사방으로 튀었다가 작아지며 사라진다. 끝나면 스스로 지워진다.
    static GameObject CreateBurstPrefab()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(SparkleMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, SparkleMaterialPath);
        }
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);

        var go = new GameObject("CoinBurst");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(Palette[Gold], Palette[Shine]);
        main.gravityModifier = 0.8f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx"); // 복셀 느낌의 작은 큐브
        renderer.sharedMaterial = mat;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, BurstPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateCoinPrefab(Mesh mesh, Material material, GameObject burst)
    {
        var go = new GameObject("Coin");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.4f; // 코인(반지름 0.3m)보다 조금 크게 → 스치기만 해도 먹힌다
        // 움직이는 트리거는 Rigidbody(Kinematic)가 있어야 물리 엔진이 정확히 따라간다.
        var body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var item = go.AddComponent<Collectible>();
        var so = new SerializedObject(item);
        so.FindProperty("burstPrefab").objectReferenceValue = burst;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, CoinPrefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"[ItemSetup] 코인 프리팹: {CoinPrefabPath}");
        return prefab;
    }

    static void SetupScene(GameObject coinPrefab)
    {
        var scene = EditorSceneManager.OpenScene(MapSetup.ScenePath, OpenSceneMode.Single);
        var map = Object.FindAnyObjectByType<MapBuilder>();
        Assign(map, "coinPrefab", coinPrefab);

        var gameGo = GameObject.Find("GameManager");
        if (gameGo == null) gameGo = new GameObject("GameManager");
        if (!gameGo.TryGetComponent(out GameManager game)) game = gameGo.AddComponent<GameManager>();
        Assign(game, "map", map);

        // UI를 매번 새로 만든다 (설정이 바뀌어도 씬에 옛 값이 남지 않게).
        var old = GameObject.Find("UI");
        if (old != null) Object.DestroyImmediate(old);
        var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 좌상단 반투명 판 + 글자
        var panel = new GameObject("ItemPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(32, -32);
        panelRect.sizeDelta = new Vector2(300, 84);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.16f, 0.55f);

        var textGo = new GameObject("ItemText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panel.transform, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24, 0);
        textRect.offsetMax = new Vector2(-16, 0);
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = ""; // 글자는 GameUI가 한글 글꼴을 입힌 뒤 채운다 (기본 글꼴에는 한글이 없음)
        text.fontSize = 44;
        text.color = new Color(1f, 0.93f, 0.62f); // 코인과 어울리는 밝은 금색
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var ui = canvasGo.AddComponent<GameUI>();
        Assign(ui, "game", game);
        Assign(ui, "itemText", text);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[ItemSetup] 씬 배치: GameManager, UI → {MapSetup.ScenePath}");
    }

    static void Assign(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
