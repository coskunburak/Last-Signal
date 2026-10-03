using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class ScopeOpticEvidence
    {
        [MenuItem("Last Signal/Optics/Capture Game View Evidence")]
        static void Capture()
        {
            if (!EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Kanıt görüntüsünü Play Mode'da alın.");
            string folder = "Docs/Implementation/Combat/OpticsEvidence";
            Directory.CreateDirectory(folder);
            string path = folder + "/scope-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Sonraki render karelerinde kaydedilecek Game View kanıtı: " + Path.GetFullPath(path));
        }
    }
}
