using UnityEngine;
namespace LastSignal.Audio
{
    [DisallowMultipleComponent] public sealed class FootstepAudioPresenter : MonoBehaviour
    {
        FirstPersonMotor motor;
        ProductionAudio director;
        float distance;
        void Awake() => motor=GetComponent<FirstPersonMotor>();
        void OnEnable() { if(motor) motor.PresentationMoved+=Advance; }
        void OnDisable() { if(motor) motor.PresentationMoved-=Advance; distance=0; }
        void Advance(Vector3 displacement, bool grounded, bool sprint, bool crouch)
        {
            if(!director) director=GetComponent<Noise.GameplayNoiseContext>()?.Session?.GetComponent<ProductionAudio>();
            if(!director || !director.Active || !grounded) { distance=0; return; }
            displacement.y=0;
            float traveled=displacement.magnitude;
            if(traveled>2 || traveled<.00001f) return;
            distance+=traveled;
            float stride=crouch?1.35f:sprint?1.9f:1.65f;
            if(distance<stride) return;
            distance%=stride;
            director.Step(transform, sprint?1:crouch?.4f:.7f, false);
        }
    }
}
