---
doc_id: LS-DOC-04
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Veri sözlüğü, kimlikler, komutlar ve olay sözleşmeleri

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §10, §21-22; Ek B. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Kimliklerin ömrü

DefinitionId oyun verisinin kalıcı anahtarıdır: item.food.canned_beans. EntityId dünya içindeki kapı/zombi/loot nesnesinin kalıcı kimliğidir. ItemInstanceId belirli tekil eşyanın kimliğidir. RuntimeHandle aktif scene temsilinin geçici erişim anahtarıdır. Gelecekte NetworkObjectId yalnız oturum içi replication kimliği olur. Bunlar birbirinin yerine kullanılamaz.

**REQ-DATA-001:** Scene duplication yeni authored entity kimliği üretir; prefab instance'larının aynı ID ile kayda girmesi build validation hatasıdır. Transform path, display name veya GetInstanceID kalıcı anahtar değildir.

Authoring'de okunabilir label ayrı tutulur. Stable ID için GUID tercih edilir; insan tarafından okunur ashcreek.c03.gasstation.door.02 etiketi debug amacıyla kalabilir. Label değişikliği save'i bozmaz. Definition ID değişirse alias/migration gerekir.

## 2. Item verisi

| Alan | Tür / birim | Invariant |
|---|---|---|
| definitionId | Stable string | Catalog içinde tek |
| instanceId | UUID | Tek owner altında bulunur |
| quantity | Integer | 1..maxStack; boş stack silinir |
| unitMassGrams | Integer | Negatif olamaz |
| conditionPermille | 0..1000 | Float yuvarlama kaynaklı merge farkı önlenir |
| contamination | Enum | Unknown/Clean/Questionable/Contaminated |
| remainingVolumeMl | Integer | Kapasiteyi aşamaz |
| ownerContainerId | UUID | World item de bir owner temsiline sahiptir |
| revision | UInt64 | Başarılı mutationda artar |

DTO composition kullanır; her item her alanı taşımak zorunda değildir. Definition veri sürümü SaveSchemaVersion ile aynı kavram değildir. Ammo payload silaha, nutrition payload yiyeceğe aittir. Unsupported payload sessizce atılmaz.

## 3. Stack uyumu

Definition eşitliği tek başına yeterli değildir. Contamination, opened state, kalite bandı ve spoilage state uyumlu olmalı. Dilimlenmiş zaman farkları stack patlaması yaratmamalı: spoilage için belirli bucket kullanılıyorsa merge sonucu daha güvenli olmayan, korumacı değer seçilir. Unique weapon, dolu magazine ve isimlendirilmiş quest item stacklenmez.

**REQ-DATA-002:** Merge toplam miktarı/volümü yaratamaz. Split edilen stack yeni instance ID alır; iki parçanın kütle toplamı orijinale eşittir.

## 4. Command envelope örneği

```json
{
  "commandId": "cmd-0184",
  "kind": "TransferItem",
  "actorId": "player-local",
  "simulationTick": 540,
  "payload": {
    "sourceContainerId": "container-cabinet-07",
    "targetContainerId": "container-player-main",
    "instanceId": "item-051",
    "quantity": 2,
    "expectedSourceRevision": 12,
    "expectedTargetRevision": 8
  }
}
```

Buradaki kısa kimlikler okunabilir örnektir; gerçek UUID format validator'ına doğrudan örnek fixture diye sokulmaz. Yerel yürütmede JSON serialization gerekmez; typed request yeterlidir. commandId tekrarlama tespiti özellikle uzun işlemler ve ileride ağ için faydalıdır. İdempotency kaydı bounded olmalı; sınırsız dictionary değildir.

## 5. Sonuç ve hata kodları

| Kod | Mutation | UI karşılığı |
|---|---|---|
| Success | Atomik commit | Transfer animasyonu ve miktar |
| StaleRevision | Yok | Listeyi yenile, tekrar seçim |
| CapacityExceeded | Yok | Slot/ağırlık sınırını göster |
| TargetUnavailable | Yok | Kapanan hedefi kaldır |
| OutOfRange | Yok | Yaklaş mesajı |
| ActorBusy | Yok | Mevcut eylem bilgisi |
| InvalidPayload | Yok | Güvenli hata; debug ayrıntısı |
| CancelledBeforeCommit | Yok | İptal geri bildirimi |
| AlreadyCommitted | Tekrar yok | Önceki sonucu kullan |

## 6. Domain event kataloğu

| Event | Yayım noktası | Dinleyiciler |
|---|---|---|
| ItemTransferred | Container commit sonrası | UI, dirty tracker, quest inventory observer |
| WeaponFired | Ammo+shot commit sonrası | Audio, recoil, noise service, statistics |
| DamageApplied | Health transaction sonrası | Feedback, AI, wound UI |
| NoiseEmitted | Gameplay stimulus oluşumu | Hearing, pressure aggregation |
| ShelterUpgraded | Recipe+module commit sonrası | Journal, map, utility resolver |
| CellReady | Hydration+nav+collision hazır | Spawn coordinator, traversal guard |
| SaveCompleted | Generation pointer güvenli yayımlandı | UI, backup rotation |
| ObjectiveCompleted | Reward receipt ile aynı commit | Journal, unlocks |

**REQ-DATA-003:** Event payload geçmişteki olayı temsil eden immutable bilgi taşır. Daha sonra değişen component'e bakarak olayın hasar miktarı yeniden hesaplanmaz.

## 7. Schema evolution ve validation

Catalog build: duplicate ID, eksik prefab/icon, invalid mass, ammo compatibility, unreachable recipe, cyclic recipe unlock, missing localization, invalid loot reference kontrol edilir. Warning kozmetik eksiklerde; error kayıt/ilerleme bozan durumlarda kullanılır. Validation bir sahnenin açılmasına bağımlı olmamalı, CI'da da çalışabilmelidir.

DTO'larda explicit version, unit ve optional semantics bulunur. Unknown enum hatasında kritik state için load reddedilir; güvenli ve belgelenmiş cosmetic alanlarda fallback olabilir. JSON nesne sıralaması determinism ölçütü değildir; semantic state karşılaştırılır.

## 8. Kabul

Aynı komutu iki kez gönderince tek değişiklik; aynı instance iki container'da görünürse validation fail; quantity=0 veya negatif quantity reject. Definition rename migrationla çalışır. UI localization değişimi save kimliklerini etkilemez. İlgili sistemler [inventory](07_Item_Inventory_Equipment.md), [save](17_Save_Load_Migration.md).
