using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 7단계 완료 기준: 재시작하면 여우 위치, 아이템, UI가 모두 처음 상태로 돌아온다.
// R 키는 가상 키보드 장치를 붙여 실제 키 입력으로 누른다.
// Input System은 기본적으로 창(Game 뷰)에 포커스가 있을 때만 키보드를 받는데 배치 모드 테스트에는 포커스가 없다.
// 그래서 테스트 동안만 "포커스와 관계없이 입력 받기" 설정을 쓰고 끝나면 원래 설정으로 되돌린다.
public class RestartTests
{
    const int Fps = 60;

    MapBuilder map;
    CameraRig rig;
    GameManager game;
    PlayerController player;
    GameUI ui;
    Keyboard keyboard;
    InputSettings.BackgroundBehavior originalBackground;
    InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.captureFramerate = Fps;
        var settings = InputSystem.settings;
        originalBackground = settings.backgroundBehavior;
        originalEditorBehavior = settings.editorInputBehaviorInPlayMode;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Main");
        yield return FindAll();
    }

    [TearDown]
    public void TearDown()
    {
        Time.captureFramerate = 0;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = originalBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorBehavior;
    }

    [UnityTest]
    public IEnumerator RKey_WhilePlaying_ResetsEverything()
    {
        // 코인 하나를 먹고 조금 움직인 뒤 R
        yield return WalkInto(map.Layout.Coins.First(), from: new Vector2Int(2, 3));
        Assert.AreEqual(1, game.ItemsCollected);
        var oldGame = game;

        yield return PressR();
        Assert.IsTrue(oldGame == null, "씬을 다시 불러 옛 GameManager는 사라짐");
        AssertInitialState();
    }

    [UnityTest]
    public IEnumerator RestartButton_AfterGameOver_ResetsEverything()
    {
        player.MoveInput = InputFor(Vector3.left); // 서쪽 맵 밖으로
        for (int i = 0; i < Fps * 3 && game.Current == GameManager.State.Playing; i++) yield return null;
        Assert.AreEqual(GameManager.State.GameOver, game.Current);

        var button = Panel("GameOverPanel").transform.Find("Box/RestartButton").GetComponent<Button>();
        Assert.IsTrue(button.isActiveAndEnabled && button.interactable);
        Assert.AreEqual("다시 하기  (R)", button.GetComponentInChildren<TMP_Text>().text);
        button.onClick.Invoke();
        yield return FindAll();
        AssertInitialState();
    }

    [UnityTest]
    public IEnumerator RKey_AfterClear_ResetsEverything()
    {
        foreach (var cell in map.Layout.Coins)
            yield return WalkInto(cell, from: Neighbor(cell));
        Assert.AreEqual(GameManager.State.Clear, game.Current);

        yield return PressR();
        AssertInitialState();
    }

    // 여러 번 재시작해도 옛 GameManager가 정적 이벤트에 남아 코인을 두 번 세지 않는다.
    [UnityTest]
    public IEnumerator RestartingTwice_CountsCoinOnlyOnce()
    {
        yield return PressR();
        yield return PressR();
        yield return WalkInto(map.Layout.Coins.First(), from: new Vector2Int(2, 3));
        Assert.AreEqual(1, game.ItemsCollected);
        Assert.AreEqual("아이템 1 / 4", ItemText());
    }

    void AssertInitialState()
    {
        Assert.AreEqual(GameManager.State.Playing, game.Current);
        Assert.AreEqual(0, game.ItemsCollected);
        Assert.AreEqual(4, game.ItemsTotal);
        Assert.Less(game.ElapsedTime, 0.5f, "걸린 시간도 처음부터");
        Assert.AreEqual("아이템 0 / 4", ItemText());
        Assert.IsFalse(Panel("GameOverPanel").activeSelf);
        Assert.IsFalse(Panel("ClearPanel").activeSelf);

        var start = map.CellToWorld(map.Layout.StartCell);
        var p = player.transform.position;
        Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(start.x, start.z)), 0.05f, "여우가 시작 칸");
        Assert.AreEqual(0f, p.y, 0.05f);
        Assert.IsTrue(player.ControlEnabled, "다시 조작 가능");
        Assert.AreEqual(4, map.Coins.Count(c => c != null), "코인 4개가 모두 다시 있음");
    }

    IEnumerator PressR()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return FindAll();
    }

    // 씬이 다시 불린 뒤 새 오브젝트를 찾는다.
    IEnumerator FindAll()
    {
        for (int i = 0; i < 5; i++) yield return null;
        map = Object.FindAnyObjectByType<MapBuilder>();
        rig = Object.FindAnyObjectByType<CameraRig>();
        game = Object.FindAnyObjectByType<GameManager>();
        ui = Object.FindAnyObjectByType<GameUI>();
        player = map.Player;
        player.readDeviceInput = false;
        for (int i = 0; i < Fps / 5; i++) yield return null;
    }

    IEnumerator WalkInto(Vector2Int cell, Vector2Int from)
    {
        player.Teleport(map.CellToWorld(from), Quaternion.identity);
        yield return null;
        player.MoveInput = InputFor(map.CellToWorld(cell) - map.CellToWorld(from));
        for (int i = 0; i < Fps * 0.8f; i++) yield return null;
        player.MoveInput = Vector2.zero;
    }

    Vector2Int Neighbor(Vector2Int cell) =>
        new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down }
            .Select(d => cell + d).First(map.Layout.Tiles.Contains);

    GameObject Panel(string name) => ui.transform.Find(name).gameObject;
    string ItemText() => GameObject.Find("ItemText").GetComponent<TMP_Text>().text;

    Vector2 InputFor(Vector3 world)
    {
        world.y = 0;
        world.Normalize();
        return new(Vector3.Dot(world, rig.MoveRight), Vector3.Dot(world, rig.MoveForward));
    }
}
