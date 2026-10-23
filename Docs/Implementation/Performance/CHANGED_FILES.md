# P00/P01 tanılama değişiklikleri ve geri alma

23 Ekim 2026. Performans hedefi henüz kabul edilmedi. Kalıcı render/LOD/physics değişikliği uygulanmadı.

## Kod / araçlar

| Tam proje-içi yol | Sınıf / önce → sonra | Gerekçe / olası yan etki |
|---|---|---|
| `Assets/LastSignal/Scripts/Runtime/Forest/ForestPrototypeCapture.cs` | ForestPrototypeCapture: her frame double[]/List, yalnız 180 kare warmup, 7 kolon → başlangıçta ayrılan 32.768 struct slotu, >=180 kare ve >=10 sn warmup, 30 sn capture, CPU/GPU timestamp, GC/batch/SetPass/vertex, metadata JSON | Ölçümün kendi heap tahsisini azaltma; FPS kazancı iddiası yok. İlk F9'da birkaç MB buffer ayrılır. CSV v2; export capture sonrasında disk/GC yükü oluşturabilir. Release Player'da Begin inert. Unity compile/runtime henüz NOT_RUN |
| `Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPerformanceAudit.cs` | Yeni explicit read-only Export menüsü | Sahne/native mesh/LOD/material/terrain/kimlik raporu; sharedMaterial/sharedMesh üzerinden okur. Yalnız açık doğru sahne, Edit Mode. Büyük JSON/Editor okuma maliyeti benchmark dışında; dosya yazar, asset/sahne kaydetmez |
| `Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPerformanceAudit.cs.meta` | Yeni GUID | Mevcut GUID değiştirilmedi |
| `Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPerformanceIsolation.cs` | Yeni B/C/A runtime snapshot menüleri | Sadece Play Mode; vegetation renderer.enabled veya tree shadow flag değişir; colliders/gameplay/assetler korunur. Restore/PlayMode exit/reload geri alır. Tanılama sırasında bitki/gölge görünümü bilinçli bozulur. Terrain grass B kapsamı dışında |
| `Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPerformanceIsolation.cs.meta` | Yeni GUID | Mevcut GUID değiştirilmedi |
| `Tools/ForestPrototype120/performance_static_audit.py` | Yeni standart Python kütüphaneleriyle kayıtlı sahne/prefab denetimi | 150 distant prefab removal override'ını hesaba katar; nested/unsupported override'da durur. Binary mesh topolojisini/visible renderers'ı tahmin etmez. Unity açmaz; output isteğe bağlı ve overwrite reddedilir |

Mevcut capture `.meta` değiştirilmedi. Bu capture dosyası görev öncesinde de Git'te untracked idi; `git diff` onu önceki kullanıcı haliyle karşılaştıramaz. `.before` checkpoint bu yüzden kullanıldı. Assembly definition, sahne serialization, prefab, terrain, shader, materyal, import, paket ve ProjectSettings değişikliği yok.

## Rapor / kanıt dosyaları

- `Docs/Implementation/Performance/PERF-P00-Baseline-Audit.md`: doğrulanmış başlangıç, envanter, source/quality/platform ve bilinmeyenler.
- `Docs/Implementation/Performance/PERF-P01-Root-Cause-Analysis.md`: hipotez sırası ve tek değişken deney matrisi; ölçülmüş kök neden olmadığı açık.
- `Docs/Implementation/Performance/PERF-P04-Platform-Benchmarks.md`: NOT_MEASURED karşılaştırma tablosu, metadata ve metrik yorum sınırları.
- `Docs/Implementation/Performance/PERF-P05-Regression-And-Acceptance.md`: owner-run UI/komutlar, ilk kanıt paketi, regresyon ve standalone gate'i.
- `Docs/Implementation/Performance/CHANGED_FILES.md`: bu kayıt.
- `Docs/Implementation/Performance/Evidence/static-baseline-20261023.json`: mevcut sahne/prefab hashleri, genişletilmiş sayım, prefab/mesh/material GUID+fileID listesi, duplicate adayları.
- `Docs/Implementation/Performance/Evidence/preservation-20261023.json`: ilgili 268 dosyanın kontrol sonucu; yalnız capture kaynağı planlandığı gibi farklı, beklenmeyen fark yok; statik AST/whitespace ve Unity NOT_RUN ayrımı.
- `Docs/Implementation/Performance/Checkpoints/20261023T073625072827Z/manifest.json`: başlangıç 268 hash, branch/HEAD ve değiştirilecek dosya.
- `Docs/Implementation/Performance/Checkpoints/20261023T073625072827Z/git-status.txt`: başlangıç tam `--porcelain -uall` çıktısı.
- `Docs/Implementation/Performance/Checkpoints/20261023T073625072827Z/ForestPrototypeCapture.cs.before`: mevcut dosyanın birebir görev-öncesi hali.

