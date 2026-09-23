using LastSignal.WorldTime;
using UnityEditor;
namespace LastSignal.Editor
{
    [CustomEditor(typeof(WorldClock))]
    public sealed class WorldClockInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var clock=(WorldClock)target;var s=clock.Simulation;if(s==null)return;
            EditorGUILayout.Space();EditorGUILayout.LabelField("Live world authority",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Absolute world seconds",s.Seconds.ToString("F3"));
            EditorGUILayout.LabelField("Day / time of day seconds",s.Day+" / "+s.TimeOfDay.ToString("F2"));
            EditorGUILayout.LabelField("Weather / next transition",(s.Raining?"Rain":"Clear")+" / "+s.NextWeather.ToString("F3"));
            EditorGUILayout.LabelField("Exposure / wetness",clock.Exposure+" / "+s.Wetness.ToString("F4"));
            EditorGUILayout.LabelField("Sleeping / last rejection",clock.Sleeping+" / "+clock.LastRejection);
            EditorGUILayout.LabelField("Last sleep reason / elapsed",s.LastSleepReason+" / "+s.LastSleepElapsed.ToString("F3"));
            if(UnityEditor.EditorApplication.isPlaying)Repaint();
        }
    }
}
