#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using LastSignal.Gathering;
using LastSignal.WorldCells;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace LastSignal.Tests
{
    public sealed partial class P01TreeStageTests
    {
        [UnityTest] public IEnumerator RealCellUnloadReloadRetainsPartialCutAndSingleFracturedYield()
        {
            var cells=gathering.GetComponent<WorldCellManager>(); Assert.IsNotNull(cells);
            var originalManifest=gathering.manifest; var originalResidents=gathering.residentViews;
            var cellSettings=new SerializedObject(cells); var definitions=cellSettings.FindProperty("definitions");
            var originals=new WorldCellContent[definitions.arraySize];
            for(int i=0;i<originals.Length;i++) originals[i]=(WorldCellContent)definitions.GetArrayElementAtIndex(i).objectReferenceValue;
            var source=AssetDatabase.LoadAssetAtPath<WorldCellContent>("Assets/LastSignal/Prefabs/WorldCells/Cell1.prefab");
            Assert.IsNotNull(source);
            var definition=Object.Instantiate(source); definition.gameObject.SetActive(false);
            var manifest=Object.Instantiate(originalManifest);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(view.physicsBody.profile))+"/HarvestableTree.prefab");
            Assert.IsNotNull(prefab);
            var template=Object.Instantiate(prefab,definition.transform).GetComponent<ResourceNodeView>();
            template.world=null;
            var originalView=view; var residentSpawn=flow.Player.transform.position;
            try
            {
                flow.ReturnToMenu();
                template.transform.position=definition.entry.position+new Vector3(4,0,4);
                var anchor=manifest.anchors.Single(a=>a.nodeId==nodeId);
                anchor.position=template.transform.position; anchor.rotation=template.transform.rotation; anchor.cellId=definition.coordinate.Id;
                manifest.revision++; Assert.IsTrue(manifest.Valid);
                gathering.manifest=manifest; gathering.residentViews=originalResidents.Where(v=>v!=originalView).ToArray();
                originalView.gameObject.SetActive(false);
                cells.Configure(new[]{definition},saves.Catalog);
                flow.BeginSession(); flow.Resume(); yield return null;
                string cellId=definition.coordinate.Id;
                Assert.IsTrue(cells.Request(cellId)); yield return WorldCellAcceptanceRoute.Ready(cells,cellId);
                Assert.IsTrue(cells.TryEnter(cellId));
                view=cells.Content(cellId).GetComponentsInChildren<ResourceNodeView>().Single();
                Assert.IsTrue(gathering.Ready,gathering.Feedback);
                Strike(); var partial=State(); Assert.IsNull(partial.pickup);
                Assert.IsTrue(cells.ReturnToResident(residentSpawn),cells.Failure);
                yield return null; Assert.AreEqual(CellState.Unloaded,cells.State(cellId)); Assert.IsNull(cells.Content(cellId));
                Assert.IsTrue(cells.Request(cellId)); yield return WorldCellAcceptanceRoute.Ready(cells,cellId);
                Assert.IsTrue(cells.TryEnter(cellId));
                view=cells.Content(cellId).GetComponentsInChildren<ResourceNodeView>().Single();
                Assert.AreEqual(partial.revision,State().revision); Assert.AreEqual(partial.tree.depth,view.physicsBody.CutDepth); Assert.IsNull(State().pickup);
                while(!State().tree.fractured && State().tree.depth<.975f)
                { Strike(); for(int i=0;i<3;i++) yield return new WaitForFixedUpdate(); }
                for(int i=0;i<100 && !State().tree.fractured;i++) yield return new WaitForFixedUpdate();
                Assert.IsTrue(State().tree.fractured); string yieldId=State().pickup.id;
                Assert.IsTrue(cells.ReturnToResident(residentSpawn),cells.Failure);
                yield return null; Assert.AreEqual(CellState.Unloaded,cells.State(cellId));
                Assert.IsTrue(cells.Request(cellId)); yield return WorldCellAcceptanceRoute.Ready(cells,cellId);
                Assert.IsTrue(cells.TryEnter(cellId));
                view=cells.Content(cellId).GetComponentsInChildren<ResourceNodeView>().Single();
                Assert.IsTrue(view.physicsBody.Fractured); Assert.AreEqual(yieldId,State().pickup.id); Assert.AreEqual(1,State().yieldSequence);
                for(int i=0;i<1800 && !view.physicsBody.CanCollect;i++) yield return new WaitForFixedUpdate();
                Assert.IsTrue(view.physicsBody.CanCollect); AssertVisible(); Assert.AreEqual(1,State().yieldSequence);
                yield return Reload(); Assert.AreEqual(yieldId,State().pickup.id); Assert.AreEqual(1,State().yieldSequence);
            }
            finally
            {
                flow.ReturnToMenu(); gathering.manifest=originalManifest; gathering.residentViews=originalResidents;
                cells.Configure(originals,saves.Catalog); originalView.gameObject.SetActive(true);
                Object.Destroy(definition.gameObject); Object.Destroy(manifest);
            }
        }
    }
}
#endif
