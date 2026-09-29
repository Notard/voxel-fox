using UnityEngine;

// 여우를 화면 중앙에 두고 따라가는 쿼터뷰 카메라 (내려다보는 각 45°, 방향 30°, 거리 16.5m).
// 이동 입력은 화면 방향에서 가장 가까운 격자 축으로 바꾼다 → ↑는 북쪽, →는 동쪽으로 타일 줄을 따라 곧게 간다.
// (4-1단계에서 변경: 처음에는 맵 전체를 비추는 고정 카메라였고, 입력이 화면 방향 그대로라 타일을 비스듬히 가로질렀다.)
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("내려다보는 각도")]
    [SerializeField, Range(20f, 80f)] float pitch = 45f;
    [Tooltip("바라보는 방향 (0 = 남쪽에서 북쪽으로)")]
    [SerializeField] float yaw = 30f;
    [Tooltip("여우까지 거리 (m). 4-2에서 11 → 16.5 (1.5배, 사용자 요청)")]
    [SerializeField] float distance = 16.5f;
    [Tooltip("높이만 이 시간(초)만큼 부드럽게 따라간다 → 점프할 때 화면이 출렁이지 않는다. 앞뒤·좌우는 지연 없이 따라가 여우가 늘 가운데에 있다")]
    [SerializeField] float verticalSmoothTime = 0.2f;
    [Tooltip("여우 발밑에서 화면 중앙까지 높이 (몸통 가운데)")]
    [SerializeField] Vector3 targetOffset = new(0f, 0.4f, 0f);
    [Tooltip("떨어질 때 이 높이 아래로는 따라가지 않는다 → 여우가 아래로 사라지는 모습이 보인다")]
    [SerializeField] float minFollowY = -1.5f;

    Vector3 focus;
    float heightVelocity;

    Quaternion Rotation => Quaternion.Euler(pitch, yaw, 0f);
    // 카메라 방향을 90° 단위로 반올림한 것이 이동 기준 축이다 (yaw 30° → 0°: ↑ = 북쪽 +Z).
    Quaternion MoveBasis => Quaternion.Euler(0f, Mathf.Round(yaw / 90f) * 90f, 0f);
    public Vector3 MoveForward => MoveBasis * Vector3.forward;
    public Vector3 MoveRight => MoveBasis * Vector3.right;
    public Transform Target => target;
    public Vector3 FocusPoint => focus;

    void Start() => SnapToTarget();

    void LateUpdate()
    {
        if (target == null) return;
        var goal = TargetFocus();
        focus = new Vector3(goal.x, Mathf.SmoothDamp(focus.y, goal.y, ref heightVelocity, verticalSmoothTime), goal.z);
        Place(focus);
    }

    // 부드럽게 따라가지 않고 곧바로 여우 위치로 옮긴다 (시작할 때, 에디터에서 배치할 때).
    public void SnapToTarget()
    {
        focus = target != null ? TargetFocus() : Vector3.zero;
        heightVelocity = 0f;
        Place(focus);
    }

    Vector3 TargetFocus()
    {
        var p = target.position;
        p.y = Mathf.Max(p.y, minFollowY);
        return p + targetOffset;
    }

    void Place(Vector3 center) =>
        transform.SetPositionAndRotation(center - Rotation * Vector3.forward * distance, Rotation);

    // 입력 (x = 오른쪽, y = 위쪽)을 격자 축 기준 월드 방향으로 바꾼다.
    public Vector3 InputToWorld(Vector2 input) => MoveRight * input.x + MoveForward * input.y;
}
