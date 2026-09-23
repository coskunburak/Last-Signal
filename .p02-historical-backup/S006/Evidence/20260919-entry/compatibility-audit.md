# S005 compatibility audit

Fresh baseline passes 97/97 + 65/65, but these tests do not establish visible normal-play loot/UI acceptance.

1. All six seed prefabs contain only Transform, BoxCollider, WorldItem (no Renderer). A visible scavenging loop cannot use them unchanged. Focused SeedItemsHaveVisibleWorldRepresentation test added before any visual changes. Planned fix: project-owned visual children using existing vendor prefabs where fitting; bounded fallback for unmapped items, no vendor edits. WorldItem/ItemDefinition code remains frozen.
2. InventorySlotUI.Configure adds OnClick on every binding without removing it. Across new sessions the same button invokes selection multiple times; two subscriptions select then deselect. RebindingSlotDoesNotDoubleInvokeClick invokes the actual button after two Configure calls. Planned one-line remove-before-add compatibility fix only.
3. SessionFlow fallback UI has an empty panel, no slots/drop controls; existing ZombieAcceptance scene has no HUD. Author a complete S006 scene UI using the existing view classes rather than introducing another inventory authority. Existing item icons are absent, so quantity-only labels omit item identity; minimal text formatting fix will include existing DisplayName. This remains event-driven.
4. Runtime dropped WorldItems have no teardown owner. New LootPopulationService snapshots pre-existing scene items at Begin and removes newly created scene WorldItems once at End, including drops. Existing inventory drop transactions remain unchanged. Focused ExactlyOncePartialFullDropAndNewCycle passed and verifies teardown leaves only pre-existing items.

No combat/input/health/AI/item-domain changes are proposed.
