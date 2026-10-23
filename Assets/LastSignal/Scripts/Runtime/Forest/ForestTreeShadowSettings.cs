using LastSignal.RenderingQuality;
using ShadowQuality = LastSignal.RenderingQuality.ShadowQuality;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastSignal.Forest
{
    /// <summary>Forest scene integration; production settings are owned by the shared graphics manager.</summary>
    public sealed class ForestTreeShadowSettings : MonoBehaviour
    {
        ForestPrototypeExplorer explorer;
        GameObject canvas, panel, ownedEventSystem;
        public bool TreeShadowsEnabled => GraphicsSettingsManager.Instance && GraphicsSettingsManager.Instance.State.treeShadows != ShadowQuality.Off;
        public static bool DiagnosticOverrideActive
        {
            get => GraphicsSettingsManager.Instance && GraphicsSettingsManager.Instance.DiagnosticActive;
            set { if (GraphicsSettingsManager.Instance) GraphicsSettingsManager.Instance.DiagnosticActive = value; }
        }
        public void Initialize(ForestPrototypeExplorer owner)
        {
            if (explorer || !owner || !owner.state) return;
            explorer = owner;
            var manager = GraphicsSettingsManager.Ensure();
            var state = owner.state;
            if (state.treeShadowRoots != null)
                foreach (var root in state.treeShadowRoots)
                {
                    if (!root) continue;
                    // Explicit authoring roots, one registration per tree. Ground apron has no LODGroup.
                    foreach (Transform tree in root)
                        if (tree.GetComponentInChildren<LODGroup>(true)) VegetationShadowController.Attach(tree.gameObject);
                }
            else Debug.LogWarning("Forest tree shadow roots are not authored; tree quality cannot control unregistered trees.");
            if (state.gameplayCamera) manager.RegisterRoot(state.gameplayCamera.gameObject);
            manager.Reapply();
            if (!EventSystem.current)
            {
                ownedEventSystem = new GameObject("Forest UI events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                ownedEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            canvas = new GameObject("Forest graphics", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 60;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            panel = SessionUi.Panel("Grafik", canvas.transform, Vector2.zero, new Vector2(1440, 780));
            panel.AddComponent<GraphicsSettingsPresenter>().Initialize(manager);
            SessionUi.Button("Oyuna dön (Escape)", panel.transform, new Vector2(0, -350), Resume);
            panel.SetActive(false);
        }
        void Resume()
        {
            explorer.input.SetGameplay(true); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }
        void Update()
        {
            if (!panel || !explorer || !explorer.input) return;
            bool open = !explorer.input.GameplayActive;
            if (panel.activeSelf == open) return;
            panel.SetActive(open);
            if (open && EventSystem.current) EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
        }
        public void SetTreeShadows(bool enabled)
        {
            var manager = GraphicsSettingsManager.Ensure(); var state = manager.State;
            state.treeShadows = enabled ? ShadowQuality.High : ShadowQuality.Off; state.preset = GraphicsPreset.Custom;
            manager.Apply(state);
        }
        void OnDestroy() { if (canvas) Destroy(canvas); if (ownedEventSystem) Destroy(ownedEventSystem); }
    }
}
