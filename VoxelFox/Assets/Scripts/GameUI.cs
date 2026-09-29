using TMPro;
using UnityEngine;

// 화면 UI. 5단계: 좌상단 "아이템 0 / 4" 카운터.
public class GameUI : MonoBehaviour
{
    [SerializeField] GameManager game;
    [SerializeField] TMP_Text itemText;

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
        if (font != null) itemText.font = font;
        else Debug.LogWarning("[GameUI] 한글 글꼴(맑은 고딕)을 찾지 못해 기본 글꼴을 씀");
    }

    void OnEnable()
    {
        game.ItemsChanged += Refresh;
        Refresh();
    }

    void OnDisable() => game.ItemsChanged -= Refresh;

    void Refresh() => itemText.text = $"아이템 {game.ItemsCollected} / {game.ItemsTotal}";

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
