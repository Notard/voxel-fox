using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 6단계 완료 기준: 떨어지면 GAME OVER, 아이템 4/4면 CLEAR! 패널이 뜨고 더 이상 조작되지 않는다.
public class GameFlowTests
{
    const int Fps = 60;

    MapBuilder map;
    CameraRig rig;
    GameManager game;
    PlayerController player;
    GameObject gameOverPanel, clearPanel;

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
        var ui = Object.FindAnyObjectByType<GameUI>().transform;
        gameOverPanel = ui.Find("GameOverPanel").gameObject;
        clearPanel = ui.Find("ClearPanel").gameObject;
        yield return Frames(0.2f);
    }

    [TearDown]
    public void TearDown() => Time.captureFramerate = 0;

    [Test]
    public void Starts_Playing_WithPanelsHidden()
    {
        Assert.AreEqual(GameManager.State.Playing, game.Current);
        Assert.IsFalse(gameOverPanel.activeSelf);
        Assert.IsFalse(clearPanel.activeSelf);
        Assert.IsTrue(player.ControlEnabled);
    }

    [UnityTest]
    public IEnumerator FallingBelowLimit_ShowsGameOver_AndStopsControl()
    {
        // 시작 칸은 맵 서쪽 끝 → 서쪽으로 걸으면 떨어진다.
        player.MoveInput = InputFor(Vector3.left);
        for (int i = 0; i < Fps * 3 && game.Current == GameManager.State.Playing; i++) yield return null;

        Assert.AreEqual(GameManager.State.GameOver, game.Current);
        Assert.Less(player.transform.position.y, game.FallLimitY + 0.5f, "y < -5에서 GAME OVER");
        Assert.IsTrue(gameOverPanel.activeSelf, "GAME OVER 패널");
        Assert.IsFalse(clearPanel.activeSelf);
        Assert.AreEqual("GAME OVER", Title(gameOverPanel));
        Assert.IsFalse(player.ControlEnabled, "조작 정지");

        // 입력을 넣어도 옆으로 움직이지 않는다 (중력으로 계속 떨어지기만 한다).
        var before = player.transform.position;
        player.MoveInput = InputFor(Vector3.right);
        yield return Frames(0.5f);
        var after = player.transform.position;
        Assert.AreEqual(before.x, after.x, 0.01f, "GAME OVER 뒤에는 움직이지 않음");
        Assert.AreEqual(before.z, after.z, 0.01f);
    }

    [UnityTest]
    public IEnumerator CollectingAll_ShowsClear_StopsControlAndTimer()
    {
        yield return CollectAllCoins();

        Assert.AreEqual(GameManager.State.Clear, game.Current);
        Assert.IsTrue(clearPanel.activeSelf, "CLEAR 패널");
        Assert.IsFalse(gameOverPanel.activeSelf);
        Assert.AreEqual("CLEAR!", Title(clearPanel));
        StringAssert.Contains("초", Detail(clearPanel), "걸린 시간 표시");
        Assert.IsFalse(player.ControlEnabled, "조작 정지");

        // 조작과 시간이 멈춘다.
        float time = game.ElapsedTime;
        var before = player.transform.position;
        player.MoveInput = InputFor(Vector3.right);
        player.RequestJump();
        yield return Frames(1f);
        Assert.Less(Vector3.Distance(before, player.transform.position), 0.02f, "CLEAR 뒤에는 움직이지도 뛰지도 않음");
        Assert.AreEqual(time, game.ElapsedTime, 1e-4f, "걸린 시간이 멈춤");
    }

    [UnityTest]
    public IEnumerator Clear_IsNotOverriddenByLaterFall()
    {
        yield return CollectAllCoins();
        // CLEAR 뒤에 (테스트로) 여우를 맵 밖으로 옮겨 떨어뜨려도 GAME OVER로 바뀌지 않는다.
        player.Teleport(new Vector3(20f, 0f, 0f), Quaternion.identity);
        yield return Frames(1.5f);
        Assert.Less(player.transform.position.y, game.FallLimitY);
        Assert.AreEqual(GameManager.State.Clear, game.Current);
        Assert.IsFalse(gameOverPanel.activeSelf);
    }

    [UnityTest]
    public IEnumerator Timer_RunsWhilePlaying()
    {
        float start = game.ElapsedTime;
        yield return Frames(1f);
        Assert.AreEqual(1f, game.ElapsedTime - start, 0.05f);
    }

    IEnumerator CollectAllCoins()
    {
        foreach (var cell in map.Layout.Coins)
        {
            var from = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down }
                .Select(d => cell + d).First(map.Layout.Tiles.Contains);
            player.Teleport(map.CellToWorld(from), Quaternion.identity);
            yield return null;
            player.MoveInput = InputFor(map.CellToWorld(cell) - map.CellToWorld(from));
            yield return Frames(0.8f);
            player.MoveInput = Vector2.zero;
        }
        Assert.AreEqual(4, game.ItemsCollected);
    }

    static string Title(GameObject panel) => panel.transform.Find("Box/Title").GetComponent<TMP_Text>().text;
    static string Detail(GameObject panel) => panel.transform.Find("Box/Detail").GetComponent<TMP_Text>().text;

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
