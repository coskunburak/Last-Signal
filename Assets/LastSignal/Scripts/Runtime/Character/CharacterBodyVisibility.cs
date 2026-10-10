using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LastSignal
{
    /// <summary>Hide a body only from its owner's FPS cameras, never from other observers.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterBodyVisibility : MonoBehaviour
    {
        [SerializeField] bool localOwner = true;
        Renderer[] renderers;
        ShadowCastingMode[] originals;
        Renderer[] viewmodels;
        bool[] originalForceOff;
        Camera ownerView;
        PlayerCombatController combat;
        readonly List<Camera> renderingCameras = new List<Camera>(2);
        bool inspecting;
        public bool LocalOwner => localOwner;
        public bool Inspecting => inspecting;
        void Awake()
        {
            var look = GetComponentInParent<FirstPersonLook>();
            ownerView = look ? look.View : null;
            combat = GetComponentInParent<PlayerCombatController>();
        }
        void OnEnable()
        {
            RefreshRenderers();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
            if (combat) combat.WeaponChanged += RefreshRenderers;
        }
        // Combat creates both viewmodels in Start; include the initially inactive crowbar too.
        void Start() => RefreshRenderers();
        public void SetLocalOwner(bool value) { localOwner = value; Apply(); }
        public void SetInspection(bool value) { inspecting = value; Apply(); }
        public void RefreshRenderers()
        {
            Restore();
            renderers = GetComponentsInChildren<Renderer>(true);
            originals = new ShadowCastingMode[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originals[i] = renderers[i].shadowCastingMode;
            viewmodels = ownerView ? ownerView.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();
            originalForceOff = new bool[viewmodels.Length];
            for (int i = 0; i < viewmodels.Length; i++) originalForceOff[i] = viewmodels[i].forceRenderingOff;
            Apply();
        }
        void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            renderingCameras.Add(camera);
            Apply();
        }
        void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            int index = renderingCameras.LastIndexOf(camera);
            if (index >= 0) renderingCameras.RemoveAt(index);
            Apply();
        }
        void Apply()
        {
            if (!isActiveAndEnabled) { Restore(); return; }
            var camera = renderingCameras.Count > 0 ? renderingCameras[renderingCameras.Count - 1] : null;
            // Scope cameras live below the owner's View. Inspection/Scene/other-player cameras do not.
            bool ownerCamera = localOwner && ownerView && camera &&
                (camera == ownerView || camera.transform.IsChildOf(ownerView.transform));
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++) if (renderers[i])
                    renderers[i].shadowCastingMode = ownerCamera ? ShadowCastingMode.ShadowsOnly : originals[i];
            // Only the primary owner view draws FPS arms. Scope, Scene and other players see the world rig.
            bool showViewmodel = localOwner && camera && camera == ownerView;
            if (viewmodels != null)
                for (int i = 0; i < viewmodels.Length; i++) if (viewmodels[i])
                    viewmodels[i].forceRenderingOff = originalForceOff[i] || !showViewmodel;
        }
        void Restore()
        {
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++) if (renderers[i]) renderers[i].shadowCastingMode = originals[i];
            if (viewmodels != null)
                for (int i = 0; i < viewmodels.Length; i++) if (viewmodels[i]) viewmodels[i].forceRenderingOff = originalForceOff[i];
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            if (!ReferenceEquals(combat, null)) combat.WeaponChanged -= RefreshRenderers;
            renderingCameras.Clear();
            inspecting = false;
            Restore();
        }
    }
}
