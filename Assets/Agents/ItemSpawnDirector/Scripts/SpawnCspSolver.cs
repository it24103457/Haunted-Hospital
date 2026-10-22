using System.Collections.Generic;
using UnityEngine;

/*
 * Constraint solver for the ItemSpawnDirector's initial layout.
 *
 * The problem: assign each planned item to a marker so no two items share one, everyone stays
 * in its pool (barricaded or ordinary markers) and the minimum-distance rule holds. The search
 * is the textbook combination: MRV (a variable gets the fewest values it has left; ties break
 * randomly so two runs differ), forward checking (an assignment that empties any remaining
 * variable's domain is rejected immediately) and backtracking (undo and try the next candidate).
 *
 * Pure and static: no scene knowledge, no spawning - the director builds the variables (with
 * their domains) and consumes the assignments afterwards. Domains are copied on construction
 * and never mutated by the solver, so a caller can reuse or rebuild variables freely.
 */
public static class SpawnCspSolver
{
    /// One item awaiting placement: an id for logs and its legal markers.
    public class Variable
    {
        public string Id { get; }
        public List<Transform> Domain { get; }
        public Transform Assignment { get; internal set; }

        public Variable(string id, params Transform[] domain)
            : this(id, domain == null ? null : new List<Transform>(domain)) { }

        public Variable(string id, List<Transform> domain)
        {
            Id = id;
            Domain = domain != null ? new List<Transform>(domain) : new List<Transform>();
        }
    }

    /// How hard the search had to work (for the director's start summary).
    public struct Stats
    {
        public int Backtracks;
    }

    /// Assigns every variable, or reports failure. Assignments are cleared first, so a failed
    /// solve leaves every variable with Assignment == null.
    public static bool Solve(List<Variable> variables, out Stats stats)
    {
        stats = new Stats();
        if (variables == null || variables.Count == 0) return true;

        foreach (Variable v in variables) v.Assignment = null;
        return SolveRecursive(variables, ref stats);
    }

    private static bool SolveRecursive(List<Variable> variables, ref Stats stats)
    {
        // MRV: the unassigned variable with the fewest legal values goes first;
        // if any unassigned variable already has none, this branch is dead.
        Variable next = null;
        int fewest = int.MaxValue;
        foreach (Variable v in variables)
        {
            if (v.Assignment != null) continue;
            int legal = CountLegal(v, variables);
            if (legal == 0) return false;
            if (legal < fewest || (legal == fewest && Random.value < 0.5f))
            {
                next = v;
                fewest = legal;
            }
        }
        if (next == null) return true; // everyone is assigned

        foreach (Transform candidate in Shuffled(next.Domain))
        {
            if (IsTakenByAnother(candidate, next, variables)) continue;

            next.Assignment = candidate;

            // Forward checking: keep going only while every remaining variable still has a value.
            if (DomainsStillFeasible(variables) && SolveRecursive(variables, ref stats)) return true;

            next.Assignment = null;
            stats.Backtracks++;
        }
        return false;
    }

    // How many of v's values are still unclaimed by other assignments.
    private static int CountLegal(Variable v, List<Variable> variables)
    {
        int count = 0;
        foreach (Transform candidate in v.Domain)
            if (!IsTakenByAnother(candidate, v, variables)) count++;
        return count;
    }

    private static bool IsTakenByAnother(Transform candidate, Variable self, List<Variable> variables)
    {
        foreach (Variable other in variables)
            if (!ReferenceEquals(other, self) && other.Assignment == candidate) return true;
        return false;
    }

    // Forward check: after the current assignment, every unassigned variable keeps at least one value.
    private static bool DomainsStillFeasible(List<Variable> variables)
    {
        foreach (Variable v in variables)
            if (v.Assignment == null && CountLegal(v, variables) == 0) return false;
        return true;
    }

    private static List<Transform> Shuffled(List<Transform> domain)
    {
        var copy = new List<Transform>(domain);
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy;
    }
}
