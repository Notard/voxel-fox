using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 3단계 완료 기준: 여우가 타일 위를 걷고 점프하며, 구멍이나 가장자리 밖으로 나가면 떨어진다.
// Time.captureFramerate로 프레임 간격을 1/60초로 고정해 결과가 매번 같게 한다.
public class PlayerMovementTests
{
    const int Fps = 60;

    MapBuilder map;
    CameraRig rig;
    PlayerController player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.captureFramerate = Fps;
        yield return SceneManager.LoadSceneAsync("Main");
        map = Object.FindAnyObjectByType<MapBuilder>();
        rig = Object.FindAnyObjectByType<CameraRig>();
        player = map.Player;
        player.readDeviceInput = false;
        yield return WaitUntilGrounded();
    }

    [TearDown]
    public void TearDown() => Time.captureFramerate = 0;

    [Test]
    public void Map_BuildsTilesAndHoles()
    {
        Assert.AreEqual(14, map.TileRoot.childCount);
        foreach (var cell in map.Layout.Tiles)
        {
            Assert.IsTrue(GroundBelow(map.CellToWorld(cell), out var y), $"타일 없음 {cell}");
            Assert.AreEqual(0f, y, 1e-3f, $"타일 윗면 높이 {cell}");
        }
        foreach (var cell in map.Layout.Holes)
            Assert.IsFalse(GroundBelow(map.CellToWorld(cell), out _), $"구멍에 바닥이 있음 {cell}");
    }

    [Test]
    public void Player_StartsOnStartTile()
    {
        var start = map.CellToWorld(map.Layout.StartCell);
        var p = player.transform.position;
        Assert.Less(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(start.x, start.z)), 0.05f);
        Assert.AreEqual(0f, p.y, 0.05f, "타일 윗면(y 0)에 서 있어야 함");
        Assert.IsTrue(player.IsGrounded);
    }

    [UnityTest]
    public IEnumerator Walk_MovesAtMoveSpeedAndFacesDirection()
    {
        var start = player.transform.position;
        yield return Walk(Vector3.right, 1f);

        var moved = player.transform.position - start;
        Assert.AreEqual(player.MoveSpeed * 1f, moved.x, 0.1f, "1초 동안 이동 거리");
        Assert.AreEqual(0f, moved.z, 0.05f, "옆으로 새지 않아야 함");
        Assert.AreEqual(0f, moved.y, 0.05f, "높이 유지");
        Assert.Greater(Vector3.Dot(player.transform.forward, Vector3.right), 0.99f, "이동 방향을 바라봐야 함");
    }

    [UnityTest]
    public IEnumerator Walk_AcrossRow_StaysOnTiles()
    {
        // 시작 줄(z 3)은 4칸 모두 타일이다. 타일 이음새에서 걸리거나 빠지면 안 된다.
        var end = map.CellToWorld(new Vector2Int(3, 3));
        yield return Walk(Vector3.right, (end.x - player.transform.position.x) / player.MoveSpeed);

        Assert.AreEqual(end.x, player.transform.position.x, 0.15f, "오른쪽 끝 타일까지 도착");
        Assert.AreEqual(0f, player.transform.position.y, 0.05f);
        Assert.IsTrue(player.IsGrounded);
    }

    [UnityTest]
    public IEnumerator Jump_ReachesJumpHeightAndLands()
    {
        float ground = player.transform.position.y;
        float top = ground;
        player.RequestJump();
        for (int i = 0; i < Fps * 1.5f; i++)
        {
            yield return null;
            top = Mathf.Max(top, player.transform.position.y);
        }

        Assert.AreEqual(player.JumpHeight, top - ground, 0.1f, "점프 높이");
        Assert.IsTrue(player.IsGrounded, "착지해야 함");
        Assert.AreEqual(ground, player.transform.position.y, 0.05f);
    }

    [UnityTest]
    public IEnumerator Jump_IgnoredInAir()
    {
        float ground = player.transform.position.y;
        player.RequestJump();
        yield return null;
        yield return null;
        player.RequestJump(); // 공중에서 다시 눌러도 더 높이 뛰지 않는다
        float top = ground;
        for (int i = 0; i < Fps; i++)
        {
            yield return null;
            top = Mathf.Max(top, player.transform.position.y);
        }
        Assert.Less(top - ground, player.JumpHeight + 0.1f);
    }

    // 4-3: 구멍 가장자리 0.4m 앞에서 뛰면 구멍(2m)을 넘어 건너편 타일에 착지한다.
    [UnityTest]
    public IEnumerator RunningJump_ClearsHole()
    {
        // (0, 2)에서 동쪽으로 달리면 (1, 2)가 구멍이고 그 너머 (2, 2)는 타일이다.
        player.Teleport(map.CellToWorld(new Vector2Int(0, 2)), Quaternion.identity);
        yield return WaitUntilGrounded();
        float holeNear = map.CellToWorld(new Vector2Int(1, 2)).x - MapBuilder.TileSize / 2f;
        float holeFar = holeNear + MapBuilder.TileSize;

        player.MoveInput = InputFor(Vector3.right);
        while (player.transform.position.x < holeNear - 0.4f) yield return null;
        player.RequestJump();
        for (int i = 0; i < Fps * 2; i++) yield return null;
        player.MoveInput = Vector2.zero;

        var p = player.transform.position;
        Assert.AreEqual(0f, p.y, 0.05f, "구멍에 빠지지 않고 타일 위에 있어야 함");
        Assert.Greater(p.x, holeFar, "구멍 건너편 (2, 2)에 착지");
    }

    [Test]
    public void JumpDistance_IsWellOverHoleWidth() =>
        Assert.Greater(player.MoveSpeed * player.AirTime, MapBuilder.TileSize * 1.3f, "달리며 뛴 거리 > 구멍 너비 2m × 1.3");

    [UnityTest]
    public IEnumerator WalkIntoHole_Falls()
    {
        // (1, 3) 타일에서 남쪽으로 걸으면 바로 아래 칸 (1, 2)가 구멍이다.
        var from = map.CellToWorld(new Vector2Int(1, 3));
        player.Teleport(from, Quaternion.identity);
        yield return WaitUntilGrounded();

        yield return Walk(Vector3.back, 0.2f);
        Assert.IsTrue(player.IsGrounded, "구멍에 닿기 전에는 타일 위");
        yield return WalkUntilFallen(Vector3.back);
        var p = player.transform.position;
        Assert.AreEqual(from.x, p.x, 0.1f, "구멍 칸 안으로 떨어짐");
    }

    [UnityTest]
    public IEnumerator WalkOffEdge_Falls()
    {
        // 시작 칸은 맵 왼쪽 끝이다. 서쪽으로 가면 낭떠러지.
        yield return WalkUntilFallen(Vector3.left);
    }

    // 4-1: 쿼터뷰(카메라 30°)에서도 화살표는 타일 줄을 따라 곧게 간다.
    [Test]
    public void Input_FollowsGridAxes()
    {
        Assert.AreEqual(Vector3.forward, rig.InputToWorld(Vector2.up), "↑ = 북쪽 (+Z)");
        Assert.AreEqual(Vector3.right, rig.InputToWorld(Vector2.right), "→ = 동쪽 (+X)");
        // 화면 안쪽 방향과 가장 가까운 격자 축이어야 한다 (45° 이내).
        var camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
        Assert.Less(Vector3.Angle(camForward, rig.InputToWorld(Vector2.up)), 45f);
    }

    [UnityTest]
    public IEnumerator UpArrow_MovesStraightNorth()
    {
        // 시작 칸 (0, 3)은 북쪽 끝이므로 한 칸 아래 (0, 1)에서 북쪽으로 걷는다. (0, 2)도 타일이다.
        player.Teleport(map.CellToWorld(new Vector2Int(0, 1)), Quaternion.identity);
        yield return WaitUntilGrounded();
        var start = player.transform.position;
        player.MoveInput = Vector2.up;
        for (int i = 0; i < Fps; i++) yield return null;
        player.MoveInput = Vector2.zero;

        var moved = player.transform.position - start;
        Assert.AreEqual(player.MoveSpeed, moved.z, 0.1f, "북쪽으로 3m");
        Assert.AreEqual(0f, moved.x, 0.02f, "옆으로(대각선으로) 새지 않아야 함");
    }

    [UnityTest]
    public IEnumerator Camera_FollowsPlayerAtCenter()
    {
        var cam = Camera.main;
        Assert.Less(ScreenOffset(cam), 0.02f, "시작할 때 여우가 화면 중앙");

        player.MoveInput = Vector2.right;
        for (int i = 0; i < Fps * 1.5f; i++)
        {
            yield return null;
            Assert.Less(ScreenOffset(cam), 0.15f, "걷는 동안에도 여우가 화면 중앙 근처");
        }
        player.MoveInput = Vector2.zero;
        for (int i = 0; i < Fps; i++) yield return null;
        Assert.Less(ScreenOffset(cam), 0.02f, "멈추면 다시 화면 중앙");
    }

    // 여우 몸통 가운데가 화면 중앙에서 얼마나 떨어져 있는지 (화면 비율, 0 = 중앙)
    float ScreenOffset(Camera cam)
    {
        var v = cam.WorldToViewportPoint(player.transform.position + Vector3.up * 0.4f);
        return new Vector2(v.x - 0.5f, v.y - 0.5f).magnitude;
    }

    // 월드 방향으로 걷도록 카메라 기준 입력을 거꾸로 계산한다.
    Vector2 InputFor(Vector3 world) =>
        new(Vector3.Dot(world, rig.MoveRight), Vector3.Dot(world, rig.MoveForward));

    IEnumerator Walk(Vector3 world, float seconds)
    {
        player.MoveInput = InputFor(world);
        for (int i = 0; i < Mathf.RoundToInt(seconds * Fps); i++)
            yield return null;
        player.MoveInput = Vector2.zero;
        yield return null;
    }

    IEnumerator WalkUntilFallen(Vector3 world)
    {
        player.MoveInput = InputFor(world);
        for (int i = 0; i < Fps * 3 && player.transform.position.y > -5f; i++)
            yield return null;
        player.MoveInput = Vector2.zero;
        Assert.Less(player.transform.position.y, -5f, "3초 안에 y < -5까지 떨어져야 함");
    }

    IEnumerator WaitUntilGrounded()
    {
        for (int i = 0; i < Fps && !player.IsGrounded; i++)
            yield return null;
        Assert.IsTrue(player.IsGrounded, "바닥에 서지 못함");
    }

    // 여우(CharacterController)는 빼고 타일만 찾는다.
    static bool GroundBelow(Vector3 point, out float y)
    {
        foreach (var hit in Physics.RaycastAll(point + Vector3.up * 5f, Vector3.down, 20f))
            if (hit.collider is not CharacterController)
            {
                y = hit.point.y;
                return true;
            }
        y = 0f;
        return false;
    }
}
