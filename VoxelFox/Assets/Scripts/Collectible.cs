using System;
using UnityEngine;

// 아이템(코인): 제자리에서 돌며 위아래로 둥실거리다가 여우가 닿으면 반짝이를 남기고 사라진다.
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] float spinSpeed = 120f;
    [Tooltip("둥실거리는 높이 (위아래 폭의 절반, m)")]
    [SerializeField] float bobHeight = 0.1f;
    [Tooltip("둥실거리는 빠르기 (초당 왕복 수)")]
    [SerializeField] float bobFrequency = 0.8f;
    [Tooltip("획득할 때 남기는 반짝이 (끝나면 스스로 사라지는 파티클)")]
    [SerializeField] GameObject burstPrefab;

    // 획득 알림. GameManager가 개수를 센다.
    public static event Action<Collectible> Collected;

    public bool IsCollected { get; private set; }

    Vector3 basePosition;
    float phase;

    void Start()
    {
        basePosition = transform.localPosition;
        // 코인마다 둥실거리는 박자가 달라 보이도록 위치로 시작 위상을 정한다.
        phase = (basePosition.x * 0.37f + basePosition.z * 0.61f) % 1f;
    }

    void Update()
    {
        float bob = Mathf.Sin((Time.time * bobFrequency + phase) * 2f * Mathf.PI) * bobHeight;
        transform.localPosition = basePosition + Vector3.up * bob;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsCollected || !other.TryGetComponent(out PlayerController _)) return;
        IsCollected = true;
        if (burstPrefab != null) Instantiate(burstPrefab, transform.position, Quaternion.identity);
        Collected?.Invoke(this);
        Destroy(gameObject);
    }
}
