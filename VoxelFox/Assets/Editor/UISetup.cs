using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 화면 UI를 Main 씬에 만든다: 좌상단 아이템 카운터(5단계), GAME OVER / CLEAR! 결과 패널(6단계).
// 매번 UI를 통째로 새로 만든다 (설정이 바뀌어도 씬에 옛 값이 남지 않게). ItemSetup 다음에 실행한다.
// 글자 내용은 비워 두고 실행할 때 GameUI가 한글 글꼴을 입힌 뒤 채운다 (기본 글꼴에는 한글이 없음).
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod UISetup.Run
public static class UISetup
{
    static readonly Color Gold = new(1f, 0.93f, 0.62f);

    [MenuItem("VoxelFox/UI Setup")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(MapSetup.ScenePath, OpenSceneMode.Single);
        var game = Object.FindAnyObjectByType<GameManager>();

        var old = GameObject.Find("UI");
        if (old != null) Object.DestroyImmediate(old);
        var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGo.transform;

        // 좌상단 아이템 카운터: 반투명 판 + 금색 글자
        var itemPanel = Box("ItemPanel", root, new Color(0.08f, 0.12f, 0.16f, 0.55f));
        Anchor(itemPanel, new Vector2(0, 1), new Vector2(32, -32), new Vector2(300, 84));
        var itemText = Text("ItemText", itemPanel.transform, 44, Gold, TextAlignmentOptions.MidlineLeft);
        Stretch(itemText.rectTransform, new Vector2(24, 0), new Vector2(-16, 0));

        // 결과 패널 2종: 화면 전체를 살짝 어둡게 + 가운데 상자 (제목, 설명)
        var gameOver = ResultPanel("GameOverPanel", root,
            box: new Color(0.24f, 0.07f, 0.09f, 0.9f), title: new Color(1f, 0.55f, 0.55f), out var gameOverTitle, out var gameOverDetail);
        var clear = ResultPanel("ClearPanel", root,
            box: new Color(0.18f, 0.13f, 0.03f, 0.9f), title: new Color(1f, 0.84f, 0.3f), out var clearTitle, out var clearDetail);

        var ui = canvasGo.AddComponent<GameUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("game").objectReferenceValue = game;
        so.FindProperty("itemText").objectReferenceValue = itemText;
        so.FindProperty("gameOverPanel").objectReferenceValue = gameOver;
        so.FindProperty("gameOverTitle").objectReferenceValue = gameOverTitle;
        so.FindProperty("gameOverDetail").objectReferenceValue = gameOverDetail;
        so.FindProperty("clearPanel").objectReferenceValue = clear;
        so.FindProperty("clearTitle").objectReferenceValue = clearTitle;
        so.FindProperty("clearDetail").objectReferenceValue = clearDetail;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[UISetup] 씬 배치: 아이템 카운터, 결과 패널 2종 → {MapSetup.ScenePath}");
    }

    static GameObject ResultPanel(string name, Transform root, Color box, Color title,
        out TMP_Text titleText, out TMP_Text detailText)
    {
        var dim = Box(name, root, new Color(0f, 0f, 0f, 0.35f));
        Stretch((RectTransform)dim.transform, Vector2.zero, Vector2.zero);

        // 카메라가 여우를 화면 가운데에 두므로 상자는 위쪽에 둔다 → 결과가 나온 순간의 여우가 가려지지 않는다.
        var panel = Box("Box", dim.transform, box);
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(760, 340));

        titleText = Text("Title", panel.transform, 110, title, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        Place(titleText.rectTransform, new Vector2(0, 50), new Vector2(720, 150));

        detailText = Text("Detail", panel.transform, 40, Gold, TextAlignmentOptions.Center);
        Place(detailText.rectTransform, new Vector2(0, -80), new Vector2(720, 80));

        dim.SetActive(false); // 결과가 나올 때 GameUI가 켠다
        return dim;
    }

    static GameObject Box(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static TMP_Text Text(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    static void Anchor(GameObject go, Vector2 corner, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = corner;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
