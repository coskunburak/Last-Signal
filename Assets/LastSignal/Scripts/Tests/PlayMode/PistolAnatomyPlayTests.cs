#if UNITY_EDITOR
using System.Collections;
using LastSignal.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed partial class WeaponGripCorrectionPlayTests
    {
        // This fixture uses the production pistol, whose normalized model no longer has
        // the old MR POLY origin (+.05 m on X). Query the actual frame cross-section.
        MeshCollider pistolContactMesh;
        Vector3[] frameVertices;
        int[] frameTriangles;
        void CreatePistolContactMesh()
        {
            var frame=probe.Fps.Visual.Find("Frame");
            Assert.That(frame,Is.Not.Null,"Expected the selected pistol's normalized rigid frame.");
            var filter=frame.GetComponent<MeshFilter>();
            Assert.That(filter && filter.sharedMesh,Is.True);
            var root=new GameObject("Pistol anatomy query only");root.layer=2;
            root.transform.SetPositionAndRotation(frame.localPosition,frame.localRotation);
            root.transform.localScale=frame.localScale;
            pistolContactMesh=root.AddComponent<MeshCollider>();
            pistolContactMesh.sharedMesh=filter.sharedMesh;
            var mesh=filter.sharedMesh;
            var localVertices=mesh.vertices;
            frameVertices=new Vector3[localVertices.Length];
            for(int i=0;i<localVertices.Length;i++)
                frameVertices[i]=frame.localPosition+frame.localRotation*Vector3.Scale(localVertices[i],frame.localScale);
            frameTriangles=mesh.triangles;
            Physics.SyncTransforms();
        }
        bool InsideFrame(Vector3 point)
        {
            // Count actual mesh crossings to the left. The pistol frame has open space
            // between some outer walls, so its full left/right span is not solid.
            int crossings=0,total=0;
            for(int i=0;i<frameTriangles.Length;i+=3)
            {
                var a=frameVertices[frameTriangles[i]];
                var b=frameVertices[frameTriangles[i+1]];
                var c=frameVertices[frameTriangles[i+2]];
                float by=b.y-a.y,bz=b.z-a.z,cy=c.y-a.y,cz=c.z-a.z;
                float determinant=by*cz-bz*cy;
                if(Mathf.Abs(determinant)<1e-10f)continue;
                float py=point.y-a.y,pz=point.z-a.z;
                float u=(py*cz-pz*cy)/determinant;
                float v=(by*pz-bz*py)/determinant;
                if(u<0||v<0||u+v>1)continue;
                total++;
                if(a.x+u*(b.x-a.x)+v*(c.x-a.x)<point.x)crossings++;
            }
            // An open cross-section has no defined inside. Only closed sections
            // can support a winding-parity penetration assertion.
            return total>=2 && (total&1)==0 && (crossings&1)!=0;
        }
        bool FrameSection(Vector3 point,out float left,out float right)
        {
            // Collider-specific rays, in normalized model coordinates; no scene-wide hit query.
            bool fromLeft=pistolContactMesh.Raycast(new Ray(new Vector3(-1,point.y,point.z),Vector3.right),out var a,2);
            bool fromRight=pistolContactMesh.Raycast(new Ray(new Vector3(1,point.y,point.z),Vector3.left),out var b,2);
            left=a.point.x;right=b.point.x;
            return fromLeft && fromRight && right-left>.001f;
        }
        void PistolFingerContact()
        {
            Contact(false);
            for(int i=6;i<15;i++)
            {
                var a=probe.PistolFingerSegments[i*2];var b=probe.PistolFingerSegments[i*2+1];
                for(int sample=1;sample<=10;sample++)
                {
                    var p=Vector3.Lerp(a,b,sample/10f);
                    if(!FrameSection(p,out _,out _))continue;
                    Assert.That(InsideFrame(p),Is.False,$"Final-frame finger {i}, sample {sample}/10 penetrates {combat.ActiveWeapon.PistolVariants.SelectedId} frame mesh at {p.ToString("F5")}; segment {a.ToString("F5")} to {b.ToString("F5")}; state={combat.ActiveWeapon.RuntimeState.State}.");
                }
                if(i%3==2)
                {
                    Assert.That(FrameSection(b,out float left,out _),Is.True,"Distal finger must overlap actual handle geometry.");
                    Assert.That(b.x,Is.InRange(left-.016f,left+.008f),"Finger must wrap the selected model's opposite side (bounded pad allowance).");
                }
            }
        }
        [UnityTest] public IEnumerator PistolFingerWrapSurvivesAdsFireAndReloadCommit()
        {
            yield return Pistol();
            foreach(var id in new[]{PistolVisualVariant.DxtId,PistolVisualVariant.GlockId,PistolVisualVariant.CustomId})
            {
                Assert.That(combat.ActiveWeapon.PistolVariants.Select(id),Is.True);
                float readyDeadline=Time.realtimeSinceStartup+5;
                while(combat.ActiveWeapon.RuntimeState.State!=WeaponState.Ready)
                {Assert.That(Time.realtimeSinceStartup,Is.LessThan(readyDeadline));yield return null;}
                yield return new WaitForSeconds(.4f);yield return Sample();
                CreatePistolContactMesh();
                try
                {
                    PistolFingerContact();
                    var weapon=combat.ActiveWeapon;var rest=probe.MagazineLocalPosition;
                    InputFixtureIsolation.QueueButton(Mouse.current.rightButton,true);
                    yield return new WaitForSeconds(.4f);yield return Sample();PistolFingerContact();
                    weapon.OnFirePressed();weapon.OnFireReleased();yield return Sample();PistolFingerContact();
                    InputFixtureIsolation.QueueButton(Mouse.current.rightButton,false);
                    player.GetComponent<PlayerInventory>().TryAdd(weapon.Definition.Ammunition,8);
                    weapon.OnReloadRequested();Assert.That(weapon.RuntimeState.State,Is.EqualTo(WeaponState.Reloading));
                    float deadline=Time.realtimeSinceStartup+8;bool extracted=false,committed=false;
                    while(weapon.RuntimeState.State==WeaponState.Reloading)
                    {
                        Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return Sample();PistolFingerContact();
                        if(Vector3.Distance(probe.MagazineLocalPosition,rest)>.02f)extracted=true;
                        if(weapon.RuntimeState.ReloadCommitted)
                        {
                            // Allow the probe to sample the commit processed in this Update.
                            yield return Sample();committed=true;
                            Assert.That(Vector3.Distance(probe.MagazineLocalPosition,rest),Is.LessThan(.001f));
                        }
                    }
                    Assert.That(extracted&&committed,Is.True);yield return Sample();
                    Assert.That(probe.Reload,Is.Zero);Assert.That(probe.Support,Is.Zero);PistolFingerContact();
                }
                finally
                {
                    if(pistolContactMesh)Object.DestroyImmediate(pistolContactMesh.gameObject);
                    pistolContactMesh=null;
                    frameVertices=null;frameTriangles=null;
                }
            }
        }
    }
}
#endif
