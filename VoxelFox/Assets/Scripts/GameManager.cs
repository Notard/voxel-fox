using System;
using UnityEngine;

// 게임 진행 상태: 아이템 개수를 세고(5단계), 떨어지면 GAME OVER · 다 모으면 CLEAR(6단계).
// 결과가 나오면 여우 조작을 멈추고 걸린 시간도 멈춘다.
public class GameManager : MonoBehaviour
{
    public enum State { Playing, GameOver, Clear }

    [SerializeField] MapBuilder map;
    [Tooltip("여우가 이 높이 아래로 떨어지면 GAME OVER (타일 윗면 y 0, 바닥 -0.5)")]
    [SerializeField] float fallLimitY = -5f;

    public State Current { get; private set; } = State.Playing;
    public int ItemsTotal { get; private set; }
    public int ItemsCollected { get; private set; }
    public float ElapsedTime { get; private set; }
    public float FallLimitY => fallLimitY;

    public event Action ItemsChanged;
    public event Action<State> StateChanged;

    void OnEnable() => Collectible.Collected += OnCollected;
    void OnDisable() => Collectible.Collected -= OnCollected;

    // 모든 Awake(MapBuilder가 코인을 만드는 시점) 뒤에 센다.
    void Start()
    {
        ItemsTotal = map.Coins.Count;
        ItemsCollected = 0;
        ItemsChanged?.Invoke();
    }

    void Update()
    {
        if (Current != State.Playing) return;
        ElapsedTime += Time.deltaTime;
        if (map.Player.transform.position.y < fallLimitY) End(State.GameOver);
    }

    void OnCollected(Collectible item)
    {
        if (Current != State.Playing) return;
        ItemsCollected++;
        ItemsChanged?.Invoke();
        if (ItemsCollected >= ItemsTotal) End(State.Clear);
    }

    void End(State result)
    {
        Current = result;
        map.Player.ControlEnabled = false;
        StateChanged?.Invoke(result);
    }
}
