using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastSignal
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class ScopeOpticPresenter : MonoBehaviour
    {
        [SerializeField] WeaponController weapon;
        [SerializeField] ScopeOpticDefinition definition;
        [SerializeField] MeshRenderer lens;
        [SerializeField] Vector3 lensCenter;
        [SerializeField] Vector2 lensHalfSize;

        FirstPersonLook look;
        FirstPersonMotor motor;
        PlayerHealth health;
        Camera scopeCamera;
        RenderTexture target;
        MaterialPropertyBlock properties;
        float visibility;
        bool allocationFailed;

        static readonly int TextureId = Shader.PropertyToID("_ScopeTex");
        static readonly int VisibilityId = Shader.PropertyToID("_Visibility");
        static readonly int CenterId = Shader.PropertyToID("_LensCenter");
        static readonly int HalfSizeId = Shader.PropertyToID("_LensHalfSize");
        static readonly int PupilId = Shader.PropertyToID("_Pupil");
        static readonly int ReticleId = Shader.PropertyToID("_ReticleColor");
        static readonly int ShapeId = Shader.PropertyToID("_ReticleShape");

        public ScopeOpticDefinition Definition => definition;
        public MeshRenderer Lens => lens;
        public Camera ScopeCamera => scopeCamera;
        public RenderTexture Target => target;
        public float Visibility => visibility;
        public bool HasAllocatedResources => scopeCamera && target;

        public void Configure(WeaponController controller, ScopeOpticDefinition profile,
            MeshRenderer lensRenderer, Vector3 center, Vector2 halfSize)
        {
            ReleaseResources();
            weapon = controller;
            definition = profile;
            lens = lensRenderer;
            lensCenter = center;
            lensHalfSize = halfSize;
            allocationFailed = false;
            BindPlayer();
        }

        void OnEnable()
        {
            allocationFailed = false;
            visibility = 0;
            properties ??= new MaterialPropertyBlock();
            BindPlayer();
            SetDark();
        }

        void OnTransformParentChanged() => BindPlayer();

        void BindPlayer()
        {
            look = GetComponentInParent<FirstPersonLook>();
            motor = GetComponentInParent<FirstPersonMotor>();
            health = GetComponentInParent<PlayerHealth>();
        }

        void LateUpdate()
        {
            if (!weapon || !definition || !lens || !weapon.AimReference ||
                lensHalfSize.x <= 0 || lensHalfSize.y <= 0)
            {
                SetDark();
                return;
            }
            if (!look) BindPlayer();
            var view = look ? look.View : null;
            var state = weapon.RuntimeState;
            bool allowed = view && view.isActiveAndEnabled && weapon.isActiveAndEnabled &&
                state != null && state.State == WeaponState.Ready &&
                (!motor || (!motor.IsSprinting && !motor.IsSliding)) &&
                (!health || health.IsAlive);

            float aim = allowed ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, .95f, state.AimAmount)) : 0;
            visibility = Mathf.MoveTowards(visibility, aim, Time.deltaTime / definition.FadeSeconds);
            if (!view || visibility <= .0001f)
            {
                SetDark();
                return;
            }

            var axis = weapon.AimReference;
            Vector3 centerWS = lens.transform.TransformPoint(lensCenter);
            Vector3 eye = lens.transform.InverseTransformPoint(view.transform.position) - lensCenter;
            float distance = Vector3.Dot(centerWS - view.transform.position, axis.forward);
            float radial = new Vector2(eye.x, eye.y).magnitude / definition.EyeBoxRadius;
            float distanceWeight = Mathf.SmoothStep(0, 1,
                Mathf.InverseLerp(definition.MinimumEyeDistance * .8f, definition.MinimumEyeDistance, distance));
            distanceWeight *= 1 - Mathf.SmoothStep(0, 1,
                Mathf.InverseLerp(definition.MaximumEyeDistance, definition.MaximumEyeDistance * 1.2f, distance));
            float eyeWeight = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.4f, 1.4f, radial));
            float visible = visibility * distanceWeight * eyeWeight;
            if (visible <= .0001f || distance <= 0)
            {
                if (scopeCamera) scopeCamera.enabled = false;
                WriteProperties(0, Vector2.zero);
                return;
            }
            if (!EnsureResources(view)) { SetDark(); return; }

            // Kamera gözden bakar; mermi/hasar çözümlemesine veri yazmaz.
            scopeCamera.transform.SetPositionAndRotation(view.transform.position, axis.rotation);
            scopeCamera.nearClipPlane = view.nearClipPlane;
            scopeCamera.farClipPlane = view.farClipPlane;
            scopeCamera.cullingMask = view.cullingMask & ~(1 << definition.ViewmodelLayer);
            scopeCamera.clearFlags = view.clearFlags;
            scopeCamera.backgroundColor = view.backgroundColor;
            scopeCamera.depth = view.depth - 1;
            float halfHeightWS = lens.transform.TransformVector(Vector3.up * lensHalfSize.y).magnitude;
            float halfWidthWS = lens.transform.TransformVector(Vector3.right * lensHalfSize.x).magnitude;
            scopeCamera.aspect = halfWidthWS / halfHeightWS;
            scopeCamera.fieldOfView = ScopeOpticDefinition.CalculateVerticalFov(
                halfHeightWS, distance, definition.Magnification);
            scopeCamera.enabled = true;
            WriteProperties(visible, new Vector2(eye.x, eye.y) / definition.EyeBoxRadius);
        }

        bool EnsureResources(Camera view)
        {
            if (HasAllocatedResources)
            {
                if (target.IsCreated() || target.Create()) return true;
                allocationFailed = true;
                ReleaseResources();
                Debug.LogError("Dürbün RenderTexture yeniden oluşturulamadı.", this);
                return false;
            }
            if (allocationFailed) return false;
            properties ??= new MaterialPropertyBlock();
            int size = definition.TextureSize;
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            target = new RenderTexture(size, size, 24, format, RenderTextureReadWrite.Linear)
            {
                name = "LastSignal.ScopeRT",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            if (!target.Create())
            {
                allocationFailed = true;
                ReleaseResources();
                Debug.LogError("Dürbün RenderTexture oluşturulamadı.", this);
                return false;
            }
            var go = new GameObject("LastSignal.ScopeCamera") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            scopeCamera = go.AddComponent<Camera>();
            scopeCamera.enabled = false;
            scopeCamera.targetTexture = target;
            scopeCamera.allowHDR = format == RenderTextureFormat.ARGBHalf;
            scopeCamera.allowMSAA = false;
            scopeCamera.useOcclusionCulling = view.useOcclusionCulling;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderType = CameraRenderType.Base;
            data.SetRenderer(definition.RendererIndex);
            data.renderShadows = true;
            // Ana kamera mercek görüntüsüne post-process uygular; ikinci tonemapping yapılmaz.
            data.renderPostProcessing = false;
            data.volumeLayerMask = 0;
            return true;
        }

        void WriteProperties(float amount, Vector2 pupil)
        {
            if (!lens || !definition) return;
            properties ??= new MaterialPropertyBlock();
            properties.Clear();
            properties.SetTexture(TextureId, target ? target : Texture2D.blackTexture);
            properties.SetFloat(VisibilityId, amount);
            properties.SetVector(CenterId, lensCenter);
            properties.SetVector(HalfSizeId, new Vector4(lensHalfSize.x, lensHalfSize.y, 0, 0));
            properties.SetVector(PupilId, new Vector4(pupil.x, pupil.y, 0, 0));
            properties.SetColor(ReticleId, definition.ReticleColor);
            properties.SetVector(ShapeId, new Vector4(definition.DotRadius, definition.RingRadius,
                definition.RingWidth, 0));
            lens.SetPropertyBlock(properties);
        }

        void SetDark()
        {
            visibility = 0;
            if (scopeCamera) scopeCamera.enabled = false;
            WriteProperties(0, Vector2.zero);
        }

        void OnDisable() => ReleaseResources();
        void OnDestroy() => ReleaseResources();

        void ReleaseResources()
        {
            visibility = 0;
            if (lens) lens.SetPropertyBlock(null);
            if (scopeCamera)
            {
                scopeCamera.enabled = false;
                scopeCamera.targetTexture = null;
                DestroyOwned(scopeCamera.gameObject);
                scopeCamera = null;
            }
            if (target)
            {
                target.Release();
                DestroyOwned(target);
                target = null;
            }
        }

        static void DestroyOwned(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
