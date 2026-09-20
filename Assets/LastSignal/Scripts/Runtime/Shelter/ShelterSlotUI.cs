using LastSignal.Inventory;
using UnityEngine;
using UnityEngine.UI;
namespace LastSignal.Shelter
{
    public sealed class ShelterSlotUI : MonoBehaviour
    {
        [SerializeField] Text label;
        Button button;
        ShelterStorageUI owner;
        bool carried;
        int index;
        void Awake(){button=GetComponent<Button>();button.onClick.AddListener(Click);}
        public void Bind(ShelterStorageUI ui,bool isCarried,int slot){owner=ui;carried=isCarried;index=slot;}
        void Click(){if(owner)owner.Select(carried,index);}
        public void Present(InventorySlot slot,bool selected)
        {
            label.text=slot.IsEmpty?"—":slot.Item.DisplayName+"  ×"+slot.Quantity;
            label.color=selected?new Color(1,.8f,.35f):Color.white;
        }
        void OnDestroy(){if(button)button.onClick.RemoveListener(Click);}
    }
}
