using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Forest
{
    public sealed class ForestPrototypeLighting : MonoBehaviour
    {
        public Light sun;
        public Volume volume;
        public VolumeProfile morning, overcast;
        public bool isOvercast;
        public void Apply(bool cloudy)
        {
            isOvercast = cloudy;
            volume.sharedProfile = cloudy ? overcast : morning;
            sun.transform.rotation = Quaternion.Euler(cloudy ? 52 : 27, cloudy ? 125 : 115, 0);
            sun.color = cloudy ? new Color(.91f, .95f, 1) : new Color(1, .93f, .82f);
            sun.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
            sun.intensity = cloudy ? 6500 : 65000;
            var hd = sun.GetComponent<HDAdditionalLightData>();
            hd.angularDiameter = cloudy ? 5 : .65f;
            hd.volumetricDimmer = cloudy ? .25f : .8f;
        }
    }
}
