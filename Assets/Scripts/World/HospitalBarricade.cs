using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shootable plank barricade. Put this on the ROOT of PF_WoodBarricade_Standard (the object that already has
/// the BoxCollider and the NavMeshObstacle). Everything is found automatically, nothing to assign.
///
/// How it works with the existing code:
///  - PlayerWeapon raycasts, finds the root BoxCollider and calls GetComponentInParent&lt;IDamageable&gt;(),
///    which finds this script. No change to PlayerWeapon is needed.
///  - Planks are removed one at a time (default: 2 hits of 50 damage each, so 8 hits for 4 planks).
///  - When the last plank breaks, the BoxCollider and the NavMeshObstacle are disabled. That makes
///    WaypointNode.IsBarricadeIntact false (so the Thief and Hunter see the route open),
///    stops the wall blocking player movement, and stops it blocking visibility raycasts.
///  - A barricade that is cleared stays cleared for the run.
/// </summary>
[DisallowMultipleComponent]
public class HospitalBarricade : MonoBehaviour, IDamageable
{
    [Header("Parts (found automatically if left empty)")]
    [Tooltip("Visible planks. Auto-filled with children whose name starts with 'plank'. Broken top to bottom.")]
    [SerializeField] private Transform[] planks;
    [Tooltip("The collider that blocks the opening. Auto-filled with the collider on this object.")]
    [SerializeField] private Collider passageBlocker;
    [Tooltip("The NavMeshObstacle that blocks the AI. Auto-filled with the one on this object.")]
    [SerializeField] private NavMeshObstacle obstacle;

    [Header("Damage")]
    [Tooltip("Damage one plank absorbs. The syringe gun does 50, so 100 means 2 hits per plank.")]
    [SerializeField, Min(1f)] private float plankHealth = 100f;

    [Header("Noise")]
    [Tooltip("Emit a BarricadeBreak noise for the Hunter when the barricade is fully cleared.")]
    [SerializeField] private bool emitNoiseWhenCleared = true;
    [SerializeField, Min(0f)] private float clearedLoudness = NoiseLevels.BarricadeBreak;

    [Header("Debug")]
    [SerializeField] private bool logEvents = false;

    private readonly List<Transform> remaining = new List<Transform>();
    private float currentPlankHealth;

    public bool IsCleared { get; private set; }
    public int PlankCount => planks != null ? planks.Length : 0;
    public int PlanksRemaining => remaining.Count;

    /// <summary>Raised once, after the passage is open and the collider and obstacle are disabled.</summary>
    public event Action<HospitalBarricade> Cleared;

    private void Reset() => AutoAssign();

    private void Awake()
    {
        AutoAssign();

        remaining.Clear();
        if (planks != null)
        {
            foreach (Transform p in planks)
                if (p != null) remaining.Add(p);
        }

        // Break from the top down.
        remaining.Sort((a, b) => b.position.y.CompareTo(a.position.y));
        currentPlankHealth = plankHealth;

        if (remaining.Count == 0)
            Debug.LogWarning($"HospitalBarricade '{name}': no planks found (children named 'plank...'). Any hit will clear it.", this);
        if (passageBlocker == null)
            Debug.LogWarning($"HospitalBarricade '{name}': no blocking collider found.", this);
    }

    private void AutoAssign()
    {
        if (passageBlocker == null) passageBlocker = GetComponent<Collider>();
        if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();

        if (planks == null || planks.Length == 0)
        {
            var found = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform) continue;
                if (!t.name.StartsWith("plank", StringComparison.OrdinalIgnoreCase)) continue;
                // Skip a plank nested inside another plank.
                if (t.parent != null && t.parent.name.StartsWith("plank", StringComparison.OrdinalIgnoreCase)) continue;
                found.Add(t);
            }
            planks = found.ToArray();
        }
    }

    // IDamageable
    public void TakeDamage(DamageInfo info)
    {
        if (IsCleared || info.Amount <= 0f || float.IsNaN(info.Amount) || float.IsInfinity(info.Amount)) return;

        currentPlankHealth -= info.Amount;
        if (currentPlankHealth > 0f) return;

        BreakNextPlank();
    }

    private void BreakNextPlank()
    {
        if (remaining.Count > 0)
        {
            Transform plank = remaining[0];
            remaining.RemoveAt(0);
            if (plank != null) plank.gameObject.SetActive(false);
            if (logEvents) Debug.Log($"[HospitalBarricade] '{name}': plank broken, {remaining.Count} left.", this);
        }

        currentPlankHealth = plankHealth;

        if (remaining.Count == 0) Clear();
    }

    private void Clear()
    {
        IsCleared = true;

        // Order matters little, but all three must happen: movement, AI and visibility all stop being blocked.
        if (passageBlocker != null) passageBlocker.enabled = false;
        if (obstacle != null) obstacle.enabled = false;

        if (emitNoiseWhenCleared)
            Noise.Emit(transform.position, clearedLoudness, NoiseType.BarricadeBreak, gameObject);

        if (logEvents) Debug.Log($"[HospitalBarricade] '{name}' cleared.", this);
        Cleared?.Invoke(this);
    }

    [ContextMenu("Debug: Break one plank")]
    private void DebugBreakOne() { if (!IsCleared) BreakNextPlank(); }
}
