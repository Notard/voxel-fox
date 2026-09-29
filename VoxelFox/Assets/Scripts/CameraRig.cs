using UnityEngine;

// 맵 전체가 보이는 고정 쿼터뷰 카메라. 이동 입력을 카메라 기준 방향으로 바꿔 준다.
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    [SerializeField] MapBuilder map;
    [Tooltip("내려다보는 각도")]
    [SerializeField, Range(20f, 80f)] float pitch = 45f;
    [Tooltip("맵을 바라보는 방향 (0 = 남쪽에서 북쪽으로)")]
    [SerializeField] float yaw = 30f;
    [Tooltip("맵 둘레 여백 배율")]
    [SerializeField] float margin = 1.08f;

    Quaternion YawRotation => Quaternion.Euler(0, yaw, 0);
    public Vector3 FlatForward => YawRotation * Vector3.forward;
    public Vector3 FlatRight => YawRotation * Vector3.right;

    // 화면 비율은 실행해야 정해지므로 시작할 때 다시 맞춘다.
    void Start() => Apply();

    public void Apply()
    {
        var cam = GetComponent<Camera>();
        var center = map != null ? map.transform.position : Vector3.zero;
        var half = map != null ? map.HalfExtents : new Vector2(4f, 4f);
        var rotation = Quaternion.Euler(pitch, yaw, 0);

        // 맵 네 모서리(타일 바닥 ~ 여우 키 높이)가 모두 화면에 들어오는 가장 가까운 거리
        float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * cam.aspect;
        var inverse = Quaternion.Inverse(rotation);
        float distance = 0f;
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
                foreach (var y in new[] { -MapBuilder.TileHeight, 1f })
                {
                    var p = inverse * new Vector3(sx * half.x * margin, y, sz * half.y * margin);
                    distance = Mathf.Max(distance, Mathf.Abs(p.x) / tanH - p.z, Mathf.Abs(p.y) / tanV - p.z);
                }

        transform.SetPositionAndRotation(center - rotation * Vector3.forward * distance, rotation);
    }

    // 입력 (x = 오른쪽, y = 위쪽)을 화면 기준 월드 방향으로 바꾼다.
    public Vector3 InputToWorld(Vector2 input) => FlatRight * input.x + FlatForward * input.y;
}
