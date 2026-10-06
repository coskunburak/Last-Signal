using FPS.Scripts.Items.Weapons;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlot : MonoBehaviour
{
    [Tooltip("CanvasGroup to fade the ammo UI")]
    public CanvasGroup canvasGroup;
    [Tooltip("Image for when the weapon slot is empty")]
    public Image weaponImage;
    [Tooltip("Image for when the weapon slot is empty")]
    public Sprite emptySlotImage;
    [Tooltip("Text for image index")]
    public TMPro.TextMeshProUGUI weaponIndexText;

    [Header("Selection")]
    [Range(0, 1)]
    [Tooltip("Opacity when weapon not selected")]
    public float unselectedOpacity = 0.5f;
    [Tooltip("Scale when weapon not selected")]
    public Vector3 unselectedScale = Vector3.one * 0.8f;
    [Tooltip("Root for the control keys")]
    public GameObject controlKeysRoot;

    private int m_slotWeaponIndex;
    
    PlayerWeaponsManager m_PlayerWeaponsManager;
    public WeaponController weapon{ private get; set; }
    

    public void Initialize(int slotIndex)
    {
        weapon = null;
        weaponImage.sprite = emptySlotImage;

        m_slotWeaponIndex = slotIndex;

        m_PlayerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, WeaponSlot>(m_PlayerWeaponsManager, this);

        weaponIndexText.text = (m_slotWeaponIndex + 1).ToString();
    }

    void Update()
    {
        bool isActiveWeapon = weapon == m_PlayerWeaponsManager.GetActiveWeapon();
        transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? Vector3.one : unselectedScale, Time.deltaTime * 10);
        controlKeysRoot.SetActive(!isActiveWeapon);

        if (weapon != null)
        {
            weaponImage.sprite = weapon.weaponIcon;
        } 
    }

    public void ResetWeaponSlot() {
        weaponImage.sprite = emptySlotImage;
        Debug.Log("reset weapon");
    }
}
