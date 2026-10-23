# SCN-S001 M4 — Forest Prototype ana sahne entegrasyonu

**IMPLEMENTED_OWNER_ACCEPTANCE_PENDING.** Var olan world yerleşimine yeni görsel çocuklar bağlandı; başka orman sahnesinin tüm hiyerarşisi kopyalanmadı.

- 346 ağaç: Spruce1/4/6/8/9 ve Birch2; 343 dekor + 3 resource görseli. Her ağaçta tek VegetationShadowController, nested controller 0, 346 LODGroup. 4152 tüm-LOD renderer, eksik materyal 0.
- 698 eski çalı görseli fern_02_a/d ile değişti; focus içine ağaç çevresinde 180 ek fern yerleşti.
- 136 eski kaya ve 25 dal görseli prototype kaynağına geçti; 27 kayalık çıkıntı ve 12 ek dal yerleşti.
- Eski dekor renderer/LOD/collider kapalı; yeni prototype collider kullanılır. Harvest köklerinde eski gameplay collider ve PhysicsNotReady durumu değişmedi.
- Mevcut 11645 Transform pozu ve 309 gameplay component authoring verisi migration sırasında korundu. Son hash/yapısal kontrol StaticValidation.json.

Ağaçlar bağımsız gölge kontrolüne dahil. ForestAuthoringState.treeShadowRoots artık VisualSlice canopy grubunu da kapsar. SessionFlow mevcut ortak ayar yöneticisini açar; SessionUi → Ayarlar → Grafik mevcut presenter'ı kullanır. Sahneye ikinci manager eklenmedi.

| Preset | Genel gölge | Ağaç gölgesi | Mesafe | LOD bias |
|---|---|---|---|---|
| Low | Low | Off | 40 m | .65 |
| Medium | Medium | Low | 60 m | .85 |
| High | High | Medium | 80 m | 1 |
| Ultra | High | High | 120 m | 1.2 |

Ağaç Low 20 m, Medium 40 m, High özgün gölgeler; Global High + Tree Off bağımsız seçilebilir. Bu kaynak/native bağlantı doğrulamasıdır; runtime apply/persistence ve FPS henüz owner tarafından çalıştırılmadı.

Eski nesne adı/kimliği kök üzerinde korunur; görünen mesh HDRP çocukta. Dolayısıyla Hierarchy'deki eski Pine/Bush adı eski görselin hâlâ çizildiği anlamına gelmez. Eşleme PrototypeObjectMapping.tsv'de.
