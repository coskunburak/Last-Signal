---
doc_id: LS-PLAN-T03
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# Faz kapısı değerlendirmesi

Boş şablon. Faz/Gate ID, değerlendirilen build, commit, catalog/schema, tarih ve karar sahibi doldurulur.

| Zorunlu kriter | Kanıt | Sonuç | Kalan iş |
|---|---|---|---|
| Faz dosyasındaki kriter | Henüz yok | NOT_RUN | Doldur |

## Karar

PASS / CONDITIONAL / FAIL / BLOCKED seçeneklerinden gerçek olanı seç. Kritik NOT_RUN veya S0/S1 bulunan release kapısı PASS/CONDITIONAL olamaz. Açık noncritical risk kabul ediliyorsa gerekçe ve takip sahibi yaz.

## Sonraki dönem

Kapsam farkı, gerçek maliyet, forecast, tüketilen rezerv, yeni giriş önkoşulu. G15 için single-player yayın/stabilizasyon kanıtı ve ayrı co-op GO kaydı gerekir.
