using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// 미리보기용 캡처 (preview/map):
//   정지 이미지 4장 (쿼터뷰 · 위에서 · 시작 지점 근접 · 코인 근접) + 결과 화면 2장 (GAME OVER · CLEAR!)
//   이동 녹화: 시작 → 동쪽으로 걷다 점프 → 코인 (3, 3) → 남쪽 코인 (3, 1) → 서쪽 구멍 (2, 1)에 빠짐
//     게임 화면에는 좌상단 아이템 카운터(UI)도 함께 찍는다 (5단계)
//     move_###.jpg 게임 화면(쿼터뷰), close_###.jpg 여우를 따라가는 근접 화면 (4단계: 동작 확인용)
// 환경 변수 VOXELFOX_PREVIEW_DIR이 있을 때만 캡처한다 (tools/build_map.sh가 넣어 줌).
// 없으면 테스트는 건너뛴다(Ignored). 그래픽 장치가 필요하므로 -nographics 없이 실행한다.
// 편집 모드에서는 스키닝이 갱신되지 않아 여우를 제대로 그릴 수 없으므로 Play 모드에서 찍는다.
public class MovementPreviewRecorder
{
    const int Fps = 60;        // 게임 프레임
    const int Every = 2;       // 2프레임마다 저장 → 30fps (Walk 한 주기 0.19초를 6장 정도로 담는다)

    string outDir;
    Camera cam;

