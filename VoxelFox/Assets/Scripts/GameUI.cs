using TMPro;
using UnityEngine;

// 화면 UI: 좌상단 "아이템 0 / 4" 카운터(5단계), GAME OVER / CLEAR! 결과 패널(6단계).
public class GameUI : MonoBehaviour
{
    [SerializeField] GameManager game;
    [SerializeField] TMP_Text itemText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text gameOverTitle;
    [SerializeField] TMP_Text gameOverDetail;
    [SerializeField] GameObject clearPanel;
    [SerializeField] TMP_Text clearTitle;
    [SerializeField] TMP_Text clearDetail;

    // TMP 기본 글꼴(LiberationSans)에는 한글이 없다.
    // 맑은 고딕은 재배포할 수 없어 저장소에 넣지 않고, 실행할 때 Windows에 설치된 글꼴로 만든다 (Windows 전용 게임).
    static readonly (string family, string style)[] KoreanFonts =
    {
        ("Malgun Gothic", "Bold"),
        ("Malgun Gothic", "Regular"),
        ("Noto Sans KR", "Regular"),
    };
    static TMP_FontAsset koreanFont;

    void Awake()
    {
        var font = KoreanFont();
        if (font != null)
            foreach (var text in GetComponentsInChildren<TMP_Text>(true)) text.font = font;
        else Debug.LogWarning("[GameUI] 한글 글꼴(맑은 고딕)을 찾지 못해 기본 글꼴을 씀");
        gameOverPanel.SetActive(false);
        clearPanel.SetActive(false);
    }

    void OnEnable()
    {
        game.ItemsChanged += RefreshItems;
        game.StateChanged += ShowResult;
        RefreshItems();
    }

    void OnDisable()
    {
        game.ItemsChanged -= RefreshItems;
        game.StateChanged -= ShowResult;
    }

    void RefreshItems() => itemText.text = $"아이템 {game.ItemsCollected} / {game.ItemsTotal}";

    void ShowResult(GameManager.State result)
    {
        if (result == GameManager.State.GameOver)
        {
            gameOverTitle.text = "GAME OVER";
            gameOverDetail.text = $"떨어졌어요 · 아이템 {game.ItemsCollected} / {game.ItemsTotal}";
            gameOverPanel.SetActive(true);
        }
        else if (result == GameManager.State.Clear)
        {
            clearTitle.text = "CLEAR!";
            clearDetail.text = $"아이템 {game.ItemsTotal}개 모두 모음 · {game.ElapsedTime:0.0}초";
            clearPanel.SetActive(true);
        }
    }

    public static TMP_FontAsset KoreanFont()
    {
        if (koreanFont != null) return koreanFont;
        foreach (var (family, style) in KoreanFonts)
        {
            koreanFont = TMP_FontAsset.CreateFontAsset(family, style);
            if (koreanFont != null) break;
        }
        return koreanFont;
    }
}
