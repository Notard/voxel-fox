using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// 4단계 완료 기준: 멈춤 → Idle, 이동 → Walk, 점프 → Jump가 자연스럽게 전환되고, 걸을 때 발이 미끄러지지 않는다.
public class FoxAnimationTests
{
    const int Fps = 60;

    CameraRig rig;
    PlayerController player;
    Animator animator;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.captureFramerate = Fps;
        yield return SceneManager.LoadSceneAsync("Main");
        rig = Object.FindAnyObjectByType<CameraRig>();
        player = Object.FindAnyObjectByType<MapBuilder>().Player;
        player.readDeviceInput = false;
        animator = player.Animator;
        yield return Frames(0.3f);
        Assert.IsTrue(player.IsGrounded);
    }

    [TearDown]
    public void TearDown() => Time.captureFramerate = 0;

    [Test]
    public void Standing_PlaysIdle() => AssertState("Idle");

    [UnityTest]
    public IEnumerator Moving_PlaysWalk_AtMatchingSpeed()
    {
        player.MoveInput = InputFor(Vector3.right);
        yield return Frames(0.5f);
        AssertState("Walk");
        var expected = player.MoveSpeed / player.WalkCycleSpeed;
        Assert.AreEqual(expected, animator.GetCurrentAnimatorStateInfo(0).speedMultiplier, 0.05f,
            "Walk 배속 = 이동 속도 ÷ 1배속 속도");
    }

    [UnityTest]
    public IEnumerator Stopping_ReturnsToIdle()
    {
        player.MoveInput = InputFor(Vector3.right);
        yield return Frames(0.5f);
        player.MoveInput = Vector2.zero;
        yield return Frames(0.3f);
        AssertState("Idle");
    }

    [UnityTest]
    public IEnumerator Jump_PlaysJump_ThenIdleAfterLanding()
    {
        player.RequestJump();
        yield return Frames(0.2f);
        AssertState("Jump");
        Assert.IsFalse(animator.GetBool("IsGrounded"), "공중에서는 IsGrounded = false");

        yield return Frames(0.8f); // 체공 약 0.69초
        Assert.IsTrue(player.IsGrounded);
        AssertState("Idle");
    }

    [UnityTest]
    public IEnumerator JumpWhileWalking_LandsIntoWalk()
    {
        player.MoveInput = InputFor(Vector3.right);
        yield return Frames(0.2f);
        player.RequestJump();
        yield return Frames(0.2f);
        AssertState("Jump");
        yield return Frames(0.8f);
        AssertState("Walk");
    }

    // 걷는 동안 디딘 발(발끝이 앞 끝에서 뒤 끝으로 가는 반 주기)이 땅에서 얼마나 미끄러지는지 잰다.
    [UnityTest]
    public IEnumerator Walking_FeetDoNotSlide()
    {
        var leg = player.GetComponentsInChildren<Transform>().First(t => t.name == "Leg_FL");
        float legLength = leg.position.y - player.transform.position.y; // 서 있을 때 엉덩이 높이

        player.MoveInput = InputFor(Vector3.right);
        yield return Frames(0.5f); // Walk로 넘어가 속도가 일정해질 때까지

        // 애니메이션이 적용된 뒤(LateUpdate)의 발 위치를 기록한다.
        // 배치 모드 테스트에서는 WaitForEndOfFrame을 쓸 수 없다.
        // 60fps에서는 디딤 한 번이 6프레임 정도라 발이 가장 앞/뒤인 순간을 정확히 못 잡는다 → 240fps로 잰다.
        Time.captureFramerate = 240;
        var recorder = player.gameObject.AddComponent<FootRecorder>();
        recorder.Init(leg, legLength);
        yield return Frames(1f);
        Object.Destroy(recorder);
        var local = recorder.Local;
        var world = recorder.World;
        player.MoveInput = Vector2.zero;

        // 끝점: 앞뒤 ±10프레임(0.04초, 디딤 반 주기 0.1초보다 짧음) 안에서 가장 앞/뒤인 점.
        // 곡선의 작은 흔들림을 끝점으로 잡으면 디딤이 쪼개져 미끄러짐이 작게 나오므로 걸러낸다.
        const int W = 10;
        bool Extreme(int i, int sign)
        {
            for (int j = Mathf.Max(0, i - W); j <= Mathf.Min(local.Count - 1, i + W); j++)
                if (sign * local[j] > sign * local[i]) return false;
            return i >= W && i < local.Count - W;
        }
        float stride = local.Max() - local.Min();
        var slips = new List<float>();
        int start = -1;
        for (int i = 0; i < local.Count; i++)
        {
            if (Extreme(i, +1)) start = i;                // 발이 가장 앞: 디딤 시작
            else if (start >= 0 && Extreme(i, -1))        // 가장 뒤: 디딤 끝
            {
                Assert.Greater(local[start] - local[i], stride * 0.7f, "디딤 한 번에 보폭만큼 뒤로 밀어야 함");
                slips.Add(Mathf.Abs(world[i] - world[start]));
                start = -1;
            }
        }
        Debug.Log($"[FoxAnimationTests] 보폭 {stride:0.000}m, 디딤 {slips.Count}번, 발 미끄러짐 최대 {slips.Max():0.000}m, 평균 {slips.Average():0.000}m");
        Assert.GreaterOrEqual(slips.Count, 3, "1초 동안 디딤이 여러 번 있어야 함");
        // 1배속 그대로 틀면 디딤 한 번에 약 0.7m 미끄러진다. 몇 프레임(240fps, 한 프레임 0.0125m) 오차만 허용한다.
        Assert.Less(slips.Max(), 0.04f, $"발 미끄러짐: {string.Join(", ", slips.Select(s => s.ToString("0.000")))}m");
    }

    void AssertState(string name)
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        Assert.IsTrue(state.IsName(name), $"{name} 상태여야 함");
        Assert.IsFalse(animator.IsInTransition(0), "전환이 끝나 있어야 함");
    }

    Vector2 InputFor(Vector3 world) =>
        new(Vector3.Dot(world, rig.FlatRight), Vector3.Dot(world, rig.FlatForward));

    static IEnumerator Frames(float seconds)
    {
        for (int i = 0; i < Mathf.RoundToInt(seconds * Time.captureFramerate); i++)
            yield return null;
    }
}

// 걷는 동안 앞왼다리 발끝 위치를 프레임마다 남긴다.
public class FootRecorder : MonoBehaviour
{
    public readonly List<float> Local = new(); // 몸 기준 앞뒤 위치
    public readonly List<float> World = new(); // 월드 x (동쪽으로 걷는 중)
    Transform leg;
    float legLength;

    public void Init(Transform leg, float legLength)
    {
        this.leg = leg;
        this.legLength = legLength;
    }

    void LateUpdate()
    {
        var foot = leg.position - leg.up * legLength;
        Local.Add(transform.InverseTransformPoint(foot).z);
        World.Add(foot.x);
    }
}
