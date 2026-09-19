# Last Signal — Production Documentation v0.2.0

17 Eylül 2026 • Türkçe • GDD v0.1 üzerinden genişletilmiş tasarım ve teknik sözleşmeler.

## Önce bunu bil

Kullanıcının kesin kararı single-player önceliğidir. Co-op gelecek seçeneğidir. Çalışma adı, hikâye, sayılar ve yeni ayrıntılar tasarım önerileri olarak işaretlenmiştir. Gerçek Unity reposu incelenmedi; oyun testleri, NotebookLM yüklemesi veya MCP kurulumu yapılmadı.

## Hızlı kullanım

1. NotebookLM için `sources/` içindeki 01-29 `.md` dosyalarını yükle. ZIP'i önce aç.
2. İlk olarak [karar kaydını](sources/01_Kararlar_ve_Kapsam.md), [üretim planını](sources/24_Production_Roadmap_Backlog.md) ve [kaynak kullanım rehberini](sources/26_NotebookLM_Kaynak_Yonetimi.md) oku.
3. Codex için paketi Unity repo'sunun `docs/` klasörüne yerleştir; [AGENTS şablonunu](templates/AGENTS.template.md) mevcut kök kurallarla birleştir.
4. [Codex/MCP runbook](sources/27_Codex_MCP_Runbook.md) bağlantıların nasıl ayrı doğrulanacağını açıklar. Hazır kurulum yapılmış değildir.
5. [MASTER_REFERENCE.md](MASTER_REFERENCE.md) tek dosyalı okuma alternatifidir. Modüler dosyalarla birlikte aynı notebook'a yükleme.
6. [GDD v0.1 arşivi](reference/GDD_v0.1_Archive.md) tarihi kaynak; yeni öneriler için 01 numaralı karar kaydı geçerlidir.

## Belge haritası

