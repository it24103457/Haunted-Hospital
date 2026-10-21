#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// One-shot editor tool: patches AC_Hunter.controller (adds IsInactive param, Inactive and Run states,
/// and their transitions) and updates PF_Hunter.prefab NavMeshAgent/CapsuleCollider values.
/// Run via: Haunted Hospital > Patch Hunter Speed Fix
/// Safe to run multiple times (idempotent).
/// </summary>
public static class HunterSpeedFixPatcher
{
    [MenuItem("Haunted Hospital/Patch Hunter Speed Fix")]
    public static void Run()
    {
        PatchController();
        PatchPrefab();
        Debug.Log("[HunterSpeedFix] All patches applied successfully.");
    }

    // 
    // Animator Controller
    // 
    private static void PatchController()
    {
        const string ctrlPath = "Assets/Agents/Hunter/Animations/AC_Hunter.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null) { Debug.LogError("[HunterSpeedFix] AC_Hunter.controller not found at " + ctrlPath); return; }

        // 1. Add IsInactive bool parameter if missing
        bool hasIsInactive = false;
        foreach (var p in ctrl.parameters)
            if (p.name == "IsInactive") { hasIsInactive = true; break; }
        if (!hasIsInactive)
        {
            ctrl.AddParameter("IsInactive", AnimatorControllerParameterType.Bool);
            Debug.Log("[HunterSpeedFix] Added IsInactive parameter.");
        }

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // 2. Load clips
        AnimationClip runClip      = null;
        AnimationClip inactiveClip = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Agents/Hunter/Animations/Zombie Run.fbx"))
            if (obj is AnimationClip c && c.name == "Hunter_Run")      { runClip = c;      break; }
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Agents/Hunter/Animations/Zombie Idle.fbx"))
            if (obj is AnimationClip c && c.name == "Hunter_Inactive") { inactiveClip = c; break; }

        if (runClip == null)      { Debug.LogError("[HunterSpeedFix] Hunter_Run clip not found."); return; }
        if (inactiveClip == null) { Debug.LogError("[HunterSpeedFix] Hunter_Inactive clip not found."); return; }

        // 3. Find or create states
        AnimatorState idleState     = FindState(sm, "Idle");
        AnimatorState walkState     = FindState(sm, "Walk");
        AnimatorState inactiveState = FindState(sm, "Inactive");
        AnimatorState runState      = FindState(sm, "Run");
        AnimatorState attackState   = FindState(sm, "Attack");
        AnimatorState flinchState   = FindState(sm, "Flinch");
        AnimatorState doorPushState = FindState(sm, "DoorPush");

        if (inactiveState == null)
        {
            inactiveState = sm.AddState("Inactive", new Vector3(-120f, 80f, 0f));
            Debug.Log("[HunterSpeedFix] Created Inactive state.");
        }
        inactiveState.motion = inactiveClip;
        sm.defaultState = inactiveState;

        if (runState == null)
        {
            runState = sm.AddState("Run", new Vector3(380f, 80f, 0f));
            Debug.Log("[HunterSpeedFix] Created Run state.");
        }
        runState.motion = runClip;

        // 4. Wire and fix transitions
        // Inactive transitions
        if (inactiveState != null)
        {
            if (idleState != null)
            {
                AddBoolTransition(inactiveState, idleState, "IsInactive", false, 0.1f);
                AddBoolTransition(idleState, inactiveState, "IsInactive", true,  0.1f);
            }
            if (runState != null)
            {
                // Direct break into run if awakened already chasing and moving
                AddChaseAndSpeedTransition(inactiveState, runState, true, true, 0.1f);
            }
        }

        // Idle transitions: Fix Idle -> Walk to require IsChasing == false so Chase never routes through Walk
        if (idleState != null)
        {
            if (walkState != null)
            {
                FixIdleToWalkTransition(idleState, walkState);
            }
            if (runState != null)
            {
                AddChaseAndSpeedTransition(idleState, runState, true, true, 0.1f);
            }
        }

        // Walk transitions
        if (walkState != null)
        {
            if (runState != null)
            {
                AddBoolTransition(walkState, runState, "IsChasing", true, 0.15f);
                // Fix Run -> Walk: when no longer chasing and still moving
                FixRunToWalkTransition(runState, walkState);
            }
            if (idleState != null && runState != null)
            {
                AddSpeedTransition(runState, idleState, 0.1f, false, 0.2f);
            }
        }

        // Action states: Attack, Flinch, DoorPush -> Run/Walk breaks (prevents sliding!)
        if (runState != null)
        {
            if (attackState != null)
            {
                AddChaseAndSpeedTransition(attackState, runState, true, true, 0.15f);
                if (walkState != null) AddChaseAndSpeedTransition(attackState, walkState, false, true, 0.2f);
                Debug.Log("[HunterSpeedFix] Wired Attack -> Run/Walk transitions.");
            }
            if (flinchState != null)
            {
                AddChaseAndSpeedTransition(flinchState, runState, true, true, 0.15f);
                if (walkState != null) AddChaseAndSpeedTransition(flinchState, walkState, false, true, 0.2f);
                Debug.Log("[HunterSpeedFix] Wired Flinch -> Run/Walk transitions.");
            }
            if (doorPushState != null)
            {
                AddChaseAndSpeedTransition(doorPushState, runState, true, true, 0.15f);
                if (walkState != null) AddChaseAndSpeedTransition(doorPushState, walkState, false, true, 0.2f);
                Debug.Log("[HunterSpeedFix] Wired DoorPush -> Run/Walk transitions.");
            }
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[HunterSpeedFix] AC_Hunter.controller patched.");
    }

    // 
    // Prefab
    // 
    private static void PatchPrefab()
    {
        const string prefabPath = "Assets/Agents/Hunter/Prefabs/PF_Hunter.prefab";
        var go = PrefabUtility.LoadPrefabContents(prefabPath);
        if (go == null) { Debug.LogError("[HunterSpeedFix] PF_Hunter.prefab not found."); return; }

        var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed        = 1.4f;
            agent.angularSpeed = 360f;
            agent.acceleration = 16f;
            Debug.Log($"[HunterSpeedFix] NavMeshAgent: speed={agent.speed} angularSpeed={agent.angularSpeed} acceleration={agent.acceleration}");
        }

        var col = go.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.radius = 0.28f;
            Debug.Log($"[HunterSpeedFix] CapsuleCollider radius={col.radius}");
        }

