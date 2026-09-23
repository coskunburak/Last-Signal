# Canonical roadmap gap matrix

Entry assessment: 2026-09-21, HEAD `5b43cfbb8532d23ae5830e8956673b4d6b9f8fc3`, clean working tree. This is requirement traceability, not a rewrite of canonical sprint status.

Source order: current instruction > accepted decision > verified implementation/current contract > canonical plan > historical proposal. `IMPLEMENTED` identifies existing behavior; fresh run results are in RECOVERY_CHECKPOINT. Historical automated evidence does not prove human playtesting.

Runtime paths below refer to `Assets/LastSignal/Scripts/Runtime`; tests to `Assets/LastSignal/Scripts/Tests`. Evidence reviewed: S001 implementation/parkour, S004 P5 report and tests, S005 inventory contracts, S006 report/distribution, S007 ammunition report/tests, S008 final XML/build/standalone.

| Canonical card | Requirement | Entry classification | Implementation / evidence / remaining gap |
|---|---|---|---|
| S001.D001 | Gerçek proje envanteri | IMPLEMENTED | ProjectVersion 6000.5.0f1, clean entry HEAD/hashes in this run. |
| S001.D002 | Teknik başlangıç buildi | IMPLEMENTED | SessionFlow / BuildUtility; actual S008 macOS build and standalone evidence. |
| S001.D003 | Sürüm kontrolü ve geri dönüş | PARTIAL | Tracked meta/package lock and ignores exist; fresh-checkout reproduction not run here. |
| S001.D004 | Input ve odak yönetimi | IMPLEMENTED | PlayerInputReader gameplay maps, neutral-input gate, focus handlers; RuntimeSmokeTests. |
| S001.D005 | Yürüme ve kamera | IMPLEMENTED | FirstPersonMotor / FirstPersonLook; MovementAcceptanceTests. |
| S001.D006 | Eğim ve basamak parkuru | IMPLEMENTED | CharacterController parkour and MovementAcceptanceTests; S001 parkour XML. |
| S001.D007 | Sprint ve çömelme | IMPLEMENTED | PlayerStance head-clearance check; MovementAcceptanceTests. |
| S001.D008 | İlk etkileşim ve kapı | IMPLEMENTED | InteractionController first blocker, DoorInteractable obstruction/busy state; RuntimeSmokeTests. |
| S001.D009 | Oturum yaşam döngüsü | IMPLEMENTED | SessionFlow teardown/unsubscribe; session soak in RuntimeSmokeTests and ShelterIntegrationTests. |
| S001.D010 | Hareket build kabulü | PARTIAL | macOS standalone evidence exists; full human movement route and Windows acceptance not established. |
| S002.D011 | On iki eşyalı başlangıç kataloğu | PARTIAL | Actual S005 catalog has six valid IDs: ammo.rifle, medical.bandage, food.canned, drink.water, material.scrap, tool.wrench. Twelve/equipment scope remains open. |
| S002.D012 | Definition ve instance ayrımı | PARTIAL | ItemDefinition immutable; InventorySlot quantity separate. Unique instance/condition not implemented; deferred in actual S005 scope. |
| S002.D013 | Container kapasite sözleşmesi | PARTIAL | InventoryContainer slot/stack constraints tested. Weight is absent; current fixed-slot contract preserved, canonical mass policy still requires reconciliation. |
| S002.D014 | Atomik pickup işlemi | PARTIAL | WorldItem pickup sequential/partial behavior tested. InventoryChanged is emitted before world decrement; reentrant capture/interaction needs transaction boundary for save. |
| S002.D015 | Taşıma, bölme ve birleştirme | IMPLEMENTED | InventoryContainer TryMove/TrySplit/TryMerge; InventoryDomainTests. |
| S002.D016 | Envanter ilk arayüzü | PARTIAL | InventoryUI request/event binding works; no weight UI because weight authority absent. |
| S002.D017 | Ekipman ve çanta kapasitesi | MISSING | No equipment/bag capacity or quick-slot authority. Do not add RPG equipment merely to match historical plan. |
| S002.D018 | Kalıcı kimlik ve snapshot | MISSING | StableItemId and LootSpawnPoint IDs exist; no door/entity IDs, snapshot DTO or schema. |
| S002.D019 | İlk save ve load yolu | MISSING | No disk SaveService or safe writer/read validator. |
| S002.D020 | Kayıtlı loot rotası kabulü | MISSING | No saved-loot route or process/session save roundtrip. |
| S003.D021 | Düşman test alanı | IMPLEMENTED | ZombieAcceptance/CombatAcceptance scenes and authored navigation; actual S004. |
| S003.D022 | Nav ve ulaşılabilir hedef | IMPLEMENTED | ZombieNavigation bounded repath/recovery; ZombieNavigationTests and ZombieRecoveryTests. |
| S003.D023 | Görüş ve son bilinen konum | IMPLEMENTED | ZombiePerception LOS, ZombieRuntimeState bounded memory, ZombieSearch; ZombieBehaviorTests. |
| S003.D024 | Karar durumları | PARTIAL | Existing idle/chase/attack/search authority; sound investigation not implemented. |
| S003.D025 | Sağlık ve hasar sözleşmesi | IMPLEMENTED | PlayerHealth, ZombieHealth, hit-region authority and DamageInfo; damage/health tests. |
| S003.D026 | Crowbar saldırı penceresi | MISSING | Player rifle exists; player crowbar/stamina do not. Rifle direction supersedes pistol choice, but no evidence permanently removes melee requirement. |
| S003.D027 | Zombi saldırı uyarısı | IMPLEMENTED | ZombieController windup/contact/recovery, ZombieMeleeValidator LOS/range; melee tests. |
| S003.D028 | Tepki ve temel animasyon | IMPLEMENTED | ZombieAnimationPresenter driven by authority; hit/death cancellation tests. |
| S003.D029 | Corpse ve kalıcı ölüm | PARTIAL | Runtime corpse/death terminal state works; persistent death ID/tombstone and corpse container absent. |
| S003.D030 | Tek karşılaşma playtesti | PARTIAL | Automated production encounter and macOS smoke evidence; human fairness observation not proven. |
| S004.D031 | Hayatta kalma başlangıç profili | PARTIAL | PlayerHealth 100 exists; no versioned survival profile or needs/time units. |
| S004.D032 | Stamina tüketim ve toparlanma | MISSING | Sprint speed exists without stamina authority. |
| S004.D033 | Yeme ve içme işlemi | MISSING | Food/water are inventory resources only; no consume/stat transaction. |
| S004.D034 | Kanama ve bandaj | MISSING | Bandage definition exists; no wound/treatment domain. |
| S004.D035 | İlk ölüm ve yeniden başlama | PARTIAL | Single Died event/input lock and menu/new session exist. Recovery/death-bag policy absent. |
| S004.D036 | Küçük keşif rotası | PARTIAL | ScavengingAcceptance and ShelterAcceptance provide loot/combat/return; no observed 10–15 minute survival route. |
| S004.D037 | İlk yönlendirme | PARTIAL | Interaction/HUD/preparation prompts exist; no treatment/onboarding completion flow. |
| S004.D038 | Döngü kayıt entegrasyonu | MISSING | No persistence; wound/death-recovery domains also missing. |
| S004.D039 | İlk dış oyuncu pilotu | BLOCKED_EXTERNAL | No qualifying external-player pilot evidence. Requires actual human observation. |
| S004.D040 | Mikro döngü kararı | MISSING | No observed canonical survival micro-loop or product decision from pilot. |
| S005.D041 | Silah runtime durumları | IMPLEMENTED | WeaponRuntimeState and PlayerCombatController action gates; weapon tests. |
| S005.D042 | Mühimmat ve şarjör modeli | IMPLEMENTED | Actual S007 magazine + PlayerInventory reserve; AmmunitionTransactionTests. Rifle replaces historical pistol choice. |
| S005.D043 | Atış ve engel kontrolü | IMPLEMENTED | WeaponFireResolver camera/muzzle checks; point-blank and obstruction regression. |
| S005.D044 | Reload commit noktaları | IMPLEMENTED | Actual S007 single commit round-pool reload; 30/60/120 Hz conservation/cancel tests. Staged detachable magazines not claimed. |
| S005.D045 | Recoil ve spread | IMPLEMENTED | WeaponRecoilController separate from WeaponFireResolver spread; WeaponStateTests/CombatAcceptanceTests. |
| S005.D046 | Silah ses ve VFX bağlama | PARTIAL | Weapon audio/VFX successful-shot presentation exists; hearing independence unverified until gameplay noise exists. |
| S005.D047 | Noise event ve spatial sorgu | MISSING | No gameplay NoiseEvent, event receipt or spatial delivery. |
| S005.D048 | İşitme ve araştırma | MISSING | ZombiePerception is visual; no hearing stimulus. Existing search should be reused. |
| S005.D049 | Silah save ve edge case | MISSING | No magazine/reload persistence; must preserve magazine plus inventory reserve without replay. |
| S005.D050 | Sesli sefer kabulü | BLOCKED_EXTERNAL | No sound-cost A/B pilot; noise/hearing and melee dependencies missing. |
| S006.D051 | Loot kaynak aileleri | IMPLEMENTED | Actual S006 semantic LootProfiles and placement; LootProfileTests. |
| S006.D052 | Seed ve RNG akışları | IMPLEMENTED | LootRandom derives independent result from seed+point; global RNG isolation tests. |
| S006.D053 | İlk açılış commit'i | PARTIAL | LootPopulationService populated/results guarantee exactly once in current session, not across load. |
| S006.D054 | Boş ve dolu container feedback'i | DEFERRED_BY_ACCEPTED_DECISION | Actual S006 explicitly defers searchable containers; visible world pickups used. Empty opportunity feedback still not implemented. |
| S006.D055 | Kritik kaynak güvence rotası | MISSING | Profile distribution exists but guaranteed critical-resource route/softlock floor not proven. |
| S006.D056 | Ağırlık ve taşıma kararları | MISSING | No carry weight or encumbrance; fixed slot decisions exist. |
| S006.D057 | Loot miktar taraması | PARTIAL | Actual S006 10000-seed distribution evidence; no human route consumption comparison. |
| S006.D058 | Çevresel loot işaretleri | PARTIAL | Visible item prefabs and placement validation exist; human environmental readability not certified. |
| S006.D059 | Loot değişimi ve eski kayıt | MISSING | No persisted generated outcomes/content-version compatibility. |
| S006.D060 | Ekonomi pilotu ve sınır kararı | BLOCKED_EXTERNAL | No two-approach economy pilot; final balance not established. |
| S007.D061 | Cell koordinat ve sahiplik | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D062 | Küçük yükleme deneyi | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D063 | Cell yaşam döngüsü | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D064 | Güvenli oyuncu sınırı | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D065 | Delta ve tombstone | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D066 | Load iptal ve generation | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D067 | Navigation birleşimi | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D068 | Unload ve save yarışı | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D069 | Bellek dolaşım ölçümü | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S007.D070 | İki-cell build kabulü | MISSING | Canonical P02 not implemented. Requires green persistence and P00/P01 reconciliation; actual S007 is ammunition, not cells. |
| S008.D071 | Saat sözleşmesi | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D072 | Gün-gece ışık geçişi | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D073 | Yağmur durum profili | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D074 | Çatı ve maruziyet | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D075 | Islaklık ve sıcaklık etkisi | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D076 | Dinlenme uygunluğu | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D077 | Kesilebilir zaman ilerletme | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D078 | Uzaktaki zamanlı işler | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D079 | Uzun zaman fixture'ı | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S008.D080 | Hava ve uyku kabulü | MISSING | Canonical P02 not implemented. Requires verified two-cell foundation; actual S008 is shelter, not time/weather. |
| S009.D081 | Baskı deney sorusu | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D082 | Yerel pressure state | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D083 | Nüfus defteri | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D084 | AI simülasyon öncelikleri | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D085 | Araştıran grup yolculuğu | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D086 | Görünürlük kontrollü materialize | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D087 | Baskı debug görünümü | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D088 | Pressure persistence | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D089 | Sessiz ve gürültülü A/B rota | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |
| S009.D090 | Deney devam veya sadeleştirme | MISSING | Canonical P02 not implemented. Requires cells, persistence and world-time basis; simple P01 hearing can precede this. |

Actual S008 storage, preparation and same-scene expeditions partially implement canonical S010 shelter early. Crafting, upgrades, sleep and persistence are not thereby complete.

## Recovery delta

No production changes yet. D018–D020 are first priority after a green fresh baseline. Entry classifications above remain historical; closure evidence will be recorded here explicitly.
