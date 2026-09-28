using System.Collections.Generic;
using FPS.Scripts.Items.Weapons;
using UnityEngine;

public class WeaponHUDManager : MonoBehaviour
{
    [Tooltip("UI panel containing the layoutGroup for displaying weapon ammos")]
    public RectTransform weaponsPanel;
    [Tooltip("Prefab for displaying selected weapons")]
    public GameObject m_WeaponSlotPrefab;

    PlayerWeaponsManager m_PlayerWeaponsManager;
    List<WeaponSlot> m_WeaponSlots = new List<WeaponSlot>();

    void Start(){
        m_PlayerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, WeaponHUDManager>(m_PlayerWeaponsManager, this);

        for (int i = 0; i < m_PlayerWeaponsManager.m_WeaponSlots.Length; i++)
        {
            var slotObject = Instantiate(m_WeaponSlotPrefab, weaponsPanel);
            var weaponSlot = slotObject.GetComponent<WeaponSlot>();
            m_WeaponSlots.Add(weaponSlot);
            weaponSlot.Initialize(i);
            WeaponController weaponAtSlot = m_PlayerWeaponsManager.GetWeaponAtSlotIndex(i);
            if (weaponAtSlot != null)
            {
                AddWeapon(weaponAtSlot, i);
            }
        }

        m_PlayerWeaponsManager.onAddedWeapon += AddWeapon;
        m_PlayerWeaponsManager.onRemovedWeapon += OnRemovedWeapon;
    }

    void AddWeapon(WeaponController newWeapon, int weaponIndex)
    {
        if (weaponIndex > m_WeaponSlots.Count)
        {
            Debug.LogError("Bad weapon index " + weaponIndex + "! Can't update GUI");
        }
        WeaponSlot weaponSlot = m_WeaponSlots[weaponIndex];
        DebugUtility.HandleErrorIfNullGetComponent<WeaponSlot, WeaponHUDManager>(weaponSlot, this, weaponsPanel.gameObject);
        weaponSlot.weapon = newWeapon;
    }

    void OnRemovedWeapon(WeaponController removedWeapon, int weaponIndex){
        m_WeaponSlots[weaponIndex].ResetWeaponSlot();
    }
}