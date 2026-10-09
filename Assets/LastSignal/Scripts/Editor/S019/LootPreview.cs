using System;
using System.Collections.Generic;
using System.Globalization;
using LastSignal.Loot;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor.S019
{
    public sealed class LootPreviewResult
    {
        public sealed class Row { public int occurrences; public long quantity; }
        public int samples, empty;
        public readonly SortedDictionary<string, Row> items = new SortedDictionary<string, Row>(StringComparer.Ordinal);
    }
    public static class LootPreview
    {
        // One real authored point across consecutive session seeds. No population service or receipts.
        public static LootPreviewResult Sample(LootProfile profile, int seed, int count, string pointId)
        {
            if (!profile) throw new ArgumentNullException(nameof(profile));
            if (count < 1 || count > 100000) throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrWhiteSpace(pointId)) throw new ArgumentException("A point ID is required.", nameof(pointId));
            if (!profile.Validate(out var error)) throw new ArgumentException(error, nameof(profile));
            var result = new LootPreviewResult { samples = count };
            for (int i = 0; i < count; i++)
            {
                var selection = profile.Select(unchecked(seed + i), pointId);
                if (selection.Outcome == LootOutcome.Empty) { result.empty++; continue; }
                if (selection.Outcome != LootOutcome.Spawned) throw new InvalidOperationException("Unexpected preview outcome: " + selection.Outcome);
                string id = selection.Item.Id.Value;
                if (!result.items.TryGetValue(id, out var row)) result.items.Add(id, row = new LootPreviewResult.Row());
                row.occurrences++; row.quantity += selection.Quantity;
            }
            return result;
        }
    }
    public sealed class LootPreviewWindow : EditorWindow
    {
        LootProfile profile; int seed = 12345, samples = 1000; string pointId = "s019.preview";
        string report = "Choose a profile or select a LootSpawnPoint, then Preview."; Vector2 scroll;
        [MenuItem("Last Signal/S019/Loot distribution preview")]
        public static void Open() => GetWindow<LootPreviewWindow>("Loot preview");
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Samples use consecutive session seeds and the real LootProfile.Select. Percentages include empty rolls; placement collisions are not simulated.", MessageType.Info);
            profile = (LootProfile)EditorGUILayout.ObjectField("Profile", profile, typeof(LootProfile), false);
            seed = EditorGUILayout.IntField("First session seed", seed);
            samples = EditorGUILayout.IntField("Samples (1..100000)", samples);
            pointId = EditorGUILayout.TextField("Authored point ID", pointId);
            if (GUILayout.Button("Use selected loot anchor"))
            {
                var anchor = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<LootSpawnPoint>() : null;
                if (anchor) { profile = anchor.Profile; pointId = anchor.StableId; }
            }
            if (GUILayout.Button("Preview"))
            {
                try
                {
                    var result = LootPreview.Sample(profile, seed, samples, pointId);
                    var text = new System.Text.StringBuilder();
                    text.AppendLine($"Profile: {AssetDatabase.GetAssetPath(profile)}\nSeed: {seed}; point: {pointId}; samples: {samples}");
                    text.AppendLine($"Empty: {result.empty} ({Percent(result.empty, samples)}%)");
                    foreach (var pair in result.items) text.AppendLine($"{pair.Key}: {pair.Value.occurrences} occurrences; {pair.Value.quantity} quantity; {Percent(pair.Value.occurrences, samples)}%");
                    report = text.ToString();
                }
                catch (Exception e) { report = e.Message; }
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true)); EditorGUILayout.EndScrollView();
        }
        static string Percent(int n, int total) => (100.0 * n / total).ToString("F2", CultureInfo.InvariantCulture);
    }
}
