using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LastSignal.WorldTime;
namespace LastSignal.Slice
{
    // Presentation only: observes the existing world clock, never advances/restores simulation.
    public sealed class S013EnvironmentPresentation : MonoBehaviour
    {
        WorldClock clock;
        float refresh;
        void LateUpdate()
        {
            if(!clock)clock=FindAnyObjectByType<WorldClock>();
            refresh-=Time.unscaledDeltaTime;if(refresh>0)return;refresh=.25f;
            if(clock&&clock.Simulation!=null)
            {
                float day=(float)(clock.Simulation.TimeOfDay/86400);
                float daylight=Mathf.Clamp01(Mathf.Sin((day-.25f)*Mathf.PI*2));
                var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(Color.Lerp(new Color(.065f,.09f,.14f),new Color(.32f,.36f,.40f),daylight));RenderSettings.ambientProbe=probe;
                RenderSettings.fogColor=Color.Lerp(new Color(.10f,.14f,.19f),new Color(.54f,.61f,.62f),daylight);
                RenderSettings.fogDensity=clock.Simulation.Raining?.010f:.006f;
            }
            var camera=Camera.main;
            if(camera){var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.Medium;}
        }
    }
}