P02/P03 uygulama raporu oluşturulmadı: owner profiling gate'i nedeniyle kalıcı geometry/HDRP iyileştirmesi henüz seçilmedi. Rapor üretmek uygulama tamamlandı demek değildir.

## Yapılan doğrulama / yapılmayanlar

- Salt okunur Python scene/prefab audit çalıştırıldı; 2.300 flat prefab instance çözüldü, 150 distant instance removal'ları düşüldü; unsupported structural override ile karşılaşılmadı.
- Python AST syntax ve bu aşamanın kaynak dosyalarında trailing whitespace kontrolü tamamlandı. Unity C# compiler çalıştırılmadı; derleniyor iddiası yok.
- 268 protected input hash kontrolünde yalnız izinli capture kaynağı farklı; static audit'in scene/prefab hashleri aynı.
- Proje-geneli `git diff --check`, mevcut unrelated LFS yollarının `.git/lfs/tmp` yazma isteği sandbox tarafından engellendiğinden tamamlanamadı. Yerine ilgili kaynakların scoped whitespace incelemesi yapıldı; tam Git kontrolü PASS denmedi. Git config, lock veya LFS dosyaları değiştirilmedi.
- Unity compile, EditMode, PlayMode, regression, benchmark, visual acceptance, build/run: **NOT_RUN**. Önceki Evidence XML/log'ları bu aşamanın sonucu olarak kullanılmadı/üzerine yazılmadı.

## Geri alma

Önce Play Mode'da A Restore veya Play Mode'dan normal çıkış. Çalışmanızı kaydedip Unity'yi kapatın. Önce `.before` ve güncel dosyayı diff edin; bu aşama sonrasında başka kullanıcı düzenlemesi varsa onu koruyarak yalnız ilgili değişikliği geri alın. Sadece bu aşamanın capture değişikliğini geri almak için, daha yeni düzenleme olmadığı doğrulandıktan sonra:

```bash
cd '/Users/burakcoskun/Last Signal'
diff -u Docs/Implementation/Performance/Checkpoints/20261023T073625072827Z/ForestPrototypeCapture.cs.before Assets/LastSignal/Scripts/Runtime/Forest/ForestPrototypeCapture.cs
# Yalnız bu aşamanın değişiklikleri olduğundan emin olduktan sonra:
cp Docs/Implementation/Performance/Checkpoints/20261023T073625072827Z/ForestPrototypeCapture.cs.before Assets/LastSignal/Scripts/Runtime/Forest/ForestPrototypeCapture.cs
```

Yeni iki Editor utility kullanılmadan kalabilir; tamamen kaldırılacaksa Unity'de yalnız bu iki `.cs` ve karşılık `.meta` kaldırılır. Python/raporlar inert. Source/prefab/TerrainData geri yükleme gerekmez. `git restore/reset/clean` kullanılmaz: mevcut dosya untracked kullanıcı çalışmasını içerir ve Git HEAD doğru geri alma noktası değildir.

## Native audit sonrası takip

`NATIVE-AUDIT-REVIEW-20261023.md`: iki owner-run raporun analizi ve güncel mesh tablosu. `PERF-P00-Baseline-Audit.md` başına güncelleme bağlantısı eklendi. `ForestPerformanceIsolation.cs` beklenen yanlış mod/sahne/capture durumlarında ayrıntılı warning ile erken döner; pause kontrolü eklendi. Öncesi `Checkpoints/20261023T080455450861Z/ForestPerformanceIsolation.cs.before`. Bu takip değişikliğinin Unity doğrulaması NOT_RUN. Önceki preservation receipt önceki aşamaya aittir.

## Oyuncu ağaç gölge seçeneği

Dosya bazlı değişiklikler, checkpoint, kullanım ve doğrulama sınırları `TREE-SHADOW-SETTING.md` içinde. Yeni test istenmeden uygulanmıştır; performans kazancı henüz ölçülmedi.
