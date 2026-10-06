using UnityEditor;
using UnityEngine;

/// <summary>
/// MR POLY – Welcome & Resources window
/// Professional intro popup for Unity Asset Store packages
/// </summary>
public class MRPolyInfoWindow : EditorWindow
{
    private const string PrefKey = "MRPOLY_BRAND_InfoWindow_Shown";

    // --- LOGO (unchanged – working version) ---
    private const string LogoResourceName = "mrpoly_logo";
    private const string LogoAssetPath = "Assets/Editor/mrpoly_logo.png";
    private static Texture2D logo;

    // ------ UI TUNABLES ------
    private const float uiScale = 0.72f;
    private const float widthShrink = 1f;
    private const float heightBoost = 1.15f;
    private const bool preserveAspect = false;
    // ------------------------

    private bool dontShowAgain;

    // -------- LINKS --------
    private static readonly string discordUrl = "https://discord.gg/wfZeU5bzTf";
    private static readonly string docsUrl = "https://sites.google.com/view/mrpoly/Documentation";
    private static readonly string supportEmail = "MrPolyStudio@gmail.com";
    private static readonly string version = "1.0.0";
    // -----------------------

    private static readonly string brandBlurb =
        "MR POLY provides professional, game-ready 3D assets for Unity developers. " +
        "From stylized low-poly environments to detailed realistic models, each asset is built with clean topology, " +
        "optimized performance, and production-ready materials — ready to drop into your project.";

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            if (HasOpenInstances<MRPolyInfoWindow>()) return;

            ShowWindow();
        };
    }

    [MenuItem("Tools/MR POLY/About & Resources")]
    public static void ShowWindow()
    {
        MRPolyInfoWindow window =
            GetWindow<MRPolyInfoWindow>(true, "MR POLY – Welcome & Resources");

        window.minSize = new Vector2(420, 360);
        window.maxSize = new Vector2(1000, 800);
        window.LoadResources();
    }

    private void OnEnable()
    {
        dontShowAgain = false;
        LoadResources();
    }

    // -------- LOGO LOADING (UNCHANGED) --------
    private void LoadResources()
    {
        if (logo != null) return;

        logo = Resources.Load<Texture2D>(LogoResourceName);

#if UNITY_EDITOR
        if (logo == null)
            logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoAssetPath);

        if (logo == null)
            logo = EditorGUIUtility.Load("mrpoly_logo.png") as Texture2D;
#endif
    }

    private void OnGUI()
    {
        GUILayout.Space(12);

        // Header
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15
        };

        GUILayout.Label("MR POLY", titleStyle);
        GUILayout.Label("Professional 3D Assets for Unity", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label($"Version {version}", EditorStyles.centeredGreyMiniLabel);

        GUILayout.Space(10);

        // Logo
        if (logo != null)
        {
            float ppp = EditorGUIUtility.pixelsPerPoint;
            float texWidth = (logo.width / ppp) * uiScale;
            float aspect = logo.width / (float)logo.height;

            float drawWidth = Mathf.Min(texWidth * widthShrink, (position.width - 40f) * 0.6f);
            float drawHeight = (drawWidth / aspect) * heightBoost;

            Rect rect = GUILayoutUtility.GetRect(drawWidth, drawHeight, GUILayout.ExpandWidth(false));
            rect.x = (position.width - rect.width) * 0.5f;

            GUI.DrawTexture(
                rect,
                logo,
                preserveAspect ? ScaleMode.ScaleToFit : ScaleMode.StretchToFill
            );

            if (Event.current.type == EventType.MouseUp && rect.Contains(Event.current.mousePosition))
                Application.OpenURL(docsUrl);

            GUILayout.Space(8);
        }

        // Body
        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUILayout.BeginVertical();

        GUILayout.Label("About This Package", EditorStyles.boldLabel);
        GUILayout.Label(
            brandBlurb + "\n\n" +
            "• Actively maintained with updates and support\n" +
            "• Clear documentation and quick-start guidance\n" +
            "• Performance-optimized for real-time Unity projects",
            EditorStyles.wordWrappedLabel
        );

        GUILayout.EndVertical();
        GUILayout.Space(12);
        GUILayout.EndHorizontal();

        GUILayout.Space(14);

        // Actions
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("View Documentation", GUILayout.Height(28)))
            Application.OpenURL(docsUrl);

        if (GUILayout.Button("Join Community", GUILayout.Height(28)))
            Application.OpenURL(discordUrl);

        if (GUILayout.Button("Contact Support", GUILayout.Height(28)))
        {
            EditorUtility.DisplayDialog(
                "MR POLY Support",
                $"Email:\n{supportEmail}",
                "OK"
            );

            try
            {
                Application.OpenURL("mailto:" + supportEmail);
            }
            catch
            {
                Debug.LogWarning("Unable to open email client. Please copy the email manually: " + supportEmail);
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        dontShowAgain = EditorGUILayout.ToggleLeft(
            "Don't show this window at startup",
            dontShowAgain
        );

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Close", GUILayout.Height(26), GUILayout.Width(90)))
        {
            if (dontShowAgain)
                EditorPrefs.SetBool(PrefKey, true);

            Close();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(8);
    }
}