| Kaynak | Başlık | GDD ilişkisi |
|---|---|---|
| [01_Kararlar_ve_Kapsam.md](sources/01_Kararlar_ve_Kapsam.md) | Karar yönetimi, kapsam ve kaynak hiyerarşisi | Belge Kontrolü; §4, §23, §25, §28 |
| [02_Vizyon_Donguler_ve_Dunya.md](sources/02_Vizyon_Donguler_ve_Dunya.md) | Ürün vizyonu, oynanış döngüleri ve dünya tasarımı | §1-6, §15-17 |
| [03_Teknik_Mimari.md](sources/03_Teknik_Mimari.md) | Teknik mimari, bağımlılıklar ve yaşam döngüsü | §21, §23; semantik mimari konuşması |
| [04_Veri_Kimlik_Command_Event.md](sources/04_Veri_Kimlik_Command_Event.md) | Veri sözlüğü, kimlikler, komutlar ve olay sözleşmeleri | §10, §21-22; Ek B |
| [05_Player_Input_Movement.md](sources/05_Player_Input_Movement.md) | Oyuncu girdisi, kamera ve hareket sözleşmesi | §7, §19 |
| [06_Interaction_Door_WorldItem.md](sources/06_Interaction_Door_WorldItem.md) | Etkileşim, kapılar ve dünya eşyaları | §7, §10-11 |
| [07_Item_Inventory_Equipment.md](sources/07_Item_Inventory_Equipment.md) | Item, envanter, ekipman ve taşıma kapasitesi | §10; Ek B |
| [08_Loot_Ekonomi_ve_Respawn.md](sources/08_Loot_Ekonomi_ve_Respawn.md) | Loot üretimi, kaynak ekonomisi ve tükenme | §11, §24 |
| [09_Survival_Health_Death.md](sources/09_Survival_Health_Death.md) | Survival statları, yaralar, tedavi ve ölüm | §8-9, §18 |
| [10_Combat_Weapons_Damage.md](sources/10_Combat_Weapons_Damage.md) | Combat, silah state machine ve hasar hesabı | §12, §9; Ek B |
| [11_Zombie_AI_Perception_Population.md](sources/11_Zombie_AI_Perception_Population.md) | Zombie AI, algı, navigation ve nüfus sürekliliği | §13-14 |
| [12_World_Pressure.md](sources/12_World_Pressure.md) | World Pressure, gürültü ve karşılaşma yönetimi | §13-14 |
| [13_World_Streaming_POI.md](sources/13_World_Streaming_POI.md) | Dünya streaming, POI üretimi ve level design | §15, §22 |
| [14_Siginak_Crafting_Progression.md](sources/14_Siginak_Crafting_Progression.md) | Sığınak, crafting, elektrik ve ilerleme sözleşmeleri | §16, §18 |
| [15_Time_Weather_Environment.md](sources/15_Time_Weather_Environment.md) | Zaman, hava, çevresel maruziyet ve uzun işlemler | §8, §18, §22 |
| [16_Narrative_Objectives_Events.md](sources/16_Narrative_Objectives_Events.md) | Anlatı, görev state machine ve dünya olayları | §5, §17 |
| [17_Save_Load_Migration.md](sources/17_Save_Load_Migration.md) | Kayıt, yükleme, snapshot ve migration tasarımı | §22; Ek B |
| [18_UI_UX_Accessibility.md](sources/18_UI_UX_Accessibility.md) | UI/UX, erişilebilirlik, localization ve oyuncu geri bildirimi | §7, §19-20 |
| [19_Art_Audio_Asset_Pipeline.md](sources/19_Art_Audio_Asset_Pipeline.md) | Sanat yönetimi, asset üretimi, animasyon, VFX ve ses | §20 |
| [20_Performance_Build_Operations.md](sources/20_Performance_Build_Operations.md) | Performans, platform doğrulama, build ve işletim | §4, §21, §26 |
| [21_Vertical_Slice_ve_Playtest.md](sources/21_Vertical_Slice_ve_Playtest.md) | Vertical slice, golden path ve oyuncu test protokolü | §6, §24, §26; Ek A |
| [22_QA_Acceptance_Traceability.md](sources/22_QA_Acceptance_Traceability.md) | QA stratejisi, acceptance senaryoları ve kanıt standardı | §26-27 |
| [23_Balance_Katalog_ve_Ekonomi.md](sources/23_Balance_Katalog_ve_Ekonomi.md) | Denge parametreleri, ilk item kataloğu ve recipe matrisi | §8, §10-14, §25; Ek C |
| [24_Production_Roadmap_Backlog.md](sources/24_Production_Roadmap_Backlog.md) | Üretim planı, bağımlılıklar ve uygulanabilir iş paketleri | §25-27 |
| [25_Coop_Gelecek_Mimarisi.md](sources/25_Coop_Gelecek_Mimarisi.md) | Gelecekte co-op için mimari sınırlar ve karar kapısı | §23; kullanıcı single-player önceliği |
| [26_NotebookLM_Kaynak_Yonetimi.md](sources/26_NotebookLM_Kaynak_Yonetimi.md) | NotebookLM kaynak düzeni, sorgulama ve güncelleme runbook’u | GDD Belge Kontrolü; bu paketin kullanım tasarımı |
| [27_Codex_MCP_Runbook.md](sources/27_Codex_MCP_Runbook.md) | Codex çalışma sözleşmesi, MCP bağlantı tasarımı ve görev runbook’u | §21-26; kullanıcı gelecek MCP kullanımı |
| [28_Gereksinim_Izlenebilirlik.md](sources/28_Gereksinim_Izlenebilirlik.md) | Gereksinim matrisi, kaynak eşlemesi ve mevcut durum | GDD tamamı; v0.2 sistem sözleşmeleri |
| [29_Kaynaklar_Glossary_ve_Dokuman_Bakimi.md](sources/29_Kaynaklar_Glossary_ve_Dokuman_Bakimi.md) | Kaynaklar, terimler ve dokümantasyon bakım standardı | GDD bütün bölümler; dış kaynaklar aşağıda |

## Şablonlar

- [AGENTS.template.md](templates/AGENTS.template.md): repo çalışma kuralları.
- [Task_Brief.template.md](templates/Task_Brief.template.md): bounded iş paketi.
- [Verification_Handoff.template.md](templates/Verification_Handoff.template.md): gerçek test/build kanıtı.
- [ADR.template.md](templates/ADR.template.md): tasarım/teknik karar kaydı.
- [Current_State.template.md](templates/Current_State.template.md): tasarım ile implementation ayrımı.

## Sürüm notu

v0.2.0, GDD v0.1'in sistem sözleşmelerini genişletir; GDD dosyasını değiştirmez. Save altyapısı erkene çekildi; saatler ayrıldı; transaction/escrow/ownership tanımlandı; co-op maliyeti ve belirsizliği açıklandı; Forward+ ölçüm kararı oldu. 72 item, 16 recipe ve 24 W iş paketi aday kataloğu eklendi. Hiçbiri uygulanmış içerik iddiası değildir.

## Bütünlük

[MANIFEST.md](MANIFEST.md) dosya boyutları, kelime sayıları ve SHA-256 değerlerini listeler. [PACKAGE_QA.md](PACKAGE_QA.md) yalnız bu dokümantasyonun kontrollerini raporlar; Unity QA sonucu değildir. Kanonik kaynaklar `sources/` dosyalarıdır; master dosya bunlardan üretilir, ayrı elle düzenlenmez.
