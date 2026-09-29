using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 5단계: 코인·반짝이 프리팹, Main 씬의 GameManager·UI 연결 상태.
public class ItemAssetTests
{
    const string CoinPrefabPath = "Assets/Prefabs/Coin.prefab";
    const string ScenePath = "Assets/Scenes/Main.unity";

    [Test]
    public void CoinPrefab_IsVoxelCoinWithTrigger()
    {
        var coin = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        Assert.IsNotNull(coin);

        var size = coin.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        Assert.AreEqual(0.6f, size.x, 0.01f, "지름 12복셀 × 0.05m");
        Assert.AreEqual(0.6f, size.y, 0.01f);
        Assert.AreEqual(0.2f, size.z, 0.01f, "두께 2복셀 + 무늬 앞뒤 1복셀씩");

        var trigger = coin.GetComponent<SphereCollider>();
        Assert.IsNotNull(trigger);
        Assert.IsTrue(trigger.isTrigger, "밟고 서는 게 아니라 닿으면 먹는 트리거");
        Assert.IsTrue(coin.GetComponent<Rigidbody>().isKinematic, "움직이는 트리거는 Kinematic Rigidbody");
        Assert.IsNotNull(coin.GetComponent<Collectible>());

        Assert.AreEqual(FilterMode.Point,
            coin.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BaseMap").filterMode);
    }

    [Test]
    public void CoinBurst_PlaysOnceAndDestroysItself()
    {
        var coin = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        var burst = (GameObject)new SerializedObject(coin.GetComponent<Collectible>())
            .FindProperty("burstPrefab").objectReferenceValue;
        Assert.IsNotNull(burst, "코인에 반짝이 프리팹이 연결되어 있어야 함");

        var main = burst.GetComponent<ParticleSystem>().main;
        Assert.IsFalse(main.loop, "한 번만 터진다");
        Assert.IsTrue(main.playOnAwake);
        Assert.AreEqual(ParticleSystemStopAction.Destroy, main.stopAction, "끝나면 스스로 지워진다");
    }

    [Test]
    public void MainScene_HasGameManagerAndCounterUI()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var map = Object.FindAnyObjectByType<MapBuilder>();
        Assert.IsNotNull(new SerializedObject(map).FindProperty("coinPrefab").objectReferenceValue,
            "MapBuilder에 코인 프리팹 연결");

        var game = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(game, "GameManager 없음");
        Assert.AreSame(map, new SerializedObject(game).FindProperty("map").objectReferenceValue);

        var ui = Object.FindAnyObjectByType<GameUI>();
        Assert.IsNotNull(ui, "GameUI 없음");
        var text = (Component)new SerializedObject(ui).FindProperty("itemText").objectReferenceValue;
        Assert.IsNotNull(text, "카운터 글자 연결");

        // 화면 좌상단에 붙어 있어야 한다.
        var panel = (RectTransform)text.transform.parent;
        Assert.AreEqual(new Vector2(0, 1), panel.anchorMin);
        Assert.AreEqual(new Vector2(0, 1), panel.anchorMax);
    }

    // 6단계: 결과 패널 2종이 연결되어 있고 처음에는 꺼져 있다.
    [TestCase("gameOverPanel", "gameOverTitle", "gameOverDetail")]
    [TestCase("clearPanel", "clearTitle", "clearDetail")]
    public void MainScene_HasHiddenResultPanel(string panelField, string titleField, string detailField)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var ui = new SerializedObject(Object.FindAnyObjectByType<GameUI>());
        var panel = (GameObject)ui.FindProperty(panelField).objectReferenceValue;
        Assert.IsNotNull(panel, $"{panelField} 연결");
        Assert.IsFalse(panel.activeSelf, "처음에는 꺼져 있어야 함");
        Assert.IsNotNull(ui.FindProperty(titleField).objectReferenceValue);
        Assert.IsNotNull(ui.FindProperty(detailField).objectReferenceValue);
    }
}
