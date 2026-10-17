using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    public enum DirectorFuzzyLevel { Low, Medium, High }

    [Serializable]
    public struct DirectorSugenoRule
    {
        public DirectorFuzzyLevel pressure;
        public DirectorFuzzyLevel vulnerability;
        [Tooltip("Low = Short, Medium = Medium, High = Long.")]
        public DirectorFuzzyLevel quietDuration;
        [Range(0f, 1f)] public float desiredTension;

        public string Id => $"P{(int)pressure}_V{(int)vulnerability}_Q{(int)quietDuration}";
    }

    [CreateAssetMenu(fileName = "DirectorSugenoRules_Hospital",
        menuName = "Haunted Hospital/Environment Director/Sugeno Rules")]
    public sealed class DirectorSugenoRules : ScriptableObject
    {
        [Tooltip("Diagnostic fallback only. A fallback result is invalid for normal planning.")]
        [SerializeField, Range(0f, 1f)] private float fallbackTension = 0.15f;
        [SerializeField] private List<DirectorSugenoRule> rules = CreateDefaults();
        public float FallbackTension => fallbackTension;
        public IReadOnlyList<DirectorSugenoRule> Rules => rules.AsReadOnly();

        private static List<DirectorSugenoRule> CreateDefaults()
        {
            // Pressure outermost, vulnerability next, quiet duration innermost.
            // More quiet raises the target; pressure and vulnerability lower it.
            float[] targets = {
                .25f, .50f, .70f, .20f, .35f, .45f, .10f, .15f, .20f,
                .20f, .35f, .45f, .15f, .25f, .30f, .08f, .10f, .12f,
                .10f, .15f, .20f, .08f, .10f, .12f, .05f, .06f, .08f
            };
            var result = new List<DirectorSugenoRule>(27);
            for (int p = 0; p < 3; p++)
                for (int v = 0; v < 3; v++)
                    for (int q = 0; q < 3; q++)
                        result.Add(new DirectorSugenoRule {
                            pressure = (DirectorFuzzyLevel)p,
                            vulnerability = (DirectorFuzzyLevel)v,
                            quietDuration = (DirectorFuzzyLevel)q,
                            desiredTension = targets[p * 9 + v * 3 + q]
                        });
            return result;
        }

        public bool TryValidate(out string error)
        {
            error = null;
            if (!Unit(fallbackTension)) { error = "Fallback tension must be finite and between 0 and 1."; return false; }
            if (rules == null || rules.Count != 27) { error = "Exactly 27 rules are required."; return false; }
            var seen = new bool[27];
            foreach (var rule in rules)
            {
                int p = (int)rule.pressure, v = (int)rule.vulnerability, q = (int)rule.quietDuration;
                if (p < 0 || p > 2 || v < 0 || v > 2 || q < 0 || q > 2 || !Unit(rule.desiredTension))
                { error = "Rule categories must be valid and outputs finite within 0–1."; return false; }
                int index = p * 9 + v * 3 + q;
                if (seen[index]) { error = "Duplicate rule combination: " + rule.Id; return false; }
                seen[index] = true;
            }
            return true;
        }

        private static bool Unit(float value) => DirectorFuzzyMembership.IsFinite(value) && value >= 0f && value <= 1f;

        [ContextMenu("Validate Sugeno Rules")]
        private void ValidateRules()
        {
            if (TryValidate(out var error)) Debug.Log("[Environment Director] All 27 rule combinations are covered.", this);
            else Debug.LogError("[Environment Director] " + error, this);
        }
    }
}
