using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 2단계 여우 캐릭터 설정: 머티리얼, Animator Controller, 프리팹 생성.
// 여러 번 실행해도 안전하다.
// 배치 실행: Unity.exe -batchmode -quit -projectPath . -executeMethod FoxSetup.Run
public static class FoxSetup
{
    public const string Dir = "Assets/Art/Characters/Fox";
    public const string FbxPath = Dir + "/Fox.fbx";
    public const string PalettePath = Dir + "/Fox_Palette.png";
    public const string MaterialPath = Dir + "/Fox.mat";
    public const string ControllerPath = Dir + "/Fox.controller";
    public const string PrefabPath = "Assets/Prefabs/Fox.prefab";

    [MenuItem("VoxelFox/Fox Setup")]
    public static void Run()
    {
        var material = CreateMaterial();
        RemapMaterial(material);
        var controller = CreateController();
        CreatePrefab(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[FoxSetup] 완료");
    }

    static Material CreateMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        mat.SetTexture("_BaseMap", palette);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        Debug.Log($"[FoxSetup] 머티리얼: {MaterialPath}");
        return mat;
    }

    static void RemapMaterial(Material mat)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        foreach (var source in AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Material>())
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), mat);
        // FBX 안의 머티리얼 이름이 FoxMat
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "FoxMat"), mat);
        importer.SaveAndReimport();
    }

    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetRepresentationsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .First(c => c.name == name);

    static AnimatorController CreateController()
    {
        AssetDatabase.DeleteAsset(ControllerPath); // 항상 새로 만든다
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter(new AnimatorControllerParameter
        {
            name = "IsGrounded",
            type = AnimatorControllerParameterType.Bool,
            defaultBool = true,
        });
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        // Walk 재생 배속. PlayerController가 이동 속도 ÷ 보폭 속도로 넣는다 (발 미끄러짐 방지).
        ctrl.AddParameter(new AnimatorControllerParameter
        {
            name = "WalkSpeed",
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f,
        });

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle", new Vector3(300, 0));
        var walk = sm.AddState("Walk", new Vector3(300, 120));
        var jump = sm.AddState("Jump", new Vector3(560, 60));
        idle.motion = Clip("Idle");
        walk.motion = Clip("Walk");
        walk.speedParameter = "WalkSpeed";
        walk.speedParameterActive = true;
        jump.motion = Clip("Jump");
        // 체공 시간(점프 1.5m, 중력 -15 → 약 0.89초)에 남은 클립(프레임 6~20, 0.47초)을 맞춘다.
        // 4-3에서 0.7 → 0.53 (점프를 높고 길게 바꿔서)
        jump.speed = 0.53f;
        sm.defaultState = idle;

        var toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.1f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var anyToJump = sm.AddAnyStateTransition(jump);
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.05f;
        // Space를 누르는 순간 이미 몸이 떠오르므로 웅크림(0~4)은 건너뛰고 도약 직전(6)부터 튼다.
        anyToJump.offset = 0.3f;
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

        // 착지하면 멈춰 있으면 Idle, 움직이고 있으면 바로 Walk.
        // PlayerController가 이동한 뒤 IsGrounded를 넣으므로 점프한 프레임부터 이미 false다.
        // 그래서 2단계에서 두었던 exit time(0.5)은 없앴다.
        var landIdle = jump.AddTransition(idle);
        landIdle.hasExitTime = false;
        landIdle.duration = 0.1f;
        landIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        landIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var landWalk = jump.AddTransition(walk);
        landWalk.hasExitTime = false;
        landWalk.duration = 0.1f;
        landWalk.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        landWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        Debug.Log($"[FoxSetup] Animator Controller: {ControllerPath}");
        return ctrl;
    }

    static void CreatePrefab(AnimatorController controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = "Fox";
        var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        // FBX 기본값(Cull Update Transforms)은 화면에 안 그려지는 동안 뼈를 움직이지 않는다.
        // 플레이어는 늘 움직여야 하고, 배치 모드 테스트에는 화면 렌더링이 없어서 다리가 멈춘 채로 측정된다.
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[FoxSetup] 프리팹: {PrefabPath}");
    }
}
