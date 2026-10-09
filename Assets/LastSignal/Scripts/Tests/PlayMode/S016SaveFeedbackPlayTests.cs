#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class S016SaveFeedbackPlayTests
    {
        const string ScenePath = "Assets/LastSignal/Scenes/Production/S013Cabin.unity";
        SessionFlow flow;
        SaveSession saves;
        WorldTimeSaveControls controls;
        Button save, load;
        Text feedback;
        string directory, path;

        T Field<T>(string name) => (T)typeof(WorldTimeSaveControls)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controls);

        [UnitySetUp] public IEnumerator OpenProduction()
        {
            directory = Path.Combine(Path.GetTempPath(), "LastSignal-S016-feedback-" + Guid.NewGuid().ToString("N"));
            path = Path.Combine(directory, "current.json");
            Time.timeScale = 1;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>();
            saves = flow.GetComponent<SaveSession>();
            controls = Object.FindAnyObjectByType<WorldTimeSaveControls>();
            Assert.That(controls, Is.Not.Null);
            save = Field<Button>("saveButton");
            load = Field<Button>("loadButton");
            feedback = Field<Text>("result");
            // Keep authored controls/listeners; route actual disk transactions to a unique temporary slot.
            controls.enabled = false;
            controls.Configure(saves, save, load, feedback, path);
            controls.enabled = true;
            flow.Resume();
            yield return new WaitForSeconds(.9f);
        }

        [UnityTearDown] public IEnumerator CloseProduction()
        {
            if (controls) controls.enabled = false;
            if (flow) flow.ReturnToMenu();
            yield return null;
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("S016 save feedback cleanup"));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            Time.timeScale = 1;
            if (directory != null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        IEnumerator WaitForLoad()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Field<bool>("loading") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Field<bool>("loading"), Is.False, "Load did not finish within 15 seconds.");
            yield return null; // Observe Update's visibility policy after completion.
        }

        [UnityTest] public IEnumerator MissingAndCorruptFilesKeepMenuAndShowFailureWithoutChangingFile()
        {
            flow.ReturnToMenu();
            yield return null;
            Assert.That(load.gameObject.activeInHierarchy && load.interactable, Is.True);
            load.onClick.Invoke();
            yield return WaitForLoad();
            Assert.That(saves.LastResult.Error, Is.EqualTo(SaveError.MissingFile));
            Assert.That(flow.InMenu, Is.True);
            Assert.That(flow.Player, Is.Null);
            Assert.That(feedback.gameObject.activeInHierarchy, Is.True);
            Assert.That(feedback.text, Does.Contain("Kayıt dosyası bulunamadı."));
            Assert.That(File.Exists(path), Is.False);

            Directory.CreateDirectory(directory);
            const string corrupt = "{\"format\":\"last-signal-save-1\",\"payload\":\"{}\",\"checksum\":\"tampered\"}";
            File.WriteAllText(path, corrupt);
            load.onClick.Invoke();
            yield return WaitForLoad();
            Assert.That(saves.LastResult.Error, Is.EqualTo(SaveError.ChecksumMismatch));
            Assert.That(flow.InMenu, Is.True);
            Assert.That(flow.Player, Is.Null);
            Assert.That(feedback.gameObject.activeInHierarchy, Is.True);
            Assert.That(feedback.text, Does.StartWith("Kayıt yüklenemedi."));
            Assert.That(feedback.text, Does.Contain("bozuk"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(corrupt));
            Assert.That(load.interactable, Is.True);
        }

        [UnityTest] public IEnumerator AuthoredButtonsSaveAndLoadThenFeedbackExpiresEvenWhilePaused()
        {
            flow.Pause();
            yield return null;
            Assert.That(save.gameObject.activeInHierarchy && save.interactable, Is.True);
            save.onClick.Invoke();
            Assert.That(saves.LastResult.Success, Is.True, saves.LastResult.Message);
            Assert.That(feedback.text, Is.EqualTo("Kontrol noktası kaydedildi."));
            Assert.That(feedback.gameObject.activeInHierarchy, Is.True);
            var original = File.ReadAllBytes(path);
            flow.ReturnToMenu();
            yield return null;
            load.onClick.Invoke();
            Assert.That(feedback.text, Is.EqualTo("Kontrol noktası yükleniyor…"));
            yield return WaitForLoad();
            Assert.That(saves.LastResult.Success, Is.True, saves.LastResult.Message);
            Assert.That(flow.Screen, Is.EqualTo(SessionScreen.Gameplay));
            Assert.That(flow.Player, Is.Not.Null);
            Assert.That(feedback.text, Is.EqualTo("Kontrol noktası yüklendi."));
            Assert.That(feedback.gameObject.activeInHierarchy, Is.True);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(feedback.gameObject.activeInHierarchy, Is.True);
            flow.Pause();
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(8.1f);
            Assert.That(feedback.gameObject.activeInHierarchy, Is.False);
            Assert.That(save.interactable, Is.True);
        }
    }
}
#endif
