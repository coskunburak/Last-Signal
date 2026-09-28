using System.Runtime.CompilerServices;
using UnityEngine;

namespace FPS.Scripts.Characters.AI{
    public class EnemyFootstepsSfx : MonoBehaviour{
        public float footstepFrequency = 1f;
        private float timeSinceLastFootstep = 0f;
        
        public AudioSource footstepAudioSource;
        public AudioClip footstepAudioClip;
        public Animator characterAnimator;
        
        void Update(){
            float movementSpeed = characterAnimator.GetFloat(EnemyAnimatorConstants.movementSpeedParameter);
            if (movementSpeed > 0.05f)
            {
                if (Time.time > timeSinceLastFootstep + footstepFrequency)
                {
                    footstepAudioSource.PlayOneShot(footstepAudioClip);
                    timeSinceLastFootstep = Time.time;
                }
            }
            else
            {
                timeSinceLastFootstep = Time.time;
            }
        }
    }
}