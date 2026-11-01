using System;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Editor.WorldReconstruction
{
    public sealed class WorldReconstructionWindow : EditorWindow
    {
        WorldPlanReport audit;
        Texture2D preview;
        string error, summary;
        Vector2 scroll;
        [MenuItem("Last Signal/World Reconstruction/WLD-S001/Open G0-G1 planner")]
        public static void Open() => GetWindow<WorldReconstructionWindow>("WLD-S001 G0-G1");
        void OnDisable() => ClearPreview();
        void ClearPreview() { if(preview)DestroyImmediate(preview); preview=null; }
        void OnGUI()
        {
            EditorGUILayout.LabelField("G0 / G1 • kaynak denetimi ve arazi planı",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Bu pencere yalnız G0/G1 kaynak önizlemesidir. Onaylanan G2 aday aracı aynı menüde: 3 Create isolated G2 candidate. G3 arazi üretimi henüz yok. Bu önizleme için tek ve kaydedilmiş FirstTownMain_HDRP açık olmalı.",MessageType.Info);
            if(GUILayout.Button("1 • Audit Source — salt okunur")) Run(false);
            if(GUILayout.Button("2 • Preview World Plan — yalnız bellekte")) Run(true);
            using(new EditorGUI.DisabledScope(audit==null))
                if(GUILayout.Button("Rapor JSON'unu panoya kopyala"))GUIUtility.systemCopyBuffer=JsonUtility.ToJson(audit,true);
            if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
            if(audit==null)return;
            if(audit.sourceHasUnsavedChanges)EditorGUILayout.HelpBox("Kaynak sahnede kaydedilmemiş değişiklik var. Envanter bellekteki sahneyi gösterir; kaydedilmiş kaynak doğrulaması veya preview kabulü değildir.",MessageType.Warning);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("OWNER_ACCEPTANCE_PENDING • stage/test/benchmark NOT_RUN",EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"Kaynak yükseklik: {audit.sourceMinHeight:F3}…{audit.sourceMaxHeight:F3} m • koruma: {audit.protectedBounds} • perimeter adayı: {audit.perimeterCandidates}",EditorStyles.wordWrappedLabel);
            foreach(var blocker in audit.blockers)EditorGUILayout.HelpBox(blocker,MessageType.Warning);
            if(preview)
            {
                EditorGUILayout.LabelField("Batı solda / +X sağda • Kuzey (+Z) yukarıda. Bu bir tasarım önizlemesidir; üretilmiş TerrainData değildir.",EditorStyles.wordWrappedLabel);
                Rect area=GUILayoutUtility.GetAspectRect(2);EditorGUI.DrawPreviewTexture(area,preview,null,ScaleMode.ScaleToFit);
                EditorGUILayout.LabelField("Turuncu: korunacak yol • beyaz: önerilen bağlantı • mavi: drenaj • kırmızı: korunmuş çekirdek • orta çizgi: tile sınırı",EditorStyles.wordWrappedLabel);
            }
            if(!string.IsNullOrEmpty(summary))EditorGUILayout.LabelField(summary,EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }
        void Run(bool draw)
        {
            audit=null;summary=null;error=null;ClearPreview();
            try
            {
                var p=WorldReconstructionProfile.Load();var survey=WorldReconstructionAudit.Inspect(p, requireSaved:draw);audit=survey.report;
                if(draw)DrawPlan(p,survey);
                Debug.Log("WLD-S001 G0/G1 read-only inspection complete. Owner acceptance pending; no scene, terrain, test, bake or production writes.");
            }
            catch(Exception e){error=e.Message;Debug.LogException(e);}
        }
        void DrawPlan(WorldReconstructionProfile p,WorldPlanSurvey survey)
        {
            var evaluator=new NaturalTerrainPlan(p,survey.originalHeights,survey.protections);
            int height=p.previewResolution,width=2*(height-1)+1;var values=new float[width*height];
            float min=float.MaxValue,max=float.MinValue,maxProtectedDelta=0;int protectedSamples=0;
            Vector2 Point(int x,int z)=>new Vector2(Mathf.Lerp(p.extension.minX,p.core.maxX,x/(float)(width-1)),Mathf.Lerp(p.core.minZ,p.core.maxZ,z/(float)(height-1)));
            for(int z=0;z<height;z++)for(int x=0;x<width;x++)
            {
                var point=Point(x,z);float value=evaluator.Height(point);values[z*width+x]=value;min=Mathf.Min(min,value);max=Mathf.Max(max,value);
                if(p.core.Contains(point)&&evaluator.Influence(point)==0){protectedSamples++;maxProtectedDelta=Mathf.Max(maxProtectedDelta,Mathf.Abs(value-evaluator.Original(point)));}
            }
            float origin=Mathf.Floor((min-p.verticalPaddingMeters)/8)*8,top=Mathf.Ceil((max+p.verticalPaddingMeters)/8)*8;
            foreach(var v in values)NaturalTerrainPlan.ProjectWorldHeight(v,origin,top-origin);
            var pixels=new Color[values.Length];
            for(int z=0;z<height;z++)for(int x=0;x<width;x++)
            {
                var point=Point(x,z);var color=Color.Lerp(new Color(.16f,.25f,.23f),new Color(.68f,.64f,.46f),Mathf.InverseLerp(min,max,values[z*width+x]));
                if(p.core.Contains(point)&&evaluator.Influence(point)==0)color=Color.Lerp(color,new Color(.65f,.2f,.18f),.35f);
                if(x==height-1)color=new Color(.8f,.8f,.65f);
                pixels[z*width+x]=color;
            }
            Vector2 Pixel(Vector2 point)=>new Vector2((point.x-p.extension.minX)/(p.core.maxX-p.extension.minX)*(width-1),(point.y-p.core.minZ)/p.core.Depth*(height-1));
            void Line(Vector2 a,Vector2 b,Color color)
            {
                a=Pixel(a);b=Pixel(b);int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*2));
                for(int i=0;i<=steps;i++){var point=Vector2.Lerp(a,b,i/(float)steps);int x=Mathf.RoundToInt(point.x),z=Mathf.RoundToInt(point.y);if(x>=0&&x<width&&z>=0&&z<height)pixels[z*width+x]=color;}
            }
            foreach(var route in p.routes)for(int i=1;i<route.points.Length;i++)Line(route.points[i-1],route.points[i],route.preserveExisting?new Color(1,.65f,.2f):Color.white);
            for(int i=1;i<p.drainage.Length;i++)Line(new Vector2(p.drainage[i-1].x,p.drainage[i-1].z),new Vector2(p.drainage[i].x,p.drainage[i].z),Color.cyan);
            preview=new Texture2D(width,height,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};preview.SetPixels(pixels);preview.Apply();
            summary=$"Seed {p.seed}; {p.generatorVersion}. Coarse plan samples {width}×{height}, height {min:F2}…{max:F2} m. Proposed derivative interval [{origin:F0},{top:F0}] m; source world heights reprojected, never multiplied by new size.y. Protected sampled delta={maxProtectedDelta:F6} m ({protectedSamples} samples). Estimated 16-bit height step={(top-origin)/65535f:F6} m.\n"+
                "These extrema are a coarse design estimate; full 513×513 per-tile sampling, quantized seam audit, SetNeighbors on BOTH terrains, grounding, road grade/turn radius and drainage crossings are G3/G4 owner gates. Shared seam world-height function is specified; no native seam has been created or accepted. Existing creek mesh remains unchanged. New drainage has no water surface yet. Additional canopy cap "+p.additionalTreeBudget+"; no vegetation generated.";
            audit.previewSummary=summary;
        }
    }
}
