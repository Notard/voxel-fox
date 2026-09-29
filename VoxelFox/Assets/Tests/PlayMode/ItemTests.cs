using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 5단계 완료 기준: 아이템을 먹으면 카운트가 오르고 아이템이 사라진다.
public class ItemTests
{
    const int Fps = 60;

    MapBuilder map;
    CameraRig rig;
    GameManager game;
    PlayerController player;
    TMP_Text itemText;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.captureFramerate = Fps;
        yield return SceneManager.LoadSceneAsync("Main");
        map = Object.FindAnyObjectByType<MapBuilder>();
        rig = Object.FindAnyObjectByType<CameraRig>();
        game = Object.FindAnyObjectByType<GameManager>();
        player = map.Player;
        player.readDeviceInput = false;
        itemText = GameObject.Find("ItemText").GetComponent<TMP_Text>();
        yield return Frames(0.2f);
    }

    [TearDown]
    public void TearDown() => Time.captureFramerate = 0;

    [Test]
    public void Map_Spawns4CoinsOnCoinCells()
    {
        Assert.AreEqual(4, map.Coins.Count);
        foreach (var cell in map.Layout.Coins)
        {
            var expected = map.CellToWorld(cell);
            Assert.IsTrue(map.Coins.Any(c =>
                    Vector2.Distance(new Vector2(c.transform.position.x, c.transform.position.z),
                                     new Vector2(expected.x, expected.z)) < 0.01f &&
                    Mathf.Abs(c.transform.position.y - MapBuilder.CoinHeight) < 0.15f),
                $"칸 {cell}에 코인이 없음");
        }
    }

    [Test]
    public void Counter_StartsAt0Of4_InKorean()
    {
        Assert.AreEqual(4, game.ItemsTotal);
        Assert.AreEqual(0, game.ItemsCollected);
        Assert.AreEqual("아이템 0 / 4", itemText.text);
        // 동적 글꼴은 쓸 때 글자를 추가하므로 tryAddCharacter로 확인한다.
        Assert.IsTrue(itemText.font.HasCharacters("아이템 0123456789/", out var missing, true, true),
            $"한글 글꼴에 없는 글자: {string.Join(", ", (missing ?? new uint[0]).Select(u => (char)u))}");
    }

    [UnityTest]
    public IEnumerator Coin_SpinsAndBobs()
    {
        var coin = map.Coins[0].transform;
        var rotation = coin.rotation;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < Fps * 1.5f; i++)
        {
            yield return null;
            minY = Mathf.Min(minY, coin.position.y);
            maxY = Mathf.Max(maxY, coin.position.y);
        }
        Assert.Greater(Quaternion.Angle(rotation, coin.rotation), 10f, "제자리에서 돌아야 함");
        Assert.Greater(maxY - minY, 0.1f, "위아래로 둥실거려야 함");
        Assert.Less(maxY - minY, 0.25f);
    }

    [UnityTest]
    public IEnumerator WalkIntoCoin_CountsUpAndCoinDisappears()
    {
        // 시작 칸 (0, 3)에서 동쪽으로 끝까지 걸으면 (3, 3)의 코인을 먹는다.
        var coin = map.Coins.First(c => c.name == "Coin_3_3");
        player.MoveInput = InputFor(Vector3.right);
        for (int i = 0; i < Fps * 3 && coin != null; i++) yield return null;
        player.MoveInput = Vector2.zero;

        Assert.IsTrue(coin == null, "먹은 코인은 사라져야 함");
        Assert.AreEqual(1, game.ItemsCollected);
        Assert.AreEqual("아이템 1 / 4", itemText.text);
        Assert.IsNotNull(GameObject.Find("CoinBurst(Clone)"), "먹은 자리에 반짝이");

        yield return Frames(1.5f);
        Assert.IsNull(GameObject.Find("CoinBurst(Clone)"), "반짝이는 끝나면 스스로 사라짐");
    }

    [UnityTest]
    public IEnumerator CollectAll_Reaches4Of4()
    {
        foreach (var cell in map.Layout.Coins)
        {
            // 코인 칸 옆의 타일 칸에서 코인 쪽으로 걷는다.
            var from = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down }
                .Select(d => cell + d).First(map.Layout.Tiles.Contains);
            player.Teleport(map.CellToWorld(from), Quaternion.identity);
            yield return null;
            player.MoveInput = InputFor(map.CellToWorld(cell) - map.CellToWorld(from));
            yield return Frames(0.8f);
            player.MoveInput = Vector2.zero;
        }
        Assert.AreEqual(4, game.ItemsCollected);
        Assert.AreEqual("아이템 4 / 4", itemText.text);
        Assert.IsTrue(map.Coins.All(c => c == null), "코인이 모두 사라져야 함");
    }

    Vector2 InputFor(Vector3 world)
    {
        world.y = 0;
        world.Normalize();
        return new(Vector3.Dot(world, rig.MoveRight), Vector3.Dot(world, rig.MoveForward));
    }

    static IEnumerator Frames(float seconds)
    {
        for (int i = 0; i < Mathf.RoundToInt(seconds * Time.captureFramerate); i++)
            yield return null;
    }
}
