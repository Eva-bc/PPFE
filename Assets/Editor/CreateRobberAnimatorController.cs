using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Editor utility that generates the Robber Animator Controller asset with Idle and Walk states.
/// Run via Assets > Create Robber Animator Controller.
/// </summary>
public static class CreateRobberAnimatorController
{
    private const string ControllerOutputPath  = "Assets/Animations/RobberController.controller";
    private const string IdleFbxPath           = "Assets/Asset/Robber Rigged/Crouch Torch Idle 01.fbx";
    private const string WalkFbxPath           = "Assets/Asset/Robber Rigged/Crouch Torch Walk Forward.fbx";
    private const string SpeedParam            = "Speed";
    private const float  WalkThreshold         = 0.1f;

    [MenuItem("Assets/Create Robber Animator Controller")]
    public static void Create()
    {
        // Ensure output directory exists.
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        // Load animation clips embedded in the FBX files.
        AnimationClip idleClip = LoadFirstClip(IdleFbxPath);
        AnimationClip walkClip = LoadFirstClip(WalkFbxPath);

        if (idleClip == null)
        {
            Debug.LogError($"[RobberController] Idle clip not found in {IdleFbxPath}");
            return;
        }

        if (walkClip == null)
        {
            Debug.LogError($"[RobberController] Walk clip not found in {WalkFbxPath}");
            return;
        }

        // Create the controller asset.
        string path       = AssetDatabase.GenerateUniqueAssetPath(ControllerOutputPath);
        var    controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add the Speed float parameter.
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Create states.
        AnimatorState idleState = sm.AddState("Idle", new Vector3(250, 0, 0));
        idleState.motion = idleClip;

        AnimatorState walkState = sm.AddState("Walk", new Vector3(550, 0, 0));
        walkState.motion = walkClip;

        // Set Idle as default.
        sm.defaultState = idleState;

        // Idle → Walk : Speed > WalkThreshold.
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration    = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, WalkThreshold, SpeedParam);

        // Walk → Idle : Speed < WalkThreshold.
        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration    = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, WalkThreshold, SpeedParam);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RobberController] Controller created at {path}");
        Selection.activeObject = controller;
    }

    /// <summary>Loads the first AnimationClip sub-asset from an FBX file.</summary>
    private static AnimationClip LoadFirstClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }
}
