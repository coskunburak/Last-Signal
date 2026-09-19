# Doğrulama ve handoff — şablon

## Kimlik

Task ID, tarih, repository commit, dirty tree durumu, Unity sürümü, package lock kimliği, OS/hardware, build path. Henüz gerçek run yoksa boş sayı yerine NOT_VERIFIED yaz.

## Sonuç

Oyuncunun artık yapabildiği somut davranış; değişiklik gerekçesi; etkilenen dosyalar ve REQ kimlikleri.

| Gate | Durum | Komut/yöntem | Kanıt | Sınır |
|---|---|---|---|---|
| Compile | NOT_RUN | Repo keşfinde belirlenecek | Yok | — |
| EditMode | NOT_RUN | İlgili suite | Yok | — |
| PlayMode | NOT_RUN | İlgili scene/suite | Yok | — |
| Visible acceptance | NOT_RUN | Gerçek build adımları | Yok | — |
| Performance | NOT_RUN | Tanımlı cihaz/rota | Yok | — |
| Save regression | NOT_RUN | Gerekirse fixture/fault | Yok | — |

## Açık konular

Bilinen bug ID/severity; uygulanmayan veya geçici alan; onaylanmamış varsayım; save/schema/migration; sonraki paket ve ön koşulu. “Bütün testler geçti” yerine toplam/passed/failed/skipped yalnız gerçek test çıktısından yazılır.
