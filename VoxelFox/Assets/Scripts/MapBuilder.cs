using UnityEngine;

// 문자열 레이아웃으로 타일맵을 만들고 여우를 시작 지점에 놓는다.
// 맵 중심이 이 오브젝트 위치이고, 타일 윗면이 y = 0이다.
public class MapBuilder : MonoBehaviour
{
    public const float TileSize = 2f;
    public const float TileHeight = 0.5f;

    [Tooltip("첫 줄이 가장 먼 줄. S 시작 · H 구멍 · C 아이템 · . 타일")]
    [SerializeField] string[] layout =
    {
        "S..C",
        ".H..",
        "C.HC",
        "...C",
    };
    [SerializeField] GameObject tilePrefab;
    [SerializeField] PlayerController player;
    [Tooltip("체커 패턴에서 어두운 칸의 밝기")]
    [SerializeField, Range(0.5f, 1f)] float checkerShade = 0.9f;

    MapLayout parsed;
    public MapLayout Layout => parsed ??= MapLayout.Parse(layout);
    public Transform TileRoot { get; private set; }
    public PlayerController Player => player;

    void Awake() => Build();

    void OnValidate() => parsed = null;

    public void Build()
    {
        parsed = null;
        if (TileRoot != null) DestroyObject(TileRoot.gameObject);
        TileRoot = new GameObject("Tiles").transform;
        TileRoot.SetParent(transform, false);

        var block = new MaterialPropertyBlock();
        foreach (var cell in Layout.Tiles)
        {
            // 같은 메시가 반복돼 보이지 않도록 칸마다 90° 단위로 돌린다.
            var rotation = Quaternion.Euler(0, 90 * ((cell.x * 7 + cell.y * 3) % 4), 0);
            var tile = Instantiate(tilePrefab, CellToWorld(cell), rotation, TileRoot);
            tile.name = $"Tile_{cell.x}_{cell.y}";
            if ((cell.x + cell.y) % 2 == 1)
            {
                block.SetColor("_BaseColor", new Color(checkerShade, checkerShade, checkerShade));
                tile.GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }

        if (player != null) PlaceAtStart(player);
    }

    // 시작 칸에 세우고 맵 중심을 바라보게 한다.
    public void PlaceAtStart(PlayerController target)
    {
        var start = CellToWorld(Layout.StartCell);
        var toCenter = transform.position - start;
        toCenter.y = 0;
        target.Teleport(start, Quaternion.LookRotation(toCenter));
    }

    public Vector3 CellToWorld(Vector2Int cell) =>
        transform.position + new Vector3(
            (cell.x - (Layout.Width - 1) * 0.5f) * TileSize,
            0,
            (cell.y - (Layout.Depth - 1) * 0.5f) * TileSize);

    static void DestroyObject(GameObject go)
    {
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    // 씬 뷰에서 레이아웃을 바로 볼 수 있게 한다 (타일은 Play 때 만들어진다).
    void OnDrawGizmos()
    {
        MapLayout map;
        try { map = Layout; }
        catch (System.ArgumentException) { return; }

        var size = new Vector3(TileSize, TileHeight, TileSize);
        var down = Vector3.down * TileHeight * 0.5f;
        Gizmos.color = new Color(0.4f, 0.8f, 0.3f);
        foreach (var cell in map.Tiles) Gizmos.DrawWireCube(CellToWorld(cell) + down, size);
        Gizmos.color = Color.red;
        foreach (var cell in map.Holes)
        {
            var c = CellToWorld(cell);
            Gizmos.DrawLine(c + new Vector3(-1, 0, -1), c + new Vector3(1, 0, 1));
            Gizmos.DrawLine(c + new Vector3(-1, 0, 1), c + new Vector3(1, 0, -1));
        }
        Gizmos.color = Color.yellow;
        foreach (var cell in map.Coins) Gizmos.DrawWireSphere(CellToWorld(cell) + Vector3.up * 0.5f, 0.3f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CellToWorld(map.StartCell) + Vector3.up * 0.5f, 0.4f);
    }
}
