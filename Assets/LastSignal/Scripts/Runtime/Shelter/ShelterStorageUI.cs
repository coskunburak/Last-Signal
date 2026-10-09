using LastSignal.Inventory;
using LastSignal.Inventory.Data;
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
        ItemDefinition carriedItem, storedItem;
        long carriedRevision, storedRevision;
        public int RefreshCount { get; private set; }
        public long PresentationAllocatedBytes { get; private set; }
        public GameObject Root => panel;
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
        public void Show()
        {
            if(!loop)return;
            SizeSlots(ref carriedSlots,inventory.Capacity,true);SizeSlots(ref storedSlots,storage.Capacity,false);
            carriedSelection=storedSelection=-1;carriedItem=storedItem=null;
            Label(depositOne,"Birini depola");Label(withdrawOne,"Birini al");
            Label(depositStack,"Sığanı depola");Label(withdrawStack,"Sığanı al");
            feedback.text="SOL: TAŞINAN  |  SAĞ: DEPO. Türü seç; birini veya sığan miktarı aktar.";
            panel.SetActive(true);Refresh();
        }
        static void Label(Button button,string value)
        { if(!button)return;var label=button.GetComponentInChildren<Text>(true);if(label)label.text=value; }
        public void Hide() { if(panel)panel.SetActive(false);carriedSelection=storedSelection=-1;carriedItem=storedItem=null; }
        public void Select(bool carried,int slot)
        {
            if(!IsOpen)return;
            if(carried)
            {
                if(slot<0||slot>=inventory.Capacity)return;
                var value=inventory.GetSlot(slot);
                carriedSelection=value.IsEmpty?-1:slot;carriedItem=value.Item;carriedRevision=inventory.Revision;
            }
            else
            {
                if(slot<0||slot>=storage.Capacity)return;
                var value=storage.GetSlot(slot);
                storedSelection=value.IsEmpty?-1:slot;storedItem=value.Item;storedRevision=storage.Revision;
            }
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
            if((deposit ? inventory.Revision != carriedRevision : storage.Revision != storedRevision)||slot.IsEmpty||slot.Item!=(deposit?carriedItem:storedItem))
            { feedback.text="Seçili eşya değişti. Yeniden seç.";Refresh();return; }
            var result=loop.Transfer(deposit,slot.Item,wholeStack?slot.Quantity:1);
            feedback.text=DescribeTransfer(deposit,slot.Item.DisplayName,result);
            Refresh();
        }
        public static string DescribeTransfer(bool deposit,string item,TransferResult result)
        {
            string verb=deposit?"depoya aktarıldı":"alındı";
            if(result.Moved>0)
                return item+": "+result.Moved+" / "+result.Requested+" "+verb+
                    (result.Remaining>0?"; "+result.Remaining+" sığmadı veya kaynakta yok.":".");
            switch(result.Reason)
            {
                case TransferReason.DestinationFull: return item+": hedef dolu; kaynak değişmedi.";
                case TransferReason.SourceEmpty: return item+": kaynakta kalmadı; işlem yapılmadı.";
                case TransferReason.Busy: return "Başka eşya işlemi sürüyor; tekrar dene.";
                case TransferReason.Unavailable: return "Depo şu anda kullanılamıyor; işlem yapılmadı.";
                default: return "Aktarım reddedildi; kaynak değişmedi.";
            }
        }
        void Refresh()
        {
            if(!IsOpen||!loop||!inventory||storage==null)return;
            if(carriedSelection>=0 && (carriedRevision!=inventory.Revision || carriedSelection>=inventory.Capacity || inventory.GetSlot(carriedSelection).Item!=carriedItem))
            {carriedSelection=-1;carriedItem=null;feedback.text="Taşınan seçim değişti. Yeniden seç.";}
            if(storedSelection>=0 && (storedRevision!=storage.Revision || storedSelection>=storage.Capacity || storage.GetSlot(storedSelection).Item!=storedItem))
            {storedSelection=-1;storedItem=null;feedback.text="Depo seçimi değişti. Yeniden seç.";}
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
