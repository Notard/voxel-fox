using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 3단계: 레이아웃 해석, 타일·플레이어 프리팹, Main 씬 연결 상태.
public class MapAssetTests
{
    const string TilePrefabPath = "Assets/Prefabs/GrassTile.prefab";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string ScenePath = "Assets/Scenes/Main.unity";

    [Test]
    public void Layout_MainMap_Has14Tiles2Holes4CoinsAndStart()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var layout = Object.FindAnyObjectByType<MapBuilder>().Layout;

        Assert.AreEqual(4, layout.Width);
        Assert.AreEqual(4, layout.Depth);
        Assert.AreEqual(14, layout.Tiles.Count, "타일 = 16칸 - 구멍 2");
        CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 2), new Vector2Int(2, 1) }, layout.Holes);
        Assert.AreEqual(4, layout.Coins.Count);
        Assert.AreEqual(new Vector2Int(0, 3), layout.StartCell, "시작은 왼쪽 위 (x0, z3)");
        CollectionAssert.Contains(layout.Tiles, layout.StartCell, "시작 칸에는 바닥이 있어야 함");
    }

    // 줄 구분은 '/' (속성 인자로 string[]을 바로 넘길 수 없어서)
    [TestCase("S../..", TestName = "Layout_Rejects_UnevenRows")]
    [TestCase("S.X/...", TestName = "Layout_Rejects_UnknownChar")]
    [TestCase("S.S/...", TestName = "Layout_Rejects_TwoStarts")]
    [TestCase(".../...", TestName = "Layout_Rejects_NoStart")]
    public void Layout_RejectsBadInput(string rows) =>
        Assert.Throws<ArgumentException>(() => MapLayout.Parse(rows.Split('/')));

    [Test]
    public void Layout_FirstRowIsFarthest()
    {
        var layout = MapLayout.Parse(new[] { "S.", "H." });
        Assert.AreEqual(new Vector2Int(0, 1), layout.StartCell);
        CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0) }, layout.Holes);
    }

    [Test]
    public void TilePrefab_Is2x05x2_WithTopAtZero()
    {
        var tile = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
        Assert.IsNotNull(tile);

        var bounds = tile.GetComponent<MeshFilter>().sharedMesh.bounds;
        AssertVector(new Vector3(2f, 0.5f, 2f), bounds.size, "메시 크기");
        Assert.AreEqual(0f, bounds.max.y, 1e-4f, "윗면 y = 0");
        Assert.AreEqual(0f, bounds.center.x, 1e-4f);
        Assert.AreEqual(0f, bounds.center.z, 1e-4f);

        var box = tile.GetComponent<BoxCollider>();
        Assert.IsNotNull(box, "BoxCollider 없음");
        AssertVector(bounds.size, box.size, "콜라이더 크기");
        AssertVector(bounds.center, box.center, "콜라이더 중심");

        var mat = tile.GetComponent<MeshRenderer>().sharedMaterial;
        Assert.AreEqual(FilterMode.Point, mat.GetTexture("_BaseMap").filterMode);
    }

    [Test]
    public void TileMaterial_HasNormalMapAndMeshHasTangents()
    {
        var tile = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
        var mat = tile.GetComponent<MeshRenderer>().sharedMaterial;
        var bump = mat.GetTexture("_BumpMap");
        Assert.IsNotNull(bump, "노멀맵 없음");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(bump));
        Assert.AreEqual(TextureImporterType.NormalMap, importer.textureType, "노멀맵 타입으로 임포트");
        Assert.IsTrue(mat.IsKeywordEnabled("_NORMALMAP"), "_NORMALMAP 키워드가 꺼져 있으면 노멀맵이 무시됨");

        var mesh = tile.GetComponent<MeshFilter>().sharedMesh;
        Assert.AreEqual(mesh.vertexCount, mesh.tangents.Length, "접선이 없으면 노멀맵 방향이 틀어짐");
    }

    [Test]
    public void PlayerPrefab_HasControllerAndFox()
    {
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Assert.IsNotNull(player);
        Assert.IsNotNull(player.GetComponent<PlayerController>());
        var cc = player.GetComponent<CharacterController>();
        Assert.IsNotNull(cc);
        Assert.AreEqual(0f, cc.center.y - cc.height / 2f, 0.01f, "캡슐 바닥 = 발바닥 (y 0)");
        Assert.Less(cc.radius * 2f, MapBuilder.TileSize / 2f, "구멍(2m)에 빠질 수 있을 만큼 작아야 함");

        var animator = player.GetComponentInChildren<Animator>();
        Assert.IsNotNull(animator, "Fox 모델 없음");
        Assert.AreEqual("Fox", animator.name);
        Assert.IsFalse(animator.applyRootMotion);

        var controller = player.GetComponent<PlayerController>();
        Assert.AreSame(animator, controller.Animator, "PlayerController에 Fox Animator 연결");
        // 다리 0.225m, ±40° → 보폭 2 × 0.225 × sin40° = 0.289m, 한 주기(0.667초)에 두 번 → 약 0.87m/s
        Assert.AreEqual(0.87f, controller.WalkCycleSpeed, 0.05f, "Walk 1배속 이동 속도");
    }

    [Test]
    public void MainScene_IsWired()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var map = Object.FindAnyObjectByType<MapBuilder>();
        var rig = Object.FindAnyObjectByType<CameraRig>();
        Assert.IsNotNull(map, "MapBuilder 없음");
        Assert.IsNotNull(rig, "CameraRig 없음");
        Assert.IsNotNull(map.Player, "MapBuilder에 Player 연결 안 됨");
        Assert.AreEqual("MainCamera", rig.tag);

        // 카메라가 맵 중심을 위에서 비스듬히 본다.
        var forward = rig.transform.forward;
        Assert.AreEqual(45f, Vector3.Angle(forward, Vector3.ProjectOnPlane(forward, Vector3.up)), 1f, "내려다보는 각도");
        var toCenter = map.transform.position - rig.transform.position;
        Assert.Less(Vector3.Angle(forward, toCenter), 1f, "맵 중심을 바라봐야 함");
    }

    static void AssertVector(Vector3 expected, Vector3 actual, string message) =>
        Assert.Less(Vector3.Distance(expected, actual), 1e-3f, $"{message}: {expected} ≠ {actual}");
}
