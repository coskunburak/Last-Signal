#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastSignal.Tests
{
    public sealed class S016ReadabilityTests
    {
        SessionFlow flow;

        [UnitySetUp] public IEnumerator OpenProduction()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = Object.FindFirstObjectByType<SessionFlow>();
            Assert.That(flow, Is.Not.Null);
            yield return null;
        }

        [UnityTearDown] public IEnumerator CloseProduction()
        {
            if (flow) flow.ReturnToMenu();
            yield return null;
            var production = SceneManager.GetSceneByPath("Assets/LastSignal/Scenes/Production/S013Cabin.unity");
            if (production.IsValid() && production.isLoaded)
            {
                var empty = SceneManager.CreateScene("S016 readability cleanup");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(production);
            }
            flow = null;
            Time.timeScale = 1;
        }

        [UnityTest] public IEnumerator ProductionLabelsUseNotoSansWithTurkishGlyphs()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Noto_Sans/static/NotoSans-Regular.ttf");
            Assert.That(font, Is.Not.Null);
            foreach (char glyph in "çğıİöşüÇĞIÖŞÜ")
                Assert.That(font.HasCharacter(glyph), Is.True, "Missing Turkish glyph: " + glyph);

            var production = SceneManager.GetSceneByPath("Assets/LastSignal/Scenes/Production/S013Cabin.unity");
            int labels = 0;
            foreach (var label in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (label.gameObject.scene != production) continue;
                labels++;
                Assert.That(label.font, Is.SameAs(font), label.name + " must use the production font.");
            }
            Assert.That(labels, Is.GreaterThanOrEqualTo(130));
            Assert.That(ProductionUiFont.Resolve(), Is.SameAs(font));
            yield return null;
        }
    }
}
#endif
