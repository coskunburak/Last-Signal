# İş paketi kartı — şablon

- Work package ID: atanacak.
- Başlık / oyuncuya görünen sonuç: doldurulacak.
- Status: NOT_STARTED.
- Kaynak belge sürümleri ve REQ kimlikleri: doldurulacak.
- İncelenen repository commit/build: henüz yok.

## Problem ve kapsam

Hangi kullanıcı/oyuncu davranışı mümkün olacak? Bugünkü eksik nedir? Scope içi işleri ve açık dış kapsamı yaz. Var olan sistemlerin yeniden kullanılacağı noktaları belirt.

## Bağımlılık ve veri etkisi

Ön koşullar; owner servis; command/result; failure states; cancellation/commit; DTO/schema; migration; UI/audio; platform/performance. Uygulanmayan alanlar N/A ve gerekçesiyle.

## Kabul

| Senaryo | Given | When | Then | Yöntem | Durum |
|---|---|---|---|---|---|
| Ana akış | Tanımlanacak | Tanımlanacak | Ölçülebilir sonuç | Edit/Play/Build | NOT_RUN |
| Kritik red | Tanımlanacak | Tanımlanacak | Mutation yok / fallback | Uygun katman | NOT_RUN |
| Save/iptal | Tanımlanacak | Tanımlanacak | Tutarlılık korunur | Uygun katman | NOT_RUN |

## Risk, geri dönüş ve teslimat

En yüksek risk; küçük spike gerekiyorsa sınırı; değişiklik geri alma yolu; gerekli evidence; tamamlanınca Current_State güncellemesi.
