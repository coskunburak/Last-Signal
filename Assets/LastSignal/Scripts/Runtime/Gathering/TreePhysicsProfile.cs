using UnityEngine;

namespace LastSignal.Gathering
{
    [CreateAssetMenu(menuName="Last Signal/Gathering/Tree Physics Profile")]
    public sealed class TreePhysicsProfile : ScriptableObject
    {
        public float radius=.25f, height=12, cutHeight=.9f, mass=450;
        public Vector3 centerOfMass=new Vector3(.04f,4,0);
        [Tooltip("Effective damaged green-wood strength (Pa); initial tuning, owner measurement required.")]
        public float bendingStrength=8000000, axialStrength=4000000;
        public float axeImpulse=8, settleSpeed=.08f, settleAngularSpeed=.06f, settleSeconds=2;
        public Mesh intactTop, intactStump;
        // side * Levels + level - 1. Owner-generated, capped bark only; foliage stays separate.
        public Mesh[] cutTops=new Mesh[32], cutStumps=new Mesh[32];
        public float[] cutDepths; // Absent = original four-stage profile; new authoring creates 16 exact support depths.
        public int StageCount => cutDepths==null || cutDepths.Length==0 ? TreeStructure.Levels : cutDepths.Length;
        public bool Valid => float.IsFinite(radius)&&radius>.03f&&radius<2 && float.IsFinite(height)&&height>2&&height<50 &&
            float.IsFinite(cutHeight)&&cutHeight>.3f&&cutHeight<2 && float.IsFinite(mass)&&mass>10&&mass<20000 &&
            TreeStructure.Finite(centerOfMass)&&centerOfMass.y>cutHeight&&centerOfMass.y<height &&
            float.IsFinite(bendingStrength)&&bendingStrength>0&&float.IsFinite(axialStrength)&&axialStrength>0 &&
            float.IsFinite(axeImpulse)&&axeImpulse>0&&axeImpulse<=20 && settleSpeed>0&&settleSpeed<=.2f &&
            settleAngularSpeed>0&&settleAngularSpeed<=.2f&&settleSeconds>=1&&settleSeconds<=10 &&
            intactTop&&intactStump&&DepthsValid()&&MeshesValid(cutTops)&&MeshesValid(cutStumps);
        bool DepthsValid()
        {
            if(cutDepths==null || cutDepths.Length==0) return true;
            if(cutDepths.Length!=TreeStructure.ProgressiveLevels) return false;
            for(int i=0;i<cutDepths.Length;i++)
                if(!float.IsFinite(cutDepths[i]) || Mathf.Abs(cutDepths[i]-TreeStructure.ProgressiveDepth(i+1))>.00001f) return false;
            return true;
        }
        bool MeshesValid(Mesh[] meshes)
        { if(meshes==null||meshes.Length!=TreeStructure.Sides*StageCount) return false; foreach(var m in meshes) if(!m||m.subMeshCount!=2||m.vertexCount<3) return false; return true; }
        public bool Supports(TreeCutSnapshot t) => t!=null && (t.version==1 || (t.version==2 && StageCount==TreeStructure.ProgressiveLevels));
        public int Index(TreeCutSnapshot t) => t.side*StageCount+Mathf.Clamp(Mathf.CeilToInt(t.depth*StageCount-.00001f),1,StageCount)-1;
    }
}
