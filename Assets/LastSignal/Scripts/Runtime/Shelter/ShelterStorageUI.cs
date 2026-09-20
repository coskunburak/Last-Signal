using LastSignal.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Shelter
{
    public sealed class ShelterStorageUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text status, readiness, feedback;
        [SerializeField] Button depositOne, depositStack, withdrawOne, withdrawStack, close;
        [SerializeField] ShelterSlotUI[] carriedSlots, storedSlots;
        ShelterLoop loop;
        PlayerInventory inventory;
        ShelterStorage storage;
        PlayerCombatController combat;
        WeaponController weapon;
        int carriedSelection = -1, storedSelection = -1;
        public int RefreshCount { get; private set; }
        public long PresentationAllocatedBytes { get; private set; }
        public bool IsOpen => isActiveAndEnabled && panel && panel.activeInHierarchy;
        public Button DepositOneButton => depositOne;
        public Button DepositStackButton => depositStack;
        public Button WithdrawOneButton => withdrawOne;
        public Button WithdrawStackButton => withdrawStack;
        public Text Feedback => feedback;
        public Text Readiness => readiness;
        void Awake()
        {
            panel.SetActive(false);
            depositOne.onClick.AddListener(DepositOne); depositStack.onClick.AddListener(DepositStack);
            withdrawOne.onClick.AddListener(WithdrawOne); withdrawStack.onClick.AddListener(WithdrawStack); close.onClick.AddListener(Close);
            for(int i=0;i<carriedSlots.Length;i++) carriedSlots[i].Bind(this,true,i);
            for(int i=0;i<storedSlots.Length;i++) storedSlots[i].Bind(this,false,i);
        }
        public void Bind(ShelterLoop owner)
        {
            Unbind();loop=owner;inventory=loop.Inventory;storage=loop.Storage;
            SizeSlots(ref carriedSlots,inventory.Capacity,true);SizeSlots(ref storedSlots,storage.Capacity,false);
            inventory.InventoryChanged+=Refresh;storage.Changed+=Refresh;loop.Changed+=Refresh;
            combat=loop.Session.Player.GetComponent<PlayerCombatController>();
            if(combat)combat.WeaponChanged+=BindWeapon;BindWeapon();
        }
        void SizeSlots(ref ShelterSlotUI[] slots,int capacity,bool carried)
        {
            int oldLength=slots.Length;
            if(capacity>oldLength)
            {
                System.Array.Resize(ref slots,capacity);
                for(int i=oldLength;i<capacity;i++)slots[i]=Instantiate(slots[0],slots[0].transform.parent);
            }
            for(int i=0;i<slots.Length;i++){slots[i].gameObject.SetActive(i<capacity);slots[i].Bind(this,carried,i);}
            var content=(RectTransform)slots[0].transform.parent;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Ceil(capacity/3f)*55);
        }
        void BindWeapon()
        {
            if(weapon)weapon.AmmoChanged-=Refresh;
            weapon=combat?combat.ActiveWeapon:null;
            if(weapon)weapon.AmmoChanged+=Refresh;Refresh();
        }
        public void Unbind()
        {
            Hide();if(inventory)inventory.InventoryChanged-=Refresh;if(storage!=null)storage.Changed-=Refresh;if(loop)loop.Changed-=Refresh;
            if(combat)combat.WeaponChanged-=BindWeapon;if(weapon)weapon.AmmoChanged-=Refresh;
            loop=null;inventory=null;storage=null;combat=null;weapon=null;
        }
        public void Show() { if(!loop)return;carriedSelection=storedSelection=-1;feedback.text="Select an item, then transfer one or its stack.";panel.SetActive(true);Refresh(); }
        public void Hide() { if(panel)panel.SetActive(false);carriedSelection=storedSelection=-1; }
        public void Select(bool carried,int slot)
        {
            if(!IsOpen)return;
            if(carried)carriedSelection=slot;else storedSelection=slot;
            Refresh();
        }
        void DepositOne()=>Transfer(true,false);
        void DepositStack()=>Transfer(true,true);
        void WithdrawOne()=>Transfer(false,false);
        void WithdrawStack()=>Transfer(false,true);
        void Close(){if(loop)loop.ClosePreparation();}
        void Transfer(bool deposit,bool wholeStack)
        {
            if(!IsOpen||!loop)return;
            int selection=deposit?carriedSelection:storedSelection;
            var slot=deposit?inventory.GetSlot(selection):storage.GetSlot(selection);
            if(slot.IsEmpty)return;
            var result=loop.Transfer(deposit,slot.Item,wholeStack?slot.Quantity:1);
            feedback.text=(deposit?"Stored ":"Took ")+result.Moved+" / "+result.Requested+" "+slot.Item.DisplayName+" — "+result.Remaining+" unmoved ("+result.Reason+")";
            Refresh();
        }
        void Refresh()
        {
            if(!IsOpen||!loop||!inventory||storage==null)return;
            long allocationStart=System.GC.GetAllocatedBytesForCurrentThread();
            RefreshCount++;
            for(int i=0;i<carriedSlots.Length;i++)carriedSlots[i].Present(inventory.GetSlot(i),i==carriedSelection);
            for(int i=0;i<storedSlots.Length;i++)storedSlots[i].Present(storage.GetSlot(i),i==storedSelection);
            bool a=!inventory.GetSlot(carriedSelection).IsEmpty,b=!storage.GetSlot(storedSelection).IsEmpty;
            depositOne.interactable=depositStack.interactable=a;withdrawOne.interactable=withdrawStack.interactable=b;
            status.text="SHELTER / PREPARATION    •    Expedition "+loop.ExpeditionIndex+" complete";
            var state=weapon?weapon.RuntimeState:null;
            readiness.text=state==null?"Weapon preparing…":"Magazine "+state.CurrentMagazine+" / "+state.Definition.MagazineCapacity+"    |    Carried reserve "+state.ReserveAmmo+"    |    Stored ammo "+storage.GetTotalQuantity(state.Definition.Ammunition);
            PresentationAllocatedBytes+=System.GC.GetAllocatedBytesForCurrentThread()-allocationStart;
        }
        void OnDisable() { if(loop && loop.Preparing) loop.ClosePreparation(); }
        void OnDestroy()
        {
            Unbind();depositOne.onClick.RemoveListener(DepositOne);depositStack.onClick.RemoveListener(DepositStack);
            withdrawOne.onClick.RemoveListener(WithdrawOne);withdrawStack.onClick.RemoveListener(WithdrawStack);close.onClick.RemoveListener(Close);
        }
    }
}
