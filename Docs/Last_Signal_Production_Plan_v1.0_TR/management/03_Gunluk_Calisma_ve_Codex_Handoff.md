---
doc_id: LS-PLAN-M03
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# Günlük çalışma, sprint yönetimi ve Codex görevlendirme

## Gün başlangıcında

Aktif sprint dosyasını, Current_State kaydını ve ilgili LS-DOC kaynaklarını oku. Doğru repo/branch/commit'i ve projeye ait AGENTS kurallarını kontrol et. Mevcut sınıf ve sahneleri araştır; eski rapordaki class adı var diye varsayma. Çalışan sistemi sırf plandaki aday isimle uyuşmuyor diye yeniden kurma. Günlük kartın giriş bağımlılıkları hazır değilse engeli açık yaz, uygun bağımsız işi seç.

600 kart sıralı bir çalışma rehberidir. Sprint bağımlılıkları mantıksal önkoşullardır; günlük kartların önceki gün ilişkisi tek geliştirici iş sırasıdır. Gün kartları ayrı bağımsız servis mimarisi talimatı değildir. Aynı sorunu çözmek için ikinci inventory, ikinci event bus veya başka save sistemi yaratma.

## Tek kart için çalışma döngüsü

1. Oyuncu sonucunu ve mevcut uygulamayı karşılaştır; en küçük güvenli değişikliği seç.
2. Veri ve kayıt etkisini belirle. Yoksa N/A gerekçesi yaz; her görsel değişime migration ekleme.
3. Uygula, gerekiyorsa veriyi ve sahneyi bağla. Derlenmeyen ara state'i DONE sayma.
4. Değişen riske uygun doğrulama yap. Veri kaybı ve dupe için negatif senaryo; görsel iş için build/sahne kontrolü; metin için içerik/link kontrolü.
5. Kartın özel kabul cümlesine karşı gerçek sonucu kaydet. NOT_RUN, FAIL ve BLOCKED dürüst kalır.
6. Actual süre, yeni risk, kalan iş ve kanıt yolunu yaz; bir sonraki kart için kısa handoff bırak.

## Kapsam sınırları

S001–S048'de ağ SDK, RPC, lobby veya relay işi yoktur. Yeni reusable abstraction yalnız mevcut ihtiyacı çözüyorsa eklenir. Bir sistemin domain mantığını test edilebilir tutmak için kapsamlı framework şart değildir. Optimize etmeden önce ölç. Test sayısı veya satır sayısı yerine görünen davranış, invariant ve profiler kanıtını raporla.

## Süre taşması ve engel çözümü

Bir kartın bir günde biteceği varsayımı yanlışsa dört saat sonunda kalan riski yeniden değerlendir. Güvenli küçük çıktı mümkünse kartı `.a`, `.b` alt işlerine böl; asıl D kimliği korunur. Alt işler 600 baseline kartın sayısını yapay artırmak için kullanılmaz. İki gün aynı kök engelde kalındıysa spike, kapsam azaltımı veya dış bilgi ihtiyacı seçilir. Rastgele paket/engine yükseltmesiyle engel çözülmüş sayılmaz.

BLOCKED raporu: eksik girdi, şimdiye kadar yapılan iş, tek somut sonraki adım ve bağımsız devam edilebilecek iş. Windows erişimi yoksa gerçek Windows sonucu BLOCKED'dır; Mac compile PASS yeterli olmaz. İnsan playtest katılımcısı yoksa ajan gözlemi insan testi yerine yazılmaz.

## Sprint planlama ve kapanış

Bir sprintte aynı anda en çok bir ana kart IN_PROGRESS, bir kart VERIFYING önerilir. Bu bir tek geliştirici WIP sınırıdır, ajan delegasyonu talimatı değildir. Önceki kritik hata yeni özellikten önce çözülür. Gün 5'te kalan kapasiteye göre Could işler çıkarılır. Gün 10'da oyuncuya görünen kısa demo, hedefli kanıt, açık sorun ve tahmin/gerçek karşılaştırması hazırlanır.

Kapanış: tamamlanan kartlar, yarım kalanlar, taşınan işin yeni yeri, açılan risk, yeni tasarım kararı, actual saat, build kimliği ve sonraki sprint giriş durumu. Bir kart taşındıysa eski ve yeni kaydı aynı canonical kimliğe bağla; iki kez tamamlanmış sayma.

## Codex için görevlendirme kalıbı

```text
Aktif kapsam: Sxxx / Dxxx. Proje önce single-player; S049 öncesinde networking ekleme.
Önce ilgili sprint, Current_State, kaynak LS-DOC ve gerçek repo kurallarını oku.
Mevcut uygulamayı incele; çalışan sistemi gereksiz yeniden yazma.
Karttaki somut çıktıyı tamamla; gerekli veri/sahne entegrasyonunu yap.
Kart kabulü ve değişiklik riskine uygun test/build kontrolü uygula.
Çalıştırmadığın veya erişemediğin kontrolü PASS yazma.
Sonuçta değişen davranış, kanıt, açık sınırlama, actual süre ve sonraki adımı raporla.
İş kapsamını sonraki sprint özelliklerine genişletme; küçük gerekli düzeltmeleri açıkla.
```

Bu şablon yerel AGENTS dosyası olarak otomatik uygulanmaz; görev metnidir. NotebookLM bilgiyi açıklayabilir ama kodun gerçek durumunu bilemez. MCP bağlantısı olmadan yerel Markdown ile devam edilebilir; bağlantı kurulmuş gibi raporlanmaz.
