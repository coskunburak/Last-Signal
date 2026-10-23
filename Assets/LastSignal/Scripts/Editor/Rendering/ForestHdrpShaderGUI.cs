using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor.Rendering
{
    // The forest shaders retain their custom property contract. Native HDRP/Lit and
    // URP inspectors both assume different passes/properties; neither is appropriate.
    public sealed class ForestHdrpShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            editor.PropertiesDefaultGUI(properties);
            if (!EditorGUI.EndChangeCheck()) return;
            foreach (var target in editor.targets)
            {
                if (!(target is Material material)) continue;
                // Only an explicit inspector edit synchronizes state. Merely importing
                // this inspector must not rewrite existing authored material keywords.
                if (material.shader.name == "LastSignal/Forest/Lit Wind")
                {
                    bool clip = material.GetFloat("_AlphaClip") > .5f;
                    Keyword(material, "_ALPHATEST_ON", clip);
                    Keyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
                    Keyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
                    Keyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
                    Keyword(material, "_EMISSION", material.GetColor("_EmissionColor").maxColorComponent > 0);
                    Keyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", material.GetFloat("_SmoothnessTextureChannel") > .5f);
                    material.SetOverrideTag("RenderType", clip ? "TransparentCutout" : "Opaque");
                    material.renderQueue = (int)(clip ? RenderQueue.AlphaTest : RenderQueue.Geometry) + (int)material.GetFloat("_QueueOffset");
                }
                else if (material.shader.name == "LastSignal/Forest/Terrain Lit")
                {
                    Keyword(material, "_TERRAIN_BLEND_HEIGHT", material.GetFloat("_EnableHeightBlend") > .5f);
                    Keyword(material, "_TERRAIN_INSTANCED_PERPIXEL_NORMAL", material.GetFloat("_EnableInstancedPerPixelNormal") > .5f);
                    // Terrain's layer binding owns the normal/mask map keywords.
                }
                EditorUtility.SetDirty(material);
            }
        }

        static void Keyword(Material material, string name, bool enabled)
        {
            if (enabled) material.EnableKeyword(name);
            else material.DisableKeyword(name);
        }
    }
}
