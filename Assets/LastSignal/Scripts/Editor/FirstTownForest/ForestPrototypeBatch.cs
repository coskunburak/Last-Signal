using UnityEditor.SceneManagement;

namespace LastSignal.Editor.FirstTownForest
{
    /// <summary>Explicit batch entry point used by the manual handoff script.</summary>
    public static class ForestPrototypeBatch
    {
        public static void ValidateSavedScene()
        {
            EditorSceneManager.OpenScene(ForestPrototypeAuthoring.ScenePath, OpenSceneMode.Single);
            ForestPrototypeAuthoring.ValidateAuthored();
        }
    }
}
