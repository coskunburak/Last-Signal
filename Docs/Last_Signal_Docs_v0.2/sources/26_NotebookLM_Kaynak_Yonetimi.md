---
doc_id: LS-DOC-26
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# NotebookLM kaynak düzeni, sorgulama ve güncelleme runbook’u

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: GDD Belge Kontrolü; bu paketin kullanım tasarımı. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Amaç

NotebookLM bu proje için okunabilir bir bilgi arama ve açıklama katmanıdır. Unity repo'su, test runner veya otomatik doğru karar veren sistem değildir. Bu paket Türkçe açıklama, İngilizce identifier ve kararlı belge/gereksinim kimlikleriyle hazırlanmıştır. Her kaynak kendi konusunu anlatır; temel kuralları anlamak için görüntü render'ına ihtiyaç yoktur.

Google'ın kontrol edilen [kaynak türleri sayfası](https://support.google.com/notebooklm/answer/16215270) Markdown (.md) desteğini listeliyor. Hizmet arayüzünde ad ve özellikler değişebilir. Bu paket herhangi bir hesaba yüklenmedi; kaynak ekleme ve yanıt kalite kontrolü aşağıdaki proje prosedürüdür.

## 2. Tavsiye edilen yükleme

1. ZIP'i aç. ZIP taşıma paketidir; kaynak olarak içindeki Markdown dosyalarını kullan.
2. Proje için tek notebook oluştur: `Last Signal - Production v0.2`.
3. `sources/` içindeki 01-29 numaralı .md dosyalarını kaynak olarak ekle.
4. Kaynakların işlenmesini bekle; dosya adlarının ve v0.2.0 kimliklerinin geldiğini kontrol et.
5. İlk sorguda LS-DOC-01 ve LS-DOC-28'i seçerek kapsam/durum ayrımını doğrula.
6. `reference/GDD_v0.1_Archive.md` tarihî kaynak olarak ayrı tutulur. Aynı notebook'a eklersen açıkça ARCHIVE başlığıyla kullan; yürürlükteki önerilerle karıştırma.
7. `MASTER_REFERENCE.md` alternatif tek dosyalı okumadır. Modüler kaynaklarla birlikte aynı içeriği ikinci kez yükleme.
8. Şablonlar yalnız görev/handoff üretirken eklenebilir. Repo talimatı şablonunu NotebookLM'ye yüklemek Codex'e otomatik talimat vermez.

**REQ-KB-001:** Bir notebook aynı belge kimliğinin birden fazla yürürlükteki sürümünü aktif kaynak gibi tutmamalıdır. Tarihî karşılaştırma gerektiğinde sürüm filtresi açıkça sorulur.

## 3. Kaynak otoritesi

NotebookLM'ye şu çalışma talimatını ver:

> Bu notebook Project: Last Signal v0.2.0 üretim dokümantasyonudur. Kullanıcı kararı, GDD baseline'ı, öneri, deney ve gerçek implementation durumunu ayır. Her teknik cevapta kaynak dosya adı, bölüm ve varsa REQ kimliğini belirt. Kaynakta yoksa “tanımlanmamış” de; sayı, test sonucu veya paket sürümü üretme. Çelişki varsa iki tarafı göster, ADR kaydına yönlendir. Bu belgeler hiçbir Unity testinin geçtiğini kanıtlamaz. Kod görevi önerirken kapsam, veri sahipliği, başarısızlık yolu ve acceptance kriterlerini beraber çıkar.

Bu talimat model hatasını tamamen ortadan kaldırmaz. Sonuçlar gerçek Markdown ve kodla karşılaştırılmalıdır. Notebook özetleri orijinal dokümanın yerini alan yeni canon değildir.

## 4. Sorgu setleri

