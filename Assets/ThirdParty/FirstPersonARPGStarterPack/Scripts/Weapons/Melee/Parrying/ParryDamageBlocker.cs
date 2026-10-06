using FPS.Scripts.Characters;
using Main.StatsAndManipulation;
using UnityEngine;

namespace FPS.Scripts.Items.Weapons.WeaponTypes.Melee{
    public class ParryDamageBlocker : DamageBlocker{
        private Parry parry;

        public override void InitDamageBlocker(){
            base.InitDamageBlocker();

            parry = GetComponent<Parry>();
            DebugUtility.HandleErrorIfNullGetComponent<Parry, ParryDamageBlocker>(parry, this, gameObject);
            
            PlayerWeaponsManager playerWeaponsManager = GetComponentInParent<PlayerWeaponsManager>();
            playerWeaponsManager.onSwitchedToWeapon += OnSwitchedWeapon;
        }

        public override bool CanBlockAttack(DamageInfo damageInfo){
            return parry.CanBlockAttack(damageInfo);
        }

        public override float CalculatedDamageAmountAfterBlock(DamageInfo damage){
            return parry.CalculateDamageAfterBlock(damage.damageAmount);
        }

        public override void OnDamageBlocked(DamageInfo damageInfo){
            parry.OnDamageBlocked(damageInfo);
        }

        private void OnSwitchedWeapon(WeaponController weapon){
            if (weapon == null) return;
            if (weapon.gameObject == gameObject)
            {
                characterHealth.currentDamageBlocker = this;
            }
        }
    }
}