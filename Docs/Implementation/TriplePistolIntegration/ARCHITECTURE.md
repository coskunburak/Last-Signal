# Shared presentation architecture

All three are explicitly cosmetic 9 mm representations of the existing `weapon.pistol` instance. They do not claim different capacities/calibers or balance. The existing 15-round capacity and 8-round starting magazine remain intact. DXT's 1911-like styling does not silently change ammunition to .45. IDs: `pistol.dxt`, `pistol.glock17`, `pistol.custom9mm`.

| Requirement | Existing owner | Asset dependency | Change | Authored test |
|---|---|---|---|---|
| Accessible selection | PlayerCombatController / FirearmSlot | three Variant assets | Rifle → DXT → Glock17 → Custom → rifle; HUD label | PlayerBindingCyclesRifleAndAllThreeWithoutNewMagazines |
| Ammo/fire/reload | WeaponController / WeaponRuntimeState | existing weapon.pistol definition | same controller/state retained across cosmetic swaps | RapidVariantsCancelReloadWithoutReplacingAmmoOwnerOrLateTransfer |
| FPS contacts/ADS | FirstPersonWeaponGrip / WeaponViewPresenter | per-model sockets/finger assets | restore old pose, bind new visual/profile/muzzle/aim | three EquipAdsFireReloadAndLifecycle cases |
| World model | CharacterWeaponRig | per-model Pose.WorldPrefab | use active PresentationProfile; cached owner for mechanics | same three lifecycle cases |
| Mechanical motion | authoritative ShotFired + reload clock | real source Slide/Trigger/Magazine | PistolMechanics; magazine stays existing rig-owned | asset checks / reload tests / manual QA |
| Save | S021 combat extension v2 | explicit variant ID | optional pistolVisualVariantId | RealSaveCodec… / EachVariantRoundTripsActualProductionSession |
| VFX/audio | existing S015/S021 presenters | inherited pistol bindings | rebind existing muzzle FX and ejection socket | cue count / combat regression |

PistolVariantPresenter binds a persistently authored default and instantiates only a replacement visual when a different variant is selected. It never creates a new WeaponRuntimeState or calls Initialize during a cosmetic switch. The player-driven switch queue waits for the authoritative unequip and is cleared on disable/death; direct save restoration bypasses that cosmetic wait. WeaponController owns cancellation/equip commands and resets ADS state; committed ammunition remains committed. No shot/damage/noise authority is added to animations.

Before destroying the old visual, the existing muzzle flash is reparented to the new muzzle. After the player-driven outgoing unequip reaches Holstered, old visuals are deactivated, then destroyed; renderer visibility caches are refreshed on switch, not per frame. CharacterWeaponRig reads the cached PresentationProfile; no per-frame hierarchy search was added. PistolMechanics caches its references/rest transforms and subscribes/unsubscribes to accepted shots.

The rifle path and its stock/support-hand semantics are unchanged. Existing world profile array remains usable; only a pistol with an explicit runtime presentation profile overrides its matching default.
