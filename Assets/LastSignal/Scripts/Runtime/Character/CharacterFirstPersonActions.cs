using UnityEngine;
namespace LastSignal
{
    /// <summary>Shared action presentation for the existing FPS rigs; preserves their own weapon animations.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class CharacterFirstPersonActions : MonoBehaviour
    {
        [SerializeField] Transform weaponParent;
        [SerializeField] PlayerLocomotionPresenter character;
        [SerializeField] Vector3 actionOffset = new Vector3(0, -.14f, -.08f);
        [SerializeField] Vector3 actionEuler = new Vector3(12, -4, 3);
        PlayerHealth health;
        Vector3 restPosition;
        Quaternion restRotation;
        float weight, hit;
        public void Configure(Transform parent, PlayerLocomotionPresenter presenter) { weaponParent=parent;character=presenter; }
        void Awake()
        {
            health=GetComponent<PlayerHealth>();
            if(weaponParent){restPosition=weaponParent.localPosition;restRotation=weaponParent.localRotation;}
        }
        void OnEnable(){if(health)health.DamageAccepted+=OnDamage;}
        void OnDamage(DamageInfo damage){hit=.035f;}
        void LateUpdate()
        {
            if(!weaponParent||!character)return;
            bool action=character.Treating || (character.UpperActionBusy &&
                (!character.Combat || character.Combat.SelectedSlot!=PlayerCombatController.CombatSlot.Melee));
            float dt=character.PresentationActive?Time.deltaTime:0;
            weight=Mathf.MoveTowards(weight,action?1:0,dt*8);hit=Mathf.MoveTowards(hit,0,dt*.2f);
            weaponParent.localPosition=restPosition+actionOffset*weight+Vector3.back*hit;
            weaponParent.localRotation=restRotation*Quaternion.Euler(actionEuler*weight);
        }
        void OnDisable()
        {
            if(health)health.DamageAccepted-=OnDamage;
            if(weaponParent){weaponParent.localPosition=restPosition;weaponParent.localRotation=restRotation;}
            hit=weight=0;
        }
    }
}
