using UnityEngine;

namespace FPS.Scripts.Items.Weapons.WeaponTypes{
    public class MeleeAnimatorConstants{
        public const string idleState = "Idle";

        public static readonly int attacksAnimationStateLayer = 0;
        
        public static readonly int startedSwingTrigger = Animator.StringToHash("Started Swing");
        public static readonly int startedChargingTrigger = Animator.StringToHash("Started Charging");
        public static readonly int interruptedChargingTrigger = Animator.StringToHash("Interrupted Charge");
        public static readonly int playerStunnedTrigger = Animator.StringToHash("Player Was Stunned");
        
        public static readonly int parryTrigger = Animator.StringToHash("Started Parrying");
        public static readonly int stoppedParryTrigger = Animator.StringToHash("Stopped Parrying");

        public static readonly int chargingTimeMultiplier = Animator.StringToHash("Charge Time Multiplier");
        public static readonly int swingTimeMultiplier = Animator.StringToHash("Swing Time Multiplier");

        public static readonly int actionDirection = Animator.StringToHash("Direction");
    }
}