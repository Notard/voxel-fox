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
//   정지 이미지 3장 (쿼터뷰 · 위에서 · 시작 지점 근접)
//   이동 녹화: 시작 → 동쪽으로 걷다 점프 → 남쪽으로 꺾어 구멍 (2, 1)에 빠짐
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
        var player = map.Player;
        player.readDeviceInput = false;
        cam = rig.GetComponent<Camera>();
        for (int i = 0; i < 5; i++) yield return null;

        // 정지 이미지
        var still = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        cam.aspect = 16f / 9f;
        rig.SnapToTarget();
        Save(still, "map_quarter.png");

        cam.transform.SetPositionAndRotation(new Vector3(0, 17, 0), Quaternion.Euler(90, 0, 0));
        Save(still, "map_top.png");

        var fox = player.transform.position;
        cam.transform.position = fox + new Vector3(2.2f, 1.6f, -2.6f);
        cam.transform.LookAt(fox + Vector3.up * 0.35f);
        Save(still, "map_start_close.png");
        Object.Destroy(still);
        rig.SnapToTarget();

        // 이동 녹화
        var rt = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
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
            frames.Add((file, phase));
        }
        Vector2 InputFor(Vector3 world) =>
            new(Vector3.Dot(world, rig.MoveRight), Vector3.Dot(world, rig.MoveForward));

        phase = "대기";
        for (int i = 0; i < 24; i++) yield return Step();

        phase = "동쪽으로 걷기";
        var turn = map.CellToWorld(new Vector2Int(2, 3));
        player.MoveInput = InputFor(Vector3.right);
        for (int i = 0; player.transform.position.x < turn.x && i < Fps * 3; i++)
        {
            if (i == 24) { player.RequestJump(); phase = "점프"; }
            if (phase == "점프" && i > 28 && player.IsGrounded) phase = "동쪽으로 걷기";
            yield return Step();
        }

        phase = "남쪽으로 → 구멍 (2, 1)";
        player.MoveInput = InputFor(Vector3.back);
        for (int i = 0; player.transform.position.y > -6f && i < Fps * 4; i++)
        {
            if (!player.IsGrounded && player.transform.position.y < -0.2f) phase = "구멍으로 낙하";
            yield return Step();
        }
        player.MoveInput = Vector2.zero;
        phase = "낙하 (화면 밖)";
        for (int i = 0; i < 16; i++) yield return Step();

        Time.captureFramerate = 0;
        Object.Destroy(rt);
        Object.Destroy(closeRt);

        var list = string.Join(",\n", frames.Select(f => $"  {{\"file\": \"{f.file}\", \"phase\": \"{f.phase}\"}}"));
        File.WriteAllText(Path.Combine(outDir, "manifest.js"),
            $"window.MAP_PREVIEW = {{\n \"fps\": {Fps / Every},\n \"frames\": [\n{list}\n ]\n}};\n");
        Debug.Log($"[MovementPreviewRecorder] 정지 3장 + {frames.Count}프레임 → {outDir}");
        Assert.Less(player.transform.position.y, -5f, "녹화 끝에는 구멍으로 떨어져 있어야 함");
    }

    // .png는 무손실(정지 이미지), .jpg는 용량을 줄인 녹화 프레임
    void Save(RenderTexture rt, string file, Camera camera = null)
    {
        camera ??= cam;
        camera.targetTexture = rt;
        camera.Render();
        camera.targetTexture = null;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;
        var bytes = file.EndsWith(".png") ? tex.EncodeToPNG() : tex.EncodeToJPG(85);
        File.WriteAllBytes(Path.Combine(outDir, file), bytes);
        Object.Destroy(tex);
    }
}
