# Gameplay, equipment and persistence

IMPLEMENTED_NOT_VERIFIED. Player.prefab secondaryWeapon now references DXT/FPS.prefab, whose catalog contains all three variants. This is the real S021 equipment workflow, not an Editor-only hotkey.

Default keyboard `1` / gamepad D-pad up (rebindable FirearmSlot action): rifle → DXT → Glock17 → Custom 9mm → rifle. HUD shows the explicit variant label and the binding hint. Selecting the pistol from a melee slot preserves its current variant; completing the firearm cycle starts at DXT again. All three share the one owned 9 mm pistol instance; reserve, loaded rounds, condition and cooldown are not reset by changing appearance.

During a player-driven variant switch the authoritative controller releases fire, cancels reload if necessary and advances the outgoing unequip timer. The presenter waits for Holstered, swaps and requests equip. A later input replaces the pending identity; disable/death/seating cancels it. Direct selection/save restoration bypasses the outgoing wait. Both paths preserve ammo and clear ADS. Geometry, profile, muzzle, sight and ejection references change together. No late animation callback can transfer rounds because this presentation code never transfers ammunition. Existing reload commit guards remain responsible for `before = after + accepted shots + explicit discards`.

`CombatEquipmentSnapshot.pistolVisualVariantId` is an optional additive field in the existing version-2 combat extension. No outer schema/content-version migration. Null/empty old saves choose DXT. Unknown nonempty IDs are rejected before load; a requested variant with missing runtime composition throws instead of silently selecting another model. SaveSession captures the variant even when melee/rifle is selected and restores it alongside the independent rifle/pistol magazines. Actual production-session tests are authored for all three.

No new caliber, item definition, weapon instance, loot stack or balance profile is introduced for a cosmetic model. This constrained representation is explicit in the HUD and manifest. Inventory ammunition ownership, rifle definition and the noise emitter remain unchanged.
