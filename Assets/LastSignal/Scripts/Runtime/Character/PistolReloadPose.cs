using UnityEngine;

namespace LastSignal
{
    /// <summary>Cosmetic sampling of authoritative reload time. Never transfers ammunition.</summary>
    public static class PistolReloadPose
    {
        public static void Sample(WeaponRuntimeState state, out float assist, out float extraction)
        {
            assist = extraction = 0;
            if (state == null || state.State != WeaponState.Reloading) return;
            float duration = state.IsEmptyReload ? state.Definition.EmptyReloadSeconds : state.Definition.TacticalReloadSeconds;
            float commit = state.IsEmptyReload ? state.Definition.EmptyReloadCommitNormalized : state.Definition.ReloadCommitNormalized;
            float t = Mathf.Clamp01(state.StateTimer / Mathf.Max(.001f, duration));
            // Reach, extract, insert at commit, then release. Hold contact if commit is delayed.
            float before = Mathf.Clamp01(t / Mathf.Max(.001f, commit));
            assist = Mathf.SmoothStep(0, 1, before / .3f);
            extraction = before < .6f ? Mathf.SmoothStep(0, 1, (before - .3f) / .3f) :
                1 - Mathf.SmoothStep(0, 1, (before - .6f) / .4f);
            if (state.ReloadCommitted)
            {
                extraction = 0;
                assist = 1 - Mathf.SmoothStep(0, 1, (t - commit) / Mathf.Max(.001f, 1 - commit));
            }
        }
    }
}