| İş | Seçilecek kaynaklar | İstenen cevap |
|---|---|---|
| Inventory implementation | 01,03,04,07,17,22,28 | Ownership, atomicity, save ve test |
| Zombie/pressure | 11,12,13,20,23 | Algı ve nüfus budget ilişkisi |
| Survival/sleep | 09,14,15,17 | Clock, interruption, fuel ve wounds |
| Yeni item | 04,07,08,19,23 | Data, UI, loot, asset ve validation |
| Sprint/task hazırlama | 01,24,27,28 | Kapsam, dependency, DoD |
| Co-op sorusu | 01,25 | Gelecek kapsam; bugün kurulacak şeyler değil |

Dar sorular geniş “projeyi anlat” sorusundan daha denetlenebilir cevap üretir. Bilgi yetersizse tüm belgeleri tekrar özetletmek yerine ilgili iki sözleşme ve çelişki kaydı sorgulanır.

## 5. Hazır üretim sorguları

**Inventory:** “REQ-INV-001, REQ-INT-002 ve REQ-SAVE-002'yi birlikte açıkla. Pickup sırasında save geldiğinde tek sahiplik nasıl korunur? Her iddiayı kaynak/bölümle bağla. Uygulanmış kod varmış gibi anlatma.”

**AI:** “Physical agent ile logical population arasında dönüşümde hangi muhasebe invariantları var? Cell unload, death ve migration aynı anda olursa olası testleri çıkar.”

**Görev:** “W05 için 01,03,04,17,22 ve 24 kaynaklarına göre bounded implementation brief hazırla. İlk sürüm tek cell; world streaming altyapısını erkenden yazma. Bilinmeyen repo/paket bilgilerini ayrı listele.”

**Çelişki:** “GDD v0.1 ile ADR-003 ve ADR-007 arasında ne değişti? Kullanıcı tarafından onaylanmış karar ile öneriyi ayır.”

**Playtest:** “35 dakikalık rota için yönlendirmesiz observer formu üret. Başarı ölçütlerini küçük pilot örneklemin sınırlamasıyla ver.”

## 6. Güncelleme prosedürü

Repo Markdown düzenlenir, belge version/date değişir, requirement etkisi değerlendirilir, manifest hash yenilenir. Ardından etkilenen notebook kaynağı güncellenir/değiştirilir ve v0.2.1 gibi yeni sürüm sorguyla doğrulanır. Yerel dosya düzenlemesinin daha önce yüklenen kopyayı otomatik değiştirdiği varsayılmaz. Google Drive bağlantılı kaynakların güncelleme davranışı ayrı bir ürün özelliğidir; bu paket local Markdown upload akışını kullanır.

**REQ-KB-002:** Kaynak senkron kontrolü yalnız başlığa bakmaz: değişen bir requirement ID ve yeni değer sorgulanır. Eski cevap görünüyorsa uygulama görevi eski veriye dayanarak başlamaz; doğrudan repo belgesi okunur.

## 7. MCP'ye hazırlık

Her kaynak başlığında doc_id/version, her normatif kuralda REQ kimliği var. Bir retrieval cevabı doc ID, filename, version, section, excerpt ve varsa source reference içermelidir. Sağlayıcı bu metadatayı vermiyorsa yerel dokümandan doğrulama gerekir. NotebookLM MCP sunucusu seçilmiş veya kurulmuş değildir; endpoint ve araç isimleri bu pakette uydurulmaz. Entegrasyon tasarımı [Codex runbook](27_Codex_MCP_Runbook.md).

## 8. Kabul sorguları

**REQ-KB-003:** Notebook şu sorularda doğru ayrımı yapmalı: “Co-op bugün gerekiyor mu?” hayır; “Unity testleri geçti mi?” kanıt yok; “Forward+ kesin üstün mü?” deney; “Save ne zaman başlar?” W05; “Aynı eşya iki kez alınabilir mi?” invariant gereği hayır; “Bu kod yazıldı mı?” repo incelenmedi. Beş doğru cevap bağlantının her konuda doğru olduğu garantisi değildir; yalnız başlangıç smoke testidir.
