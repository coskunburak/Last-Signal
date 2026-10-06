using System;
using UnityEngine;
using UnityEngine.Events;

namespace FPS.Scripts.Items.Weapons{
    [Serializable]
    public struct CrosshairData
    {
        [Tooltip("The image that will be used for this weapon's crosshair")]
        public Sprite crosshairSprite;
        [Tooltip("The size of the crosshair image")]
        public int crosshairSize;
        [Tooltip("The color of the crosshair image")]
        public Color crosshairColor;
    }

    [RequireComponent(typeof(AudioSource))]
    public abstract class WeaponController : MonoBehaviour
    {
        [Header("Information")]
        [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string weaponName;
        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite weaponIcon;

        [Tooltip("Default data for the crosshair")]
        public CrosshairData crosshairDataDefault;
        [Tooltip("Data for the crosshair when targeting an enemy")]
        public CrosshairData crosshairDataTargetInSight;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        public GameObject weaponRoot;

        [Header("Audio & Visual")]
        [Tooltip("Optional weapon animator for animations")]
        public Animator weaponAnimator;
        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip changeWeaponSFX;
    
        public UnityAction onAttack;

        public GameObject owner { get; set; }
        public GameObject sourcePrefab { get; set; }
        public bool isWeaponActive { get; private set; }

        protected AudioSource mWeaponAudioSource;
        
        [Tooltip("Time taken between switching to this weapon")]
        public float weaponSwitchTime = 0.4f;
        private float lastSwitchTime;

        public Transform initTransform { private set; get; }

        protected WeaponSecondaryBehaviour secondaryBehaviour;
        
        public virtual void OnAttackAnimStarted() {}
        public virtual void OnAttackAnimEnded() {}

        public void ShowWeapon(bool show){
            lastSwitchTime = Time.time;
            weaponRoot.SetActive(show);

            if (show && changeWeaponSFX)
            {
                mWeaponAudioSource.PlayOneShot(changeWeaponSFX);
            }

            isWeaponActive = show;
        }

        public void Awake(){
            initTransform = transform;
            secondaryBehaviour = GetComponent<WeaponSecondaryBehaviour>();
            mWeaponAudioSource = GetComponent<AudioSource>();
            InitialiseWeapon();
        }

        public void Update(){
            UpdateWeapon();
        }

        public bool IsSwitchingWeapon(){
            return weaponSwitchTime + lastSwitchTime > Time.time;
        }

        public abstract bool HandleAttackInputs(bool inputDown, bool inputHeld, bool inputReleased);

        public abstract void InitialiseWeapon();
        public abstract void UpdateWeapon();

        public WeaponSecondaryBehaviour GetWeaponSecondaryBehaviour(){
            return secondaryBehaviour; 
        }

        public virtual bool CanSwitchWeapon(){
            return true;
        }

        public virtual void AssignOwner(GameObject owner){
            this.owner = owner;
            if (secondaryBehaviour)
            {
                secondaryBehaviour.AssignOwner(owner);
            }
        }
    }
}