using System;
using UnityEngine;
using UnityEngine.Events;

namespace FPS.Scripts.Items.Weapons{
    [RequireComponent(typeof(WeaponController))]
    public abstract class WeaponSecondaryBehaviour : MonoBehaviour{
        private GameObject owner;

        public abstract void HandleInputs(bool inputDown, bool inputHeld, bool inputReleased);
        
        protected virtual void InitialiseSecondaryBehaviour() {}

        public virtual void AssignOwner(GameObject owner){
            this.owner = owner;
        }

        private void Awake(){
            InitialiseSecondaryBehaviour();
        }
    }
}