    [UnityTest]
    public IEnumerator Record_WalkJumpFall()
    {
        outDir = Environment.GetEnvironmentVariable("VOXELFOX_PREVIEW_DIR");
        if (string.IsNullOrEmpty(outDir)) Assert.Ignore("VOXELFOX_PREVIEW_DIR 없음 → 캡처 안 함");
        Directory.CreateDirectory(outDir);
        foreach (var old in Directory.GetFiles(outDir, "*_???.jpg")) File.Delete(old);

        Time.captureFramerate = Fps;
        yield return SceneManager.LoadSceneAsync("Main");
        var map = Object.FindAnyObjectByType<MapBuilder>();
        var rig = Object.FindAnyObjectByType<CameraRig>();
        var game = Object.FindAnyObjectByType<GameManager>();
        var player = map.Player;
        player.readDeviceInput = false;
        cam = rig.GetComponent<Camera>();

        // 화면 UI(Screen Space Overlay)는 카메라 렌더에 안 들어가므로 녹화하는 동안 카메라 기준 UI로 바꾼다.
        // 카메라가 늘 렌더 텍스처에 그리게 해 두어야 UI 크기·위치가 녹화 화면에 맞는다.
        var canvas = Object.FindAnyObjectByType<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;

        // 정지 이미지
        var still = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = still;
        for (int i = 0; i < 5; i++) yield return null;
        rig.SnapToTarget();
        Save(still, "map_quarter.png");

        canvas.enabled = false; // 위에서 본 레이아웃과 근접 컷에는 UI를 빼고 찍는다
        cam.transform.SetPositionAndRotation(new Vector3(0, 17, 0), Quaternion.Euler(90, 0, 0));
        Save(still, "map_top.png");

        var fox = player.transform.position;
        cam.transform.position = fox + new Vector3(2.2f, 1.6f, -2.6f);
        cam.transform.LookAt(fox + Vector3.up * 0.35f);
        Save(still, "map_start_close.png");

        var coinPos = map.Coins.First(c => c.name == "Coin_3_3").transform.position;
        cam.transform.position = coinPos + new Vector3(-0.9f, 0.5f, -1.3f);
        cam.transform.LookAt(coinPos);
        Save(still, "coin_close.png");
        canvas.enabled = true;

        // 이동 녹화
        var rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        yield return null;
        rig.SnapToTarget();
        var closeRt = new RenderTexture(480, 270, 24, RenderTextureFormat.ARGB32);
        var closeCam = Object.Instantiate(cam); // 같은 배경·설정의 카메라를 하나 더
        Object.Destroy(closeCam.GetComponent<CameraRig>());
        if (closeCam.TryGetComponent(out AudioListener listener)) Object.Destroy(listener);
        closeCam.tag = "Untagged";
        closeCam.fieldOfView = 35f;
        closeCam.aspect = 16f / 9f;
        closeCam.enabled = false;
        var frames = new List<(string file, string phase)>();
        int frame = 0;
        string phase = "";
        IEnumerator Step()
        {
            yield return null;
            if (frame++ % Every != 0) yield break;
            var file = $"move_{frames.Count:000}.jpg";
            Save(rt, file);
            // 남쪽 약간 위에서 여우 옆모습을 따라간다 (동쪽으로 걸을 때 옆모습).
            var fox = player.transform.position;
            closeCam.transform.position = fox + new Vector3(0.4f, 0.9f, -2.6f);
            closeCam.transform.LookAt(fox + Vector3.up * 0.35f);
            Save(closeRt, $"close_{frames.Count:000}.jpg", closeCam);
            frames.Add((file, $"{phase} · 아이템 {game.ItemsCollected} / {game.ItemsTotal}"));
        }
        Vector2 InputFor(Vector3 world) =>
            new(Vector3.Dot(world, rig.MoveRight), Vector3.Dot(world, rig.MoveForward));

        phase = "대기";
        for (int i = 0; i < 24; i++) yield return Step();

        phase = "동쪽으로 → 코인 (3, 3)";
        var east = map.CellToWorld(new Vector2Int(3, 3));
        player.MoveInput = InputFor(Vector3.right);
        for (int i = 0; player.transform.position.x < east.x && i < Fps * 4; i++)
        {
            if (i == 24) { player.RequestJump(); phase = "점프"; }
            if (phase == "점프" && i > 28 && player.IsGrounded) phase = "동쪽으로 → 코인 (3, 3)";
            yield return Step();
        }

        phase = "남쪽으로 → 코인 (3, 1)";
        var south = map.CellToWorld(new Vector2Int(3, 1));
        player.MoveInput = InputFor(Vector3.back);
        for (int i = 0; player.transform.position.z > south.z && i < Fps * 3; i++)
            yield return Step();

        phase = "서쪽으로 → 구멍 (2, 1)";
        player.MoveInput = InputFor(Vector3.left);
        for (int i = 0; player.transform.position.y > -6f && i < Fps * 4; i++)
        {
            if (!player.IsGrounded && player.transform.position.y < -0.2f) phase = "구멍으로 낙하";
            yield return Step();
        }
        player.MoveInput = Vector2.zero;
        Assert.AreEqual(2, game.ItemsCollected, "가는 길에 코인 (3, 3), (3, 1)을 먹어야 함");
        phase = "GAME OVER";
        for (int i = 0; i < 50; i++) yield return Step();
        Assert.AreEqual(GameManager.State.GameOver, game.Current, "떨어지면 GAME OVER");
        cam.targetTexture = still = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        yield return null;
        Save(still, "result_gameover.png");

        // CLEAR 결과 화면: 씬을 다시 불러 코인 4개를 차례로 먹는다 (옆 타일로 옮긴 뒤 코인 쪽으로 걷기).
        yield return SceneManager.LoadSceneAsync("Main");
        map = Object.FindAnyObjectByType<MapBuilder>();
        rig = Object.FindAnyObjectByType<CameraRig>();
        game = Object.FindAnyObjectByType<GameManager>();
        player = map.Player;
        player.readDeviceInput = false;
        cam = rig.GetComponent<Camera>();
        canvas = Object.FindAnyObjectByType<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        cam.targetTexture = still;
        foreach (var cell in map.Layout.Coins)
        {
            var from = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down }
                .Select(d => cell + d).First(map.Layout.Tiles.Contains);
            player.Teleport(map.CellToWorld(from), Quaternion.identity);
            yield return null;
            var dir = map.CellToWorld(cell) - map.CellToWorld(from);
            player.MoveInput = InputFor(dir.normalized);
            for (int i = 0; i < 48; i++) yield return null;
            player.MoveInput = Vector2.zero;
        }
        for (int i = 0; i < 30; i++) yield return null;
        Assert.AreEqual(GameManager.State.Clear, game.Current, "코인 4개를 다 먹으면 CLEAR");
        Save(still, "result_clear.png");

        Time.captureFramerate = 0;
        cam.targetTexture = null;
        Object.Destroy(rt);
        Object.Destroy(still);
        Object.Destroy(closeRt);

        var list = string.Join(",\n", frames.Select(f => $"  {{\"file\": \"{f.file}\", \"phase\": \"{f.phase}\"}}"));
        File.WriteAllText(Path.Combine(outDir, "manifest.js"),
            $"window.MAP_PREVIEW = {{\n \"fps\": {Fps / Every},\n \"frames\": [\n{list}\n ]\n}};\n");
        Debug.Log($"[MovementPreviewRecorder] 정지 4장 + {frames.Count}프레임 → {outDir}");

    }

    // .png는 무손실(정지 이미지), .jpg는 용량을 줄인 녹화 프레임
    void Save(RenderTexture rt, string file, Camera camera = null)
    {
        camera ??= cam;
        var previous = camera.targetTexture;
        camera.targetTexture = rt;
        camera.Render();
        camera.targetTexture = previous;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;
        var bytes = file.EndsWith(".png") ? tex.EncodeToPNG() : tex.EncodeToJPG(85);
        File.WriteAllBytes(Path.Combine(outDir, file), bytes);
        Object.Destroy(tex);
    }
}
