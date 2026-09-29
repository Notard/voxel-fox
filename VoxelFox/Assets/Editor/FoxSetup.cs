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

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle", new Vector3(300, 0));
        var walk = sm.AddState("Walk", new Vector3(300, 120));
        var jump = sm.AddState("Jump", new Vector3(560, 60));
        idle.motion = Clip("Idle");
        walk.motion = Clip("Walk");
        jump.motion = Clip("Jump");
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
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");

        // 점프 직후 한 프레임은 아직 땅에 붙어 있으므로 exit time으로 최소 재생 시간을 둔다.
        var land = jump.AddTransition(idle);
        land.hasExitTime = true;
        land.exitTime = 0.5f;
        land.duration = 0.1f;
        land.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

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
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[FoxSetup] 프리팹: {PrefabPath}");
    }
}
