using System;
using UnityEngine;

// 게임 진행 상태. 5단계: 아이템 개수를 센다. (6단계에서 Playing / GameOver / Clear 상태를 더한다)
public class GameManager : MonoBehaviour
{
    [SerializeField] MapBuilder map;

    public int ItemsTotal { get; private set; }
    public int ItemsCollected { get; private set; }
    public event Action ItemsChanged;

    void OnEnable() => Collectible.Collected += OnCollected;
    void OnDisable() => Collectible.Collected -= OnCollected;

    // 모든 Awake(MapBuilder가 코인을 만드는 시점) 뒤에 센다.
    void Start()
    {
        ItemsTotal = map.Coins.Count;
        ItemsCollected = 0;
        ItemsChanged?.Invoke();
    }

    void OnCollected(Collectible item)
    {
        ItemsCollected++;
        ItemsChanged?.Invoke();
    }
}