        var animDriver = go.GetComponent<HunterAnimationDriver>();
        if (animDriver != null)
        {
            var so = new SerializedObject(animDriver);
            var walkProp = so.FindProperty("walkAuthoredSpeed");
            var runProp  = so.FindProperty("runAuthoredSpeed");
            if (walkProp != null) walkProp.floatValue = 1.15f;
            if (runProp  != null) runProp.floatValue  = 4.25f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[HunterSpeedFix] HunterAnimationDriver authored speeds updated to walk=1.15, run=4.25.");
        }

        var audioDriver = go.GetComponent<HunterAudioDriver>();
        if (audioDriver != null)
        {
            var so = new SerializedObject(audioDriver);
            var run1  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Models/Agents/Hunter/Audio/Hunter_Step_Run1.wav");
            var run2  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Models/Agents/Hunter/Audio/Hunter_Step_Run2.wav");
            var walk1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Models/Agents/Hunter/Audio/Hunter_Step_Walk1.wav");
            var walk2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Models/Agents/Hunter/Audio/Hunter_Step_Walk2.wav");
            var growl = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Models/Agents/Hunter/Audio/zombie growling.mp3");

            var runClipsProp = so.FindProperty("runStepClips");
            if (runClipsProp != null)
            {
                runClipsProp.arraySize = 2;
                runClipsProp.GetArrayElementAtIndex(0).objectReferenceValue = run1;
                runClipsProp.GetArrayElementAtIndex(1).objectReferenceValue = run2;
            }

            var walkClipsProp = so.FindProperty("walkStepClips");
            if (walkClipsProp != null)
            {
                walkClipsProp.arraySize = 2;
                walkClipsProp.GetArrayElementAtIndex(0).objectReferenceValue = walk1;
                walkClipsProp.GetArrayElementAtIndex(1).objectReferenceValue = walk2;
            }

            var growlProp = so.FindProperty("growlClip");
            if (growlProp != null) growlProp.objectReferenceValue = growl;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[HunterSpeedFix] HunterAudioDriver footstep clips wired.");
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        PrefabUtility.UnloadPrefabContents(go);
        Debug.Log("[HunterSpeedFix] PF_Hunter.prefab patched.");
    }

    // Helpers
    
    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var cs in sm.states)
            if (cs.state.name == name) return cs.state;
        return null;
    }

    private static bool HasTransitionTo(AnimatorState from, AnimatorState to)
    {
        foreach (var t in from.transitions)
            if (t.destinationState == to) return true;
        return false;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, float duration)
    {
        if (HasTransitionTo(from, to)) return;
        var t = from.AddTransition(to);
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        t.hasExitTime = false;
        t.duration = duration;
    }

    private static void AddChaseAndSpeedTransition(AnimatorState from, AnimatorState to, bool isChasing, bool speedGreater, float duration)
    {
        if (HasTransitionTo(from, to)) return;
        var t = from.AddTransition(to);
        t.AddCondition(isChasing ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
        t.AddCondition(speedGreater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, 0.1f, "Speed");
        t.hasExitTime = false;
        t.duration = duration;
    }

    private static void FixIdleToWalkTransition(AnimatorState idle, AnimatorState walk)
    {
        foreach (var t in idle.transitions)
        {
            if (t.destinationState == walk)
            {
                // Ensure IsChasing == false is present
                bool hasChaseCondition = false;
                foreach (var c in t.conditions)
                {
                    if (c.parameter == "IsChasing") { hasChaseCondition = true; break; }
                }
                if (!hasChaseCondition)
                {
                    t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChasing");
                    Debug.Log("[HunterSpeedFix] Added IsChasing == false condition to Idle -> Walk transition.");
                }
                return;
            }
        }
    }

    private static void FixRunToWalkTransition(AnimatorState run, AnimatorState walk)
    {
        // Remove any old run->walk transitions that incorrectly checked Speed < 0.1
        for (int i = run.transitions.Length - 1; i >= 0; i--)
        {
            var t = run.transitions[i];
            if (t.destinationState == walk)
            {
                run.RemoveTransition(t);
            }
        }

        // Add correct transition: IsChasing == false AND Speed > 0.1
        var newT = run.AddTransition(walk);
        newT.AddCondition(AnimatorConditionMode.IfNot, 0, "IsChasing");
        newT.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        newT.hasExitTime = false;
        newT.duration = 0.2f;
        Debug.Log("[HunterSpeedFix] Fixed Run -> Walk transition (IsChasing == false && Speed > 0.1).");
    }

    private static void AddSpeedTransition(AnimatorState from, AnimatorState to, float threshold, bool greater, float duration)
    {
        if (HasTransitionTo(from, to)) return;
        var t = from.AddTransition(to);
        t.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, threshold, "Speed");
        t.hasExitTime = false;
        t.duration = duration;
    }
}

#endif

