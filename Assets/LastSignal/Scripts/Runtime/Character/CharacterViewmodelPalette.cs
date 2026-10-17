using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Per-character shared asset remaps. Source viewmodel UVs and rigging stay intact.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterViewmodelPalette : MonoBehaviour
    {
        [Serializable] public struct Entry { public Material Source, Replacement; }
        [SerializeField] Entry[] entries;
        readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
        PlayerCombatController combat;
        Transform view;
        public void Configure(Entry[] values) => entries = values;
        void Awake()
        {
            combat = GetComponentInParent<PlayerCombatController>();
            var look = GetComponentInParent<FirstPersonLook>();
            view = look && look.View ? look.View.transform : null;
        }
        void OnEnable() { if (combat) combat.WeaponChanged += Refresh; Refresh(); }
        void Start() => Refresh(); // Combat creates its viewmodels in Start.
        public void Refresh()
        {
            Restore();
            if (!view || entries == null) return;
            foreach (var renderer in view.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                Material[] changed = null;
                for (int i = 0; i < materials.Length; i++)
                    foreach (var entry in entries)
                        if (entry.Source && entry.Replacement && materials[i] == entry.Source)
                        {
                            if (changed == null) changed = (Material[])materials.Clone();
                            changed[i] = entry.Replacement; break;
                        }
                if (changed == null) continue;
                originals.Add(renderer, materials); renderer.sharedMaterials = changed;
            }
        }
        void Restore()
        {
            foreach (var pair in originals) if (pair.Key) pair.Key.sharedMaterials = pair.Value;
            originals.Clear();
        }
        void OnDisable() { if (combat) combat.WeaponChanged -= Refresh; Restore(); }
    }
}
