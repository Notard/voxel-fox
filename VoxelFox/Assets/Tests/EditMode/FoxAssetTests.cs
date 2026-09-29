using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 2단계 완료 기준: 세 클립이 정상 재생되고 복셀 형태가 깨지지 않는다.
public class FoxAssetTests
{
    const string Dir = "Assets/Art/Characters/Fox";
    const string FbxPath = Dir + "/Fox.fbx";
    const string ControllerPath = Dir + "/Fox.controller";
    const string PrefabPath = "Assets/Prefabs/Fox.prefab";

    static readonly string[] Bones =
        { "Root", "Body", "Head", "Leg_FL", "Leg_FR", "Leg_BL", "Leg_BR", "Tail" };

    GameObject fox;

    [SetUp]
    public void SetUp() =>
        fox = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(fox);

    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetRepresentationsAtPath(FbxPath)
            .OfType<AnimationClip>().SingleOrDefault(c => c.name == name);

    static Transform Find(GameObject root, string name) =>
        root.GetComponentsInChildren<Transform>().SingleOrDefault(t => t.name == name);

    [TestCase("Idle", 40, true)]
    [TestCase("Walk", 20, true)]
    [TestCase("Jump", 20, false)]
    public void Clip_ExistsWithLengthAndLoop(string name, int frames, bool loop)
    {
        var clip = Clip(name);
        Assert.IsNotNull(clip, $"{name} 클립 없음");
        Assert.AreEqual(frames / 30f, clip.length, 0.02f, "길이");
        Assert.AreEqual(loop, clip.isLooping, "루프");
    }

    [Test]
    public void Rig_HasAllBones()
    {
        foreach (var bone in Bones)
            Assert.IsNotNull(Find(fox, bone), $"본 없음: {bone}");
    }

    [Test]
    public void Mesh_IsRigidSkinned()
    {
        // 모든 정점이 본 하나에 가중치 1 → 관절이 돌아도 복셀이 찌그러지지 않는다.
        var smr = fox.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.IsNotNull(smr);
        var weights = smr.sharedMesh.boneWeights;
        Assert.IsNotEmpty(weights);
        Assert.IsTrue(weights.All(w => Mathf.Approximately(w.weight0, 1f)), "가중치 1이 아닌 정점 있음");
    }

    [Test]
    public void Model_IsUprightFacingForwardAndSized()
    {
        Clip("Idle").SampleAnimation(fox, 0);
        var head = Find(fox, "Head").position;
        var tail = Find(fox, "Tail").position;
        Assert.Greater(head.z, tail.z, "여우가 +Z(앞)를 바라봐야 함");
        Assert.Greater(head.y, 0.35f, "머리가 위쪽에 있어야 함");

        var bounds = fox.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.bounds;
        var size = bounds.size;
        var length = Mathf.Max(size.x, size.y, size.z);
        // 22복셀 × 0.0625m × 0.9 = 1.24m
        Assert.AreEqual(1.24f, length, 0.03f, "몸길이(꼬리~코) 약 1.24m (0.9배)");
    }

    [Test]
    public void Walk_MovesLegsInOpposition()
    {
        var walk = Clip("Walk");
        walk.SampleAnimation(fox, 0);
        var fl0 = Find(fox, "Leg_FL").localRotation;
        var fr0 = Find(fox, "Leg_FR").localRotation;
        walk.SampleAnimation(fox, 10 / 30f);
        var fl1 = Find(fox, "Leg_FL").localRotation;
        Assert.Greater(Quaternion.Angle(fl0, fl1), 40f, "Walk에서 앞다리가 충분히 움직여야 함");
        Assert.Greater(Quaternion.Angle(fl0, fr0), 40f, "좌우 앞다리는 반대로 움직여야 함");
    }

    [Test]
    public void Controller_HasParametersAndStates()
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.IsNotNull(ctrl);
        CollectionAssert.AreEquivalent(
            new[] { "Speed", "IsGrounded", "Jump", "WalkSpeed" }, ctrl.parameters.Select(p => p.name));
        var states = ctrl.layers[0].stateMachine.states.Select(s => s.state).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Idle", "Walk", "Jump" }, states.Select(s => s.name));
        Assert.IsTrue(states.All(s => s.motion != null), "모든 상태에 클립 연결");
        Assert.AreEqual("Idle", ctrl.layers[0].stateMachine.defaultState.name);
    }

    [Test]
    public void Prefab_AnimatorAlwaysAnimates() =>
        Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, fox.GetComponent<Animator>().cullingMode);

    [Test]
    public void Prefab_UsesPaletteMaterial()
    {
        var mat = fox.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
        Assert.AreEqual("Fox", mat.name);
        Assert.IsNotNull(mat.GetTexture("_BaseMap"));
        Assert.AreEqual(FilterMode.Point, mat.GetTexture("_BaseMap").filterMode);
    }
}
